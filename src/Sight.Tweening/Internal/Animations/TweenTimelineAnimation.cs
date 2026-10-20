namespace Sight.Tweening.Internal.Animations;

/// <summary>
/// Plays child animations at given start times. Children are set up lazily, when the playhead first reaches them,
/// so that they capture the values left by the previous children.
/// Moving forward renders children in start order; moving backward renders them in reverse order so the earliest
/// children restore the values they captured.
/// </summary>
internal class TweenTimelineAnimation : ITweenAnimation
{
    private readonly List<Entry> _entries = new();
    private readonly List<ITweenAnimation> _animations = new();

    public TweenTimelineAnimation()
    {
        Animations = _animations.AsReadOnly();
    }

    public TimeSpan Duration { get; private set; }

    public IReadOnlyList<ITweenAnimation> Animations { get; }

    public void Insert(TimeSpan start, ITweenAnimation animation)
    {
        if (start < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(start), start, "Start time must be positive");

        if (animation is null)
            throw new ArgumentNullException(nameof(animation));

        var index = _entries.Count;
        while (index > 0 && _entries[index - 1].Start > start)
            index--;

        _entries.Insert(index, new Entry(start, animation));
        _animations.Insert(index, animation);
        Duration = TweenTime.Max(Duration, TweenTime.Add(start, animation.Duration));
    }

    public void SetUp(TweenStateContext context)
    {
        var children = new ChildState[_entries.Count];
        for (var i = 0; i < children.Length; i++)
            children[i].Context = new TweenStateContext(context.Target);

        context.Initialize(new TimelineState(children), null);
    }

    public void Update(TweenStateContext context)
    {
        if (context.InitialState is not TimelineState state)
            throw new InvalidOperationException("Animation must be set up");

        var time = context.Time;
        var children = state.Children;
        if (!state.HasRendered || time >= state.LastTime)
        {
            for (var i = 0; i < children.Length; i++)
                RenderChild(ref children[i], _entries[i], time);
        }
        else
        {
            for (var i = children.Length - 1; i >= 0; i--)
                RenderChild(ref children[i], _entries[i], time);
        }

        state.LastTime = time;
        state.HasRendered = true;
    }

    private static void RenderChild(ref ChildState child, Entry entry, TimeSpan time)
    {
        var local = TweenTime.Subtract(time, entry.Start);
        if (!child.IsSetUp)
        {
            if (local < TimeSpan.Zero)
                return;

            entry.Animation.SetUp(child.Context);
            child.IsSetUp = true;
        }

        var phase = local < TimeSpan.Zero ? Phase.Before : local >= entry.Animation.Duration ? Phase.After : Phase.Active;
        if (child.HasRendered && child.LastPhase == phase && (phase != Phase.Active || child.LastLocal == local))
            return;

        child.Context.Time = local;
        entry.Animation.Update(child.Context);
        child.HasRendered = true;
        child.LastPhase = phase;
        child.LastLocal = local;
    }

    private readonly struct Entry
    {
        public Entry(TimeSpan start, ITweenAnimation animation)
        {
            Start = start;
            Animation = animation;
        }

        public TimeSpan Start { get; }

        public ITweenAnimation Animation { get; }
    }

    private enum Phase
    {
        Before,
        Active,
        After,
    }

    private struct ChildState
    {
        public TweenStateContext Context;
        public bool IsSetUp;
        public bool HasRendered;
        public Phase LastPhase;
        public TimeSpan LastLocal;
    }

    private sealed class TimelineState
    {
        public TimelineState(ChildState[] children)
        {
            Children = children;
        }

        public ChildState[] Children { get; }

        public bool HasRendered { get; set; }

        public TimeSpan LastTime { get; set; }
    }
}

internal sealed class TweenSequenceAnimation : TweenTimelineAnimation
{
    public void Add(ITweenAnimation animation) => Insert(Duration, animation);
}

internal sealed class TweenParallelAnimation : TweenTimelineAnimation
{
    public void Add(ITweenAnimation animation) => Insert(TimeSpan.Zero, animation);
}
