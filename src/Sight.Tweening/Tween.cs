using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Sight.Tweening.Internal;
using Sight.Tweening.Internal.Animations;
using Sight.Tweening.Internal.Properties;

namespace Sight.Tweening;

/// <summary>
/// Entry point to describe animations with builders.
/// </summary>
public static class Tween
{
    /// <summary>
    /// Loop count repeating an animation forever.
    /// </summary>
    public const int Infinite = -1;

    /// <summary>
    /// Animates a public property found by reflection (values are boxed).
    /// </summary>
    public static PropertyBuilder Property(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type targetType,
        string propertyName)
    {
        var info = targetType.GetProperty(propertyName) ?? throw new ArgumentException($"Cannot find '{propertyName}' in type '{targetType.FullName}'");
        var property = new TweenReflectionProperty(info);
        return new PropertyBuilder(property);
    }

    /// <summary>
    /// Animates a writable member of the target, described by an expression like <c>t => t.Position</c> or <c>t => t.Child.X</c>.
    /// </summary>
    public static PropertyBuilder<TValue> Property<TTarget, TValue>(Expression<Func<TTarget, TValue?>> expression)
        where TTarget : class
    {
        var property = new TweenExpressionProperty<TTarget, TValue?>(expression);
        return new PropertyBuilder<TValue>(property);
    }

    /// <summary>
    /// Animates a value accessed through untyped delegates receiving the target (values are boxed).
    /// </summary>
    public static PropertyBuilder Property(Type valueType, Func<object?, object?> getter, Action<object?, object?> setter)
    {
        var property = new TweenDelegateProperty(valueType, getter, setter);
        return new PropertyBuilder(property);
    }

    /// <summary>
    /// Animates a value accessed through untyped delegates (values are boxed).
    /// </summary>
    public static PropertyBuilder Property(Type valueType, Func<object?> getter, Action<object?> setter)
    {
        var property = new TweenDelegateProperty(valueType, getter, setter);
        return new PropertyBuilder(property);
    }

    /// <summary>
    /// Animates a value of the target accessed through typed delegates.
    /// </summary>
    public static PropertyBuilder<TValue> Property<TTarget, TValue>(Func<TTarget, TValue> getter, Action<TTarget, TValue> setter)
        where TTarget : class
    {
        return new PropertyBuilder<TValue>(new TweenDelegateProperty<TTarget, TValue>(getter, setter));
    }

    /// <summary>
    /// Animates a value accessed through typed delegates.
    /// </summary>
    [Obsolete("TTarget is not used, call Property<TValue>(getter, setter) instead.")]
    public static PropertyBuilder<TValue> Property<TTarget, TValue>(Func<TValue> getter, Action<TValue> setter)
    {
        return Property(getter, setter);
    }

    /// <summary>
    /// Animates a value accessed through typed delegates, ignoring the target (closures, static values...).
    /// </summary>
    public static PropertyBuilder<TValue> Property<TValue>(Func<TValue> getter, Action<TValue> setter)
    {
        return new PropertyBuilder<TValue>(new TweenDelegateProperty<TValue>(getter, setter));
    }

    /// <summary>
    /// Plays the animations one after the other.
    /// </summary>
    public static SequenceBuilder Sequence(params Builder[] tweens)
    {
        return new SequenceBuilder(tweens);
    }

    /// <summary>
    /// Plays the animations at the same time.
    /// </summary>
    public static ParallelBuilder Parallel(params Builder[] tweens)
    {
        return new ParallelBuilder(tweens);
    }

    /// <summary>
    /// Creates a timeline, where animations can be appended, joined or inserted at any time.
    /// </summary>
    public static TimelineBuilder Timeline()
    {
        return new TimelineBuilder();
    }

    /// <summary>
    /// Plays the animations with a constant delay between their starts.
    /// </summary>
    public static TimelineBuilder Stagger(TimeSpan interval, params Builder[] tweens)
    {
        ThrowIfNegative(interval, nameof(interval));
        if (tweens is null)
            throw new ArgumentNullException(nameof(tweens));

        var timeline = new TimelineBuilder();
        for (var i = 0; i < tweens.Length; i++)
            timeline.Insert(TweenTime.Scale(interval, i), tweens[i]);

        return timeline;
    }

