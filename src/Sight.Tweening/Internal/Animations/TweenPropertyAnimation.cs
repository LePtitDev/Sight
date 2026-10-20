namespace Sight.Tweening.Internal.Animations;

/// <summary>
/// Untyped property animation (values are boxed).
/// </summary>
internal sealed class TweenPropertyAnimation : ITweenAnimation
{
    public TweenPropertyAnimation(
        TimeSpan duration,
        ITweenProperty property,
        ITweenTransition transition,
        ITweenInterpolator? interpolator = null)
    {
        TweenTime.ThrowIfInvalidDuration(duration, nameof(duration));
        Duration = duration;
        Property = property ?? throw new ArgumentNullException(nameof(property));
        Transition = transition ?? throw new ArgumentNullException(nameof(transition));
        Interpolator = interpolator ?? TweenInterpolator.GetDefault(property.Type);
    }

    public TimeSpan Duration { get; }

    public ITweenProperty Property { get; }

    public ITweenTransition Transition { get; }

    public ITweenInterpolator Interpolator { get; }

    public Func<double, double> Easing { get; set; } = TweenEasing.Linear;

    public void SetUp(TweenStateContext context)
    {
        var initialState = Transition.GetSourceValue() ?? Property.Get(context.Target);
        if (initialState is null && Property.Type.IsValueType)
            throw new NotSupportedException("Null value is not supported");

        var transitionTarget = Transition.GetTargetValue(initialState);
        if (transitionTarget is null && Property.Type.IsValueType)
            throw new NotSupportedException("Null value is not supported");

        context.Initialize(initialState, transitionTarget);
    }

    public void Update(TweenStateContext context)
    {
        if (!context.Initialized)
            throw new InvalidOperationException("Tween context must be set up");

        var delta = TweenTime.Progress(context.Time, Duration);
        if (Easing is not null)
            delta = Easing(delta);

        var currentValue = Interpolator.Interpolate(delta, context.InitialState, context.TransitionTarget);
        Property.Set(context.Target, currentValue);
    }
}

/// <summary>
/// Typed property animation: no boxing nor allocation per update.
/// </summary>
internal sealed class TweenPropertyAnimation<T> : ITweenAnimation
{
    private readonly ITweenProperty<T> _property;
    private readonly TweenPropertySegment<T> _segment;
    private readonly Func<double, double> _easing;
    private readonly bool _hasStart;
    private readonly T? _start;

    public TweenPropertyAnimation(TimeSpan duration, ITweenProperty<T> property, TweenPropertySegment<T> segment, Func<double, double>? easing = null)
        : this(duration, property, segment, easing, false, default)
    {
    }

    public TweenPropertyAnimation(TimeSpan duration, ITweenProperty<T> property, TweenPropertySegment<T> segment, Func<double, double>? easing, bool hasStart, T? start)
    {
        TweenTime.ThrowIfInvalidDuration(duration, nameof(duration));
        Duration = duration;
        _property = property ?? throw new ArgumentNullException(nameof(property));
        _segment = segment ?? throw new ArgumentNullException(nameof(segment));
        _easing = easing ?? TweenEasing.Linear;
        _hasStart = hasStart;
        _start = start;
    }

    public TimeSpan Duration { get; }

    public void SetUp(TweenStateContext context)
    {
        var begin = _hasStart ? _start : _property.Get(context.Target);
        context.Initialize(_segment.Begin(begin), null);
    }

    public void Update(TweenStateContext context)
    {
        if (context.InitialState is not TweenSegmentState<T> state)
            throw new InvalidOperationException("Tween context must be set up");

        var delta = _easing(TweenTime.Progress(context.Time, Duration));
        _property.Set(context.Target, _segment.Evaluate(state, delta));
    }
}
