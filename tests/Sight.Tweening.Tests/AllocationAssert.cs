namespace Sight.Tweening.Tests;

internal static class AllocationAssert
{
#if NET5_0_OR_GREATER
    /// <summary>
    /// Asserts that updating a running context eventually stops allocating.
    /// </summary>
    /// <remarks>
    /// The JIT may allocate a little while it tiers the code up: the assertion waits for a window of updates without any allocation.
    /// A real allocation per update would make every window allocate.
    /// </remarks>
    public static void SteadyStateDoesNotAllocate(TweenManualAnimationContext context)
    {
        var allocatingWindows = 0;
        for (var window = 0; window < 50; window++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 1_000; i++)
                context.Tick(0.016);

            if (GC.GetAllocatedBytesForCurrentThread() == before)
                break;

            allocatingWindows++;
            Thread.Sleep(10);
        }

        Assert.That(allocatingWindows, Is.LessThan(50), "Updates keep allocating");
    }
#endif
}
