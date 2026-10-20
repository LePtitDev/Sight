using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Sight.Tweening.Internal.Interpolators;

namespace Sight.Tweening;

/// <summary>
/// Base class of untyped interpolators, and registry of the default interpolators.
/// </summary>
public abstract class TweenInterpolator : ITweenInterpolator
{
    private static readonly ConcurrentDictionary<Type, ITweenInterpolator> s_interpolators = CreateBuiltInInterpolators();

    /// <inheritdoc />
    public abstract object? Interpolate(double delta, object? begin, object? end);

    /// <summary>
    /// Gets the default interpolator of <paramref name="type"/>.
    /// </summary>
    /// <remarks>
    /// Built-in and registered types are resolved without reflection. Types implementing
    /// <see cref="ITweenInterpolable{T}"/> that were never used through <see cref="TweenInterpolator{T}.Default"/>
    /// need dynamic code, which is not available with native AOT.
    /// </remarks>
    /// <exception cref="NotSupportedException">The type cannot be interpolated.</exception>
    public static ITweenInterpolator GetDefault(Type type)
    {
        if (s_interpolators.TryGetValue(type, out var interpolator))
            return interpolator;

        if (!RuntimeFeature.IsDynamicCodeSupported)
            throw new NotSupportedException($"No interpolator registered for type '{type.FullName}'. Register one with '{nameof(TweenInterpolator)}.{nameof(Register)}' or use the generic API.");

        // Guarded by RuntimeFeature.IsDynamicCodeSupported (the .NET 8 analyzer does not recognize the guard)
#pragma warning disable IL3050
        return GetDefaultDynamic(type);
#pragma warning restore IL3050
    }

    /// <summary>
    /// Registers <paramref name="interpolator"/> as the default interpolator of <typeparamref name="T"/>.
    /// </summary>
    public static void Register<T>(TweenInterpolator<T> interpolator) => TweenInterpolator<T>.Default = interpolator;

    /// <summary>
    /// Creates an interpolator from a delegate.
    /// </summary>
    /// <param name="interpolate">Function receiving the progress (usually 0..1, can overshoot with some easings), the begin value and the end value.</param>
    public static TweenInterpolator<T> Create<T>(Func<double, T?, T?, T?> interpolate)
    {
        if (interpolate is null)
            throw new ArgumentNullException(nameof(interpolate));

        return new TweenFuncInterpolator<T>(interpolate);
    }

    /// <summary>
    /// Creates an interpolator of nullable values from the interpolator of the underlying type.
    /// When one of the values is <see langword="null"/>, the value switches at half of the transition.
    /// </summary>
    public static TweenInterpolator<T?> Nullable<T>(TweenInterpolator<T> interpolator)
        where T : struct
    {
        if (interpolator is null)
            throw new ArgumentNullException(nameof(interpolator));

        return new TweenNullableInterpolator<T>(interpolator);
    }

    /// <summary>
    /// Creates an interpolator of <see cref="System.Drawing.Color"/> working in the given color space.
    /// </summary>
    /// <remarks>
    /// Like CSS Color 4, alpha is premultiplied during the interpolation, and achromatic colors (grays) take the hue of the other color.
    /// </remarks>
    /// <param name="space">Color space in which colors are interpolated.</param>
    /// <param name="huePath">Direction around the color wheel, for <see cref="TweenColorSpace.Hsl"/> and <see cref="TweenColorSpace.Oklch"/>.</param>
    public static TweenInterpolator<Color> Color(TweenColorSpace space = TweenColorSpace.Srgb, TweenHuePath huePath = TweenHuePath.Shorter)
    {
        return new TweenColorInterpolator<Color>(TweenColorInterpolator<Color>.ColorToVector, TweenColorInterpolator<Color>.VectorToColor, space, huePath);
    }

    /// <summary>
    /// Creates an interpolator of any color type working in the given color space, from conversions to and from
    /// normalized sRGB components (X = red, Y = green, Z = blue, W = alpha, all between 0 and 1).
    /// </summary>
    /// <remarks>
    /// Like CSS Color 4, alpha is premultiplied during the interpolation, and achromatic colors (grays) take the hue of the other color.
    /// </remarks>
    /// <param name="toRgba">Converts a color to normalized sRGB components.</param>
    /// <param name="fromRgba">Converts normalized sRGB components to a color.</param>
    /// <param name="space">Color space in which colors are interpolated.</param>
    /// <param name="huePath">Direction around the color wheel, for <see cref="TweenColorSpace.Hsl"/> and <see cref="TweenColorSpace.Oklch"/>.</param>
    public static TweenInterpolator<T> Color<T>(Func<T, Vector4> toRgba, Func<Vector4, T> fromRgba, TweenColorSpace space = TweenColorSpace.Srgb, TweenHuePath huePath = TweenHuePath.Shorter)
    {
        return new TweenColorInterpolator<T>(toRgba, fromRgba, space, huePath);
    }

#if NET7_0_OR_GREATER
    /// <summary>
    /// Creates an interpolator (with arithmetic support) for any type implementing the generic math interfaces.
    /// Integral types are rounded to the nearest value.
    /// </summary>
    public static TweenInterpolator<T> Number<T>()
        where T : struct, INumberBase<T>
    {
        return new TweenGenericNumberInterpolator<T>();
    }
#endif

