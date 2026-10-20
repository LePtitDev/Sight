using Sight.Tweening.Internal;

namespace Sight.Tweening;

/// <summary>
/// Plays many animations from a single update loop (game loop, render callback...).
/// Completed animations are removed automatically.
/// </summary>
/// <remarks>This class is not thread-safe: use it from the thread calling <see cref="Tick(TimeSpan)"/>.</remarks>
public sealed class TweenRunner
{
    private readonly List<Entry> _entries = new();
    private bool _isTicking;

    /// <summary>
    /// Gets the number of animations managed by the runner.
    /// </summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Starts playing <paramref name="animation"/> on <paramref name="target"/>.
    /// </summary>
    /// <param name="animation">Animation to play.</param>
    /// <param name="target">Object the animation is applied to.</param>
    /// <param name="id">Optional identifier, used by <see cref="KillById"/>.</param>
    /// <returns>The context controlling the playback.</returns>
    public ITweenAnimationContext Play(ITweenAnimation animation, object? target = null, object? id = null)
    {
        var context = new TweenManualAnimationContext(animation, target);
        _entries.Add(new Entry(context, id));
        context.Start();
        return context;
    }

    /// <summary>
    /// Builds and starts playing <paramref name="builder"/> on <paramref name="target"/>.
    /// </summary>
    public ITweenAnimationContext Play(Tween.Builder builder, object? target = null, object? id = null)
    {
        if (builder is null)
            throw new ArgumentNullException(nameof(builder));

        return Play(builder.Build(), target, id);
    }

    /// <summary>
    /// Advances all the running animations by <paramref name="delta"/>.
    /// </summary>
    public void Tick(TimeSpan delta)
    {
        if (_isTicking)
            throw new InvalidOperationException("Tick cannot be called recursively");

        _isTicking = true;
        try
        {
            // Animations added by callbacks during this tick start on the next one
            var count = _entries.Count;
            for (var i = 0; i < count; i++)
            {
                var entry = _entries[i];
                if (!entry.IsKilled)
                    entry.Context.Tick(delta);
            }
        }
        finally
        {
            _isTicking = false;
            Cleanup();
        }
    }

    /// <summary>
    /// Advances all the running animations by <paramref name="deltaSeconds"/> seconds.
    /// </summary>
    public void Tick(double deltaSeconds) => Tick(TweenTime.Scale(TimeSpan.FromSeconds(1), deltaSeconds));

    /// <summary>
    /// Stops and removes the animations applied to <paramref name="target"/>.
    /// </summary>
    /// <param name="target">Target of the animations to remove.</param>
    /// <param name="complete">Whether to jump to the end of the animations before removing them.</param>
    /// <returns>The number of removed animations.</returns>
    public int Kill(object target, bool complete = false)
    {
        return Kill(e => Equals(e.Context.Target, target), complete);
    }

    /// <summary>
    /// Stops and removes the animations with the identifier <paramref name="id"/>.
    /// </summary>
    /// <param name="id">Identifier given to <see cref="Play(ITweenAnimation, object?, object?)"/>.</param>
    /// <param name="complete">Whether to jump to the end of the animations before removing them.</param>
    /// <returns>The number of removed animations.</returns>
    public int KillById(object id, bool complete = false)
    {
        return Kill(e => Equals(e.Id, id), complete);
    }

    /// <summary>
    /// Stops and removes all the animations.
    /// </summary>
    /// <param name="complete">Whether to jump to the end of the animations before removing them.</param>
    /// <returns>The number of removed animations.</returns>
    public int KillAll(bool complete = false)
    {
        return Kill(_ => true, complete);
    }

    /// <summary>
    /// Pauses all the animations.
    /// </summary>
    public void PauseAll()
    {
        foreach (var entry in _entries)
            entry.Context.Stop();
    }

    /// <summary>
    /// Resumes all the paused animations.
    /// </summary>
    public void ResumeAll()
    {
        foreach (var entry in _entries)
        {
            if (entry.Context.State == TweenAnimationState.Paused)
                entry.Context.Start();
        }
    }

    private int Kill(Func<Entry, bool> predicate, bool complete)
    {
        var count = 0;
        foreach (var entry in _entries)
        {
            if (entry.IsKilled || !predicate(entry))
                continue;

            if (complete && !TweenTime.IsInfinite(entry.Context.Duration))
                entry.Context.Complete();

            entry.IsKilled = true;
            entry.Context.Dispose();
            count++;
        }

        if (!_isTicking)
            Cleanup();

        return count;
    }

    private void Cleanup()
    {
        _entries.RemoveAll(static e =>
        {
            if (e.IsKilled)
                return true;

            if (e.Context.State is TweenAnimationState.Completed or TweenAnimationState.Idle)
            {
                e.Context.Dispose();
                return true;
            }

            return false;
        });
    }

    private sealed class Entry
    {
        public Entry(TweenManualAnimationContext context, object? id)
        {
            Context = context;
            Id = id;
        }

        public TweenManualAnimationContext Context { get; }

        public object? Id { get; }

        public bool IsKilled { get; set; }
    }
}
