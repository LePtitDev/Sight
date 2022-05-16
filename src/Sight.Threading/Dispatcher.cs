using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sight.Threading
{
    /// <summary>
    /// Implementation of the <see cref="IDispatcher"/>
    /// </summary>
    public class Dispatcher : IDispatcher
    {
        private readonly ConcurrentDictionary<long, Invokable> _bag = new();
        private long _increment = 1;

        /// <summary>
        /// Run the next delegate available or return false if empty
        /// </summary>
        public async Task<bool> RunNextAsync()
        {
            var dateTime = DateTime.UtcNow;
            var invPair = _bag.ToArray().Where(x => dateTime >= x.Value.From).OrderByDescending(x => x.Value.Priority).FirstOrDefault();
            if (invPair.Key == 0 || !_bag.TryRemove(invPair.Key, out _))
                return false;

            var invoker = invPair.Value;
            if (!invoker.CancellationToken.IsCancellationRequested)
                await invoker.Func(invoker.State);

            return true;
        }

        /// <inheritdoc />
        public Task InvokeAsync(Func<object?, Task> func, TimeSpan delay, int priority, object? state, CancellationToken cancellationToken)
        {
            var semaphore = new SemaphoreSlim(1, 1);
            var id = Interlocked.Increment(ref _increment);
            _bag[id] = new Invokable(async o =>
            {
                await func(o);
                semaphore.Release();
            }, DateTime.UtcNow.Add(delay), priority, state, cancellationToken);
            return semaphore.WaitAsync(cancellationToken);
        }

        private record struct Invokable(Func<object?, Task> Func, DateTime From, int Priority, object? State, CancellationToken CancellationToken);
    }
}
