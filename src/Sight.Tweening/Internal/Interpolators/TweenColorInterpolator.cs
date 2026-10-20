using System.Drawing;
using System.Numerics;

namespace Sight.Tweening.Internal.Interpolators;

/// <summary>
/// Interpolates colors in a given color space, following CSS Color 4: premultiplied alpha,
/// missing hues of achromatic colors, hue paths and gamut mapping.
/// </summary>
/// <typeparam name="T">Color type, converted from and to normalized sRGB components (X = red, Y = green, Z = blue, W = alpha).</typeparam>
internal sealed class TweenColorInterpolator<T> : TweenInterpolator<T>
{
    private readonly Func<T, Vector4> _toRgba;
    private readonly Func<Vector4, T> _fromRgba;
    private readonly TweenColorSpace _space;
    private readonly TweenHuePath _huePath;

    public TweenColorInterpolator(Func<T, Vector4> toRgba, Func<Vector4, T> fromRgba, TweenColorSpace space, TweenHuePath huePath)
    {
        _toRgba = toRgba ?? throw new ArgumentNullException(nameof(toRgba));
        _fromRgba = fromRgba ?? throw new ArgumentNullException(nameof(fromRgba));
        _space = space is >= TweenColorSpace.Srgb and <= TweenColorSpace.Oklch
            ? space
            : throw new ArgumentOutOfRangeException(nameof(space), space, "Unknown color space");
        _huePath = huePath is >= TweenHuePath.Shorter and <= TweenHuePath.Decreasing
            ? huePath
            : throw new ArgumentOutOfRangeException(nameof(huePath), huePath, "Unknown hue path");
    }

    public override T? Interpolate(double delta, T? begin, T? end)
    {
        if (delta == 0)
            return begin;

        if (delta == 1)
            return end;

        var result = TweenColorMath.Interpolate(delta, _toRgba(begin!), _toRgba(end!), _space, _huePath);
        return _fromRgba(result);
    }

    internal static Vector4 ColorToVector(Color color) => new(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);

    internal static Color VectorToColor(Vector4 color) => Color.FromArgb(ToByte(color.W), ToByte(color.X), ToByte(color.Y), ToByte(color.Z));

    private static int ToByte(float value) => (int)Math.Round(Math.Clamp(value, 0f, 1f) * 255);
}

internal static class TweenColorMath
{
    private const double AchromaticChroma = 1e-4;
    private const double GamutEpsilon = 1e-6;

    // Linear values of the 8-bit channels, which are the most common inputs
    private static readonly double[] s_byteToLinear = CreateByteToLinear();

    public static Vector4 Interpolate(double delta, Vector4 begin, Vector4 end, TweenColorSpace space, TweenHuePath huePath)
    {
        var a = FromSrgb(begin, space);
        var b = FromSrgb(end, space);
        var hue = HueIndex(space);
        var alphaA = Math.Clamp(begin.W, 0f, 1f);
        var alphaB = Math.Clamp(end.W, 0f, 1f);

        if (hue >= 0)
        {
            // An achromatic color has no hue: it takes the hue of the other color
            var h1 = a[hue];
            var h2 = b[hue];
            if (double.IsNaN(h1))
                h1 = double.IsNaN(h2) ? 0 : h2;

            if (double.IsNaN(h2))
                h2 = h1;

            FixHues(ref h1, ref h2, huePath);
            a[hue] = h1;
            b[hue] = h2;
        }

        var alpha = Math.Clamp(alphaA + (alphaB - alphaA) * delta, 0, 1);
        var result = default(Components);
        for (var i = 0; i < 3; i++)
        {
            if (i == hue)
            {
                result[i] = a[i] + (b[i] - a[i]) * delta;
                continue;
            }

            // Premultiplied alpha: a transparent color does not bleed into the other one
            var premultiplied = a[i] * alphaA + (b[i] * alphaB - a[i] * alphaA) * delta;
            result[i] = alpha > 0 ? premultiplied / alpha : a[i] + (b[i] - a[i]) * delta;
        }

        return ToSrgb(result, space, alpha);
    }

    private static int HueIndex(TweenColorSpace space) => space switch
    {
        TweenColorSpace.Hsl => 0,
        TweenColorSpace.Oklch => 2,
        _ => -1,
    };

