using System.Runtime.Serialization;

namespace Sight.Benchmarks
{
    [Serializable]
    public class BenchmarkException : Exception
    {
        public BenchmarkException()
        {
        }

        public BenchmarkException(string message)
            : base(message)
        {
        }

        public BenchmarkException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected BenchmarkException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
