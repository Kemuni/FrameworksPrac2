using FrameworksPrac2.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FrameworksPrac2.Modules;

public sealed class LoggingModule : IAppModule
{
    public string Name => "Logging";

    public IReadOnlyCollection<string> Requires => new[] { "Core" };

    public void RegisterServices(IServiceCollection services)
    {
        services.AddLogging(b => b.AddConsole());
        services.AddSingleton<IAppAction, LoggingAction>();
    }

    public Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        => Task.CompletedTask;

    private sealed class LoggingAction : IAppAction
    {
        private readonly ILogger<LoggingAction> _logger;

        public LoggingAction(ILogger<LoggingAction> logger) => _logger = logger;

        public string Title => "Проверка журнала событий";

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Сообщение из модуля журналирования");
            return Task.CompletedTask;
        }
    }
}
