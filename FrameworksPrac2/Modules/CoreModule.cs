using FrameworksPrac2.Core;
using FrameworksPrac2.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FrameworksPrac2.Modules;

/// <summary>
/// Базовый модуль приложения.
/// </summary>
public sealed class CoreModule : IAppModule
{
    public string Name => "Core";

    public IReadOnlyCollection<string> Requires => Array.Empty<string>();

    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IStorage, InMemoryStorage>();
    }

    public Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        // Базовый модуль не выполняет действий, он только подготавливает инфраструктуру
        return Task.CompletedTask;
    }
}
