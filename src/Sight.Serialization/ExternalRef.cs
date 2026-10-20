using System;
using Sight.Serialization.Typed;

namespace Sight.Serialization;

/// <summary>
/// Reference to the root value or to an export of another SBDL file (a dependency)
/// </summary>
/// <typeparam name="T">Type of the referenced value</typeparam>
public sealed class ExternalRef<T> : IExternalRef
{
    private readonly object _lock = new();
    private DependencyContext? _context;
    private bool _resolved;
    private T? _value;

    /// <summary>
    /// Initialize a new instance of <see cref="ExternalRef{T}"/>
    /// </summary>
    /// <param name="dependencyId">Identifier of the dependency</param>
    /// <param name="key">Export name, or empty for the root value</param>
    /// <param name="version">Version of the dependency; if <c>null</c>, it is taken from <see cref="SerializationOptions.Dependencies"/> when writing</param>
    public ExternalRef(string dependencyId, string key = "", uint? version = null)
    {
        DependencyId = string.IsNullOrEmpty(dependencyId) ? throw new ArgumentException("Dependency identifier cannot be empty", nameof(dependencyId)) : dependencyId;
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Version = version;
    }

    internal ExternalRef(string dependencyId, string key, uint version, DependencyContext context)
        : this(dependencyId, key, version)
    {
        _context = context;
    }

    /// <summary>
    /// Identifier of the dependency
    /// </summary>
    public string DependencyId { get; }

    /// <summary>
    /// Export name, or empty for the root value
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Version of the dependency
    /// </summary>
    public uint? Version { get; }

    /// <summary>
    /// Indicates whether the value has already been resolved
    /// </summary>
    public bool IsResolved => _resolved;

    /// <summary>
    /// Resolve the referenced value, with the options used to read this reference (or the default options)
    /// </summary>
    public T Resolve() => Resolve(null);

    /// <summary>
    /// Resolve the referenced value with the given options
    /// </summary>
    public T Resolve(SerializationOptions? options)
    {
        lock (_lock)
        {
            if (_resolved)
                return _value!;

            var context = options != null ? DependencyContext.For(options) : _context ?? DependencyContext.For(SerializationOptions.Default);
            var version = Version ?? (options ?? SerializationOptions.Default).Dependencies.FindVersion(DependencyId)
                ?? throw new SerializationException($"Dependency '{DependencyId}' has no version");

            _value = context.Resolve<T>(DependencyId, version, Key);
            _resolved = true;
            return _value!;
        }
    }

    /// <inheritdoc />
    public override string ToString() => Key.Length == 0 ? $"<{DependencyId}>" : $"<{DependencyId}#{Key}>";
}
