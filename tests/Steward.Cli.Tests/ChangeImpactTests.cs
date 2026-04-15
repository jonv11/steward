using FluentAssertions;
using Steward.Core.Configuration;
using Steward.Core.Validation;
using Steward.Cli.Commands;
using Xunit;

namespace Steward.Cli.Tests;

public class ChangeImpactTests
{
    [Fact]
    public void ComputeImpactSignals_DiagInSource_ReportsImpact()
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
                        Source = "src/"
                    }
                ]
            }
        };

        var diagnostics = new List<Diagnostic>
        {
            new("STWD-008", DiagnosticSeverity.Warning, "broken-link",
                "src/Steward.Core/Foo.md", null, "Broken link", null, null)
        };

        var impacts = CheckCommand.ComputeImpactSignals(policy, diagnostics);

        impacts.Should().HaveCount(1);
        impacts[0].ArtifactId.Should().Be("structure");
        impacts[0].SourcePath.Should().Contain("src/");
    }

    [Fact]
    public void ComputeImpactSignals_DiagOutsideSource_NoImpact()
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
                        Source = "src/"
                    }
                ]
            }
        };

        var diagnostics = new List<Diagnostic>
        {
            new("STWD-008", DiagnosticSeverity.Warning, "broken-link",
                "docs/guide.md", null, "Broken link", null, null)
        };

        var impacts = CheckCommand.ComputeImpactSignals(policy, diagnostics);

        impacts.Should().BeEmpty();
    }

    [Fact]
    public void ComputeImpactSignals_NoMaintenance_ReturnsEmpty()
    {
        var policy = new RepositoryPolicy();
        var diagnostics = new List<Diagnostic>
        {
            new("STWD-001", DiagnosticSeverity.Error, "path-policy",
                "README.md", null, "Missing", null, null)
        };

        var impacts = CheckCommand.ComputeImpactSignals(policy, diagnostics);

        impacts.Should().BeEmpty();
    }
}
