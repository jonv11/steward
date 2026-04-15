using FluentAssertions;
using Steward.Core.Configuration;
using Steward.Cli.Commands;
using Xunit;

namespace Steward.Cli.Tests;

public class StagedCompletenessTests
{
    [Fact]
    public void ComputeStagedCompleteness_SourceStagedArtifactNot_ReportsIncomplete()
    {
        var policy = new RepositoryPolicy
        {
            Maintenance = new MaintenanceConfig
            {
                Artifacts =
                [
                    new MaintenanceArtifactDef
                    {
                        Id = "structure",
                        Path = "STRUCTURE.md",
                        Type = "structure-document",
                        Source = "src"
                    }
                ]
            }
        };

        var staged = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "src/Foo.cs"
        };

        var result = CheckCommand.ComputeStagedCompleteness(policy, staged);

        result.Should().HaveCount(1);
        result[0].ArtifactId.Should().Be("structure");
        result[0].ArtifactPath.Should().Be("STRUCTURE.md");
    }

    [Fact]
    public void ComputeStagedCompleteness_BothStaged_NoIncomplete()
    {
        var policy = new RepositoryPolicy
        {
            Maintenance = new MaintenanceConfig
            {
                Artifacts =
                [
                    new MaintenanceArtifactDef
                    {
                        Id = "structure",
                        Path = "STRUCTURE.md",
                        Type = "structure-document",
                        Source = "src"
                    }
                ]
            }
        };

        var staged = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "src/Foo.cs",
            "STRUCTURE.md"
        };

        var result = CheckCommand.ComputeStagedCompleteness(policy, staged);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ComputeStagedCompleteness_NoSourceStaged_NoIncomplete()
    {
        var policy = new RepositoryPolicy
        {
            Maintenance = new MaintenanceConfig
            {
                Artifacts =
                [
                    new MaintenanceArtifactDef
                    {
                        Id = "structure",
                        Path = "STRUCTURE.md",
                        Type = "structure-document",
                        Source = "src"
                    }
                ]
            }
        };

        var staged = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "docs/guide.md"
        };

        var result = CheckCommand.ComputeStagedCompleteness(policy, staged);

        result.Should().BeEmpty();
    }
}
