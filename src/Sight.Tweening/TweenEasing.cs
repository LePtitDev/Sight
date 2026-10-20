namespace Sight.Tweening;

/// <summary>
/// Easing functions: they map a linear progress (0..1) to an eased progress.
/// Some easings (back, elastic, spring) overshoot outside of 0..1.
/// </summary>
/// <remarks>
/// Based on https://gist.github.com/Kryzarel/bba64622057f21a1d6d44879f9cd7bd4.
/// Factories (<see cref="Power"/>, <see cref="Back"/>, <see cref="Elastic"/>) create "in" easings:
/// use <see cref="Out"/>, <see cref="InOut"/> or <see cref="OutIn"/> to derive the other variants.
/// </remarks>
public static class TweenEasing
{
    private const double DefaultBackOvershoot = 1.70158;
    private const double DefaultElasticPeriod = 0.3;

#pragma warning disable CS1591 // Standard easing curves, see https://easings.net
    public static double Linear(double t) => t;

    public static double InQuad(double t) => t * t;
    public static double OutQuad(double t) => 1 - InQuad(1 - t);
    public static double InOutQuad(double t) => t < 0.5 ? InQuad(t * 2) / 2 : 1 - InQuad((1 - t) * 2) / 2;

    public static double InCubic(double t) => t * t * t;
    public static double OutCubic(double t) => 1 - InCubic(1 - t);
    public static double InOutCubic(double t) => t < 0.5 ? InCubic(t * 2) / 2 : 1 - InCubic((1 - t) * 2) / 2;

    public static double InQuart(double t) => t * t * t * t;
    public static double OutQuart(double t) => 1 - InQuart(1 - t);
    public static double InOutQuart(double t) => t < 0.5 ? InQuart(t * 2) / 2 : 1 - InQuart((1 - t) * 2) / 2;

    public static double InQuint(double t) => t * t * t * t * t;
    public static double OutQuint(double t) => 1 - InQuint(1 - t);
    public static double InOutQuint(double t) => t < 0.5 ? InQuint(t * 2) / 2 : 1 - InQuint((1 - t) * 2) / 2;

    public static double InSine(double t) => 1 - Math.Cos(t * Math.PI / 2);
    public static double OutSine(double t) => Math.Sin(t * Math.PI / 2);
    public static double InOutSine(double t) => (Math.Cos(t * Math.PI) - 1) / -2;

    public static double InExpo(double t) => t <= 0 ? 0 : Math.Pow(2, 10 * (t - 1));
    public static double OutExpo(double t) => 1 - InExpo(1 - t);
    public static double InOutExpo(double t) => t < 0.5 ? InExpo(t * 2) / 2 : 1 - InExpo((1 - t) * 2) / 2;

    public static double InCirc(double t) => -(Math.Sqrt(1 - t * t) - 1);
    public static double OutCirc(double t) => 1 - InCirc(1 - t);
    public static double InOutCirc(double t) => t < 0.5 ? InCirc(t * 2) / 2 : 1 - InCirc((1 - t) * 2) / 2;

    public static double InElastic(double t) => 1 - OutElastic(1 - t);
    public static double OutElastic(double t)
    {
        if (t <= 0)
            return 0;

        if (t >= 1)
            return 1;

        const double p = DefaultElasticPeriod;
        return Math.Pow(2, -10 * t) * Math.Sin((t - p / 4) * (2 * Math.PI) / p) + 1;
    }
    public static double InOutElastic(double t) => t < 0.5 ? InElastic(t * 2) / 2 : 1 - InElastic((1 - t) * 2) / 2;

    public static double InBack(double t) => t * t * ((DefaultBackOvershoot + 1) * t - DefaultBackOvershoot);
    public static double OutBack(double t) => 1 - InBack(1 - t);
    public static double InOutBack(double t) => t < 0.5 ? InBack(t * 2) / 2 : 1 - InBack((1 - t) * 2) / 2;

    public static double InBounce(double t) => 1 - OutBounce(1 - t);
    public static double OutBounce(double t)
    {
        const double div = 2.75;
        const double mult = 7.5625;

        if (t < 1 / div)
            return mult * t * t;

        if (t < 2 / div)
        {
            t -= 1.5 / div;
            return mult * t * t + 0.75;
        }

        if (t < 2.5 / div)
        {
            t -= 2.25 / div;
            return mult * t * t + 0.9375;
        }

        t -= 2.625 / div;
        return mult * t * t + 0.984375;
    }
    public static double InOutBounce(double t) => t < 0.5 ? InBounce(t * 2) / 2 : 1 - InBounce((1 - t) * 2) / 2;
#pragma warning restore CS1591

