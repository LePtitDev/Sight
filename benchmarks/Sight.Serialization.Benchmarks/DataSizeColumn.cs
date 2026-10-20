using System.Collections.Concurrent;
using System.Globalization;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;

namespace Sight.Serialization.Benchmarks;

/// <summary>
/// Benchmark class able to report the size of the data handled by each benchmark
/// </summary>
public interface IDataSizeProvider
{
    /// <summary>
    /// Size in bytes of the data written or read by a benchmark method, or null if not relevant
    /// </summary>
    public long? GetDataSize(string benchmark);
}

/// <summary>
/// Column showing the size of the data produced or consumed by a benchmark
/// </summary>
public sealed class DataSizeColumn : IColumn
{
    private static readonly ConcurrentDictionary<Type, IDataSizeProvider?> s_providers = new();

    public string Id => nameof(DataSizeColumn);

    public string ColumnName => "Data (B)";

    public bool AlwaysShow => true;

    public ColumnCategory Category => ColumnCategory.Custom;

    public int PriorityInCategory => 0;

    public bool IsNumeric => true;

    public UnitType UnitType => UnitType.Dimensionless;

    public string Legend => "Size in bytes of the serialized data (UTF-8 for text formats)";

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase) => GetValue(summary, benchmarkCase, SummaryStyle.Default);

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
    {
        var provider = s_providers.GetOrAdd(benchmarkCase.Descriptor.Type, type =>
        {
            if (!typeof(IDataSizeProvider).IsAssignableFrom(type))
                return null;

            var instance = Activator.CreateInstance(type);
            benchmarkCase.Descriptor.GlobalSetupMethod?.Invoke(instance, null);
            return (IDataSizeProvider?)instance;
        });

        var size = provider?.GetDataSize(benchmarkCase.Descriptor.WorkloadMethod.Name);
        return size?.ToString("N0", CultureInfo.InvariantCulture) ?? "-";
    }

    public bool IsAvailable(Summary summary) => true;

    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
}
