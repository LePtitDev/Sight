namespace Sight.IoC
{
    /// <summary>
    /// Registration predicate
    /// </summary>
    public delegate bool RegistrationPredicate(Registration registration, Type type, ResolveOptions resolveOptions);
}
