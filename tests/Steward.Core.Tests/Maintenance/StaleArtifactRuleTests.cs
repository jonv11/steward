using FluentAssertions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests.Maintenance;

public class StaleArtifactRuleTests
{
    [Fact]
    public async Task Evaluate_NoMaintenanceConfig_NoDiagnostics()
    {
        var policy = new RepositoryPolicy();
        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [],
            FileSystem = new InMemoryFileSystem(),
            RepositoryRoot = "/repo"
        };

        var rule = new StaleArtifactRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_StaleArtifact_ReportsWarning()
    {
        var fs = new InMemoryFileSystem();
        // Structure doc is missing — maintainer will want to create it
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
                        Type = "structure-document"
                    }
                ]
            }
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("README.md", 100, false)],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new StaleArtifactRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].RuleId.Should().Be("STWD-007");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostics[0].Message.Should().Contain("structure");
    }

    [Fact]
    public async Task Evaluate_FreshArtifact_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();
        var files = new[] { new DiscoveredFile("README.md", 100, false) };

        // Generate the expected content first
        var engine = new Core.Maintenance.MaintenanceEngine();
        var mCtx = new Core.Maintenance.MaintenanceContext
        {
            RepositoryRoot = "/repo",
            FileSystem = fs,
            Files = files.ToList()
        };

        var tempPolicy = new RepositoryPolicy
        {
            Maintenance = new MaintenanceConfig
            {
                Artifacts =
                [
                    new MaintenanceArtifactDef
                    {
                        Id = "structure",
                        Path = "STRUCTURE.md",
                        Type = "structure-document"
                    }
                ]
            }
        };

        var plan = engine.Evaluate(tempPolicy, mCtx);
        var expectedContent = plan.Actions[0].ExpectedContent!;
        fs.AddFile("/repo/STRUCTURE.md", expectedContent);

        var context = new ValidationContext
        {
            Policy = tempPolicy,
            PathPolicy = null,
            TargetFiles = files.ToList(),
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new StaleArtifactRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task RuleMetadata_IsCorrect()
    {
        var rule = new StaleArtifactRule();

        rule.RuleId.Should().Be("STWD-007");
        rule.Category.Should().Be("stale-artifact");
        rule.DefaultSeverity.Should().Be(DiagnosticSeverity.Warning);
    }

    [Fact]
    public void StaleArtifactRule_ImplementsIFixableRule()
    {
        var rule = new StaleArtifactRule();
        rule.Should().BeAssignableTo<IFixableRule>();
    }

    [Fact]
    public async Task ComputeFixes_StaleArtifact_ReturnsFix()
    {
        var fs = new InMemoryFileSystem();
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
                        Type = "structure-document"
                    }
                ]
            }
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("README.md", 100, false)],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new StaleArtifactRule();
        var fixes = await ((IFixableRule)rule).ComputeFixesAsync(context);

        fixes.Should().HaveCount(1);
        fixes[0].RuleId.Should().Be("STWD-007");
        fixes[0].FilePath.Should().Be("STRUCTURE.md");
        fixes[0].NewContent.Should().Contain("# Repository Structure");
    }

    [Fact]
    public async Task ComputeFixes_FreshArtifact_NoFixes()
    {
        var fs = new InMemoryFileSystem();
        var files = new[] { new DiscoveredFile("README.md", 100, false) };

        var engine = new Core.Maintenance.MaintenanceEngine();
        var mCtx = new Core.Maintenance.MaintenanceContext
        {
            RepositoryRoot = "/repo",
            FileSystem = fs,
            Files = files.ToList()
        };

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
                        Type = "structure-document"
                    }
                ]
            }
        };

        var plan = engine.Evaluate(policy, mCtx);
        fs.AddFile("/repo/STRUCTURE.md", plan.Actions[0].ExpectedContent!);

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = files.ToList(),
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new StaleArtifactRule();
        var fixes = await ((IFixableRule)rule).ComputeFixesAsync(context);

        fixes.Should().BeEmpty();
    }

    [Fact]
    public async Task ComputeFixes_NoMaintenance_NoFixes()
    {
        var context = new ValidationContext
        {
            Policy = new RepositoryPolicy(),
            PathPolicy = null,
            TargetFiles = [],
            FileSystem = new InMemoryFileSystem(),
            RepositoryRoot = "/repo"
        };

        var rule = new StaleArtifactRule();
        var fixes = await ((IFixableRule)rule).ComputeFixesAsync(context);

        fixes.Should().BeEmpty();
    }
}
