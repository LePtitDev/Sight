namespace Sight.IoC
{
    public class ResolveOptions
    {
        private List<object>? _additionalParameters;
        private Dictionary<string, object>? _namedParameters;

        public List<object> AdditionalParameters => _additionalParameters ??= new List<object>();

        public Dictionary<string, object> NamedParameters => _namedParameters ??= new Dictionary<string, object>();

        public bool AutoResolve { get; set; }

        public bool IsOptional { get; set; }
    }
}
