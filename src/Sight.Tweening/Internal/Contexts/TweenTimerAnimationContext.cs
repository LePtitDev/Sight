using System.Diagnostics;

namespace Sight.Tweening.Internal.Contexts;

/// <summary>
/// Animation context updated periodically by a <see cref="Timer"/>.
/// </summary>
internal sealed class TweenTimerAnimationContext : TweenAnimationContext
{
    private readonly Timer _timer;
    private readonly Stopwatch _stopwatch = new();
    private readonly SynchronizationContext? _synchronizationContext;
    private TimeSpan _lastElapsed;
    private int _isTicking;

    public TweenTimerAnimationContext(ITweenAnimation animation, object? target, TimeSpan period, SynchronizationContext? synchronizationContext = null)
        : base(animation, target)
    {
        if (period <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(period), period, "Period must be positive");

        Period = period;
        _synchronizationContext = synchronizationContext;
        _timer = new Timer(static state => ((TweenTimerAnimationContext)state!).OnTimer(), this, Timeout.Infinite, Timeout.Infinite);
    }

    public TimeSpan Period { get; }

    protected override void OnStarted()
    {
        _lastElapsed = TimeSpan.Zero;
        _stopwatch.Restart();
        _timer.Change(Period, Period);
    }

    protected override void OnStopped()
    {
        _stopwatch.Stop();
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _timer.Dispose();
    }

    private void OnTimer()
    {
        // Skip the tick while the previous one is still being processed
        if (Interlocked.Exchange(ref _isTicking, 1) != 0)
            return;

        if (_synchronizationContext is null)
            Tick();
        else
            _synchronizationContext.Post(static state => ((TweenTimerAnimationContext)state!).Tick(), this);
    }

    private void Tick()
    {
        try
        {
            var elapsed = _stopwatch.Elapsed;
            var delta = elapsed - _lastElapsed;
            _lastElapsed = elapsed;
            Advance(delta);
        }
        finally
        {
            Volatile.Write(ref _isTicking, 0);
        }
    }
}
