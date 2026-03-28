using FrameworksPrac2.Core;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FrameworksPrac2.Tests;

/// <summary>
/// Тесты для проверки порядка запуска модулей и обработки ошибок.
/// </summary>
public class ModuleCatalogTests
{
    #region Тестовые модули

    private sealed class ModuleA : IAppModule
    {
        public string Name => "A";
        public IReadOnlyCollection<string> Requires => Array.Empty<string>();
        public void RegisterServices(IServiceCollection services) { }
        public Task InitializeAsync(IServiceProvider sp, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class ModuleB : IAppModule
    {
        public string Name => "B";
        public IReadOnlyCollection<string> Requires => new[] { "A" };
        public void RegisterServices(IServiceCollection services) { }
        public Task InitializeAsync(IServiceProvider sp, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class ModuleC : IAppModule
    {
        public string Name => "C";
        public IReadOnlyCollection<string> Requires => new[] { "A", "B" };
        public void RegisterServices(IServiceCollection services) { }
        public Task InitializeAsync(IServiceProvider sp, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class ModuleD : IAppModule
    {
        public string Name => "D";
        public IReadOnlyCollection<string> Requires => new[] { "B" };
        public void RegisterServices(IServiceCollection services) { }
        public Task InitializeAsync(IServiceProvider sp, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class CyclicModuleX : IAppModule
    {
        public string Name => "X";
        public IReadOnlyCollection<string> Requires => new[] { "Y" };
        public void RegisterServices(IServiceCollection services) { }
        public Task InitializeAsync(IServiceProvider sp, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class CyclicModuleY : IAppModule
    {
        public string Name => "Y";
        public IReadOnlyCollection<string> Requires => new[] { "X" };
        public void RegisterServices(IServiceCollection services) { }
        public Task InitializeAsync(IServiceProvider sp, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class CyclicModuleP : IAppModule
    {
        public string Name => "P";
        public IReadOnlyCollection<string> Requires => new[] { "Q" };
        public void RegisterServices(IServiceCollection services) { }
        public Task InitializeAsync(IServiceProvider sp, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class CyclicModuleQ : IAppModule
    {
        public string Name => "Q";
        public IReadOnlyCollection<string> Requires => new[] { "R" };
        public void RegisterServices(IServiceCollection services) { }
        public Task InitializeAsync(IServiceProvider sp, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class CyclicModuleR : IAppModule
    {
        public string Name => "R";
        public IReadOnlyCollection<string> Requires => new[] { "P" };
        public void RegisterServices(IServiceCollection services) { }
        public Task InitializeAsync(IServiceProvider sp, CancellationToken ct) => Task.CompletedTask;
    }

    #endregion

    #region Тесты корректного порядка запуска

    [Fact]
    public void BuildExecutionOrder_SingleModule_ReturnsCorrectOrder()
    {
        // Arrange
        var modules = new Dictionary<string, IAppModule>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = new ModuleA()
        };
        var enabled = new[] { "A" };

        // Act
        var result = ModuleCatalog.BuildExecutionOrder(modules, enabled);

        // Assert
        Assert.Single(result);
        Assert.Equal("A", result[0].Name);
    }

    [Fact]
    public void BuildExecutionOrder_LinearDependency_ReturnsCorrectOrder()
    {
        // Arrange: A <- B <- C (C зависит от B, B зависит от A)
        var modules = new Dictionary<string, IAppModule>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = new ModuleA(),
            ["B"] = new ModuleB(),
            ["C"] = new ModuleC()
        };
        var enabled = new[] { "A", "B", "C" };

        // Act
        var result = ModuleCatalog.BuildExecutionOrder(modules, enabled);

        // Assert
        Assert.Equal(3, result.Count);

        var indexA = result.ToList().FindIndex(m => m.Name == "A");
        var indexB = result.ToList().FindIndex(m => m.Name == "B");
        var indexC = result.ToList().FindIndex(m => m.Name == "C");

        Assert.True(indexA < indexB, "A должен быть запущен перед B");
        Assert.True(indexB < indexC, "B должен быть запущен перед C");
    }

    [Fact]
    public void BuildExecutionOrder_DiamondDependency_ReturnsCorrectOrder()
    {
        // Arrange: A <- B, A <- D, B <- C (ромбовидная зависимость)
        var modules = new Dictionary<string, IAppModule>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = new ModuleA(),
            ["B"] = new ModuleB(),
            ["C"] = new ModuleC(),
            ["D"] = new ModuleD()
        };
        var enabled = new[] { "A", "B", "C", "D" };

        // Act
        var result = ModuleCatalog.BuildExecutionOrder(modules, enabled);

        // Assert
        Assert.Equal(4, result.Count);

        var indexA = result.ToList().FindIndex(m => m.Name == "A");
        var indexB = result.ToList().FindIndex(m => m.Name == "B");
        var indexC = result.ToList().FindIndex(m => m.Name == "C");
        var indexD = result.ToList().FindIndex(m => m.Name == "D");

        Assert.True(indexA < indexB, "A должен быть запущен перед B");
        Assert.True(indexA < indexC, "A должен быть запущен перед C");
        Assert.True(indexB < indexC, "B должен быть запущен перед C");
        Assert.True(indexB < indexD, "B должен быть запущен перед D");
    }

    [Fact]
    public void BuildExecutionOrder_ReverseConfigOrder_StillReturnsCorrectOrder()
    {
        // Arrange: модули указаны в конфигурации в обратном порядке
        var modules = new Dictionary<string, IAppModule>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = new ModuleA(),
            ["B"] = new ModuleB(),
            ["C"] = new ModuleC()
        };
        var enabled = new[] { "C", "B", "A" }; // Обратный порядок

        // Act
        var result = ModuleCatalog.BuildExecutionOrder(modules, enabled);

        // Assert
        var indexA = result.ToList().FindIndex(m => m.Name == "A");
        var indexB = result.ToList().FindIndex(m => m.Name == "B");
        var indexC = result.ToList().FindIndex(m => m.Name == "C");

        Assert.True(indexA < indexB, "A должен быть запущен перед B независимо от порядка в конфигурации");
        Assert.True(indexB < indexC, "B должен быть запущен перед C независимо от порядка в конфигурации");
    }

    #endregion

    #region Тесты ошибки отсутствующего модуля

    [Fact]
    public void BuildExecutionOrder_MissingModule_ThrowsWithClearMessage()
    {
        // Arrange
        var modules = new Dictionary<string, IAppModule>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = new ModuleA()
        };
        var enabled = new[] { "A", "NonExistent" };

        // Act & Assert
        var ex = Assert.Throws<ModuleLoadException>(() =>
            ModuleCatalog.BuildExecutionOrder(modules, enabled));

        Assert.Contains("NonExistent", ex.Message);
        Assert.Contains("не найден", ex.Message.ToLower());
    }

    [Fact]
    public void BuildExecutionOrder_MissingDependency_ThrowsWithClearMessage()
    {
        // Arrange: B требует A, но A не включен
        var modules = new Dictionary<string, IAppModule>(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = new ModuleA(),
            ["B"] = new ModuleB()
        };
        var enabled = new[] { "B" }; // A не включен, но B его требует

        // Act & Assert
        var ex = Assert.Throws<ModuleLoadException>(() =>
            ModuleCatalog.BuildExecutionOrder(modules, enabled));

        Assert.Contains("B", ex.Message);
        Assert.Contains("A", ex.Message);
        Assert.Contains("требует", ex.Message.ToLower());
    }

    [Fact]
    public void BuildExecutionOrder_MissingModule_MessageIsHumanReadable()
    {
        // Arrange
        var modules = new Dictionary<string, IAppModule>(StringComparer.OrdinalIgnoreCase);
        var enabled = new[] { "MissingModule" };

        // Act & Assert
        var ex = Assert.Throws<ModuleLoadException>(() =>
            ModuleCatalog.BuildExecutionOrder(modules, enabled));

        // Проверяем, что сообщение понятное и содержит имя модуля
        Assert.False(string.IsNullOrWhiteSpace(ex.Message), "Сообщение об ошибке не должно быть пустым");
        Assert.Contains("MissingModule", ex.Message);
        Assert.True(ex.Message.Length > 20, "Сообщение должно быть достаточно информативным");
    }

    #endregion

    #region Тесты ошибки циклических зависимостей

    [Fact]
    public void BuildExecutionOrder_DirectCycle_ThrowsWithClearMessage()
    {
        // Arrange: X <- Y <- X (прямой цикл)
        var modules = new Dictionary<string, IAppModule>(StringComparer.OrdinalIgnoreCase)
        {
            ["X"] = new CyclicModuleX(),
            ["Y"] = new CyclicModuleY()
        };
        var enabled = new[] { "X", "Y" };

        // Act & Assert
        var ex = Assert.Throws<ModuleLoadException>(() =>
            ModuleCatalog.BuildExecutionOrder(modules, enabled));

        Assert.Contains("цикл", ex.Message.ToLower());
    }

    [Fact]
    public void BuildExecutionOrder_IndirectCycle_ThrowsWithClearMessage()
    {
        // Arrange: P <- Q <- R <- P (цикл из трех модулей)
        var modules = new Dictionary<string, IAppModule>(StringComparer.OrdinalIgnoreCase)
        {
            ["P"] = new CyclicModuleP(),
            ["Q"] = new CyclicModuleQ(),
            ["R"] = new CyclicModuleR()
        };
        var enabled = new[] { "P", "Q", "R" };

        // Act & Assert
        var ex = Assert.Throws<ModuleLoadException>(() =>
            ModuleCatalog.BuildExecutionOrder(modules, enabled));

        Assert.Contains("цикл", ex.Message.ToLower());
    }

    [Fact]
    public void BuildExecutionOrder_CyclicDependency_MessageContainsProblematicModules()
    {
        // Arrange
        var modules = new Dictionary<string, IAppModule>(StringComparer.OrdinalIgnoreCase)
        {
            ["X"] = new CyclicModuleX(),
            ["Y"] = new CyclicModuleY()
        };
        var enabled = new[] { "X", "Y" };

        // Act & Assert
        var ex = Assert.Throws<ModuleLoadException>(() =>
            ModuleCatalog.BuildExecutionOrder(modules, enabled));

        // Проверяем, что сообщение содержит имена проблемных модулей
        Assert.True(ex.Message.Contains("X") || ex.Message.Contains("Y"),
            "Сообщение должно содержать имена модулей, образующих цикл");
    }

    #endregion
}
