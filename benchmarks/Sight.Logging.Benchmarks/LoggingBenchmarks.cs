using System.IO;
using Sight.Logging.Loggers;
using Sight.Logging.Logs;

namespace Sight.Logging.Benchmarks;

/// <summary>
/// Cost of one <c>LogInformation</c> call (each benchmark method logs <see cref="Count"/> messages)
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class LoggingBenchmarks
{
    private const int Count = 1000;

    private static readonly TimeSpan Elapsed = TimeSpan.FromMilliseconds(1500);

    private string _directory = null!;
    private ILogger _null = null!;
    private ILogger _filteredOut = null!;
    private MemoryLogger _memory = null!;
    private FileLogger _fileSync = null!;
    private FileLogger _fileBackground = null!;

    [GlobalSetup]
    public void Setup()
    {
        _directory = Path.Combine(Path.GetTempPath(), "sight-logging-benchmarks-" + Guid.NewGuid().ToString("N"));
        _null = NullLogger.Instance;
        _filteredOut = NullLogger.Instance.WithMinimumLevel(LogLevels.Error);
        _memory = new MemoryLogger(Count);
        _fileSync = new FileLogger(Path.Combine(_directory, "sync.log"));
        _fileBackground = new FileLogger(
            Path.Combine(_directory, "background.log"),
            new FileLoggerOptions { CreateThread = true, Queue = new QueueOptions { Capacity = 100_000 } });
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _fileSync.Dispose();
        _fileBackground.Dispose();
        Directory.Delete(_directory, true);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public void NullLogger_Plain() => Run(_null);

    [Benchmark(OperationsPerInvoke = Count)]
    public void NullLogger_WithParams() => RunWithParams(_null);

    [Benchmark(OperationsPerInvoke = Count)]
    public void Filtered_Plain() => Run(_filteredOut);

    [Benchmark(OperationsPerInvoke = Count)]
    public void Filtered_WithParams() => RunWithParams(_filteredOut);

    [Benchmark(OperationsPerInvoke = Count)]
    public void MemoryLogger_Plain() => Run(_memory);

    [Benchmark(OperationsPerInvoke = Count)]
    public void FileLogger_Sync_Plain() => Run(_fileSync);

    [Benchmark(OperationsPerInvoke = Count)]
    public void FileLogger_Sync_WithParams() => RunWithParams(_fileSync);

    [Benchmark(OperationsPerInvoke = Count)]
    public void FileLogger_Background_Plain() => Run(_fileBackground);

    private static void Run(ILogger logger)
    {
        for (var i = 0; i < Count; i++)
            logger.LogInformation("Hello World!");
    }

    private static void RunWithParams(ILogger logger)
    {
        for (var i = 0; i < Count; i++)
            logger.LogInformation("Request done in %t (100%% ok)", Elapsed);
    }
}
