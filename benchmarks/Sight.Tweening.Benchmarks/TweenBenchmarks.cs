using System.Drawing;
using System.Numerics;
using BenchmarkDotNet.Attributes;
using Sight.Tweening;

namespace Sight.Tweening.Benchmarks;

/// <summary>
/// Cost of a frame update for common animation shapes.
/// </summary>
[MemoryDiagnoser]
public class TweenUpdateBenchmarks
{
    private const int FramesPerOperation = 1000;
    private static readonly TimeSpan s_frame = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60);

    private TweenManualAnimationContext _expression = null!;
    private TweenManualAnimationContext _delegate = null!;
    private TweenManualAnimationContext _reflection = null!;
    private TweenManualAnimationContext _sequence = null!;
    private TweenManualAnimationContext _vectorPath = null!;
    private TweenManualAnimationContext _colorSrgb = null!;
    private TweenManualAnimationContext _colorOklch = null!;

    [GlobalSetup]
    public void SetUp()
    {
        _expression = Play(Tween.Property<State, double>(s => s.X).To(100, TimeSpan.FromSeconds(1)).Loops(Tween.Infinite, TweenLoopType.Yoyo));
        _delegate = Play(Tween.Property<State, double>(s => s.X, (s, v) => s.X = v).To(100, TimeSpan.FromSeconds(1)).Loops(Tween.Infinite, TweenLoopType.Yoyo));
        _reflection = Play(Tween.Property(typeof(State), nameof(State.X)).To(100.0, TimeSpan.FromSeconds(1)).Loops(Tween.Infinite, TweenLoopType.Yoyo));
        _sequence = Play(Tween.Sequence(Enumerable.Range(0, 10)
                .Select(i => (Tween.Builder)Tween.Property<State, double>(s => s.X).To(i * 10, TimeSpan.FromSeconds(0.2), TweenEase.InOutCubic))
                .ToArray())
            .Loops(Tween.Infinite));
        _vectorPath = Play(Tween.Property<State, Vector3>(s => s.Position)
            .Path([new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 1), Vector3.Zero], TimeSpan.FromSeconds(2), TweenPathType.CatmullRom)
            .Loops(Tween.Infinite));
        _colorSrgb = Play(Tween.Property<State, Color>(s => s.Color)
            .To(Color.DodgerBlue, TimeSpan.FromSeconds(1))
            .Loops(Tween.Infinite, TweenLoopType.Yoyo));
        _colorOklch = Play(Tween.Property<State, Color>(s => s.Color)
            .To(Color.DodgerBlue, TimeSpan.FromSeconds(1))
            .InColorSpace(TweenColorSpace.Oklch)
            .Loops(Tween.Infinite, TweenLoopType.Yoyo));
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = FramesPerOperation)]
    public void ExpressionProperty() => Run(_expression);

    [Benchmark(OperationsPerInvoke = FramesPerOperation)]
    public void DelegateProperty() => Run(_delegate);

    [Benchmark(OperationsPerInvoke = FramesPerOperation)]
    public void ReflectionProperty() => Run(_reflection);

    [Benchmark(OperationsPerInvoke = FramesPerOperation)]
    public void SequenceOfTen() => Run(_sequence);

    [Benchmark(OperationsPerInvoke = FramesPerOperation)]
    public void Vector3CatmullRomPath() => Run(_vectorPath);

    [Benchmark(OperationsPerInvoke = FramesPerOperation)]
    public void ColorSrgb() => Run(_colorSrgb);

    [Benchmark(OperationsPerInvoke = FramesPerOperation)]
    public void ColorOklch() => Run(_colorOklch);

    private static TweenManualAnimationContext Play(Tween.Builder builder)
    {
        var context = builder.Build().CreateManualContext(new State());
        context.Start();
        return context;
    }

    private static void Run(TweenManualAnimationContext context)
    {
        for (var i = 0; i < FramesPerOperation; i++)
            context.Tick(s_frame);
    }

    public sealed class State
    {
        public double X { get; set; }

        public Vector3 Position { get; set; }

        public Color Color { get; set; } = Color.OrangeRed;
    }
}

/// <summary>
/// Cost of a frame of a runner playing many animations at once.
/// </summary>
[MemoryDiagnoser]
public class TweenRunnerBenchmarks
{
    private readonly TweenRunner _runner = new();

    [Params(100, 10_000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void SetUp()
    {
        var animation = Tween.Property<State, float>(s => s.Value)
            .To(1, TimeSpan.FromSeconds(1), TweenEase.OutQuad)
            .Loops(Tween.Infinite, TweenLoopType.Yoyo)
            .Build();

        for (var i = 0; i < Count; i++)
            _runner.Play(animation, new State());
    }

    [Benchmark]
    public void Tick() => _runner.Tick(1.0 / 60);

    public sealed class State
    {
        public float Value { get; set; }
    }
}

/// <summary>
/// Cost of building and starting an animation.
/// </summary>
[MemoryDiagnoser]
public class TweenSetupBenchmarks
{
    private readonly ITweenAnimation _animation = Tween.Sequence(
            Tween.Property<State, double>(s => s.X).To(10, TimeSpan.FromSeconds(1)),
            Tween.Property<State, double>(s => s.X).By(5, TimeSpan.FromSeconds(1)).Delay(TimeSpan.FromSeconds(0.5)))
        .Build();

    private readonly State _state = new();

    [Benchmark]
    public ITweenAnimation BuildExpression() => Tween.Property<State, double>(s => s.X).To(10, TimeSpan.FromSeconds(1)).Build();

    [Benchmark]
    public TweenManualAnimationContext StartPrebuilt()
    {
        var context = _animation.CreateManualContext(_state);
        context.Start();
        return context;
    }

    public sealed class State
    {
        public double X { get; set; }
    }
}
