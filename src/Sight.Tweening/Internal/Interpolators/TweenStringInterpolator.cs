namespace Sight.Tweening.Internal.Interpolators;

internal class TweenStringInterpolator : TweenInterpolator<string>
{
    public override string Interpolate(double delta, string? begin, string? end)
    {
        return $"{GetPartialString(begin, 1 - delta)}{GetPartialString(end, delta)}";
    }

    private static string GetPartialString(string? text, double delta)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var length = (int)Math.Clamp(delta * text.Length, 0, text.Length);
        if (length == 0)
            return string.Empty;

        if (length == text.Length)
            return text;

        return text[..length];
    }
}
