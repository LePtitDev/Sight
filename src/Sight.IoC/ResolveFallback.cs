namespace Sight.IoC
{
    /// <summary>
    /// Describe resolve fallback when no registration found
    /// </summary>
    public class ResolveFallback
    {
        /// <summary>
        /// Initialize a new instance of <see cref="ResolveFallback"/> class
        /// </summary>
        public ResolveFallback(ResolvePredicate predicate, ResolveDelegate resolver)
        {
            Predicate = predicate;
            Resolver = resolver;
        }

        /// <summary>
        /// Service predicate
        /// </summary>
        public ResolvePredicate Predicate { get; }

        /// <summary>
        /// Service resolver
        /// </summary>
        public ResolveDelegate Resolver { get; }
    }
}
