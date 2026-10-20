using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Order;

namespace Sight.Serialization.Benchmarks;

/// <summary>
/// Configuration of the serialization benchmarks: short job, allocations, data size
/// </summary>
public sealed class BenchmarkConfig : ManualConfig
{
    public const string MessagePackCategory = "MessagePack";

    public BenchmarkConfig()
    {
        AddJob(Job.Default.WithLaunchCount(1).WithWarmupCount(3).WithIterationCount(10));
        AddDiagnoser(MemoryDiagnoser.Default);
        AddColumn(new DataSizeColumn());
        AddFilter(new SimpleFilter(x => !x.Descriptor.Categories.Contains(MessagePackCategory) || !x.Descriptor.Type.IsDefined(typeof(NoMessagePackAttribute), false)));
        WithOrderer(new DefaultOrderer(SummaryOrderPolicy.Declared));
    }
}
