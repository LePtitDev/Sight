namespace Sight.Tweening;

/// <summary>
/// Playback state of an <see cref="ITweenAnimation"/>: its target, its current time and the values it captured.
/// </summary>
public class TweenStateContext
{
    /// <summary>
    /// Initializes a state applied to <paramref name="target"/>.
    /// </summary>
    public TweenStateContext(object? target)
    {
        Target = target;
    }

    /// <summary>
    /// Gets the object the animation is applied to.
    /// </summary>
    public object? Target { get; }

    /// <summary>
    /// Gets or sets the time at which the animation is rendered.
    /// </summary>
    public TimeSpan Time { get; set; }

    /// <summary>
    /// Gets the state stored by the animation when it was set up (start value, or any animation-specific state).
    /// </summary>
    public object? InitialState { get; private set; }

    /// <summary>
    /// Gets the end value stored by the animation when it was set up.
    /// </summary>
    public object? TransitionTarget { get; private set; }

    /// <summary>
    /// Gets whether the animation was set up.
    /// </summary>
    public bool Initialized { get; private set; }

    /// <summary>
    /// Stores the state of the animation; called by <see cref="ITweenAnimation.SetUp"/>.
    /// </summary>
    public void Initialize(object? initialState, object? transitionTarget)
    {
        InitialState = initialState;
        TransitionTarget = transitionTarget;
        Initialized = true;
    }
}
