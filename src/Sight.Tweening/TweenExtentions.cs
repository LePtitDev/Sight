using System.Diagnostics;
using Sight.Tweening.Internal.Contexts;

namespace Sight.Tweening;

/// <summary>
/// Extensions to play animations.
/// </summary>
public static class TweenExtentions
{
    /// <summary>
    /// Default interval between two frames of <see cref="PlayAsync(ITweenAnimation, object?, CancellationToken)"/> (60 FPS).
    /// </summary>
    public static readonly TimeSpan DefaultFrameInterval = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60);

    /// <summary>
    /// Creates a context updating the animation periodically on a thread pool timer.
    /// </summary>
    /// <param name="animation">Animation to play.</param>
    /// <param name="target">Object the animation is applied to.</param>
    /// <param name="period">Update period, in milliseconds.</param>
    public static ITweenAnimationContext CreateTimerContext(this ITweenAnimation animation, object? target, int period)
    {
        return new TweenTimerAnimationContext(animation, target, TimeSpan.FromMilliseconds(period));
    }

    /// <summary>
    /// Creates a context updating the animation periodically on a thread pool timer.
    /// </summary>
    /// <param name="animation">Animation to play.</param>
    /// <param name="target">Object the animation is applied to.</param>
    /// <param name="period">Update period.</param>
    public static ITweenAnimationContext CreateTimerContext(this ITweenAnimation animation, object? target, TimeSpan period)
    {
        return new TweenTimerAnimationContext(animation, target, period);
    }

    /// <summary>
    /// Creates a context updating the animation periodically, the updates being posted to <paramref name="synchronizationContext"/>
    /// (for instance to update a UI from its own thread).
    /// </summary>
    /// <param name="animation">Animation to play.</param>
    /// <param name="target">Object the animation is applied to.</param>
    /// <param name="period">Update period.</param>
    /// <param name="synchronizationContext">Context the updates are posted to, usually <see cref="SynchronizationContext.Current"/> on a UI thread.</param>
    public static ITweenAnimationContext CreateTimerContext(this ITweenAnimation animation, object? target, TimeSpan period, SynchronizationContext? synchronizationContext)
    {
        return new TweenTimerAnimationContext(animation, target, period, synchronizationContext);
    }

    /// <summary>
    /// Creates a context updated manually with <see cref="TweenManualAnimationContext.Tick(TimeSpan)"/>.
    /// </summary>
    public static TweenManualAnimationContext CreateManualContext(this ITweenAnimation animation, object? target)
    {
        return new TweenManualAnimationContext(animation, target);
    }

    /// <summary>
    /// Plays the animation at 60 frames per second until it completes.
    /// </summary>
    /// <remarks>Updates run on the captured synchronization context, if any (UI thread).</remarks>
    /// <exception cref="OperationCanceledException">The playback was canceled; animated values are left as they are.</exception>
    public static Task PlayAsync(this ITweenAnimation animation, object? target, CancellationToken cancellationToken = default)
    {
        return PlayAsync(animation, target, DefaultFrameInterval, cancellationToken);
    }

    /// <summary>
    /// Plays the animation until it completes, updating it every <paramref name="frameInterval"/>.
    /// </summary>
    /// <remarks>Updates run on the captured synchronization context, if any (UI thread).</remarks>
    /// <exception cref="OperationCanceledException">The playback was canceled; animated values are left as they are.</exception>
    public static async Task PlayAsync(this ITweenAnimation animation, object? target, TimeSpan frameInterval, CancellationToken cancellationToken = default)
    {
        if (animation is null)
            throw new ArgumentNullException(nameof(animation));

        if (frameInterval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(frameInterval), frameInterval, "Frame interval must be positive");

        cancellationToken.ThrowIfCancellationRequested();

        using var context = new TweenManualAnimationContext(animation, target);
        context.Start();

        var stopwatch = Stopwatch.StartNew();
        var lastElapsed = TimeSpan.Zero;
#if NET6_0_OR_GREATER
        using var timer = new PeriodicTimer(frameInterval);
#endif
        while (context.State == TweenAnimationState.Running)
        {
#if NET6_0_OR_GREATER
            await timer.WaitForNextTickAsync(cancellationToken);
#else
            await Task.Delay(frameInterval, cancellationToken);
#endif
            var elapsed = stopwatch.Elapsed;
            context.Tick(elapsed - lastElapsed);
            lastElapsed = elapsed;
        }
    }
}
