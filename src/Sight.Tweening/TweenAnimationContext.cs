using Sight.Tweening.Internal;

namespace Sight.Tweening;

/// <summary>
/// Base class of animation contexts. Subclasses provide the clock and call <see cref="Advance"/>.
/// </summary>
/// <remarks>Public members are thread-safe; events and callbacks are raised on the thread updating the animation.</remarks>
public abstract class TweenAnimationContext : ITweenAnimationContext
{
    private readonly object _sync = new();
    private TweenStateContext? _stateContext;
    private TaskCompletionSource<bool> _completion = CreateCompletion();
    private TweenAnimationState _state;
    private TimeSpan _time;
    private double _timeScale = 1;
    private bool _isReversed;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a context playing <paramref name="animation"/> on <paramref name="target"/>.
    /// </summary>
    protected TweenAnimationContext(ITweenAnimation animation, object? target)
    {
        Animation = animation ?? throw new ArgumentNullException(nameof(animation));
        Target = target;
    }

    /// <inheritdoc />
    public event EventHandler? Started;

    /// <inheritdoc />
    public event EventHandler? Stopped;

    /// <inheritdoc />
    public event EventHandler? Updated;

    /// <inheritdoc />
    public event EventHandler? Completed;

    /// <inheritdoc />
    public ITweenAnimation Animation { get; }

    /// <inheritdoc />
    public object? Target { get; }

    /// <inheritdoc />
    public TweenAnimationState State
    {
        get
        {
            lock (_sync)
                return _state;
        }
    }

    /// <inheritdoc />
    public bool IsRunning => State == TweenAnimationState.Running;

    /// <inheritdoc />
    public TimeSpan Time
    {
        get
        {
            lock (_sync)
                return _time;
        }
    }

    /// <inheritdoc />
    public TimeSpan Duration => Animation.Duration;

    /// <inheritdoc />
    public double Progress
    {
        get
        {
            var duration = Duration;
            return TweenTime.IsInfinite(duration) ? 0 : TweenTime.Progress(Time, duration);
        }
    }

