using System;
using System.Threading;
using System.Threading.Tasks;
using Sight.Logging.Loggers;

namespace Sight.Logging.Internal;

/// <summary>
/// Process messages one by one in a background task, shared by loggers that write in a separated thread
/// </summary>
internal sealed class BackgroundSink : IDisposable
{
    private readonly QueueLogger _queue;
    private readonly Action<object> _process;
    private readonly Action? _onDrained;
    private readonly TimeSpan _disposeTimeout;
    private readonly Task _task;
    private int _disposed;

    /// <param name="process">Write a message, exceptions are swallowed (logging must never throw)</param>
    /// <param name="onDrained">Called each time the queue becomes empty (e.g. to flush a stream)</param>
    /// <param name="options">Queue options</param>
    public BackgroundSink(Action<object> process, Action? onDrained, QueueOptions? options)
    {
        _process = process;
        _onDrained = onDrained;
        _disposeTimeout = options?.DisposeTimeout ?? new QueueOptions().DisposeTimeout;
        _queue = new QueueLogger(options);
        _task = Task.Run(Run);
    }

    public void Post(object message)
    {
        _queue.Log(message);
    }

    /// <summary>
    /// Stop accepting messages then wait for the queued ones to be processed (up to the dispose timeout)
    /// </summary>
    public void Dispose()
    {
        if (!Complete())
            return;

        try
        {
            _task.Wait(_disposeTimeout);
        }
        catch (AggregateException)
        {
        }
    }

#if !NETSTANDARD2_0
    public async ValueTask DisposeAsync()
    {
        if (!Complete())
            return;

        await Task.WhenAny(_task, Task.Delay(_disposeTimeout)).ConfigureAwait(false);
    }
#endif

    private bool Complete()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return false;

        _queue.Dispose();
        return true;
    }

    private async Task Run()
    {
        var reader = _queue.Reader;
        try
        {
            while (await reader.WaitToReadAsync().ConfigureAwait(false))
            {
                while (reader.TryRead(out var message))
                {
                    try
                    {
                        _process(message);
                    }
                    catch (Exception)
                    {
                        // A failing sink must not stop the processing of the next messages
                    }
                }

                try
                {
                    _onDrained?.Invoke();
                }
                catch (Exception)
                {
                }
            }
        }
        catch (Exception)
        {
        }
    }
}
