using FluentAssertions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Cli.Commands;
using Xunit;

namespace Steward.Cli.Tests;

public class GovernanceCoverageTests
{
    [Fact]
    public void ComputeCoverage_ArtifactFilesAreGoverned()
    {
        var policy = new RepositoryPolicy
        {
            Artifacts = [new() { Path = "README.md", Role = "authoritative" }]
        };

        var files = new List<DiscoveredFile>
        {
            new("README.md", 100, false),
            new("orphan.md", 50, false)
        };

        var result = StatusCommand.ComputeCoverage(policy, files);

        result.TotalMarkdownFiles.Should().Be(2);
        result.GovernedCount.Should().Be(1);
        result.Ungoverned.Should().Contain("orphan.md");
    }

    [Fact]
    public void ComputeCoverage_MaintenanceSourceCoversFiles()
    {
        var policy = new RepositoryPolicy
        {
            Maintenance = new MaintenanceConfig
            {
                Artifacts =
                [
                    new MaintenanceArtifactDef
                    {
                        Id = "idx",
                        Path = "docs/index.md",
                        Type = "index",
                        Source = "docs"
                    }
                ]
            }
        };

        var files = new List<DiscoveredFile>
        {
            new("docs/index.md", 100, false),
            new("docs/guide.md", 80, false),
            new("unrelated.md", 40, false)
        };

        var result = StatusCommand.ComputeCoverage(policy, files);

        result.GovernedCount.Should().Be(2);
        result.Ungoverned.Should().Contain("unrelated.md");
        result.Ungoverned.Should().NotContain("docs/guide.md");
    }

    [Fact]
    public void ComputeCoverage_StartHereIsGoverned()
    {
        var policy = new RepositoryPolicy
        {
            Governance = new GovernanceConfig { StartHere = ["getting-started.md"] }
        };

        var files = new List<DiscoveredFile>
        {
            new("getting-started.md", 50, false)
        };

        var result = StatusCommand.ComputeCoverage(policy, files);

        result.GovernedCount.Should().Be(1);
        result.Percentage.Should().Be(100);
    }

    [Fact]
    public void ComputeCoverage_NoMarkdownFiles_100Percent()
    {
        var policy = new RepositoryPolicy();
        var files = new List<DiscoveredFile>
        {
            new("src/Program.cs", 200, false)
        };

        var result = StatusCommand.ComputeCoverage(policy, files);

        result.TotalMarkdownFiles.Should().Be(0);
        result.Percentage.Should().Be(100);
    }
}