    internal static bool TryGetRegistered(Type type, [NotNullWhen(true)] out ITweenInterpolator? interpolator)
    {
        return s_interpolators.TryGetValue(type, out interpolator);
    }

    internal static void SetRegistered(Type type, ITweenInterpolator interpolator)
    {
        s_interpolators[type] = interpolator;
    }

    internal static void TryAddRegistered(Type type, ITweenInterpolator interpolator)
    {
        s_interpolators.TryAdd(type, interpolator);
    }

    [RequiresDynamicCode("Resolving the interpolator of an arbitrary type requires dynamic code.")]
    private static ITweenInterpolator GetDefaultDynamic(Type type)
    {
        var genericType = typeof(TweenInterpolator<>).MakeGenericType(type);
        var property = genericType.GetProperty(nameof(TweenInterpolator<object>.Default), BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("Cannot find Default static property");

        try
        {
            return (ITweenInterpolator)property.GetValue(null)!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    private static ConcurrentDictionary<Type, ITweenInterpolator> CreateBuiltInInterpolators()
    {
        var interpolators = new ConcurrentDictionary<Type, ITweenInterpolator>();

        void AddStruct<T>(TweenInterpolator<T> interpolator)
            where T : struct
        {
            interpolators[typeof(T)] = interpolator;
            interpolators[typeof(T?)] = new TweenNullableInterpolator<T>(interpolator);
        }

        AddStruct(new TweenBooleanInterpolator());
        AddStruct(new TweenInt8Interpolator());
        AddStruct(new TweenUInt8Interpolator());
        AddStruct(new TweenInt16Interpolator());
        AddStruct(new TweenUInt16Interpolator());
        AddStruct(new TweenInt32Interpolator());
        AddStruct(new TweenUInt32Interpolator());
        AddStruct(new TweenInt64Interpolator());
        AddStruct(new TweenUInt64Interpolator());
        AddStruct(new TweenSingleInterpolator());
        AddStruct(new TweenDoubleInterpolator());
        AddStruct(new TweenDecimalInterpolator());
        AddStruct(new TweenTimeSpanInterpolator());
        AddStruct(new TweenDateTimeInterpolator());
        AddStruct(new TweenDateTimeOffsetInterpolator());
        AddStruct(new TweenVector2Interpolator());
        AddStruct(new TweenVector3Interpolator());
        AddStruct(new TweenVector4Interpolator());
        AddStruct(new TweenQuaternionInterpolator());
        AddStruct(Color());
#if NET5_0_OR_GREATER
        AddStruct(new TweenHalfInterpolator());
#endif
#if NET7_0_OR_GREATER
        AddStruct(new TweenGenericNumberInterpolator<Int128>());
        AddStruct(new TweenGenericNumberInterpolator<UInt128>());
#endif
        interpolators[typeof(string)] = new TweenStringInterpolator();

        return interpolators;
    }
}

/// <summary>
/// Base class of typed interpolators. <see cref="Default"/> gives the interpolator used for a type.
/// </summary>
/// <typeparam name="T">Interpolated value type.</typeparam>
public abstract class TweenInterpolator<T> : ITweenInterpolator
{
    private static TweenInterpolator<T>? s_default;

    /// <summary>
    /// Gets or sets the default interpolator of <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// Built-in types are resolved automatically, as well as types implementing <see cref="ITweenInterpolable{T}"/>.
    /// Setting this property registers a custom interpolator for the type.
    /// </remarks>
    /// <exception cref="NotSupportedException">The type cannot be interpolated and no interpolator was registered.</exception>
    public static TweenInterpolator<T> Default
    {
        get => Volatile.Read(ref s_default) ?? ResolveDefault();
        set
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            Volatile.Write(ref s_default, value);
            TweenInterpolator.SetRegistered(typeof(T), value);
        }
    }

    /// <summary>
    /// Interpolates between <paramref name="begin"/> and <paramref name="end"/>.
    /// </summary>
    /// <param name="delta">Progress of the transition, usually between 0 and 1 (can overshoot with some easings).</param>
    /// <param name="begin">Value at progress 0.</param>
    /// <param name="end">Value at progress 1.</param>
    public abstract T? Interpolate(double delta, T? begin, T? end);

    object? ITweenInterpolator.Interpolate(double delta, object? begin, object? end)
    {
        return Interpolate(delta, Unbox(begin), Unbox(end));
    }

    private static T? Unbox(object? value)
    {
        if (value is null)
        {
            if (default(T) is not null)
                throw new ArgumentException($"Value must be of type '{typeof(T).FullName}'");

            return default;
        }

        return value is T val ? val : throw new ArgumentException($"Value must be of type '{typeof(T).FullName}'");
    }

    private static TweenInterpolator<T> ResolveDefault()
    {
        TweenInterpolator<T> interpolator;
        if (TweenInterpolator.TryGetRegistered(typeof(T), out var registered) && registered is TweenInterpolator<T> typed)
            interpolator = typed;
        else if (typeof(ITweenInterpolable<T>).IsAssignableFrom(typeof(T)))
            interpolator = new TweenDelegateInterpolator<T>();
        else
            throw new NotSupportedException($"Type '{typeof(T).FullName}' does not implement '{nameof(ITweenInterpolable<T>)}' and no interpolator is registered for it");

        interpolator = Interlocked.CompareExchange(ref s_default, interpolator, null) ?? interpolator;
        TweenInterpolator.TryAddRegistered(typeof(T), interpolator);
        return interpolator;
    }
}
