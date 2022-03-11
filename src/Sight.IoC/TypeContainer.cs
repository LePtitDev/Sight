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
        public TypeContainer(RegistrationPredicate? registrationPredicate = null)
            : this(new List<Registration>(), new object(), registrationPredicate)
        {
        }

        /// <summary>
        /// Initialize a new instance of <see cref="TypeContainer"/> class
        /// </summary>
        public TypeContainer(ICollection<Registration> registrations, object? syncRoot = null, RegistrationPredicate? registrationPredicate = null)
            : base(registrations, syncRoot, registrationPredicate: registrationPredicate)
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
