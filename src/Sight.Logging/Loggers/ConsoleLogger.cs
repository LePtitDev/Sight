using System;
using System.Text;
using System.Threading.Tasks;
using Sight.Logging.Internal;
using Sight.Logging.Logs;

namespace Sight.Logging.Loggers;

/// <summary>
/// Implement logger for console outputs
/// </summary>
#if NETSTANDARD2_0
public sealed class ConsoleLogger : ILogger, IDisposable
#else
public sealed class ConsoleLogger : ILogger, IDisposable, IAsyncDisposable
#endif
{
    private readonly BackgroundSink? _sink;
    private readonly LogRenderer _outputRenderer;
    private readonly LogRenderer _errorRenderer;

    /// <summary>
    /// Initialize a new instance of the class <see cref="ConsoleLogger"/>
    /// </summary>
    /// <param name="createThread">Indicates if console will be written in a separated thread</param>
    /// <param name="queueOptions">Options of the message queue used when <paramref name="createThread"/> is true</param>
    public ConsoleLogger(bool createThread = false, QueueOptions? queueOptions = null)
        : this(new ConsoleLoggerOptions { CreateThread = createThread, Queue = queueOptions })
    {
    }

    /// <summary>
    /// Initialize a new instance of the class <see cref="ConsoleLogger"/>
    /// </summary>
    public ConsoleLogger(ConsoleLoggerOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        if (options.Utf8Output)
            TrySetUtf8Output();

        var outputAnsi = IsAnsiEnabled(options.ColorMode, Console.IsOutputRedirected);
        var errorAnsi = IsAnsiEnabled(options.ColorMode, Console.IsErrorRedirected);
        _outputRenderer = new ConsoleRenderer(options, outputAnsi, options.Hyperlinks);
        _errorRenderer = errorAnsi == outputAnsi ? _outputRenderer : new ConsoleRenderer(options, errorAnsi, options.Hyperlinks);

        if (options.CreateThread)
            _sink = new BackgroundSink(message => LogImpl(message, flush: false), FlushOutputs, options.Queue);
    }

    /// <summary>
    /// Wait for the queued messages to be written (in background mode)
    /// </summary>
    public void Dispose()
    {
        _sink?.Dispose();
    }

#if !NETSTANDARD2_0
    /// <inheritdoc cref="Dispose"/>
    public ValueTask DisposeAsync()
    {
        return _sink?.DisposeAsync() ?? default;
    }
#endif

    /// <inheritdoc />
    public void Log(object message)
    {
        if (_sink == null)
        {
            try
            {
                LogImpl(message, flush: true);
            }
            catch (Exception ex) when (ex is System.IO.IOException or ObjectDisposedException)
            {
                // Logging must never throw into the application (e.g. closed standard output)
            }
        }
        else
        {
            _sink.Post(message);
        }
    }

    private static bool IsAnsiEnabled(ConsoleColorMode mode, bool redirected)
    {
        switch (mode)
        {
            case ConsoleColorMode.Always:
                return true;
            case ConsoleColorMode.Never:
                return false;
            default:
                // https://no-color.org: any non-empty value disables colors
                return string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR")) && !redirected;
        }
    }

    private static void TrySetUtf8Output()
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
        }
        catch (Exception)
        {
            // No console (service, GUI application) or not supported
        }
    }

    private static void FlushOutputs()
    {
        Console.Out.Flush();
        Console.Error.Flush();
    }

    private void LogImpl(object message, bool flush)
    {
        // Errors go to the error stream, which may be redirected (and so colorless) independently
        var isError = LogMessage.ResolveLevel(message) >= LogLevels.Error;
        var text = (isError ? _errorRenderer : _outputRenderer).Render(message);

        var writer = isError ? Console.Error : Console.Out;
        writer.WriteLine(text);
        if (flush)
            writer.Flush();
    }
}
