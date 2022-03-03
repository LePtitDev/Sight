namespace Sight.IoC
{
    /// <summary>
    /// Describe a service registration
    /// </summary>
    public class Registration
    {
        /// <summary>
        /// Initialize a new instance of <see cref="Registration"/> class
        /// </summary>
        public Registration(Type type, ResolveDelegate resolver)
        {
            Type = type;
            Resolver = resolver;
        }

        /// <summary>
        /// Service type
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Resolution method
        /// </summary>
        public ResolveDelegate Resolver { get; }
    }
}
