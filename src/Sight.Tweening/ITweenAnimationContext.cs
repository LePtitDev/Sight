namespace Sight.Tweening;

/// <summary>
/// Plays an <see cref="ITweenAnimation"/> on a target and controls its playback.
/// </summary>
public interface ITweenAnimationContext : IDisposable
{
    /// <summary>
    /// Raised when the playback starts or resumes.
    /// </summary>
    public event EventHandler? Started;

    /// <summary>
    /// Raised when the playback is stopped (paused) or reset while running.
    /// </summary>
    public event EventHandler? Stopped;

    /// <summary>
    /// Raised after each rendering of the animation.
    /// </summary>
    public event EventHandler? Updated;

    /// <summary>
    /// Raised when the animation reaches its end (or its start when reversed).
    /// </summary>
    public event EventHandler? Completed;

    /// <summary>
    /// Gets the played animation.
    /// </summary>
    public ITweenAnimation Animation { get; }

    /// <summary>
    /// Gets the object the animation is applied to.
    /// </summary>
    public object? Target { get; }

    /// <summary>
    /// Gets the playback state.
    /// </summary>
    public TweenAnimationState State { get; }

    /// <summary>
    /// Gets whether the animation is playing.
    /// </summary>
    public bool IsRunning { get; }

    /// <summary>
    /// Gets the position of the playhead.
    /// </summary>
    public TimeSpan Time { get; }

    /// <summary>
    /// Gets the total duration of the animation (<see cref="TimeSpan.MaxValue"/> when infinite).
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Gets the normalized position of the playhead (0..1), always 0 for infinite animations.
    /// </summary>
    public double Progress { get; }

    /// <summary>
    /// Gets or sets the playback speed multiplier (1 by default).
    /// </summary>
    public double TimeScale { get; set; }

    /// <summary>
    /// Gets or sets whether the animation plays backward.
    /// </summary>
    public bool IsReversed { get; set; }

    /// <summary>
    /// Gets a task completing when the current playback completes. It is canceled when the context is reset or disposed.
    /// </summary>
    public Task Completion { get; }

    /// <summary>
    /// Starts or resumes the playback. A completed animation restarts from its beginning.
    /// </summary>
    public void Start();

    /// <summary>
    /// Stops (pauses) the playback, keeping the playhead position.
    /// </summary>
    public void Stop();

    /// <summary>
    /// Rewinds the animation to its start values and forgets them, so that the next start captures them again.
    /// </summary>
    public void Reset();

    /// <summary>
    /// Resets then starts the animation.
    /// </summary>
    public void Restart();

    /// <summary>
    /// Moves the playhead to <paramref name="time"/> and renders the animation, without completing it.
    /// </summary>
    public void Seek(TimeSpan time);

    /// <summary>
    /// Moves the playhead to a normalized position (0..1).
    /// </summary>
    public void SeekProgress(double progress);

    /// <summary>
    /// Jumps to the end (or the start when reversed) and completes the animation.
    /// </summary>
    public void Complete();

    /// <summary>
    /// Inverts the playback direction.
    /// </summary>
    public void Reverse();
}
