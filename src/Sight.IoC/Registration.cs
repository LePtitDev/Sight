namespace Sight.IoC
{
    /// <summary>
    /// Describe a service registration
    /// </summary>
    public class Registration : RegistrationId
    {
        /// <summary>
        /// Initialize a new instance of <see cref="Registration"/> class
        /// </summary>
        public Registration(Type type, ResolveDelegate resolver, ResolvePredicate? predicate = null)
            : base(type)
        {
            Resolver = resolver;
            Predicate = predicate;
        }

        /// <summary>
        /// Resolution method
        /// </summary>
        public ResolveDelegate Resolver { get; }

        /// <summary>
        /// Resolution predicate (optional)
        /// </summary>
        public ResolvePredicate? Predicate { get; }
    }
}
