namespace Sight.Tweening;

/// <summary>
/// Animation description. It holds no playback state: the state lives in the <see cref="TweenStateContext"/>,
/// so the same animation can be played by several contexts at once.
/// </summary>
public interface ITweenAnimation
{
    /// <summary>
    /// Gets the duration of the animation (<see cref="TimeSpan.MaxValue"/> when infinite).
    /// </summary>
    public TimeSpan Duration { get; }

    /// <summary>
    /// Initializes <paramref name="context"/> before the first update (captures the start values).
    /// </summary>
    public void SetUp(TweenStateContext context);

    /// <summary>
    /// Renders the animation at <see cref="TweenStateContext.Time"/>. The time can go backward,
    /// and can be outside of 0..<see cref="Duration"/> (the animation is then rendered at its start or end).
    /// </summary>
    public void Update(TweenStateContext context);
}