    /// <summary>
    /// Gets the easing function matching <paramref name="ease"/>.
    /// </summary>
    public static Func<double, double> Get(TweenEase ease) => ease switch
    {
        TweenEase.Linear => Linear,
        TweenEase.InQuad => InQuad,
        TweenEase.OutQuad => OutQuad,
        TweenEase.InOutQuad => InOutQuad,
        TweenEase.InCubic => InCubic,
        TweenEase.OutCubic => OutCubic,
        TweenEase.InOutCubic => InOutCubic,
        TweenEase.InQuart => InQuart,
        TweenEase.OutQuart => OutQuart,
        TweenEase.InOutQuart => InOutQuart,
        TweenEase.InQuint => InQuint,
        TweenEase.OutQuint => OutQuint,
        TweenEase.InOutQuint => InOutQuint,
        TweenEase.InSine => InSine,
        TweenEase.OutSine => OutSine,
        TweenEase.InOutSine => InOutSine,
        TweenEase.InExpo => InExpo,
        TweenEase.OutExpo => OutExpo,
        TweenEase.InOutExpo => InOutExpo,
        TweenEase.InCirc => InCirc,
        TweenEase.OutCirc => OutCirc,
        TweenEase.InOutCirc => InOutCirc,
        TweenEase.InElastic => InElastic,
        TweenEase.OutElastic => OutElastic,
        TweenEase.InOutElastic => InOutElastic,
        TweenEase.InBack => InBack,
        TweenEase.OutBack => OutBack,
        TweenEase.InOutBack => InOutBack,
        TweenEase.InBounce => InBounce,
        TweenEase.OutBounce => OutBounce,
        TweenEase.InOutBounce => InOutBounce,
        _ => throw new ArgumentOutOfRangeException(nameof(ease), ease, "Unknown ease"),
    };

    /// <summary>
    /// Creates the "out" variant of an "in" easing (played backwards).
    /// </summary>
    public static Func<double, double> Out(Func<double, double> easeIn)
    {
        ThrowIfNull(easeIn);
        return t => 1 - easeIn(1 - t);
    }

    /// <summary>
    /// Creates the "in-out" variant of an "in" easing (in on the first half, out on the second half).
    /// </summary>
    public static Func<double, double> InOut(Func<double, double> easeIn)
    {
        ThrowIfNull(easeIn);
        return t => t < 0.5 ? easeIn(t * 2) / 2 : 1 - easeIn((1 - t) * 2) / 2;
    }

    /// <summary>
    /// Creates the "out-in" variant of an "in" easing (out on the first half, in on the second half).
    /// </summary>
    public static Func<double, double> OutIn(Func<double, double> easeIn)
    {
        ThrowIfNull(easeIn);
        return t => t < 0.5 ? (1 - easeIn(1 - t * 2)) / 2 : 0.5 + easeIn(t * 2 - 1) / 2;
    }

    /// <summary>
    /// Creates a polynomial "in" easing: <c>t^exponent</c>.
    /// </summary>
    public static Func<double, double> Power(double exponent)
    {
        if (exponent <= 0 || double.IsNaN(exponent))
            throw new ArgumentOutOfRangeException(nameof(exponent), exponent, "Exponent must be positive");

        return t => t <= 0 ? 0 : Math.Pow(t, exponent);
    }

    /// <summary>
    /// Creates a "back" "in" easing that goes backward by an amount controlled by <paramref name="overshoot"/> before moving forward.
    /// </summary>
    public static Func<double, double> Back(double overshoot = DefaultBackOvershoot)
    {
        return t => t * t * ((overshoot + 1) * t - overshoot);
    }

    /// <summary>
    /// Creates an elastic "in" easing.
    /// </summary>
    /// <param name="amplitude">Amplitude of the oscillations (at least 1).</param>
    /// <param name="period">Period of the oscillations, relative to the duration.</param>
    public static Func<double, double> Elastic(double amplitude = 1, double period = DefaultElasticPeriod)
    {
        if (period <= 0)
            throw new ArgumentOutOfRangeException(nameof(period), period, "Period must be positive");

        double shift;
        if (amplitude < 1)
        {
            amplitude = 1;
            shift = period / 4;
        }
        else
        {
            shift = period / (2 * Math.PI) * Math.Asin(1 / amplitude);
        }

        return t =>
        {
            if (t <= 0)
                return 0;

            if (t >= 1)
                return 1;

            return -(amplitude * Math.Pow(2, 10 * (t - 1)) * Math.Sin((t - 1 - shift) * (2 * Math.PI) / period));
        };
    }

