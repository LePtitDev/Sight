namespace Sight.IoC
{
    /// <summary>
    /// Resolution delegate
    /// </summary>
    public delegate object ResolveDelegate(Type type, ResolveOptions resolveOptions);

    /// <inheritdoc cref="ResolveDelegate"/>
    public delegate T ResolveDelegate<out T>(Type type, ResolveOptions resolveOptions) where T : notnull;
}
