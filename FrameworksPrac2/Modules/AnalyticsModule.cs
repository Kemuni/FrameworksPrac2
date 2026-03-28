using FrameworksPrac2.Core;
using FrameworksPrac2.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FrameworksPrac2.Modules;

/// <summary>
/// Модуль аналитики для анализа данных хранилища.
/// </summary>
public sealed class AnalyticsModule : IAppModule
{
    public string Name => "Analytics";

    public IReadOnlyCollection<string> Requires => new[] { "Core", "Validation" };

    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IAnalyticsService, AnalyticsService>();
        services.AddSingleton<IAppAction, AnalyticsAction>();
    }

    public Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        => Task.CompletedTask;

    private sealed class AnalyticsAction : IAppAction
    {
        private readonly IAnalyticsService _analyticsService;
        private readonly IStorage _storage;

        public AnalyticsAction(IAnalyticsService analyticsService, IStorage storage)
        {
            _analyticsService = analyticsService;
            _storage = storage;
        }

        public string Title => "Анализ данных хранилища";

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            // Добавляем тестовые данные для демонстрации аналитики
            _storage.Add("Тестовая запись 1");
            _storage.Add("Тестовая запись 2");
            _storage.Add("Аналитика работает");
            _storage.Add("Аналитика работает"); // дубликат

            // Выводим аналитику
            _analyticsService.PrintStorageAnalytics();

            return Task.CompletedTask;
        }
    }
}