    /// <summary>
    /// Creates a cubic Bézier easing, like CSS <c>cubic-bezier(x1, y1, x2, y2)</c>.
    /// </summary>
    /// <param name="x1">X coordinate of the first control point, between 0 and 1.</param>
    /// <param name="y1">Y coordinate of the first control point.</param>
    /// <param name="x2">X coordinate of the second control point, between 0 and 1.</param>
    /// <param name="y2">Y coordinate of the second control point.</param>
    public static Func<double, double> CubicBezier(double x1, double y1, double x2, double y2)
    {
        if (x1 is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(x1), x1, "Value must be between 0 and 1");

        if (x2 is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(x2), x2, "Value must be between 0 and 1");

        var cx = 3 * x1;
        var bx = 3 * (x2 - x1) - cx;
        var ax = 1 - cx - bx;
        var cy = 3 * y1;
        var by = 3 * (y2 - y1) - cy;
        var ay = 1 - cy - by;

        double SampleX(double t) => ((ax * t + bx) * t + cx) * t;
        double SampleY(double t) => ((ay * t + by) * t + cy) * t;
        double SampleDerivativeX(double t) => (3 * ax * t + 2 * bx) * t + cx;

        double SolveX(double x)
        {
            const double epsilon = 1e-7;

            // Newton-Raphson converges quickly in most cases
            var t = x;
            for (var i = 0; i < 8; i++)
            {
                var error = SampleX(t) - x;
                if (Math.Abs(error) < epsilon)
                    return t;

                var derivative = SampleDerivativeX(t);
                if (Math.Abs(derivative) < 1e-6)
                    break;

                t -= error / derivative;
            }

            // Fall back to bisection
            var low = 0.0;
            var high = 1.0;
            t = x;
            for (var i = 0; i < 64 && low < high; i++)
            {
                var value = SampleX(t);
                if (Math.Abs(value - x) < epsilon)
                    return t;

                if (x > value)
                    low = t;
                else
                    high = t;

                t = (high - low) / 2 + low;
            }

            return t;
        }

        return t =>
        {
            if (t <= 0)
                return 0;

            if (t >= 1)
                return 1;

            return SampleY(SolveX(t));
        };
    }

    /// <summary>
    /// Creates a stepped easing, like CSS <c>steps(count, position)</c>.
    /// </summary>
    /// <param name="count">Number of steps.</param>
    /// <param name="position">Where the jumps happen.</param>
    public static Func<double, double> Steps(int count, TweenStepPosition position = TweenStepPosition.End)
    {
        var jumps = position switch
        {
            TweenStepPosition.End or TweenStepPosition.Start => count,
            TweenStepPosition.Both => count + 1,
            TweenStepPosition.None => count - 1,
            _ => throw new ArgumentOutOfRangeException(nameof(position), position, "Unknown step position"),
        };

        if (count < 1 || jumps < 1)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Not enough steps for this step position");

        var jumpAtStart = position is TweenStepPosition.Start or TweenStepPosition.Both;
        return t =>
        {
            var step = Math.Floor(t * count);
            if (jumpAtStart)
                step++;

            if (t >= 0 && step < 0)
                step = 0;

            if (t <= 1 && step > jumps)
                step = jumps;

            return step / jumps;
        };
    }

    /// <summary>
    /// Creates an easing simulating a damped spring going from 0 to 1.
    /// The physical simulation is stretched so that the spring settles at the end of the transition.
    /// </summary>
    /// <param name="stiffness">Spring stiffness (higher is snappier).</param>
    /// <param name="damping">Damping force (lower makes more oscillations).</param>
    /// <param name="mass">Mass attached to the spring.</param>
    /// <param name="initialVelocity">Initial velocity toward the end value.</param>
    public static Func<double, double> Spring(double stiffness = 100, double damping = 10, double mass = 1, double initialVelocity = 0)
    {
        if (stiffness <= 0)
            throw new ArgumentOutOfRangeException(nameof(stiffness), stiffness, "Stiffness must be positive");

        if (damping <= 0)
            throw new ArgumentOutOfRangeException(nameof(damping), damping, "Damping must be positive");

        if (mass <= 0)
            throw new ArgumentOutOfRangeException(nameof(mass), mass, "Mass must be positive");

        const double epsilon = 0.001;
        var omega = Math.Sqrt(stiffness / mass);
        var zeta = damping / (2 * Math.Sqrt(stiffness * mass));

        Func<double, double> displacement; // distance to the target over the physical time, starting at -1
        double settleTime;
        if (zeta < 1)
        {
            var omegaD = omega * Math.Sqrt(1 - zeta * zeta);
            var b = (initialVelocity - zeta * omega) / omegaD;
            displacement = time => Math.Exp(-zeta * omega * time) * (-Math.Cos(omegaD * time) + b * Math.Sin(omegaD * time));
            settleTime = Math.Log(Math.Sqrt(1 + b * b) / epsilon) / (zeta * omega);
        }
        else
        {
            if (zeta == 1)
            {
                var b = initialVelocity - omega;
                displacement = time => (-1 + b * time) * Math.Exp(-omega * time);
            }
            else
            {
                var root = Math.Sqrt(zeta * zeta - 1);
                var r1 = -omega * (zeta - root);
                var r2 = -omega * (zeta + root);
                var c2 = (initialVelocity + r1) / (r2 - r1);
                var c1 = -1 - c2;
                displacement = time => c1 * Math.Exp(r1 * time) + c2 * Math.Exp(r2 * time);
            }

            // Non oscillating: find when the displacement stays below epsilon
            var step = 1 / (omega * 50);
            settleTime = step;
            for (var i = 0; i < 1_000_000; i++)
            {
                var current = Math.Abs(displacement(settleTime));
                if (current < epsilon && Math.Abs(displacement(settleTime + step)) <= current)
                    break;

                settleTime += step;
            }
        }

        return t =>
        {
            if (t <= 0)
                return 0;

            if (t >= 1)
                return 1;

            return 1 + displacement(t * settleTime);
        };
    }

    private static void ThrowIfNull(Func<double, double> ease)
    {
        if (ease is null)
            throw new ArgumentNullException(nameof(ease));
    }
}
