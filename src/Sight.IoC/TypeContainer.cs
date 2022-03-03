namespace Sight.IoC
{
    public class TypeContainer : TypeResolver, ITypeContainer
    {
        private readonly ICollection<Registration> _registrations;

        public TypeContainer()
            : this(new List<Registration>(), new object())
        {
        }

        public TypeContainer(ICollection<Registration> registrations, object? syncRoot = null)
            : base(registrations, syncRoot)
        {
            _registrations = registrations;
        }

        public void Register(Registration registration)
        {
            EnsureSync(() => _registrations.Add(registration));
        }
    }
}