    /// <inheritdoc />
    public double TimeScale
    {
        get
        {
            lock (_sync)
                return _timeScale;
        }
        set
        {
            if (value < 0 || double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value), value, "Time scale must be positive and finite");

            lock (_sync)
                _timeScale = value;
        }
    }

    /// <inheritdoc />
    public bool IsReversed
    {
        get
        {
            lock (_sync)
                return _isReversed;
        }
        set
        {
            lock (_sync)
                _isReversed = value;
        }
    }

    /// <inheritdoc />
    public Task Completion
    {
        get
        {
            lock (_sync)
                return _completion.Task;
        }
    }

    /// <inheritdoc />
    public void Start()
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            if (_state == TweenAnimationState.Running)
                return;

            var restart = _state is TweenAnimationState.Idle or TweenAnimationState.Completed;
            if (_state == TweenAnimationState.Idle)
                SetUp();
            else if (_state == TweenAnimationState.Completed)
                _completion = CreateCompletion();

            _state = TweenAnimationState.Running;
            OnStarted();
            Started?.Invoke(this, EventArgs.Empty);

            if (restart)
                Render(_isReversed ? Duration : TimeSpan.Zero, true);
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        lock (_sync)
        {
            if (_state != TweenAnimationState.Running)
                return;

            _state = TweenAnimationState.Paused;
            OnStopped();
            Stopped?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        lock (_sync)
        {
            if (_state == TweenAnimationState.Idle)
                return;

            // Rendering at the start restores the values captured by the animation
            Render(TimeSpan.Zero, false);

            var wasRunning = _state == TweenAnimationState.Running;
            _stateContext = null;
            _time = TimeSpan.Zero;
            _state = TweenAnimationState.Idle;
            if (wasRunning)
            {
                OnStopped();
                Stopped?.Invoke(this, EventArgs.Empty);
            }

            _completion.TrySetCanceled();
            _completion = CreateCompletion();
            OnReset();
        }
    }

    /// <inheritdoc />
    public void Restart()
    {
        lock (_sync)
        {
            Reset();
            Start();
        }
    }

    /// <inheritdoc />
    public void Seek(TimeSpan time)
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            if (_state == TweenAnimationState.Idle)
            {
                SetUp();
                _state = TweenAnimationState.Paused;
            }
            else if (_state == TweenAnimationState.Completed)
            {
                _state = TweenAnimationState.Paused;
                _completion = CreateCompletion();
            }

            Render(time, false);
        }
    }

    /// <inheritdoc />
    public void SeekProgress(double progress)
    {
        if (double.IsNaN(progress))
            throw new ArgumentOutOfRangeException(nameof(progress), progress, "Progress must be a number");

        var duration = Duration;
        if (TweenTime.IsInfinite(duration))
            throw new InvalidOperationException("Cannot seek a progress in an infinite animation");

        Seek(TweenTime.Scale(duration, Math.Clamp(progress, 0, 1)));
    }

    /// <inheritdoc />
    public void Complete()
    {
        lock (_sync)
        {
            ThrowIfDisposed();
            if (TweenTime.IsInfinite(Duration))
                throw new InvalidOperationException("Cannot complete an infinite animation");

            if (_state == TweenAnimationState.Completed)
                return;

            if (_state == TweenAnimationState.Idle)
                SetUp();

            var wasRunning = _state == TweenAnimationState.Running;
            Render(_isReversed ? TimeSpan.Zero : Duration, false);
            SetCompleted(wasRunning);
        }
    }

    /// <inheritdoc />
    public void Reverse()
    {
        lock (_sync)
            _isReversed = !_isReversed;
    }

    /// <summary>
    /// Stops the playback and cancels <see cref="Completion"/>. Animated values are left as they are.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Moves the playhead by <paramref name="delta"/> (scaled by <see cref="TimeScale"/>, in the playback direction)
    /// when the animation is running, and completes it when it reaches its end.
    /// </summary>
    protected void Advance(TimeSpan delta)
    {
        lock (_sync)
        {
            if (_state != TweenAnimationState.Running)
                return;

            var scaled = TweenTime.Scale(delta, _timeScale);
            Render(_isReversed ? TweenTime.Subtract(_time, scaled) : TweenTime.Add(_time, scaled), true);
        }
    }

    /// <summary>
    /// Moves the playhead to the absolute <paramref name="time"/>. When running, the animation completes when it reaches its end.
    /// </summary>
    protected void Update(TimeSpan time)
    {
        lock (_sync)
        {
            if (_stateContext is null)
            {
                SetUp();
                _state = TweenAnimationState.Paused;
            }

            Render(time, _state == TweenAnimationState.Running);
        }
    }

    /// <summary>
    /// Called when the playback starts or resumes.
    /// </summary>
    protected virtual void OnStarted()
    {
    }

    /// <summary>
    /// Called when the playback stops, either paused, completed or reset.
    /// </summary>
    protected virtual void OnStopped()
    {
    }

    /// <summary>
    /// Called when the context is reset.
    /// </summary>
    protected virtual void OnReset()
    {
    }

    /// <summary>
    /// Releases the resources of the context.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        lock (_sync)
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            if (_state == TweenAnimationState.Running)
            {
                _state = TweenAnimationState.Paused;
                OnStopped();
            }

            _completion.TrySetCanceled();
        }
    }

    private void SetUp()
    {
        _stateContext = new TweenStateContext(Target);
        _time = TimeSpan.Zero;
        Animation.SetUp(_stateContext);
    }

    private void Render(TimeSpan time, bool canComplete)
    {
        var duration = Duration;
        _time = TweenTime.Clamp(time, TimeSpan.Zero, duration);
        _stateContext!.Time = _time;
        Animation.Update(_stateContext);
        Updated?.Invoke(this, EventArgs.Empty);

        if (!canComplete || _state != TweenAnimationState.Running)
            return;

        var isEnded = _isReversed ? _time <= TimeSpan.Zero : !TweenTime.IsInfinite(duration) && _time >= duration;
        if (isEnded)
            SetCompleted(true);
    }

    private void SetCompleted(bool wasRunning)
    {
        _state = TweenAnimationState.Completed;
        if (wasRunning)
            OnStopped();

        Completed?.Invoke(this, EventArgs.Empty);
        _completion.TrySetResult(true);
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(GetType().Name);
    }

    private static TaskCompletionSource<bool> CreateCompletion() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}

/// <summary>
/// Animation context driven manually by calling <see cref="Tick(TimeSpan)"/>, typically from a game or render loop.
/// </summary>
public sealed class TweenManualAnimationContext : TweenAnimationContext
{
    /// <summary>
    /// Initializes a context playing <paramref name="animation"/> on <paramref name="target"/>.
    /// </summary>
    public TweenManualAnimationContext(ITweenAnimation animation, object? target)
        : base(animation, target)
    {
    }

    /// <summary>
    /// Advances the animation by <paramref name="delta"/> when it is running.
    /// </summary>
    public void Tick(TimeSpan delta) => Advance(delta);

    /// <summary>
    /// Advances the animation by <paramref name="deltaSeconds"/> seconds when it is running.
    /// </summary>
    public void Tick(double deltaSeconds) => Advance(TweenTime.Scale(TimeSpan.FromSeconds(1), deltaSeconds));
}
