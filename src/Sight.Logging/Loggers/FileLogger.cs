using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sight.Logging.Internal;

namespace Sight.Logging.Loggers;

/// <summary>
/// Implement logger for file outputs
/// </summary>
#if NETSTANDARD2_0
public sealed class FileLogger : ILogger, IDisposable
#else
public sealed class FileLogger : ILogger, IDisposable, IAsyncDisposable
#endif
{
    private readonly string _filePath;
    private readonly FileLoggerOptions _options;
    private readonly Encoding _encoding;
    private readonly Func<DateTime> _clock;
    private readonly Regex _rolledFileRegex;
    private readonly BackgroundSink? _sink;
    private readonly LogRenderer _renderer;
    private readonly object _writerLock = new object();
    private readonly bool _autoFlush;
    private TextWriter _writer;
    private long _size;
    private DateTime _period;
    private bool _disposed;

    /// <summary>
    /// Initialize a new instance of the class <see cref="FileLogger"/>
    /// </summary>
    /// <param name="filePath">Output file path (missing directories are created)</param>
    /// <param name="createThread">Indicates if file will be written in a separated thread</param>
    /// <param name="append">Indicates if messages are appended to an existing file (otherwise the file is overwritten)</param>
    /// <param name="queueOptions">Options of the message queue used when <paramref name="createThread"/> is true</param>
    public FileLogger(string filePath, bool createThread = false, bool append = true, QueueOptions? queueOptions = null)
        : this(filePath, new FileLoggerOptions { CreateThread = createThread, Append = append, Queue = queueOptions })
    {
    }

    /// <summary>
    /// Initialize a new instance of the class <see cref="FileLogger"/>
    /// </summary>
    /// <param name="filePath">Output file path (missing directories are created)</param>
    /// <param name="options">Logger options</param>
    public FileLogger(string filePath, FileLoggerOptions options)
    {
        if (filePath == null)
            throw new ArgumentNullException(nameof(filePath));

        if (options == null)
            throw new ArgumentNullException(nameof(options));

        if (options.MaxFileSize is <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), options.MaxFileSize, "Max file size must be strictly positive");

        if (options.MaxFiles is < 0)
            throw new ArgumentOutOfRangeException(nameof(options), options.MaxFiles, "Max files count must not be negative");

        _filePath = Path.GetFullPath(filePath);
        _options = options;
        _encoding = options.Encoding ?? new UTF8Encoding(false);
        _clock = options.Clock ?? (() => DateTime.Now);
        _autoFlush = options.AutoFlush;
        _renderer = new FileRenderer(options);

        var name = Regex.Escape(Path.GetFileNameWithoutExtension(_filePath));
        var extension = Regex.Escape(Path.GetExtension(_filePath));
        _rolledFileRegex = new Regex($@"^{name}\.\d{{8}}-\d{{6}}(-\d+)?{extension}$", RegexOptions.CultureInvariant);

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        _writer = Open(options.Append);

        if (options.CreateThread)
            _sink = new BackgroundSink(message => LogImpl(message, flush: false), Flush, options.Queue);
    }

    /// <summary>
    /// Wait for the queued messages to be written (in background mode) then close the file
    /// </summary>
    public void Dispose()
    {
        _sink?.Dispose();
        CloseWriter();
    }

#if !NETSTANDARD2_0
    /// <inheritdoc cref="Dispose"/>
    public async ValueTask DisposeAsync()
    {
        if (_sink != null)
            await _sink.DisposeAsync().ConfigureAwait(false);

        CloseWriter();
    }
#endif

    /// <inheritdoc />
    /// <remarks>Never throws: messages are ignored once the logger is disposed</remarks>
    public void Log(object message)
    {
        if (_sink == null)
        {
            try
            {
                LogImpl(message, flush: _autoFlush);
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException or UnauthorizedAccessException)
            {
                // Logging must never throw into the application (e.g. disk full)
            }
        }
        else
        {
            _sink.Post(message);
        }
    }

    /// <summary>
    /// Write the buffered messages to the file (in background mode, queued messages may not be processed yet)
    /// </summary>
    public void Flush()
    {
        lock (_writerLock)
        {
            if (!_disposed)
                _writer.Flush();
        }
    }

    private TextWriter Open(bool append)
    {
        var exists = File.Exists(_filePath);
        var stream = new FileStream(_filePath, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read);
        _size = stream.Length;
        _period = PeriodOf(append && exists ? File.GetLastWriteTime(_filePath) : _clock());

        // The preamble is written with the first message of an empty file
        if (_size == 0)
            _size = _encoding.GetPreamble().Length;

        return new StreamWriter(stream, _encoding);
    }

    private void CloseWriter()
    {
        lock (_writerLock)
        {
            _disposed = true;
            _writer.Dispose();
        }
    }

    private void LogImpl(object message, bool flush)
    {
        var text = _renderer.Render(message);

        lock (_writerLock)
        {
            if (_disposed)
                return;

            var length = _encoding.GetByteCount(text) + _encoding.GetByteCount(Environment.NewLine);
            if (ShouldRoll(length))
                Roll();

            _writer.WriteLine(text);
            _size += length;
            if (flush)
                _writer.Flush();
        }
    }

    private bool ShouldRoll(int nextLength)
    {
        // Never roll an empty file, a message bigger than the max size would roll forever
        if (_size <= _encoding.GetPreamble().Length)
            return false;

        if (_options.MaxFileSize is { } max && _size + nextLength > max)
            return true;

        return _options.RollInterval != FileRollInterval.None && PeriodOf(_clock()) != _period;
    }

    private DateTime PeriodOf(DateTime time)
    {
        return _options.RollInterval switch
        {
            FileRollInterval.Hourly => new DateTime(time.Year, time.Month, time.Day, time.Hour, 0, 0, time.Kind),
            FileRollInterval.Daily => time.Date,
            _ => DateTime.MinValue
        };
    }

    /// <summary>
    /// Rename the current file <c>name.yyyyMMdd-HHmmss.ext</c>, start a new one and delete the oldest rolled files
    /// </summary>
    private void Roll()
    {
        _writer.Dispose();

        try
        {
            var directory = Path.GetDirectoryName(_filePath)!;
            var stem = Path.GetFileNameWithoutExtension(_filePath) + "." + _clock().ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            var extension = Path.GetExtension(_filePath);

            var target = Path.Combine(directory, stem + extension);
            for (var i = 1; File.Exists(target); i++)
                target = Path.Combine(directory, stem + "-" + i.ToString(CultureInfo.InvariantCulture) + extension);

            File.Move(_filePath, target);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Keep logging in the current file
            _writer = Open(append: true);
            return;
        }

        _writer = Open(append: false);
        DeleteOldFiles();
    }

    private void DeleteOldFiles()
    {
        if (_options.MaxFiles is not { } maxFiles)
            return;

        try
        {
            var rolled = Directory.EnumerateFiles(Path.GetDirectoryName(_filePath)!)
                .Where(path => _rolledFileRegex.IsMatch(Path.GetFileName(path)))
                .OrderByDescending(path => Path.GetFileName(path), StringComparer.Ordinal)
                .Skip(maxFiles);

            foreach (var path in rolled)
            {
                try
                {
                    File.Delete(path);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
