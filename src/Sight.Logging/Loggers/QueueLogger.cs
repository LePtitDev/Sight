using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sight.Logging.Loggers
{
    /// <summary>
    /// Implement logger to keep not processed messages
    /// </summary>
    public sealed class QueueLogger : ILogger, IDisposable
    {
        private readonly Queue<object> _messages = new Queue<object>();
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);
        private readonly object _lock = new object();

        /// <summary>
        /// Warning: use it only when queue is disposed to get not processed messages
        /// </summary>
        public IReadOnlyCollection<object> UnsafeMessages => _messages;

        /// <inheritdoc />
        public void Dispose()
        {
            _semaphore.Dispose();
        }

        /// <inheritdoc />
        public void Log(object message)
        {
            lock (_lock)
            {
                _messages.Enqueue(message);
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Wait for a message
        /// </summary>
        public object WaitForMessage()
        {
            _semaphore.Wait();
            lock (_lock)
            {
                return _messages.Dequeue();
            }
        }

        /// <summary>
        /// Wait for a message with timeout
        /// </summary>
        /// <returns>null if timeout, otherwise the message</returns>
        public object? WaitForMessage(TimeSpan timeout)
        {
            if (!_semaphore.Wait(timeout))
                return null;

            lock (_lock)
            {
                return _messages.Dequeue();
            }
        }

        /// <summary>
        /// Wait for a message
        /// </summary>
        public async Task<object> WaitForMessageAsync()
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            lock (_lock)
            {
                return _messages.Dequeue();
            }
        }

        /// <summary>
        /// Wait for a message with timeout
        /// </summary>
        /// <returns>null if timeout, otherwise the message</returns>
        public async Task<object?> WaitForMessageAsync(TimeSpan timeout)
        {
            if (!await _semaphore.WaitAsync(timeout).ConfigureAwait(false))
                return null;

            lock (_lock)
            {
                return _messages.Dequeue();
            }
        }
    }
}
