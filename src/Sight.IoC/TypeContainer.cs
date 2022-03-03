namespace Sight.IoC
{
    /// <summary>
    /// Implementation of <see cref="ITypeContainer"/>
    /// </summary>
    public class TypeContainer : TypeResolver, ITypeContainer
    {
        private readonly ICollection<Registration> _registrations;

        /// <summary>
        /// Initialize a new instance of <see cref="TypeContainer"/> class
        /// </summary>
        public TypeContainer()
            : this(new List<Registration>(), new object())
        {
        }

        /// <summary>
        /// Initialize a new instance of <see cref="TypeContainer"/> class with a specific registrations collection
        /// </summary>
        public TypeContainer(ICollection<Registration> registrations, object? syncRoot = null)
            : base(registrations, syncRoot)
        {
            _registrations = registrations;
        }

        /// <inheritdoc />
        public void Register(Registration registration)
        {
            EnsureSync(() => _registrations.Add(registration));
        }
    }
}
