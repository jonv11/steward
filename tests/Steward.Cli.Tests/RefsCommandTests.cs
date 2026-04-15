using FluentAssertions;
using Steward.Core.Discovery;
using Steward.Cli.Commands;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Cli.Tests;

public class RefsCommandTests
{
    [Fact]
    public void BuildReferenceGraph_ExtractsLinks()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/root/README.md", "# Root\n[Guide](docs/guide.md)\n[Other](docs/other.md)");
        fs.AddFile("/root/docs/guide.md", "# Guide\n[Back](../README.md)");
        fs.AddFile("/root/docs/other.md", "# Other");

        var files = new List<DiscoveredFile>
        {
            new("README.md", 50, false),
            new("docs/guide.md", 30, false),
            new("docs/other.md", 20, false)
        };

        var graph = RefsCommand.BuildReferenceGraph(files, fs, "/root");

        graph.Should().ContainKey("README.md");
        graph["README.md"].Should().Contain("docs/guide.md");
        graph["README.md"].Should().Contain("docs/other.md");
        graph.Should().ContainKey("docs/guide.md");
        graph["docs/guide.md"].Should().Contain("README.md");
    }

    [Fact]
    public void GetOutbound_ReturnsTargets()
    {
        var graph = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["README.md"] = ["docs/a.md", "docs/b.md"]
        };

        var result = RefsCommand.GetOutbound(graph, "README.md");

        result.Should().HaveCount(2);
        result.Should().Contain("docs/a.md");
    }

    [Fact]
    public void GetInbound_ReturnsSources()
    {
        var graph = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["README.md"] = ["docs/guide.md"],
            ["docs/index.md"] = ["docs/guide.md"]
        };

        var result = RefsCommand.GetInbound(graph, "docs/guide.md");

        result.Should().HaveCount(2);
        result.Should().Contain("README.md");
        result.Should().Contain("docs/index.md");
    }

    [Fact]
    public void GetInbound_NoneFound_ReturnsEmpty()
    {
        var graph = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["README.md"] = ["docs/guide.md"]
        };

        var result = RefsCommand.GetInbound(graph, "orphan.md");

        result.Should().BeEmpty();
    }
}
