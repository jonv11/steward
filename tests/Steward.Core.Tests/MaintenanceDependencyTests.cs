using FluentAssertions;
using Steward.Core.Maintenance;
using Xunit;

namespace Steward.Core.Tests;

public class MaintenanceDependencyTests
{
    [Fact]
    public void TopologicalSort_ProcessesDependenciesFirst()
    {
        var configs = new List<MaintenanceArtifactConfig>
        {
            new() { Id = "b", Path = "b.md", Type = "index", DependsOn = ["a"] },
            new() { Id = "a", Path = "a.md", Type = "index" }
        };

        var result = MaintenanceEngine.TopologicalSort(configs, out var cyclic);

        cyclic.Should().BeEmpty();
        result.Should().HaveCount(2);
        result[0].Id.Should().Be("a");
        result[1].Id.Should().Be("b");
    }

    [Fact]
    public void TopologicalSort_DetectsCircularDependency()
    {
        var configs = new List<MaintenanceArtifactConfig>
        {
            new() { Id = "a", Path = "a.md", Type = "index", DependsOn = ["b"] },
            new() { Id = "b", Path = "b.md", Type = "index", DependsOn = ["a"] }
        };

        var result = MaintenanceEngine.TopologicalSort(configs, out var cyclic);

        cyclic.Should().NotBeEmpty();
    }

    [Fact]
    public void TopologicalSort_NoDependencies_PreservesOrder()
    {
        var configs = new List<MaintenanceArtifactConfig>
        {
            new() { Id = "x", Path = "x.md", Type = "index" },
            new() { Id = "y", Path = "y.md", Type = "index" },
            new() { Id = "z", Path = "z.md", Type = "index" }
        };

        var result = MaintenanceEngine.TopologicalSort(configs, out var cyclic);

        cyclic.Should().BeEmpty();
        result.Should().HaveCount(3);
        result[0].Id.Should().Be("x");
        result[1].Id.Should().Be("y");
        result[2].Id.Should().Be("z");
    }

    [Fact]
    public void TopologicalSort_ChainedDependencies()
    {
        var configs = new List<MaintenanceArtifactConfig>
        {
            new() { Id = "c", Path = "c.md", Type = "index", DependsOn = ["b"] },
            new() { Id = "b", Path = "b.md", Type = "index", DependsOn = ["a"] },
            new() { Id = "a", Path = "a.md", Type = "index" }
        };

        var result = MaintenanceEngine.TopologicalSort(configs, out var cyclic);

        cyclic.Should().BeEmpty();
        result.Should().HaveCount(3);
        result[0].Id.Should().Be("a");
        result[1].Id.Should().Be("b");
        result[2].Id.Should().Be("c");
    }
}
