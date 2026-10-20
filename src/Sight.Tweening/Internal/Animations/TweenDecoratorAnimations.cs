namespace Sight.Tweening.Internal.Animations;

/// <summary>
/// Base class of animations wrapping another animation in its own state context.
/// </summary>
internal abstract class TweenDecoratorAnimation : ITweenAnimation
{
    protected TweenDecoratorAnimation(ITweenAnimation inner)
    {
        Inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public ITweenAnimation Inner { get; }

    public virtual TimeSpan Duration => Inner.Duration;

    public void SetUp(TweenStateContext context)
    {
        var state = new DecoratorState(new TweenStateContext(GetInnerTarget(context)));
        Inner.SetUp(state.Inner);
        context.Initialize(state, null);
        OnSetUp();
    }

    public void Update(TweenStateContext context)
    {
        if (context.InitialState is not DecoratorState state)
            throw new InvalidOperationException("Animation must be set up");

        Update(state, context.Time);
    }

    protected virtual object? GetInnerTarget(TweenStateContext context) => context.Target;

    protected virtual void OnSetUp()
    {
    }

    protected abstract void Update(DecoratorState state, TimeSpan time);

    protected void UpdateInner(DecoratorState state, TimeSpan time)
    {
        state.Inner.Time = time;
        Inner.Update(state.Inner);
    }

    protected sealed class DecoratorState
    {
        public DecoratorState(TweenStateContext inner)
        {
            Inner = inner;
        }

        public TweenStateContext Inner { get; }

        public bool HasRendered { get; set; }

        public bool IsCompleted { get; set; }

        public int Iteration { get; set; }
    }
}

/// <summary>
/// Repeats an animation.
/// </summary>
internal sealed class TweenLoopAnimation : TweenDecoratorAnimation
{
    private readonly int _count;
    private readonly TweenLoopType _type;
    private readonly Action<int>[] _onLoop;

    public TweenLoopAnimation(ITweenAnimation inner, int count, TweenLoopType type, Action<int>[]? onLoop = null)
        : base(inner)
    {
        if (count is 0 or < -1)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Loop count must be positive or infinite (-1)");

        _count = count;
        _type = type;
        _onLoop = onLoop ?? [];
        Duration = TweenTime.Multiply(inner.Duration, count);
    }

    public override TimeSpan Duration { get; }

    protected override void Update(DecoratorState state, TimeSpan time)
    {
        var innerDuration = Inner.Duration;
        int iteration;
        TimeSpan local;
        if (innerDuration <= TimeSpan.Zero || time < TimeSpan.Zero)
        {
            iteration = 0;
            local = time;
        }
        else
        {
            var index = time.Ticks / innerDuration.Ticks;
            if (_count > 0 && index >= _count)
            {
                iteration = _count - 1;
                local = innerDuration;
            }
            else
            {
                iteration = (int)Math.Min(index, int.MaxValue);
                local = new TimeSpan(time.Ticks - index * innerDuration.Ticks);
            }

            if (IsMirrored(iteration))
                local = innerDuration - local;
        }

        var previous = state.Iteration;
        if (state.HasRendered && previous != iteration)
        {
            // Render the boundary of the iteration being left so that its end (or start) is never skipped
            var leavingForward = iteration > previous;
            var atEnd = leavingForward != IsMirrored(previous);
            UpdateInner(state, atEnd ? innerDuration : TimeSpan.Zero);
        }

        UpdateInner(state, local);
        state.Iteration = iteration;
        state.HasRendered = true;

        if (previous != iteration)
        {
            foreach (var callback in _onLoop)
                callback(iteration);
        }
    }

    private bool IsMirrored(int iteration) => _type == TweenLoopType.Yoyo && iteration % 2 == 1;
}

/// <summary>
/// Invokes user callbacks around an animation.
/// </summary>
internal sealed class TweenHooksAnimation : TweenDecoratorAnimation
{
    private readonly Action[] _onStart;
    private readonly Action[] _onUpdate;
    private readonly Action[] _onComplete;

    public TweenHooksAnimation(ITweenAnimation inner, Action[]? onStart, Action[]? onUpdate, Action[]? onComplete)
        : base(inner)
    {
        _onStart = onStart ?? [];
        _onUpdate = onUpdate ?? [];
        _onComplete = onComplete ?? [];
    }

    protected override void OnSetUp()
    {
        foreach (var callback in _onStart)
            callback();
    }

    protected override void Update(DecoratorState state, TimeSpan time)
    {
        UpdateInner(state, time);

        foreach (var callback in _onUpdate)
            callback();

        var duration = Duration;
        var isCompleted = !TweenTime.IsInfinite(duration) && time >= duration;
        if (isCompleted && !state.IsCompleted)
        {
            foreach (var callback in _onComplete)
                callback();
        }

        state.IsCompleted = isCompleted;
    }
}

/// <summary>
/// Plays an animation on another target than its parent.
/// </summary>
internal sealed class TweenTargetAnimation : TweenDecoratorAnimation
{
    private readonly object? _target;

    public TweenTargetAnimation(ITweenAnimation inner, object? target)
        : base(inner)
    {
        _target = target;
    }

    protected override object? GetInnerTarget(TweenStateContext context) => _target;

    protected override void Update(DecoratorState state, TimeSpan time) => UpdateInner(state, time);
}

/// <summary>
/// Does nothing during a given duration.
/// </summary>
internal sealed class TweenIntervalAnimation : ITweenAnimation
{
    public TweenIntervalAnimation(TimeSpan duration)
    {
        TweenTime.ThrowIfInvalidDuration(duration, nameof(duration));
        Duration = duration;
    }

    public TimeSpan Duration { get; }

    public void SetUp(TweenStateContext context) => context.Initialize(null, null);

    public void Update(TweenStateContext context)
    {
    }
}

/// <summary>
/// Invokes a callback each time the playhead crosses its position, in both directions.
/// </summary>
internal sealed class TweenCallbackAnimation : ITweenAnimation
{
    private readonly Action _callback;

    public TweenCallbackAnimation(Action callback)
    {
        _callback = callback ?? throw new ArgumentNullException(nameof(callback));
    }

    public TimeSpan Duration => TimeSpan.Zero;

    public void SetUp(TweenStateContext context) => context.Initialize(new CallbackState(), null);

    public void Update(TweenStateContext context)
    {
        if (context.InitialState is not CallbackState state)
            throw new InvalidOperationException("Animation must be set up");

        var isAfter = context.Time >= TimeSpan.Zero;
        var crossed = state.HasRendered ? isAfter != state.IsAfter : isAfter;
        state.HasRendered = true;
        state.IsAfter = isAfter;

        if (crossed)
            _callback();
    }

    private sealed class CallbackState
    {
        public bool HasRendered { get; set; }

        public bool IsAfter { get; set; }
    }
}
