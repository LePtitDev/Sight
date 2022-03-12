namespace Sight.IoC
{
    /// <summary>
    /// Describe a service registration only for identification
    /// </summary>
    public class RegistrationId
    {
        /// <summary>
        /// Initialize a new instance of <see cref="RegistrationId"/> class
        /// </summary>
        public RegistrationId(Type type)
        {
            Type = type;
        }

        /// <summary>
        /// Service type
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Service name (optional)
        /// </summary>
        public string? Name { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return Name == null ? Type.FullName ?? Type.Name : $"{Type.FullName} ({Name})";
        }

        /// <summary>
        /// Convert type to registration identifier
        /// </summary>
        public static implicit operator RegistrationId(Type type) => new RegistrationId(type);
    }
}
