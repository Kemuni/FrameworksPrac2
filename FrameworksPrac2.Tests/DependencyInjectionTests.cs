using FrameworksPrac2.Core;
using FrameworksPrac2.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrameworksPrac2.Tests;

/// <summary>
/// Тесты для проверки внедрения зависимостей через DI-контейнер.
/// </summary>
public class DependencyInjectionTests
{
    #region Интерфейсы и реализации для тестов

    public interface ITestService
    {
        string GetValue();
    }

    public sealed class TestServiceImpl : ITestService
    {
        public string GetValue() => "injected";
    }

    public interface IDependentService
    {
        ITestService TestService { get; }
    }

    public sealed class DependentServiceImpl : IDependentService
    {
        public ITestService TestService { get; }

        public DependentServiceImpl(ITestService testService)
        {
            TestService = testService;
        }
    }

    private sealed class TestServiceModule : IAppModule
    {
        public string Name => "TestService";
        public IReadOnlyCollection<string> Requires => Array.Empty<string>();

        public void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton<ITestService, TestServiceImpl>();
        }

        public Task InitializeAsync(IServiceProvider sp, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class DependentModule : IAppModule
    {
        public string Name => "Dependent";
        public IReadOnlyCollection<string> Requires => new[] { "TestService" };

        public void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton<IDependentService, DependentServiceImpl>();
        }

        public Task InitializeAsync(IServiceProvider sp, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class ActionWithDependency : IAppAction
    {
        public ITestService TestService { get; }

        public ActionWithDependency(ITestService testService)
        {
            TestService = testService;
        }

        public string Title => "Действие с зависимостью";

        public Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class ActionModule : IAppModule
    {
        public string Name => "Action";
        public IReadOnlyCollection<string> Requires => new[] { "TestService" };

        public void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton<IAppAction, ActionWithDependency>();
        }

        public Task InitializeAsync(IServiceProvider sp, CancellationToken ct) => Task.CompletedTask;
    }

    #endregion

    [Fact]
    public void Services_AreResolvedFromContainer_NotCreatedManually()
    {
        // Arrange
        var services = new ServiceCollection();
        var testModule = new TestServiceModule();
        var dependentModule = new DependentModule();

        testModule.RegisterServices(services);
        dependentModule.RegisterServices(services);

        var provider = services.BuildServiceProvider();

        // Act
        var testService = provider.GetRequiredService<ITestService>();
        var dependentService = provider.GetRequiredService<IDependentService>();

        // Assert
        Assert.NotNull(testService);
        Assert.NotNull(dependentService);
        Assert.Equal("injected", testService.GetValue());

        // Проверяем, что зависимость внедрена контейнером
        Assert.Same(testService, dependentService.TestService);
    }

    [Fact]
    public void Singleton_ReturnsSameInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        var testModule = new TestServiceModule();
        testModule.RegisterServices(services);

        var provider = services.BuildServiceProvider();

        // Act
        var instance1 = provider.GetRequiredService<ITestService>();
        var instance2 = provider.GetRequiredService<ITestService>();

        // Assert - синглтон должен возвращать один и тот же экземпляр
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void AppAction_ReceivesDependenciesFromContainer()
    {
        // Arrange
        var services = new ServiceCollection();
        var testModule = new TestServiceModule();
        var actionModule = new ActionModule();

        testModule.RegisterServices(services);
        actionModule.RegisterServices(services);

        var provider = services.BuildServiceProvider();

        // Act
        var action = provider.GetRequiredService<IAppAction>();
        var testService = provider.GetRequiredService<ITestService>();

        // Assert
        Assert.IsType<ActionWithDependency>(action);
        var typedAction = (ActionWithDependency)action;

        // Проверяем, что действие получило зависимость из контейнера
        Assert.Same(testService, typedAction.TestService);
    }

    [Fact]
    public void RealModules_RegisterServicesCorrectly()
    {
        // Arrange - используем реальные модули из приложения
        var services = new ServiceCollection();
        var modules = new Dictionary<string, IAppModule>(StringComparer.OrdinalIgnoreCase);

        // Создаем реальные модули через рефлексию (как в Program.cs)
        var assembly = typeof(IAppModule).Assembly;
        var moduleTypes = assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Where(t => typeof(IAppModule).IsAssignableFrom(t))
            .ToList();

        foreach (var type in moduleTypes)
        {
            var module = (IAppModule)Activator.CreateInstance(type)!;
            modules[module.Name] = module;
        }

        // Регистрируем службы всех модулей
        foreach (var module in modules.Values)
        {
            module.RegisterServices(services);
        }

        var provider = services.BuildServiceProvider();

        // Act & Assert - проверяем, что базовые службы доступны
        var clock = provider.GetService<IClock>();
        var storage = provider.GetService<IStorage>();

        Assert.NotNull(clock);
        Assert.NotNull(storage);
    }

    [Fact]
    public void ModuleServices_AreInjected_NotCreatedInModules()
    {
        // Этот тест проверяет, что службы действительно внедряются через DI,
        // а не создаются вручную внутри модулей

        // Arrange
        var services = new ServiceCollection();

        // Регистрируем тестовую реализацию
        var customTestService = new TestServiceImpl();
        services.AddSingleton<ITestService>(customTestService);

        var dependentModule = new DependentModule();
        dependentModule.RegisterServices(services);

        var provider = services.BuildServiceProvider();

        // Act
        var dependentService = provider.GetRequiredService<IDependentService>();

        // Assert
        // Если зависимость создавалась бы вручную внутри модуля,
        // она была бы другим экземпляром
        Assert.Same(customTestService, dependentService.TestService);
    }

    [Fact]
    public void MultipleActions_AllResolvedFromContainer()
    {
        // Arrange
        var services = new ServiceCollection();
        var testModule = new TestServiceModule();
        testModule.RegisterServices(services);

        // Регистрируем несколько действий
        services.AddSingleton<IAppAction, ActionWithDependency>();
        services.AddSingleton<IAppAction>(sp =>
            new ActionWithDependency(sp.GetRequiredService<ITestService>()));

        var provider = services.BuildServiceProvider();

        // Act
        var actions = provider.GetServices<IAppAction>().ToArray();
        var testService = provider.GetRequiredService<ITestService>();

        // Assert
        Assert.Equal(2, actions.Length);

        foreach (var action in actions)
        {
            var typedAction = (ActionWithDependency)action;
            // Все действия должны получить один и тот же экземпляр службы
            Assert.Same(testService, typedAction.TestService);
        }
    }
}
