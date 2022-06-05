using System.Collections.Generic;

namespace Sight.Logging.Loggers
{
    /// <summary>
    /// Implement logger to keep it in memory
    /// </summary>
    public sealed class MemoryLogger : ILogger
    {
        private readonly Queue<object> _messages = new Queue<object>();

        /// <summary>
        /// Initialize a new instance of the class <see cref="MemoryLogger"/>
        /// </summary>
        /// <param name="maxCount">If specified remove oldest messages</param>
        public MemoryLogger(int? maxCount = null)
        {
            MaxCount = maxCount;
        }

        /// <summary>
        /// Max messages count
        /// </summary>
        public int? MaxCount { get; }

        /// <summary>
        /// Object used to synchronize messages collection
        /// </summary>
        public object SyncRoot { get; } = new object();

        /// <inheritdoc />
        public void Log(object message)
        {
            lock (SyncRoot)
            {
                if (MaxCount != null && _messages.Count > MaxCount)
                    _messages.Dequeue();

                _messages.Enqueue(message);
            }
        }

        /// <summary>
        /// Get messages to immutable array
        /// </summary>
        public object[] GetMessages()
        {
            lock (SyncRoot)
            {
                return _messages.ToArray();
            }
        }

        /// <summary>
        /// Messages list, make sure to lock collection with <see cref="SyncRoot"/> before
        /// </summary>
        public IReadOnlyCollection<object> GetUnsafeMessages()
        {
            return _messages;
        }
    }
}
