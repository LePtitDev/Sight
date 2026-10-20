namespace Sight.Tweening;

/// <summary>
/// Untyped description of the start and end values of a property transition.
/// </summary>
public interface ITweenTransition
{
    /// <summary>
    /// Gets the start value, or <see langword="null"/> to start from the current value.
    /// </summary>
    public object? GetSourceValue();

    /// <summary>
    /// Gets the end value from the start value.
    /// </summary>
    public object? GetTargetValue(object? initialValue);
}
