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
            : this(new List<Registration>())
        {
        }

        /// <summary>
        /// Initialize a new instance of <see cref="TypeContainer"/> class
        /// </summary>
        public TypeContainer(ICollection<Registration> registrations)
            : this(new CreateOptions(registrations, new object()))
        {
        }

        /// <summary>
        /// Initialize a new instance of <see cref="TypeContainer"/> class
        /// </summary>
        public TypeContainer(CreateOptions createOptions)
            : base(new TypeResolver.CreateOptions(createOptions.Registrations)
            {
                SyncRoot = createOptions.SyncRoot,
                Predicate = createOptions.Predicate,
                Fallback = createOptions.Fallback
            })
        {
            _registrations = createOptions.Registrations;
        }

        /// <inheritdoc />
        public void Register(Registration registration)
        {
            EnsureSync(() => _registrations.Add(registration));
        }

        /// <summary>
        /// Describes initialization options of <see cref="TypeContainer"/>
        /// </summary>
        public new class CreateOptions
        {
            /// <summary>
            /// Initialize a new instance of <see cref="CreateOptions"/> class
            /// </summary>
            public CreateOptions()
                : this(new List<Registration>(), new object())
            {
            }

            /// <summary>
            /// Initialize a new instance of <see cref="CreateOptions"/> class
            /// </summary>
            public CreateOptions(ICollection<Registration> registrations, object? syncRoot = null)
            {
                Registrations = registrations;
                SyncRoot = syncRoot;
            }

            /// <summary>
            /// Registrations collection
            /// </summary>
            public ICollection<Registration> Registrations { get; }

            /// <summary>
            /// Object that can be used to synchronize registration access
            /// </summary>
            public object? SyncRoot { get; set; }

            /// <summary>
            /// Registration predicate for service search
            /// </summary>
            public RegistrationPredicate? Predicate { get; set; }

            /// <summary>
            /// Resolution fallback when no registration found
            /// </summary>
            public ResolveFallback? Fallback { get; set; }
        }
    }
}
