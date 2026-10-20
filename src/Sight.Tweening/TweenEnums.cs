namespace Sight.Tweening;

/// <summary>
/// Predefined easings, convenient to store in configuration files. See <see cref="TweenEasing.Get"/>.
/// </summary>
public enum TweenEase
{
#pragma warning disable CS1591 // Names match the functions of TweenEasing
    Linear,
    InQuad, OutQuad, InOutQuad,
    InCubic, OutCubic, InOutCubic,
    InQuart, OutQuart, InOutQuart,
    InQuint, OutQuint, InOutQuint,
    InSine, OutSine, InOutSine,
    InExpo, OutExpo, InOutExpo,
    InCirc, OutCirc, InOutCirc,
    InElastic, OutElastic, InOutElastic,
    InBack, OutBack, InOutBack,
    InBounce, OutBounce, InOutBounce,
#pragma warning restore CS1591
}

/// <summary>
/// Position of the jumps of <see cref="TweenEasing.Steps"/>, like CSS <c>steps()</c>.
/// </summary>
public enum TweenStepPosition
{
    /// <summary>
    /// The last jump happens at the end.
    /// </summary>
    End,

    /// <summary>
    /// The first jump happens at the start.
    /// </summary>
    Start,

    /// <summary>
    /// No jump at the start nor at the end.
    /// </summary>
    None,

    /// <summary>
    /// Jumps at the start and at the end.
    /// </summary>
    Both,
}

/// <summary>
/// How an animation repeats.
/// </summary>
public enum TweenLoopType
{
    /// <summary>
    /// Each iteration restarts from the beginning.
    /// </summary>
    Restart,

    /// <summary>
    /// Iterations alternate forward and backward.
    /// </summary>
    Yoyo,
}

/// <summary>
/// How the waypoints of a path are joined.
/// </summary>
public enum TweenPathType
{
    /// <summary>
    /// Straight lines between the waypoints, each segment taking the same time.
    /// </summary>
    Linear,

    /// <summary>
    /// Smooth Catmull-Rom spline through all the waypoints (requires <see cref="ITweenArithmetic{T}"/>).
    /// </summary>
    CatmullRom,

    /// <summary>
    /// Single Bézier curve: intermediate waypoints are control points, the last one is the end.
    /// </summary>
    Bezier,
}

/// <summary>
/// Playback state of an <see cref="ITweenAnimationContext"/>.
/// </summary>
public enum TweenAnimationState
{
    /// <summary>
    /// Not started yet, or reset.
    /// </summary>
    Idle,

    /// <summary>
    /// Playing.
    /// </summary>
    Running,

    /// <summary>
    /// Started then stopped (or seeked) before the end.
    /// </summary>
    Paused,

    /// <summary>
    /// Reached its end (or its start when reversed).
    /// </summary>
    Completed,
}

/// <summary>
/// Color space in which colors are interpolated.
/// </summary>
public enum TweenColorSpace
{
    /// <summary>
    /// Gamma-encoded sRGB components: simple and fast, but the middle of a transition between saturated colors looks dull.
    /// </summary>
    Srgb,

    /// <summary>
    /// Linear-light RGB components: physically correct mix of lights.
    /// </summary>
    LinearRgb,

    /// <summary>
    /// Hue, saturation and lightness: goes around the color wheel, but the perceived lightness varies along the way.
    /// </summary>
    Hsl,

    /// <summary>
    /// Perceptually uniform Oklab space: smooth transitions without dull middle (default of CSS Color 4).
    /// </summary>
    Oklab,

    /// <summary>
    /// Polar form of Oklab (lightness, chroma, hue): goes around the color wheel with a constant perceived lightness.
    /// Colors outside of the sRGB gamut are mapped back by reducing their chroma.
    /// </summary>
    Oklch,
}

/// <summary>
/// Direction taken around the color wheel by hue interpolations (<see cref="TweenColorSpace.Hsl"/> and <see cref="TweenColorSpace.Oklch"/>), like CSS.
/// </summary>
public enum TweenHuePath
{
    /// <summary>
    /// The shortest arc (at most 180°).
    /// </summary>
    Shorter,

    /// <summary>
    /// The longest arc (at least 180°).
    /// </summary>
    Longer,

    /// <summary>
    /// Hue always increases.
    /// </summary>
    Increasing,

    /// <summary>
    /// Hue always decreases.
    /// </summary>
    Decreasing,
}
