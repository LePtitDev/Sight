namespace Sight.Tweening.Internal.Animations;

internal sealed class TweenSegmentState<T>
{
    public T? Begin;
    public T? End;
    public T?[]? Points;
    public T?[]? Scratch;
}

/// <summary>
/// Describes how a property evolves during a transition, from the value it has when the transition starts.
/// </summary>
internal abstract class TweenPropertySegment<T>
{
    protected TweenPropertySegment(TweenInterpolator<T> interpolator)
    {
        Interpolator = interpolator ?? throw new ArgumentNullException(nameof(interpolator));
    }

    protected TweenInterpolator<T> Interpolator { get; }

    public abstract TweenSegmentState<T> Begin(T? current);

    public abstract T? Evaluate(TweenSegmentState<T> state, double delta);

    protected static ITweenArithmetic<T> RequireArithmetic(TweenInterpolator<T> interpolator)
    {
        return interpolator as ITweenArithmetic<T>
            ?? throw new NotSupportedException($"The interpolator of type '{typeof(T).FullName}' does not support arithmetic ('{nameof(ITweenArithmetic<T>)}')");
    }

    protected T? EvaluatePolyline(T?[] points, double delta)
    {
        if (points.Length == 1)
            return points[0];

        var position = Math.Clamp(delta, 0, 1) * (points.Length - 1);
        var index = Math.Min((int)position, points.Length - 2);
        return Interpolator.Interpolate(position - index, points[index], points[index + 1]);
    }
}

internal sealed class TweenToSegment<T> : TweenPropertySegment<T>
{
    private readonly T? _value;

    public TweenToSegment(TweenInterpolator<T> interpolator, T? value)
        : base(interpolator)
    {
        _value = value;
    }

    public override TweenSegmentState<T> Begin(T? current) => new() { Begin = current, End = _value };

    public override T? Evaluate(TweenSegmentState<T> state, double delta) => Interpolator.Interpolate(delta, state.Begin, state.End);
}

internal sealed class TweenFromSegment<T> : TweenPropertySegment<T>
{
    private readonly T? _value;

    public TweenFromSegment(TweenInterpolator<T> interpolator, T? value)
        : base(interpolator)
    {
        _value = value;
    }

    public override TweenSegmentState<T> Begin(T? current) => new() { Begin = _value, End = current };

    public override T? Evaluate(TweenSegmentState<T> state, double delta) => Interpolator.Interpolate(delta, state.Begin, state.End);
}

internal sealed class TweenBySegment<T> : TweenPropertySegment<T>
{
    private readonly ITweenArithmetic<T> _arithmetic;
    private readonly T _delta;

    public TweenBySegment(TweenInterpolator<T> interpolator, T delta)
        : base(interpolator)
    {
        _arithmetic = RequireArithmetic(interpolator);
        _delta = delta;
    }

    public override TweenSegmentState<T> Begin(T? current) => new() { Begin = current, End = _arithmetic.Add(current!, _delta) };

    public override T? Evaluate(TweenSegmentState<T> state, double delta) => Interpolator.Interpolate(delta, state.Begin, state.End);
}

internal sealed class TweenPathSegment<T> : TweenPropertySegment<T>
{
    private readonly T?[] _waypoints;
    private readonly TweenPathType _type;
    private readonly ITweenArithmetic<T>? _arithmetic;

    public TweenPathSegment(TweenInterpolator<T> interpolator, IEnumerable<T?> waypoints, TweenPathType type)
        : base(interpolator)
    {
        _waypoints = waypoints?.ToArray() ?? throw new ArgumentNullException(nameof(waypoints));
        if (_waypoints.Length == 0)
            throw new ArgumentException("Path must contain at least one waypoint", nameof(waypoints));

        _type = type;
        _arithmetic = type switch
        {
            TweenPathType.Linear or TweenPathType.Bezier => null,
            TweenPathType.CatmullRom => RequireArithmetic(interpolator),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown path type"),
        };
    }

    public override TweenSegmentState<T> Begin(T? current)
    {
        var points = new T?[_waypoints.Length + 1];
        points[0] = current;
        Array.Copy(_waypoints, 0, points, 1, _waypoints.Length);
        return new TweenSegmentState<T>
        {
            Points = points,
            Scratch = _type == TweenPathType.Bezier ? new T?[points.Length] : null,
        };
    }

