using System;
using Sight.Serialization.Typed;

namespace Sight.Serialization;

/// <summary>
/// Value of a <c>ref&lt;T&gt;</c> position that may be local (an object of the file, possibly shared) or external
/// (the root value or an export of another SBDL file, a dependency, resolved on demand)
/// </summary>
/// <remarks>
/// A member of type <typeparamref name="T"/> at a reference position also accepts external references, but resolves them
/// while reading; <see cref="Link{T}"/> keeps them unresolved until <see cref="Resolve()"/> is called.
/// </remarks>
/// <typeparam name="T">Type of the referenced value</typeparam>
public sealed class Link<T> : ILink
{
    private readonly object _lock = new();
    private readonly DependencyContext? _context;
    private bool _resolved;
    private T? _value;

    /// <summary>
    /// Initialize a local link: the value is written in the file (once, and referenced by its other uses)
    /// </summary>
    public Link(T value)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
        _resolved = true;
        Key = string.Empty;
    }

    /// <summary>
    /// Initialize an external link
    /// </summary>
    /// <param name="dependencyId">Identifier of the dependency</param>
    /// <param name="key">Export name, or empty for the root value</param>
    /// <param name="version">Version of the dependency; if <c>null</c>, it is taken from <see cref="SerializationOptions.Dependencies"/> when writing</param>
    public Link(string dependencyId, string key = "", uint? version = null)
    {
        DependencyId = string.IsNullOrEmpty(dependencyId) ? throw new ArgumentException("Dependency identifier cannot be empty", nameof(dependencyId)) : dependencyId;
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Version = version;
    }

    internal Link(string dependencyId, string key, uint version, DependencyContext context)
        : this(dependencyId, key, version)
    {
        _context = context;
    }

    /// <summary>
    /// Indicates whether the value is in another file
    /// </summary>
    public bool IsExternal => DependencyId != null;

    /// <summary>
    /// Identifier of the dependency, <c>null</c> for a local link
    /// </summary>
    public string? DependencyId { get; }

    /// <summary>
    /// Export name, or empty for the root value (always empty for a local link)
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Version of the dependency
    /// </summary>
    public uint? Version { get; }

    /// <summary>
    /// Indicates whether the value is available without resolution (local link, or external link already resolved)
    /// </summary>
    public bool IsResolved => _resolved;

    object? ILink.LocalValue => IsExternal ? null : _value;

    /// <summary>
    /// Get the value: the local value, or the external one, resolved with the options used to read this link (or the default options)
    /// </summary>
    public T Resolve() => Resolve(null);

    /// <summary>
    /// Get the value: the local value, or the external one, resolved with the given options
    /// </summary>
    public T Resolve(SerializationOptions? options)
    {
        lock (_lock)
        {
            if (_resolved)
                return _value!;

            var context = options != null ? DependencyContext.For(options) : _context ?? DependencyContext.For(SerializationOptions.Default);
            var version = Version ?? (options ?? SerializationOptions.Default).Dependencies.FindVersion(DependencyId!)
                ?? throw new SerializationException($"Dependency '{DependencyId}' has no version");

            _value = (T)context.Resolve(typeof(T), DependencyId!, version, Key)!;
            _resolved = true;
            return _value!;
        }
    }

    /// <summary>
    /// Create a local link
    /// </summary>
    public static implicit operator Link<T>(T value) => new(value);

    /// <inheritdoc />
    public override string ToString()
    {
        if (!IsExternal)
            return $"Link({_value})";

        return Key.Length == 0 ? $"<{DependencyId}>" : $"<{DependencyId}#{Key}>";
    }
}

/// <summary>
/// Untyped access to a <see cref="Link{T}"/>
/// </summary>
internal interface ILink
{
    public bool IsExternal { get; }

    public string? DependencyId { get; }

    public string Key { get; }

    public uint? Version { get; }

    public object? LocalValue { get; }
}
