namespace Sight.IoC
{
    /// <summary>
    /// Describe a container that can register services
    /// </summary>
    public interface ITypeRegistrar
    {
        /// <summary>
        /// Register a service
        /// </summary>
        public void Register(Registration registration);
    }
}
