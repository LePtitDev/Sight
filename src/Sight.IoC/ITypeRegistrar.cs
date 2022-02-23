namespace Sight.IoC
{
    public interface ITypeRegistrar
    {
        public void Register(Type type, ResolveDelegate resolver);
    }
}
