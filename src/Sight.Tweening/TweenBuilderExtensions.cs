using System.Drawing;

namespace Sight.Tweening;

/// <summary>
/// Options common to all the builders. They return the builder itself to keep chaining its specific methods.
/// </summary>
public static class TweenBuilderExtensions
{
    /// <summary>
    /// Waits for <paramref name="delay"/> before starting the animation (applied once, not on each loop).
    /// </summary>
    public static TBuilder Delay<TBuilder>(this TBuilder builder, TimeSpan delay)
        where TBuilder : Tween.Builder
    {
        builder.SetDelay(delay);
        return builder;
    }

    /// <summary>
    /// Repeats the animation <paramref name="count"/> times (<see cref="Tween.Infinite"/> to repeat forever).
    /// </summary>
    public static TBuilder Loops<TBuilder>(this TBuilder builder, int count, TweenLoopType type = TweenLoopType.Restart)
        where TBuilder : Tween.Builder
    {
        builder.SetLoops(count, type);
        return builder;
    }

    /// <summary>
    /// Plays the animation on <paramref name="target"/> instead of the target of its parent or context.
    /// </summary>
    public static TBuilder WithTarget<TBuilder>(this TBuilder builder, object? target)
        where TBuilder : Tween.Builder
    {
        builder.SetTarget(target);
        return builder;
    }

    /// <summary>
    /// Invokes <paramref name="callback"/> when the animation starts (after its delay).
    /// </summary>
    public static TBuilder OnStart<TBuilder>(this TBuilder builder, Action callback)
        where TBuilder : Tween.Builder
    {
        builder.AddOnStart(callback);
        return builder;
    }

    /// <summary>
    /// Invokes <paramref name="callback"/> each time the animation is rendered.
    /// </summary>
    public static TBuilder OnUpdate<TBuilder>(this TBuilder builder, Action callback)
        where TBuilder : Tween.Builder
    {
        builder.AddOnUpdate(callback);
        return builder;
    }

    /// <summary>
    /// Invokes <paramref name="callback"/> when the animation reaches its end (after all its loops).
    /// </summary>
    public static TBuilder OnComplete<TBuilder>(this TBuilder builder, Action callback)
        where TBuilder : Tween.Builder
    {
        builder.AddOnComplete(callback);
        return builder;
    }

    /// <summary>
    /// Invokes <paramref name="callback"/> with the iteration index each time the animation enters a new loop iteration.
    /// </summary>
    public static TBuilder OnLoop<TBuilder>(this TBuilder builder, Action<int> callback)
        where TBuilder : Tween.Builder
    {
        builder.AddOnLoop(callback);
        return builder;
    }

    /// <summary>
    /// Interpolates the colors of this property in <paramref name="space"/>.
    /// </summary>
    /// <param name="builder">Builder of a color property.</param>
    /// <param name="space">Color space in which colors are interpolated.</param>
    /// <param name="huePath">Direction around the color wheel, for <see cref="TweenColorSpace.Hsl"/> and <see cref="TweenColorSpace.Oklch"/>.</param>
    public static Tween.PropertyBuilder<Color> InColorSpace(this Tween.PropertyBuilder<Color> builder, TweenColorSpace space, TweenHuePath huePath = TweenHuePath.Shorter)
    {
        if (builder is null)
            throw new ArgumentNullException(nameof(builder));

        return builder.WithInterpolator(TweenInterpolator.Color(space, huePath));
    }
}
