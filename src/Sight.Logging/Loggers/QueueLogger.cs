using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Sight.Logging.Loggers;

/// <summary>
/// Implement logger to keep not processed messages in a queue
/// </summary>
/// <remarks>
/// Once disposed, new messages are silently ignored, messages already queued can still be consumed
/// and <c>WaitForMessage</c> methods throw <see cref="ObjectDisposedException"/> when the queue is empty.
/// </remarks>
#if NETSTANDARD2_0
public sealed class QueueLogger : ILogger, IDisposable
#else
public sealed class QueueLogger : ILogger, IDisposable, IAsyncDisposable
#endif
{
    private readonly Channel<object> _channel;
    private readonly QueueFullMode _fullMode;

    /// <summary>
    /// Initialize a new instance of the class <see cref="QueueLogger"/>
    /// </summary>
    /// <param name="options">Queue options, unbounded queue if not specified</param>
    public QueueLogger(QueueOptions? options = null)
    {
        options ??= new QueueOptions();
        _fullMode = options.FullMode;

        if (options.Capacity is { } capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(options), capacity, "Queue capacity must be strictly positive");

            _channel = Channel.CreateBounded<object>(new BoundedChannelOptions(capacity)
            {
                FullMode = options.FullMode switch
                {
                    QueueFullMode.DropOldest => BoundedChannelFullMode.DropOldest,
                    QueueFullMode.DropNewest => BoundedChannelFullMode.DropNewest,
                    _ => BoundedChannelFullMode.Wait
                }
            });
        }
        else
        {
            _channel = Channel.CreateUnbounded<object>();
        }
    }

    internal ChannelReader<object> Reader => _channel.Reader;

    /// <inheritdoc />
    public void Dispose()
    {
        _channel.Writer.TryComplete();
    }

#if !NETSTANDARD2_0
    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }
#endif

    /// <inheritdoc />
    /// <remarks>Never throws: messages are ignored once the queue is disposed</remarks>
    public void Log(object message)
    {
        var writer = _channel.Writer;
        if (writer.TryWrite(message) || _fullMode != QueueFullMode.Wait)
            return;

        // Bounded queue is full: block the producer until a slot is available (or the queue is disposed)
        try
        {
            writer.WriteAsync(message).AsTask().GetAwaiter().GetResult();
        }
        catch (ChannelClosedException)
        {
        }
    }

    /// <summary>
    /// Wait for a message
    /// </summary>
    /// <exception cref="ObjectDisposedException">Queue is disposed and empty</exception>
    public object WaitForMessage()
    {
        return WaitForMessageAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Wait for a message with timeout
    /// </summary>
    /// <returns>null if timeout, otherwise the message</returns>
    /// <exception cref="ObjectDisposedException">Queue is disposed and empty</exception>
    public object? WaitForMessage(TimeSpan timeout)
    {
        return WaitForMessageAsync(timeout).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Wait for a message
    /// </summary>
    /// <exception cref="ObjectDisposedException">Queue is disposed and empty</exception>
    public async Task<object> WaitForMessageAsync()
    {
        return await ReadAsync(CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Wait for a message with timeout
    /// </summary>
    /// <returns>null if timeout, otherwise the message</returns>
    /// <exception cref="ObjectDisposedException">Queue is disposed and empty</exception>
    public async Task<object?> WaitForMessageAsync(TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            return await ReadAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    private async Task<object> ReadAsync(CancellationToken cancellationToken)
    {
        var reader = _channel.Reader;
        while (true)
        {
            if (reader.TryRead(out var message))
                return message;

            if (!await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                throw new ObjectDisposedException(nameof(QueueLogger));
        }
    }
}
