namespace Sight.IoC
{
    /// <summary>
    /// Describe container that implement <see cref="ITypeResolver"/> and <see cref="ITypeRegistrar"/>
    /// </summary>
    public interface ITypeContainer : ITypeResolver, ITypeRegistrar
    {
    }
}
