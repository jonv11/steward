using FluentAssertions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class BrokenArtifactReferenceRuleTests
{
    [Fact]
    public async Task Evaluate_NoPolicy_NoDiagnostics()
    {
        var context = new ValidationContext
        {
            Policy = null,
            PathPolicy = null,
            TargetFiles = [],
            FileSystem = new InMemoryFileSystem(),
            RepositoryRoot = "/repo"
        };

        var rule = new BrokenArtifactReferenceRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_NoArtifacts_NoDiagnostics()
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

        var rule = new BrokenArtifactReferenceRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_OptionalArtifactMissing_ReportsWarning()
    {
        var policy = new RepositoryPolicy
        {
            Artifacts =
            [
                new ArtifactDefinition
                {
                    Path = "CHANGELOG.md",
                    Role = "changelog",
                    Required = false
                }
            ]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("README.md", 100, false)],
            FileSystem = new InMemoryFileSystem().AddFile("/repo/README.md", "# Hello"),
            RepositoryRoot = "/repo"
        };

        var rule = new BrokenArtifactReferenceRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].RuleId.Should().Be("STWD-009");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostics[0].Message.Should().Contain("CHANGELOG.md");
        diagnostics[0].Category.Should().Be("broken-reference");
    }

    [Fact]
    public async Task Evaluate_RequiredArtifactMissing_NotReported()
    {
        // Required missing artifacts are reported by STWD-001, not STWD-009
        var policy = new RepositoryPolicy
        {
            Artifacts =
            [
                new ArtifactDefinition
                {
                    Path = "README.md",
                    Role = "readme",
                    Required = true
                }
            ]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [],
            FileSystem = new InMemoryFileSystem(),
            RepositoryRoot = "/repo"
        };

        var rule = new BrokenArtifactReferenceRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_OptionalArtifactPresent_NoDiagnostics()
    {
        var policy = new RepositoryPolicy
        {
            Artifacts =
            [
                new ArtifactDefinition
                {
                    Path = "CHANGELOG.md",
                    Role = "changelog",
                    Required = false
                }
            ]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("CHANGELOG.md", 50, false)],
            FileSystem = new InMemoryFileSystem().AddFile("/repo/CHANGELOG.md", "# Changelog"),
            RepositoryRoot = "/repo"
        };

        var rule = new BrokenArtifactReferenceRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_MultipleOptionalMissing_ReportsAll()
    {
        var policy = new RepositoryPolicy
        {
            Artifacts =
            [
                new ArtifactDefinition { Path = "CHANGELOG.md", Role = "changelog", Required = false },
                new ArtifactDefinition { Path = "CONTRIBUTING.md", Role = "contributing", Required = false },
                new ArtifactDefinition { Path = "README.md", Role = "readme", Required = true }
            ]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [],
            FileSystem = new InMemoryFileSystem(),
            RepositoryRoot = "/repo"
        };

        var rule = new BrokenArtifactReferenceRule();
        var diagnostics = await rule.EvaluateAsync(context);

        // Only the 2 optional ones; required ones are covered by STWD-001
        diagnostics.Should().HaveCount(2);
        diagnostics.Should().OnlyContain(d => d.RuleId == "STWD-009");
    }
}
