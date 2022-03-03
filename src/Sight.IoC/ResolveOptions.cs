﻿namespace Sight.IoC
{
    /// <summary>
    /// Describe resolution options
    /// </summary>
    public class ResolveOptions
    {
        private List<object>? _additionalParameters;
        private Dictionary<string, object>? _namedParameters;

        /// <summary>
        /// Parameters that can be used to resolve a service but are not registered in resolver
        /// </summary>
        public List<object> AdditionalParameters => _additionalParameters ??= new List<object>();

        /// <summary>
        /// Named parameters that can be used to resolve a service but are not registered in resolver
        /// </summary>
        public Dictionary<string, object> NamedParameters => _namedParameters ??= new Dictionary<string, object>();

        /// <summary>
        /// Allow to resolve concrete services that are not registered in resolver
        /// </summary>
        public bool AutoResolve { get; set; }

        /// <summary>
        /// Do not throw if service is not resolved and return null
        /// </summary>
        public bool IsOptional { get; set; }
    }
}
