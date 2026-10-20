# Sight.Tweening

[![](https://img.shields.io/nuget/v/Sight.Tweening.svg)](https://www.nuget.org/packages/Sight.Tweening/)

Standalone C# library to make animations with tweens.

- Targets .NET 8, 9, 10 and .NET Standard 2.1 (Unity, Godot, Xamarin...), no dependency
- Typed animations without boxing nor allocation per frame, trimming and native AOT compatible
- Sequences, parallel groups, timelines, stagger, delays, loops and yoyo
- Full playback control: pause, seek, reverse, time scale, events and `await`
- Relative tweens, paths (linear, Catmull-Rom, Bézier), shake and punch effects
- 30 standard easings, plus cubic Bézier, steps, spring and custom ones
- Driven by a timer, by your own game loop, by a runner managing many animations, or with `async`/`await`

## Getting started

```csharp
public class Sprite
{
    public double X { get; set; }
    public float Opacity { get; set; }
}

var animation = Tween
    .Sequence(
        Tween.Property<Sprite, double>(s => s.X)
            .To(24, TimeSpan.FromSeconds(3))
            .To(12, TimeSpan.FromSeconds(3), TweenEase.OutBounce),
        Tween.Property<Sprite, float>(s => s.Opacity)
            .To(0, TimeSpan.FromSeconds(1)))
    .Build();

var sprite = new Sprite { X = 8, Opacity = 1 };
await animation.PlayAsync(sprite);
```

A built animation holds no state: the same `ITweenAnimation` can be played on several targets at once.

## Animating values

Properties are described by an expression, by delegates, or by name (reflection):

```csharp
Tween.Property<Sprite, double>(s => s.X);                           // expression (members can be nested: s => s.Transform.X)
Tween.Property<Sprite, double>(s => s.X, (s, v) => s.X = v);        // typed delegates
Tween.Property(() => volume, v => volume = v);                      // closure, ignores the target
Tween.Property(typeof(Sprite), nameof(Sprite.X));                   // reflection (values are boxed)
```

Transitions are chained, each one starting from the value left by the previous one:

```csharp
Tween
    .Property<Sprite, double>(s => s.X)
    .From(0)                                       // start value of the first transition (current value otherwise)
    .To(100, TimeSpan.FromSeconds(1))              // absolute
    .By(-20, TimeSpan.FromSeconds(1))              // relative to the current value
    .Ease(TweenEase.InOutSine);                    // default easing of the transitions

Tween
    .Property<Sprite, float>(s => s.Opacity)
    .From(0, TimeSpan.FromSeconds(1));             // from 0 to the current value
```

### Paths, shake and punch

```csharp
Tween
    .Property<Ship, Vector2>(s => s.Position)
    .Path([new(100, 0), new(100, 100), new(0, 100)], TimeSpan.FromSeconds(3), TweenPathType.CatmullRom);

Tween
    .Property<Camera, Vector3>(c => c.Position)
    .Shake(new Vector3(0.5f, 0.5f, 0), TimeSpan.FromSeconds(0.4), vibrato: 15);

Tween
    .Property<Button, Vector2>(b => b.Scale)
    .Punch(new Vector2(0.2f), TimeSpan.FromSeconds(0.5), vibrato: 6, elasticity: 0.5);
```

`TweenPathType.Bezier` treats the intermediate waypoints as control points of a single curve.

### Supported types

`bool`, all integral types (rounded), `float`, `double`, `decimal`, `Half`, `Int128`, `UInt128`, `TimeSpan`, `DateTime`,
`DateTimeOffset`, `Vector2`, `Vector3`, `Vector4`, `Quaternion` (slerp), `System.Drawing.Color`, `string` (typewriter effect),
and their nullable versions.

Other types can implement `ITweenInterpolable<T>`, or get their own interpolator:

```csharp
TweenInterpolator.Register(TweenInterpolator.Create<Size>((t, a, b) =>
    new Size((int)(a.Width + (b.Width - a.Width) * t), (int)(a.Height + (b.Height - a.Height) * t))));

TweenInterpolator.Register(TweenInterpolator.Number<MyFixedPoint>()); // any INumberBase<T> (.NET 7+)
```

Relative tweens, shakes, punches and Catmull-Rom paths need the interpolator to implement `ITweenArithmetic<T>`
(built-in for numbers, `TimeSpan` and vectors).

### Colors

Colors can be interpolated in several color spaces. The default is sRGB, the simplest one; Oklab and Oklch give more natural transitions:

```csharp
Tween.Property<Button, Color>(b => b.Background)
    .To(Color.DodgerBlue, TimeSpan.FromSeconds(1))
    .InColorSpace(TweenColorSpace.Oklch, TweenHuePath.Shorter);
```

| Space | Use |
|---|---|
| `Srgb` | Simple and fast, but the middle of a transition between saturated colors looks dull |
| `LinearRgb` | Physically correct mix of lights |
| `Hsl` | Goes around the color wheel (rainbows), but the perceived lightness varies |
| `Oklab` | Perceptually uniform, smooth transitions (default of CSS Color 4) |
| `Oklch` | Goes around the color wheel with a constant perceived lightness; out of gamut colors are mapped back |

Like CSS, alpha is premultiplied (a transparent color does not darken the transition), grays take the hue of the other color,
and `TweenHuePath` chooses the direction around the color wheel (`Shorter`, `Longer`, `Increasing`, `Decreasing`).

Any color type (Avalonia, WPF, MAUI, Unity, Godot, SkiaSharp...) can use these color spaces, from conversions to normalized sRGB components:

```csharp
TweenInterpolator.Register(TweenInterpolator.Color<Avalonia.Media.Color>(
    c => new Vector4(c.R, c.G, c.B, c.A) / 255,
    v => Avalonia.Media.Color.FromArgb((byte)(v.W * 255), (byte)(v.X * 255), (byte)(v.Y * 255), (byte)(v.Z * 255)),
    TweenColorSpace.Oklab));
```

## Easing

```csharp
.To(1, duration, TweenEase.OutBack)                       // predefined (serializable enum)
.To(1, duration, TweenEasing.OutBack)                     // same, as a function
.To(1, duration, TweenEasing.CubicBezier(0.25, 0.1, 0.25, 1))
.To(1, duration, TweenEasing.Steps(4, TweenStepPosition.End))
.To(1, duration, TweenEasing.Spring(stiffness: 200, damping: 8))
.To(1, duration, TweenEasing.Out(TweenEasing.Elastic(amplitude: 1.2, period: 0.4)))
.To(1, duration, t => t * t)                              // custom
```

`Power`, `Back` and `Elastic` create "in" easings; `Out`, `InOut` and `OutIn` derive the other variants of any easing.

## Composition

```csharp
Tween.Sequence(a, b, c);                                  // one after the other
Tween.Parallel(a, b, c);                                  // at the same time

Tween
    .Timeline()
    .Append(slideIn)
    .Join(fadeIn)                                         // same start as slideIn
    .Append(bounce, TimeSpan.FromSeconds(-0.2))           // overlaps the end of slideIn
    .AppendInterval(TimeSpan.FromSeconds(1))
    .AppendCallback(() => Console.WriteLine("done"))
    .Insert(TimeSpan.FromSeconds(0.5), sparkle);          // absolute position

Tween.Stagger(cards, TimeSpan.FromMilliseconds(50),       // one animation per target
    card => Tween.Property<Card, float>(c => c.Opacity).From(0, TimeSpan.FromSeconds(0.3)));
```

Every builder accepts common options:

```csharp
builder
    .Delay(TimeSpan.FromSeconds(1))
    .Loops(3, TweenLoopType.Yoyo)                         // Tween.Infinite to repeat forever
    .WithTarget(otherObject)                              // animate another object than the context target
    .OnStart(() => ...)
    .OnUpdate(() => ...)
    .OnLoop(iteration => ...)
    .OnComplete(() => ...);
```

## Playing animations

### Playback control

Every context exposes the same controls:

```csharp
using var context = animation.CreateTimerContext(sprite, TimeSpan.FromMilliseconds(16));
context.Completed += (_, _) => Console.WriteLine("completed");

context.Start();                       // starts or resumes
context.Stop();                        // pauses
context.TimeScale = 0.5;               // slow motion
context.Reverse();                     // plays backward
context.Seek(TimeSpan.FromSeconds(2)); // or SeekProgress(0.5)
context.Complete();                    // jumps to the end
context.Reset();                       // restores the start values

await context.Completion;
```

Seeking works in both directions, also inside sequences and loops: values are restored as the playhead moves back.

### Timer

`CreateTimerContext` updates the animation from a thread pool timer. Pass a `SynchronizationContext` to update a UI from its own thread:

```csharp
animation.CreateTimerContext(control, TimeSpan.FromMilliseconds(16), SynchronizationContext.Current);
```

### Async

```csharp
await animation.PlayAsync(sprite, cancellationToken);                 // 60 FPS
await animation.PlayAsync(sprite, TimeSpan.FromMilliseconds(33));     // 30 FPS
```

Updates run on the captured synchronization context (UI thread), if any.

### Game loop

Use a manual context, or a `TweenRunner` to update many animations at once:

```csharp
var runner = new TweenRunner();
runner.Play(animation, sprite, id: "intro");

// in the update loop
runner.Tick(deltaTime);

runner.Kill(sprite);                   // stops the animations of a target
runner.KillById("intro", complete: true);
```

Completed animations are removed from the runner automatically.

## Performance

Typed property animations (expressions or delegates) do not box values nor allocate during updates.
The `Sight.Tweening.Benchmarks` project measures the cost per frame:

```bash
dotnet run -c Release --project benchmarks/Sight.Tweening.Benchmarks -- --filter "*"
```

With native AOT, expressions are interpreted: prefer the delegate overloads of `Tween.Property` in that case.