    /// <summary>
    /// Creates an animation for each target, each one starting <paramref name="interval"/> after the previous one.
    /// Each created animation is applied to its own target.
    /// </summary>
    public static TimelineBuilder Stagger<TTarget>(IEnumerable<TTarget> targets, TimeSpan interval, Func<TTarget, Builder> factory)
    {
        if (targets is null)
            throw new ArgumentNullException(nameof(targets));

        if (factory is null)
            throw new ArgumentNullException(nameof(factory));

        return Stagger(interval, targets.Select(t => factory(t).WithTarget(t)).ToArray());
    }

    /// <summary>
    /// Waits for <paramref name="duration"/>.
    /// </summary>
    public static Builder Delay(TimeSpan duration)
    {
        TweenTime.ThrowIfInvalidDuration(duration, nameof(duration));
        return new IntervalBuilder(duration);
    }

    /// <summary>
    /// Invokes <paramref name="callback"/> each time the playhead crosses this point (in both directions).
    /// </summary>
    public static Builder Callback(Action callback)
    {
        if (callback is null)
            throw new ArgumentNullException(nameof(callback));

        return new CallbackBuilder(callback);
    }

    private static void ThrowIfNegative(TimeSpan time, string paramName)
    {
        if (time < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(paramName, time, "Time must be positive");
    }

    /// <summary>
    /// Base class of animation builders. Common options (delay, loops, callbacks, target) are set with the
    /// extension methods of <see cref="TweenBuilderExtensions"/>.
    /// </summary>
    public abstract class Builder
    {
        private List<Action>? _onStart;
        private List<Action>? _onUpdate;
        private List<Action>? _onComplete;
        private List<Action<int>>? _onLoop;
        private bool _hasTarget;
        private object? _target;

        /// <summary>
        /// Initializes the builder.
        /// </summary>
        protected internal Builder()
        {
        }

        /// <summary>
        /// Gets the total duration of the built animation, including delay and loops (<see cref="TimeSpan.MaxValue"/> when infinite).
        /// </summary>
        public TimeSpan Duration => TweenTime.Add(StartDelay, TweenTime.Multiply(CoreDuration, LoopCount));

        /// <summary>
        /// Gets the delay before the animation starts.
        /// </summary>
        public TimeSpan StartDelay { get; private set; }

        /// <summary>
        /// Gets the number of iterations (<see cref="Infinite"/> to repeat forever).
        /// </summary>
        public int LoopCount { get; private set; } = 1;

        /// <summary>
        /// Gets how the iterations are chained.
        /// </summary>
        public TweenLoopType LoopType { get; private set; }

        /// <summary>
        /// Gets the duration of a single iteration.
        /// </summary>
        protected abstract TimeSpan CoreDuration { get; }

        /// <summary>
        /// Creates the animation. The builder can be reused to create other animations.
        /// </summary>
        public ITweenAnimation Build()
        {
            var animation = BuildCore();

            if (LoopCount != 1)
                animation = new TweenLoopAnimation(animation, LoopCount, LoopType, _onLoop?.ToArray());

            if (_onStart is not null || _onUpdate is not null || _onComplete is not null)
                animation = new TweenHooksAnimation(animation, _onStart?.ToArray(), _onUpdate?.ToArray(), _onComplete?.ToArray());

            if (StartDelay > TimeSpan.Zero)
            {
                var delayed = new TweenTimelineAnimation();
                delayed.Insert(StartDelay, animation);
                animation = delayed;
            }

            if (_hasTarget)
                animation = new TweenTargetAnimation(animation, _target);

            return animation;
        }

        /// <summary>
        /// Creates the animation of a single iteration.
        /// </summary>
        protected abstract ITweenAnimation BuildCore();

        internal void SetDelay(TimeSpan delay)
        {
            TweenTime.ThrowIfInvalidDuration(delay, nameof(delay));
            StartDelay = delay;
        }

        internal void SetLoops(int count, TweenLoopType type)
        {
            if (count is 0 or < Infinite)
                throw new ArgumentOutOfRangeException(nameof(count), count, "Loop count must be positive or infinite (-1)");

            LoopCount = count;
            LoopType = type;
        }

        internal void SetTarget(object? target)
        {
            _hasTarget = true;
            _target = target;
        }

        internal void AddOnStart(Action callback) => (_onStart ??= new()).Add(callback ?? throw new ArgumentNullException(nameof(callback)));

        internal void AddOnUpdate(Action callback) => (_onUpdate ??= new()).Add(callback ?? throw new ArgumentNullException(nameof(callback)));

        internal void AddOnComplete(Action callback) => (_onComplete ??= new()).Add(callback ?? throw new ArgumentNullException(nameof(callback)));

        internal void AddOnLoop(Action<int> callback) => (_onLoop ??= new()).Add(callback ?? throw new ArgumentNullException(nameof(callback)));
    }

    /// <summary>
    /// Base class of property animation builders.
    /// </summary>
    public abstract class PropertyBuilderBase : Builder
    {
        /// <summary>
        /// Initializes a builder animating <paramref name="property"/>.
        /// </summary>
        protected internal PropertyBuilderBase(ITweenProperty property)
        {
            Property = property ?? throw new ArgumentNullException(nameof(property));
        }

        /// <summary>
        /// Gets the animated property.
        /// </summary>
        public ITweenProperty Property { get; }

        /// <summary>
        /// Gets the interpolator, or <see langword="null"/> to use the default one of the property type.
        /// </summary>
        public ITweenInterpolator? Interpolator { get; protected set; }

        /// <summary>
        /// Gets the easing of the transitions that do not specify one.
        /// </summary>
        public Func<double, double> DefaultEasing { get; protected set; } = TweenEasing.Linear;
    }

    /// <summary>
    /// Builds the animation of an untyped property (values are boxed).
    /// </summary>
    public class PropertyBuilder : PropertyBuilderBase
    {
        private readonly List<(object? Value, TimeSpan Duration, Func<double, double>? Easing)> _values = new();
        private object? _initialValue;

        /// <summary>
        /// Initializes a builder animating <paramref name="property"/>.
        /// </summary>
        public PropertyBuilder(ITweenProperty property)
            : base(property)
        {
        }

        /// <inheritdoc />
        protected override TimeSpan CoreDuration => _values.Aggregate(TimeSpan.Zero, (total, v) => TweenTime.Add(total, v.Duration));

        /// <summary>
        /// Sets the value the first transition starts from, instead of the current value.
        /// </summary>
        public PropertyBuilder From(object? value)
        {
            _initialValue = value;
            return this;
        }

        /// <summary>
        /// Adds a transition to <paramref name="value"/>.
        /// </summary>
        public PropertyBuilder To(object? value, TimeSpan duration, Func<double, double>? easing = null)
        {
            TweenTime.ThrowIfInvalidDuration(duration, nameof(duration));
            _values.Add((value, duration, easing));
            return this;
        }

        /// <summary>
        /// Adds a transition to <paramref name="value"/>.
        /// </summary>
        public PropertyBuilder To(object? value, TimeSpan duration, TweenEase ease) => To(value, duration, TweenEasing.Get(ease));

        /// <summary>
        /// Sets the easing of the transitions that do not specify one.
        /// </summary>
        public PropertyBuilder Ease(Func<double, double> func)
        {
            DefaultEasing = func ?? throw new ArgumentNullException(nameof(func));
            return this;
        }

        /// <summary>
        /// Sets the easing of the transitions that do not specify one.
        /// </summary>
        public PropertyBuilder Ease(TweenEase ease) => Ease(TweenEasing.Get(ease));

        /// <summary>
        /// Sets the interpolator used instead of the default one of the property type.
        /// </summary>
        public PropertyBuilder WithInterpolator(ITweenInterpolator interpolator)
        {
            Interpolator = interpolator ?? throw new ArgumentNullException(nameof(interpolator));
            return this;
        }

        /// <inheritdoc />
        protected override ITweenAnimation BuildCore()
        {
            if (_values.Count == 0)
                throw new InvalidOperationException("Transition(s) must be set");

            var animations = new ITweenAnimation[_values.Count];
            for (var i = 0; i < _values.Count; i++)
            {
                var (value, duration, easing) = _values[i];
                var transition = TweenTransition.To(value, i == 0 ? _initialValue : null);
                animations[i] = new TweenPropertyAnimation(duration, Property, transition, Interpolator)
                {
                    Easing = easing ?? DefaultEasing,
                };
            }

            return Chain(animations);
        }
    }

    /// <summary>
    /// Builds the animation of a typed property. Transitions are chained: each one starts from the value left by the previous one.
    /// </summary>
    /// <typeparam name="T">Type of the animated value.</typeparam>
    public class PropertyBuilder<T> : PropertyBuilderBase
    {
        private readonly List<Step> _steps = new();
        private readonly ITweenProperty<T> _property;
        private TweenInterpolator<T>? _interpolator;
        private bool _hasInitialValue;
        private T? _initialValue;

        /// <summary>
        /// Initializes a builder animating <paramref name="property"/>.
        /// </summary>
        public PropertyBuilder(ITweenProperty property)
            : base(property)
        {
            _property = property as ITweenProperty<T> ?? new TweenPropertyAdapter<T>(property);
        }

        /// <inheritdoc />
        protected override TimeSpan CoreDuration => _steps.Aggregate(TimeSpan.Zero, (total, s) => TweenTime.Add(total, s.Duration));

        /// <summary>
        /// Sets the value the first transition starts from, instead of the current value.
        /// </summary>
        public PropertyBuilder<T> From(T value)
        {
            _hasInitialValue = true;
            _initialValue = value;
            return this;
        }

        /// <summary>
        /// Adds a transition from <paramref name="value"/> to the current value.
        /// </summary>
        public PropertyBuilder<T> From(T value, TimeSpan duration, Func<double, double>? easing = null)
        {
            return AddStep(duration, easing, i => new TweenFromSegment<T>(i, value));
        }

        /// <summary>
        /// Adds a transition from <paramref name="value"/> to the current value.
        /// </summary>
        public PropertyBuilder<T> From(T value, TimeSpan duration, TweenEase ease) => From(value, duration, TweenEasing.Get(ease));

        /// <summary>
        /// Adds a transition to <paramref name="value"/>.
        /// </summary>
        public PropertyBuilder<T> To(T value, TimeSpan duration, Func<double, double>? easing = null)
        {
            return AddStep(duration, easing, i => new TweenToSegment<T>(i, value));
        }

        /// <summary>
        /// Adds a transition to <paramref name="value"/>.
        /// </summary>
        public PropertyBuilder<T> To(T value, TimeSpan duration, TweenEase ease) => To(value, duration, TweenEasing.Get(ease));

        /// <summary>
        /// Adds a transition to the current value plus <paramref name="delta"/> (requires <see cref="ITweenArithmetic{T}"/>).
        /// </summary>
        public PropertyBuilder<T> By(T delta, TimeSpan duration, Func<double, double>? easing = null)
        {
            return AddStep(duration, easing, i => new TweenBySegment<T>(i, delta));
        }

        /// <summary>
        /// Adds a transition to the current value plus <paramref name="delta"/> (requires <see cref="ITweenArithmetic{T}"/>).
        /// </summary>
        public PropertyBuilder<T> By(T delta, TimeSpan duration, TweenEase ease) => By(delta, duration, TweenEasing.Get(ease));

        /// <summary>
        /// Adds a transition along a path going through <paramref name="waypoints"/>, starting from the current value.
        /// </summary>
        /// <param name="waypoints">Points of the path; the last one is the end value.</param>
        /// <param name="duration">Duration of the whole path.</param>
        /// <param name="type">How the waypoints are joined.</param>
        /// <param name="easing">Easing of the progress along the path.</param>
        public PropertyBuilder<T> Path(IEnumerable<T> waypoints, TimeSpan duration, TweenPathType type = TweenPathType.Linear, Func<double, double>? easing = null)
        {
            var points = waypoints?.ToArray() ?? throw new ArgumentNullException(nameof(waypoints));
            return AddStep(duration, easing, i => new TweenPathSegment<T>(i, points, type));
        }

        /// <summary>
        /// Adds a random shake around the current value, fading out until it comes back to it (requires <see cref="ITweenArithmetic{T}"/>).
        /// </summary>
        /// <param name="strength">Maximum offset (per component for vectors).</param>
        /// <param name="duration">Duration of the shake.</param>
        /// <param name="vibrato">Number of random positions visited.</param>
        /// <param name="seed">Optional seed to get the same shake each time.</param>
        public PropertyBuilder<T> Shake(T strength, TimeSpan duration, int vibrato = 10, int? seed = null)
        {
            if (vibrato < 1)
                throw new ArgumentOutOfRangeException(nameof(vibrato), vibrato, "Vibrato must be positive");

            return AddStep(duration, TweenEasing.Linear, i => new TweenShakeSegment<T>(i, strength, vibrato, seed));
        }

        /// <summary>
        /// Adds a punch: the value is pushed toward <paramref name="punch"/> then oscillates back to the current value
        /// (requires <see cref="ITweenArithmetic{T}"/>).
        /// </summary>
        /// <param name="punch">Offset applied at the peak of the punch.</param>
        /// <param name="duration">Duration of the punch.</param>
        /// <param name="vibrato">Number of half oscillations.</param>
        /// <param name="elasticity">How far (0..1) the value goes in the opposite direction when bouncing back.</param>
        public PropertyBuilder<T> Punch(T punch, TimeSpan duration, int vibrato = 10, double elasticity = 1)
        {
            if (vibrato < 1)
                throw new ArgumentOutOfRangeException(nameof(vibrato), vibrato, "Vibrato must be positive");

            if (elasticity is < 0 or > 1)
                throw new ArgumentOutOfRangeException(nameof(elasticity), elasticity, "Elasticity must be between 0 and 1");

            return AddStep(duration, TweenEasing.Linear, i => new TweenPunchSegment<T>(i, punch, vibrato, elasticity));
        }

        /// <summary>
        /// Sets the easing of the transitions that do not specify one.
        /// </summary>
        public PropertyBuilder<T> Ease(Func<double, double> func)
        {
            DefaultEasing = func ?? throw new ArgumentNullException(nameof(func));
            return this;
        }

        /// <summary>
        /// Sets the easing of the transitions that do not specify one.
        /// </summary>
        public PropertyBuilder<T> Ease(TweenEase ease) => Ease(TweenEasing.Get(ease));

        /// <summary>
        /// Sets the interpolator used instead of <see cref="TweenInterpolator{T}.Default"/>.
        /// </summary>
        public PropertyBuilder<T> WithInterpolator(TweenInterpolator<T> interpolator)
        {
            _interpolator = interpolator ?? throw new ArgumentNullException(nameof(interpolator));
            Interpolator = interpolator;
            return this;
        }

        /// <inheritdoc />
        protected override ITweenAnimation BuildCore()
        {
            if (_steps.Count == 0)
                throw new InvalidOperationException("Transition(s) must be set");

            var interpolator = _interpolator ?? TweenInterpolator<T>.Default;
            var animations = new ITweenAnimation[_steps.Count];
            for (var i = 0; i < _steps.Count; i++)
            {
                var step = _steps[i];
                animations[i] = new TweenPropertyAnimation<T>(
                    step.Duration,
                    _property,
                    step.CreateSegment(interpolator),
                    step.Easing ?? DefaultEasing,
                    i == 0 && _hasInitialValue,
                    _initialValue);
            }

            return Chain(animations);
        }

        private PropertyBuilder<T> AddStep(TimeSpan duration, Func<double, double>? easing, Func<TweenInterpolator<T>, TweenPropertySegment<T>> createSegment)
        {
            TweenTime.ThrowIfInvalidDuration(duration, nameof(duration));
            _steps.Add(new Step(duration, easing, createSegment));
            return this;
        }

        private sealed class Step
        {
            public Step(TimeSpan duration, Func<double, double>? easing, Func<TweenInterpolator<T>, TweenPropertySegment<T>> createSegment)
            {
                Duration = duration;
                Easing = easing;
                CreateSegment = createSegment;
            }

            public TimeSpan Duration { get; }

            public Func<double, double>? Easing { get; }

            public Func<TweenInterpolator<T>, TweenPropertySegment<T>> CreateSegment { get; }
        }
    }

    /// <summary>
    /// Plays animations one after the other.
    /// </summary>
    public sealed class SequenceBuilder : Builder
    {
        internal SequenceBuilder(Builder[] builders)
        {
            Builders = builders ?? throw new ArgumentNullException(nameof(builders));
        }

        /// <summary>
        /// Gets the chained builders.
        /// </summary>
        public Builder[] Builders { get; }

        /// <inheritdoc />
        protected override TimeSpan CoreDuration => Builders.Aggregate(TimeSpan.Zero, (total, b) => TweenTime.Add(total, b.Duration));

        /// <inheritdoc />
        protected override ITweenAnimation BuildCore()
        {
            var animation = new TweenSequenceAnimation();
            foreach (var sub in Builders)
                animation.Add(sub.Build());

            return animation;
        }
    }

    /// <summary>
    /// Plays animations at the same time.
    /// </summary>
    public sealed class ParallelBuilder : Builder
    {
        internal ParallelBuilder(Builder[] builders)
        {
            Builders = builders ?? throw new ArgumentNullException(nameof(builders));
        }

        /// <summary>
        /// Gets the grouped builders.
        /// </summary>
        public Builder[] Builders { get; }

        /// <inheritdoc />
        protected override TimeSpan CoreDuration => Builders.Aggregate(TimeSpan.Zero, (max, b) => TweenTime.Max(max, b.Duration));

        /// <inheritdoc />
        protected override ITweenAnimation BuildCore()
        {
            var animation = new TweenParallelAnimation();
            foreach (var sub in Builders)
                animation.Add(sub.Build());

            return animation;
        }
    }

    /// <summary>
    /// Places animations on a timeline. Positions are resolved when the timeline is built, from the durations of the builders.
    /// </summary>
    public sealed class TimelineBuilder : Builder
    {
        private readonly List<(Placement Placement, TimeSpan Time, Builder Builder)> _items = new();

        internal TimelineBuilder()
        {
        }

        /// <inheritdoc />
        protected override TimeSpan CoreDuration => Resolve().Duration;

        /// <summary>
        /// Adds an animation at the end of the timeline, shifted by <paramref name="offset"/>
        /// (a negative offset overlaps the previous animations).
        /// </summary>
        public TimelineBuilder Append(Builder builder, TimeSpan offset = default)
        {
            return Add(Placement.Append, offset, builder);
        }

        /// <summary>
        /// Adds an animation starting at the same time as the previously added one.
        /// </summary>
        public TimelineBuilder Join(Builder builder)
        {
            return Add(Placement.Join, TimeSpan.Zero, builder);
        }

        /// <summary>
        /// Adds an animation starting at <paramref name="time"/>.
        /// </summary>
        public TimelineBuilder Insert(TimeSpan time, Builder builder)
        {
            ThrowIfNegative(time, nameof(time));
            return Add(Placement.Insert, time, builder);
        }

        /// <summary>
        /// Adds a pause at the end of the timeline.
        /// </summary>
        public TimelineBuilder AppendInterval(TimeSpan duration) => Append(Delay(duration));

        /// <summary>
        /// Adds a callback at the end of the timeline.
        /// </summary>
        public TimelineBuilder AppendCallback(Action callback) => Append(Callback(callback));

        /// <summary>
        /// Adds a callback at <paramref name="time"/>.
        /// </summary>
        public TimelineBuilder InsertCallback(TimeSpan time, Action callback) => Insert(time, Callback(callback));

        /// <inheritdoc />
        protected override ITweenAnimation BuildCore()
        {
            var animation = new TweenTimelineAnimation();
            foreach (var (start, builder) in Resolve().Items)
                animation.Insert(start, builder.Build());

            return animation;
        }

        private TimelineBuilder Add(Placement placement, TimeSpan time, Builder builder)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            if (ReferenceEquals(builder, this))
                throw new ArgumentException("A timeline cannot contain itself", nameof(builder));

            _items.Add((placement, time, builder));
            return this;
        }

        private (TimeSpan Duration, List<(TimeSpan Start, Builder Builder)> Items) Resolve()
        {
            var items = new List<(TimeSpan Start, Builder Builder)>(_items.Count);
            var end = TimeSpan.Zero;
            var lastStart = TimeSpan.Zero;
            foreach (var (placement, time, builder) in _items)
            {
                var start = placement switch
                {
                    Placement.Append => TweenTime.Max(TimeSpan.Zero, TweenTime.Add(end, time)),
                    Placement.Join => lastStart,
                    _ => time,
                };

                items.Add((start, builder));
                lastStart = start;
                end = TweenTime.Max(end, TweenTime.Add(start, builder.Duration));
            }

            return (end, items);
        }

        private enum Placement
        {
            Append,
            Join,
            Insert,
        }
    }

    private static ITweenAnimation Chain(ITweenAnimation[] animations)
    {
        if (animations.Length == 1)
            return animations[0];

        var sequence = new TweenSequenceAnimation();
        foreach (var animation in animations)
            sequence.Add(animation);

        return sequence;
    }

    private sealed class IntervalBuilder : Builder
    {
        private readonly TimeSpan _duration;

        public IntervalBuilder(TimeSpan duration)
        {
            _duration = duration;
        }

        protected override TimeSpan CoreDuration => _duration;

        protected override ITweenAnimation BuildCore() => new TweenIntervalAnimation(_duration);
    }

    private sealed class CallbackBuilder : Builder
    {
        private readonly Action _callback;

        public CallbackBuilder(Action callback)
        {
            _callback = callback;
        }

        protected override TimeSpan CoreDuration => TimeSpan.Zero;

        protected override ITweenAnimation BuildCore() => new TweenCallbackAnimation(_callback);
    }
}
