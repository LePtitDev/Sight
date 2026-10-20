namespace Sight.Tweening.Internal;

/// <summary>
/// Saturating time arithmetic. <see cref="TimeSpan.MaxValue"/> stands for an infinite duration.
/// </summary>
internal static class TweenTime
{
    public static readonly TimeSpan Infinite = TimeSpan.MaxValue;

    public static bool IsInfinite(TimeSpan time) => time == TimeSpan.MaxValue;

    public static TimeSpan Add(TimeSpan left, TimeSpan right)
    {
        if (IsInfinite(left) || IsInfinite(right))
            return Infinite;

        var result = left.Ticks + right.Ticks;
        if (right.Ticks > 0 && result < left.Ticks)
            return TimeSpan.MaxValue;

        if (right.Ticks < 0 && result > left.Ticks)
            return TimeSpan.MinValue;

        return new TimeSpan(result);
    }

    public static TimeSpan Subtract(TimeSpan left, TimeSpan right)
    {
        if (IsInfinite(left))
            return Infinite;

        if (right == TimeSpan.MinValue)
            return TimeSpan.MaxValue;

        return Add(left, new TimeSpan(-right.Ticks));
    }

    public static TimeSpan Multiply(TimeSpan time, int count)
    {
        if (time <= TimeSpan.Zero)
            return TimeSpan.Zero;

        if (count < 0 || IsInfinite(time))
            return Infinite;

        var result = (decimal)time.Ticks * count;
        return result >= long.MaxValue ? Infinite : new TimeSpan((long)result);
    }

    public static TimeSpan Scale(TimeSpan time, double factor)
    {
        var result = time.Ticks * factor;
        if (result >= long.MaxValue)
            return TimeSpan.MaxValue;

        if (result <= long.MinValue)
            return TimeSpan.MinValue;

        return new TimeSpan((long)result);
    }

    public static TimeSpan Max(TimeSpan left, TimeSpan right) => left >= right ? left : right;

    public static TimeSpan Clamp(TimeSpan time, TimeSpan min, TimeSpan max)
    {
        if (time < min)
            return min;

        return time > max ? max : time;
    }

    /// <summary>
    /// Gets the normalized progress (0..1) of <paramref name="time"/> within <paramref name="duration"/>.
    /// A zero duration jumps directly from 0 to 1 when the time reaches 0.
    /// </summary>
    public static double Progress(TimeSpan time, TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
            return time >= TimeSpan.Zero ? 1 : 0;

        if (time <= TimeSpan.Zero)
            return 0;

        if (time >= duration)
            return 1;

        return (double)time.Ticks / duration.Ticks;
    }

    public static void ThrowIfInvalidDuration(TimeSpan duration, string paramName)
    {
        if (duration < TimeSpan.Zero || IsInfinite(duration))
            throw new ArgumentOutOfRangeException(paramName, duration, "Duration must be positive and finite");
    }
}
