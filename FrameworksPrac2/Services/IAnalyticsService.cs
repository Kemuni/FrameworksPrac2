namespace FrameworksPrac2.Services;

/// <summary>
/// Служба аналитики данных хранилища.
/// </summary>
public interface IAnalyticsService
{
    void PrintStorageAnalytics();
    
    StorageAnalyticsReport GenerateReport();
}

/// <summary>
/// Отчёт аналитики хранилища.
/// </summary>
public sealed class StorageAnalyticsReport
{
    public int TotalRecords { get; init; }
    public int UniqueRecords { get; init; }
    public int DuplicateCount { get; init; }
    public double AverageLength { get; init; }
    public int MinLength { get; init; }
    public int MaxLength { get; init; }
    public IReadOnlyDictionary<char, int> FirstCharDistribution { get; init; } = new Dictionary<char, int>();
}
