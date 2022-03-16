namespace Sight.IoC
{
    /// <summary>
    /// Implementation of <see cref="ITypeResolver"/>
    /// </summary>
    public class TypeResolver : ITypeResolver
    {
        private readonly Func<IEnumerable<Registration>> _provider;

        /// <summary>
        /// Initialize a new instance of <see cref="TypeResolver"/> class
        /// </summary>
        public TypeResolver(IEnumerable<Registration> registrations)
            : this(new CreateOptions(registrations))
        {
        }

        /// <summary>
        /// Initialize a new instance of <see cref="TypeResolver"/> class
        /// </summary>
        public TypeResolver(Func<IEnumerable<Registration>> registrationsFunc)
            : this(new CreateOptions(registrationsFunc))
        {
        }

        /// <summary>
        /// Initialize a new instance of <see cref="TypeResolver"/> class
        /// </summary>
        public TypeResolver(CreateOptions createOptions)
        {
            _provider = createOptions.Provider;
            Predicate = createOptions.Predicate;
            SyncRoot = createOptions.SyncRoot;
            Fallback = createOptions.Fallback;
        }

        /// <inheritdoc />
        public IEnumerable<Registration> Registrations => _provider();

        /// <inheritdoc />
        public object? SyncRoot { get; }

        /// <inheritdoc />
        public RegistrationPredicate? Predicate { get; }

        /// <inheritdoc />
        public ResolveFallback? Fallback { get; }

        /// <inheritdoc />
        public bool IsRegistered(RegistrationId identifier)
        {
            return EnsureSync(() => Registrations.Any(x => IoCHelpers.IsRegistrationFor(this, x, identifier)));
        }

        /// <inheritdoc />
        public bool IsResolvable(RegistrationId identifier, ResolveOptions resolveOptions)
        {
            return IoCHelpers.TryResolveActivator(this, identifier, resolveOptions, out _);
        }

        /// <inheritdoc />
        public object? Resolve(RegistrationId identifier, ResolveOptions resolveOptions)
        {
            if (IoCHelpers.TryResolveActivator(this, identifier, resolveOptions, out var activator))
                return activator!();

            if (resolveOptions.IsOptional)
                return null;

            throw new IoCException($"Cannot resolve {identifier}");
        }

        /// <summary>
        /// Ensure to lock collection if needed when calling action
        /// </summary>
        protected void EnsureSync(Action func)
        {
            _ = EnsureSync(() =>
            {
                func();
                return 0;
            });
        }

        /// <summary>
        /// Ensure to lock collection if needed when calling action
        /// </summary>
        protected T EnsureSync<T>(Func<T> func)
        {
            if (SyncRoot == null)
            {
                return func();
            }

            lock (SyncRoot)
            {
                return func();
            }
        }

        /// <summary>
        /// Describes initialization options of <see cref="TypeResolver"/>
        /// </summary>
        public class CreateOptions
        {
            /// <summary>
            /// Initialize a new instance of <see cref="CreateOptions"/> class with a registrations collection
            /// </summary>
            public CreateOptions(IEnumerable<Registration> registrations)
                : this(() => registrations)
            {
            }

            /// <summary>
            /// Initialize a new instance of <see cref="CreateOptions"/> class with a registrations provider
            /// </summary>
            public CreateOptions(Func<IEnumerable<Registration>> provider)
            {
                Provider = provider;
            }

            /// <summary>
            /// Provider of registrations
            /// </summary>
            public Func<IEnumerable<Registration>> Provider { get; }

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
