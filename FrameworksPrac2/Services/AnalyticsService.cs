namespace FrameworksPrac2.Services;

/// <summary>
/// Реализация службы аналитики хранилища.
/// </summary>
public sealed class AnalyticsService : IAnalyticsService
{
    private readonly IStorage _storage;
    private readonly IClock _clock;

    public AnalyticsService(IStorage storage, IClock clock)
    {
        _storage = storage;
        _clock = clock;
    }

    public StorageAnalyticsReport GenerateReport()
    {
        var data = _storage.GetAll();

        if (data.Count == 0)
        {
            return new StorageAnalyticsReport
            {
                TotalRecords = 0,
                UniqueRecords = 0,
                DuplicateCount = 0,
                AverageLength = 0,
                MinLength = 0,
                MaxLength = 0,
                FirstCharDistribution = new Dictionary<char, int>()
            };
        }

        var uniqueSet = new HashSet<string>(data);
        var lengths = data.Select(s => s.Length).ToList();
        var firstChars = data
            .Where(s => s.Length > 0)
            .GroupBy(s => char.ToUpper(s[0]))
            .ToDictionary(g => g.Key, g => g.Count());

        return new StorageAnalyticsReport
        {
            TotalRecords = data.Count,
            UniqueRecords = uniqueSet.Count,
            DuplicateCount = data.Count - uniqueSet.Count,
            AverageLength = lengths.Average(),
            MinLength = lengths.Min(),
            MaxLength = lengths.Max(),
            FirstCharDistribution = firstChars
        };
    }

    public void PrintStorageAnalytics()
    {
        var report = GenerateReport();

        Console.WriteLine();
        Console.WriteLine("------ АНАЛИТИКА ХРАНИЛИЩА --------");
        Console.WriteLine($"Время анализа: {_clock.Now:HH:mm:ss}");
        Console.WriteLine("--");
        Console.WriteLine($"Всего записей:      {report.TotalRecords,10}");
        Console.WriteLine($"Уникальных:         {report.UniqueRecords,10}");
        Console.WriteLine($"Дубликатов:         {report.DuplicateCount,10}");
        Console.WriteLine("--");
        Console.WriteLine($"Средняя длина:      {report.AverageLength,10:F2}");
        Console.WriteLine($"Мин. длина:         {report.MinLength,10}");
        Console.WriteLine($"Макс. длина:        {report.MaxLength,10}");
        Console.WriteLine("----------");
        Console.WriteLine();
    }
}