    private static void FixHues(ref double h1, ref double h2, TweenHuePath path)
    {
        h1 = NormalizeHue(h1);
        h2 = NormalizeHue(h2);
        var diff = h2 - h1;
        switch (path)
        {
            case TweenHuePath.Shorter:
                if (diff > 180)
                    h1 += 360;
                else if (diff < -180)
                    h2 += 360;
                break;
            case TweenHuePath.Longer:
                if (diff > 0 && diff < 180)
                    h1 += 360;
                else if (diff > -180 && diff <= 0)
                    h2 += 360;
                break;
            case TweenHuePath.Increasing:
                if (h2 < h1)
                    h2 += 360;
                break;
            case TweenHuePath.Decreasing:
                if (h1 < h2)
                    h1 += 360;
                break;
        }
    }

    private static double NormalizeHue(double hue)
    {
        hue %= 360;
        return hue < 0 ? hue + 360 : hue;
    }

    private static Components FromSrgb(Vector4 color, TweenColorSpace space)
    {
        double r = color.X, g = color.Y, b = color.Z;
        switch (space)
        {
            case TweenColorSpace.LinearRgb:
                return new Components(ToLinear(r), ToLinear(g), ToLinear(b));
            case TweenColorSpace.Hsl:
                return RgbToHsl(r, g, b);
            case TweenColorSpace.Oklab:
                return LinearToOklab(ToLinear(r), ToLinear(g), ToLinear(b));
            case TweenColorSpace.Oklch:
                var lab = LinearToOklab(ToLinear(r), ToLinear(g), ToLinear(b));
                var chroma = Math.Sqrt(lab[1] * lab[1] + lab[2] * lab[2]);
                var hue = chroma < AchromaticChroma ? double.NaN : Math.Atan2(lab[2], lab[1]) * 180 / Math.PI;
                return new Components(lab[0], chroma, hue);
            default:
                return new Components(r, g, b);
        }
    }

    private static Vector4 ToSrgb(Components c, TweenColorSpace space, double alpha)
    {
        double r, g, b;
        switch (space)
        {
            case TweenColorSpace.LinearRgb:
                (r, g, b) = (FromLinear(c[0]), FromLinear(c[1]), FromLinear(c[2]));
                break;
            case TweenColorSpace.Hsl:
                (r, g, b) = HslToRgb(c[0], c[1], c[2]);
                break;
            case TweenColorSpace.Oklab:
                (r, g, b) = OklchToSrgbInGamut(c[0], Math.Sqrt(c[1] * c[1] + c[2] * c[2]), Math.Atan2(c[2], c[1]));
                break;
            case TweenColorSpace.Oklch:
                (r, g, b) = OklchToSrgbInGamut(c[0], Math.Max(c[1], 0), c[2] * Math.PI / 180);
                break;
            default:
                (r, g, b) = (c[0], c[1], c[2]);
                break;
        }

        return new Vector4(Clamp01(r), Clamp01(g), Clamp01(b), (float)alpha);
    }

    /// <summary>
    /// Converts an Oklch color to sRGB, reducing its chroma until it fits in the sRGB gamut.
    /// </summary>
    private static (double R, double G, double B) OklchToSrgbInGamut(double lightness, double chroma, double hueRadians)
    {
        if (lightness >= 1)
            return (1, 1, 1);

        if (lightness <= 0)
            return (0, 0, 0);

        // The sRGB transfer function is monotonic: the gamut is checked on linear components, without any power
        var cos = Math.Cos(hueRadians);
        var sin = Math.Sin(hueRadians);
        var rgb = OklabToLinear(lightness, chroma * cos, chroma * sin);
        if (!IsInGamut(rgb))
        {
            var low = 0.0;
            var high = chroma;
            for (var i = 0; i < 16; i++)
            {
                var mid = (low + high) / 2;
                if (IsInGamut(OklabToLinear(lightness, mid * cos, mid * sin)))
                    low = mid;
                else
                    high = mid;
            }

            rgb = OklabToLinear(lightness, low * cos, low * sin);
        }

        return (FromLinear(rgb.R), FromLinear(rgb.G), FromLinear(rgb.B));
    }

