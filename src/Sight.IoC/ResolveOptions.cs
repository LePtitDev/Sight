namespace Sight.IoC
{
    /// <summary>
    /// Describe resolution options
    /// </summary>
    public class ResolveOptions
    {
        private List<object>? _additionalParameters;
        private Dictionary<string, object>? _namedParameters;
        private Dictionary<Type, object>? _typedParameters;

        /// <summary>
        /// Default resolution options
        /// </summary>
        public static ResolveOptions Default => new ResolveOptions();

        /// <summary>
        /// Initialize a new instance of the class <see cref="ResolveOptions"/>
        /// </summary>
        public ResolveOptions()
        {
        }

        /// <summary>
        /// Initialize a new instance of the class <see cref="ResolveOptions"/>
        /// </summary>
        public ResolveOptions(ResolveOptions other)
        {
            _additionalParameters = new List<object>(other.AdditionalParameters);
            _namedParameters = other.NamedParameters.ToDictionary(x => x.Key, x => x.Value);
            _typedParameters = other.TypedParameters.ToDictionary(x => x.Key, x => x.Value);
            AutoResolve = other.AutoResolve;
            AutoWiring = other.AutoWiring;
            IsOptional = other.IsOptional;
            IsAsync = other.IsAsync;
            NewInstance = other.NewInstance;
        }

        /// <summary>
        /// Parameters that can be used to resolve a service but are not registered in resolver
        /// </summary>
        public List<object> AdditionalParameters => _additionalParameters ??= new List<object>();

        /// <summary>
        /// Named parameters that can be used to resolve a service but are not registered in resolver
        /// </summary>
        public Dictionary<string, object> NamedParameters => _namedParameters ??= new Dictionary<string, object>();

        /// <summary>
        /// Typed parameters that can be used to resolve a service but are not registered in resolver
        /// </summary>
        public Dictionary<Type, object> TypedParameters => _typedParameters ??= new Dictionary<Type, object>();

        /// <summary>
        /// Allow to resolve concrete services that are not registered in resolver
        /// </summary>
        public bool AutoResolve { get; set; }

        /// <summary>
        /// Allow to resolve concrete dependencies that are not registered in resolver
        /// </summary>
        public bool AutoWiring { get; set; }

        /// <summary>
        /// Do not throw if service is not resolved and return null
        /// </summary>
        public bool IsOptional { get; set; }

        /// <summary>
        /// Allow to resolve services in async context
        /// </summary>
        public bool IsAsync { get; set; }

        /// <summary>
        /// Bypass registered already registered service and initialize a new instance of the class. This option is not applied for dependencies
        /// </summary>
        public bool NewInstance { get; set; }
    }
}
