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
        public Registration(Type type, ResolveDelegate resolver)
            : base(type)
        {
            Resolver = resolver;
        }

        /// <summary>
        /// Resolution method
        /// </summary>
        public ResolveDelegate Resolver { get; }
    }
}
