using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sight.Threading
{
    /// <summary>
    /// Component that allow you to synchronize code in an other thread
    /// </summary>
    public interface IDispatcher
    {
        /// <summary>
        /// Execute an async method on the thread associated to the dispatcher
        /// </summary>
        /// <param name="func">Async delegate to invoke</param>
        /// <param name="delay">Delay before invoking the delegate</param>
        /// <param name="priority">Ordering priority of the call</param>
        /// <param name="state">User specific state object sent in delegate</param>
        /// <param name="cancellationToken">Object that indicate if the invoke must be cancelled</param>
        Task InvokeAsync(Func<object?, Task> func, TimeSpan delay, int priority, object? state, CancellationToken cancellationToken);
    }
}
