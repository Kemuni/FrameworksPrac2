using Microsoft.Extensions.DependencyInjection;

namespace FrameworksPrac2.Core;

/// <summary>
/// Контракт модуля расширения.
/// </summary>
public interface IAppModule
{
    string Name { get; }

    IReadOnlyCollection<string> Requires { get; }

    void RegisterServices(IServiceCollection services);

    Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken);
}