    public override T? Evaluate(TweenSegmentState<T> state, double delta)
    {
        var points = state.Points!;
        return _type switch
        {
            TweenPathType.Bezier => EvaluateBezier(points, state.Scratch!, Math.Clamp(delta, 0, 1)),
            TweenPathType.CatmullRom => EvaluateCatmullRom(points, delta),
            _ => EvaluatePolyline(points, delta),
        };
    }

    private T? EvaluateBezier(T?[] points, T?[] scratch, double delta)
    {
        // De Casteljau's algorithm
        Array.Copy(points, scratch, points.Length);
        for (var length = scratch.Length - 1; length > 0; length--)
        {
            for (var i = 0; i < length; i++)
                scratch[i] = Interpolator.Interpolate(delta, scratch[i], scratch[i + 1]);
        }

        return scratch[0];
    }

    private T? EvaluateCatmullRom(T?[] points, double delta)
    {
        if (points.Length == 1)
            return points[0];

        var position = Math.Clamp(delta, 0, 1) * (points.Length - 1);
        var index = Math.Min((int)position, points.Length - 2);
        var t = position - index;
        var t2 = t * t;
        var t3 = t2 * t;

        var p0 = points[Math.Max(index - 1, 0)]!;
        var p1 = points[index]!;
        var p2 = points[index + 1]!;
        var p3 = points[Math.Min(index + 2, points.Length - 1)]!;

        var arithmetic = _arithmetic!;
        return arithmetic.Add(
            arithmetic.Add(arithmetic.Scale(p0, (-t + 2 * t2 - t3) / 2), arithmetic.Scale(p1, (2 - 5 * t2 + 3 * t3) / 2)),
            arithmetic.Add(arithmetic.Scale(p2, (t + 4 * t2 - 3 * t3) / 2), arithmetic.Scale(p3, (t3 - t2) / 2)));
    }
}

internal sealed class TweenShakeSegment<T> : TweenPropertySegment<T>
{
    private readonly ITweenArithmetic<T> _arithmetic;
    private readonly T _strength;
    private readonly int _vibrato;
    private readonly int? _seed;

    public TweenShakeSegment(TweenInterpolator<T> interpolator, T strength, int vibrato, int? seed)
        : base(interpolator)
    {
        if (vibrato < 1)
            throw new ArgumentOutOfRangeException(nameof(vibrato), vibrato, "Vibrato must be positive");

        _arithmetic = RequireArithmetic(interpolator);
        _strength = strength;
        _vibrato = vibrato;
        _seed = seed;
    }

    public override TweenSegmentState<T> Begin(T? current)
    {
        // Random offsets are drawn once, so that seeking is deterministic
        var random = _seed.HasValue ? new Random(_seed.Value) : new Random();
        var points = new T?[_vibrato + 1];
        points[0] = current;
        points[_vibrato] = current;
        for (var i = 1; i < _vibrato; i++)
        {
            var amplitude = _arithmetic.Scale(_strength, 1 - (double)i / _vibrato);
            points[i] = _arithmetic.Add(current!, _arithmetic.RandomOffset(amplitude, random));
        }

        return new TweenSegmentState<T> { Begin = current, Points = points };
    }

    public override T? Evaluate(TweenSegmentState<T> state, double delta) => EvaluatePolyline(state.Points!, delta);
}

internal sealed class TweenPunchSegment<T> : TweenPropertySegment<T>
{
    private readonly ITweenArithmetic<T> _arithmetic;
    private readonly T _punch;
    private readonly int _vibrato;
    private readonly double _elasticity;

    public TweenPunchSegment(TweenInterpolator<T> interpolator, T punch, int vibrato, double elasticity)
        : base(interpolator)
    {
        if (vibrato < 1)
            throw new ArgumentOutOfRangeException(nameof(vibrato), vibrato, "Vibrato must be positive");

        if (elasticity is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(elasticity), elasticity, "Elasticity must be between 0 and 1");

        _arithmetic = RequireArithmetic(interpolator);
        _punch = punch;
        _vibrato = vibrato;
        _elasticity = elasticity;
    }

    public override TweenSegmentState<T> Begin(T? current) => new() { Begin = current };

    public override T? Evaluate(TweenSegmentState<T> state, double delta)
    {
        delta = Math.Clamp(delta, 0, 1);
        if (delta >= 1)
            return state.Begin;

        // Damped oscillation around the initial value; the backward swings are scaled by the elasticity
        var factor = Math.Sin(Math.PI * _vibrato * delta) * (1 - delta);
        if (factor < 0)
            factor *= _elasticity;

        return _arithmetic.Add(state.Begin!, _arithmetic.Scale(_punch, factor));
    }
}
