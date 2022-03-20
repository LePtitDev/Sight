using System.Text;

namespace Sight.IoC
{
    /// <summary>
    /// Describe a service registration
    /// </summary>
    public class Registration
    {
        /// <summary>
        /// Initialize a new instance of <see cref="Registration"/> class
        /// </summary>
        public Registration(Type type, ResolveDelegate resolver, string? name = null, ResolvePredicate? predicate = null)
            : this(new[] { type }, resolver, name, predicate)
        {
        }

        /// <inheritdoc cref="Registration(Type,ResolveDelegate,string?,ResolvePredicate?)"/>
        public Registration(Type[] types, ResolveDelegate resolver, string? name = null, ResolvePredicate? predicate = null)
        {
            Types = types;
            Name = name;
            Resolver = resolver;
            Predicate = predicate;
        }

        /// <summary>
        /// Service types
        /// </summary>
        public Type[] Types { get; }

        /// <summary>
        /// Service name (optional)
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// Resolution method
        /// </summary>
        public ResolveDelegate Resolver { get; }

        /// <summary>
        /// Resolution predicate (optional)
        /// </summary>
        public ResolvePredicate? Predicate { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            var bld = new StringBuilder();
            bld.Append("{ Types: [");
            bld.Append(string.Join(", ", Types.Select(x => x.FullName)));
            bld.Append(']');

            if (!string.IsNullOrEmpty(Name))
            {
                bld.Append(", Name: '");
                bld.Append(Name);
                bld.Append("'");
            }

            bld.Append(", Resolver: ");
            bld.Append(Resolver.Method);

            if (Predicate != null)
            {
                bld.Append(", Predicate: ");
                bld.Append(Predicate.Method);
            }

            bld.Append(" }");
            return bld.ToString();
        }
    }
}