    private static bool IsInGamut((double R, double G, double B) rgb)
    {
        return rgb.R >= -GamutEpsilon && rgb.R <= 1 + GamutEpsilon
            && rgb.G >= -GamutEpsilon && rgb.G <= 1 + GamutEpsilon
            && rgb.B >= -GamutEpsilon && rgb.B <= 1 + GamutEpsilon;
    }

    private static double ToLinear(double c)
    {
        var scaled = c * 255;
        var index = Math.Round(scaled);
        if (index >= 0 && index <= 255 && Math.Abs(scaled - index) < 1e-4)
            return s_byteToLinear[(int)index];

        return ComputeLinear(c);
    }

    private static double ComputeLinear(double c)
    {
        var abs = Math.Abs(c);
        var linear = abs <= 0.04045 ? abs / 12.92 : Math.Pow((abs + 0.055) / 1.055, 2.4);
        return c < 0 ? -linear : linear;
    }

    private static double[] CreateByteToLinear()
    {
        var values = new double[256];
        for (var i = 0; i < values.Length; i++)
            values[i] = ComputeLinear(i / 255.0);

        return values;
    }

    private static double FromLinear(double c)
    {
        var abs = Math.Abs(c);
        var encoded = abs <= 0.0031308 ? abs * 12.92 : 1.055 * Math.Pow(abs, 1 / 2.4) - 0.055;
        return c < 0 ? -encoded : encoded;
    }

    // Oklab matrices from https://bottosson.github.io/posts/oklab/
    private static Components LinearToOklab(double r, double g, double b)
    {
        var l = Math.Cbrt(0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b);
        var m = Math.Cbrt(0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b);
        var s = Math.Cbrt(0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b);

        return new Components(
            0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s,
            1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s,
            0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s);
    }

    private static (double R, double G, double B) OklabToLinear(double lightness, double a, double b)
    {
        var l = lightness + 0.3963377774 * a + 0.2158037573 * b;
        var m = lightness - 0.1055613458 * a - 0.0638541728 * b;
        var s = lightness - 0.0894841775 * a - 1.2914855480 * b;
        l = l * l * l;
        m = m * m * m;
        s = s * s * s;

        return (
            4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
            -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
            -0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s);
    }

    private static Components RgbToHsl(double r, double g, double b)
    {
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var lightness = (max + min) / 2;
        var d = max - min;
        if (d < 1e-9)
            return new Components(double.NaN, 0, lightness);

        var saturation = lightness <= 0 || lightness >= 1 ? 0 : (max - lightness) / Math.Min(lightness, 1 - lightness);
        double hue;
        if (max == r)
            hue = (g - b) / d + (g < b ? 6 : 0);
        else if (max == g)
            hue = (b - r) / d + 2;
        else
            hue = (r - g) / d + 4;

        return new Components(hue * 60, saturation, lightness);
    }

    private static (double R, double G, double B) HslToRgb(double hue, double saturation, double lightness)
    {
        hue = NormalizeHue(hue);
        saturation = Math.Clamp(saturation, 0, 1);
        lightness = Math.Clamp(lightness, 0, 1);
        var a = saturation * Math.Min(lightness, 1 - lightness);

        double Channel(double n)
        {
            var k = (n + hue / 30) % 12;
            return lightness - a * Math.Max(-1, Math.Min(Math.Min(k - 3, 9 - k), 1));
        }

        return (Channel(0), Channel(8), Channel(4));
    }

    private static float Clamp01(double value) => (float)Math.Clamp(value, 0, 1);

    /// <summary>
    /// Components of a color in a color space, without allocation.
    /// </summary>
    private struct Components
    {
        public double C0;
        public double C1;
        public double C2;

        public Components(double c0, double c1, double c2)
        {
            C0 = c0;
            C1 = c1;
            C2 = c2;
        }

        public double this[int index]
        {
            readonly get => index switch
            {
                0 => C0,
                1 => C1,
                _ => C2,
            };
            set
            {
                switch (index)
                {
                    case 0:
                        C0 = value;
                        break;
                    case 1:
                        C1 = value;
                        break;
                    default:
                        C2 = value;
                        break;
                }
            }
        }
    }
}
