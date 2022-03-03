using System.Runtime.Serialization;

namespace Sight.IoC
{
    /// <summary>
    /// Describe IoC exception
    /// </summary>
    [Serializable]
    public class IoCException : Exception
    {
        /// <summary>
        /// Initialize a new instance of the <see cref="IoCException"/> class
        /// </summary>
        public IoCException()
        {
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="IoCException"/> class with a specific error message
        /// </summary>
        public IoCException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initialize a new instance of the <see cref="IoCException"/> class with a specific error message and a reference to the inner exception that cause this exception
        /// </summary>
        public IoCException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <inheritdoc />
        protected IoCException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
