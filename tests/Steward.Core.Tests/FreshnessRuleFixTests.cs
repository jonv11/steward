using FluentAssertions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class FreshnessRuleFixTests
{
    [Fact]
    public async Task Evaluate_MessageContainsArtifactPath()
    {
        var staleDate = DateTime.UtcNow.AddDays(-90);
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/docs/status.md", "# Status", staleDate);

        var policy = new RepositoryPolicy
        {
            Artifacts =
            [
                new ArtifactDefinition
                {
                    Path = "docs/status.md",
                    Freshness = new FreshnessConfig { MaxAgeDays = 60 }
                }
            ]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/status.md", 100, false)],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new FreshnessRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Message.Should().Contain("docs/status.md",
            because: "STWD-012 message must include the artifact path for self-containment");
    }

    [Fact]
    public async Task Evaluate_MessageContainsRoleWhenPresent()
    {
        var staleDate = DateTime.UtcNow.AddDays(-90);
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/docs/status.md", "# Status", staleDate);

        var policy = new RepositoryPolicy
        {
            Artifacts =
            [
                new ArtifactDefinition
                {
                    Path = "docs/status.md",
                    Role = "status",
                    Freshness = new FreshnessConfig { MaxAgeDays = 60 }
                }
            ]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/status.md", 100, false)],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new FreshnessRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Message.Should().Contain("role: status");
    }

    [Fact]
    public async Task Evaluate_RemediationIncludesTodayDateAndFixCommand()
    {
        var staleDate = DateTime.UtcNow.AddDays(-90);
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/docs/status.md", "# Status", staleDate);

        var policy = new RepositoryPolicy
        {
            Artifacts =
            [
                new ArtifactDefinition
                {
                    Path = "docs/status.md",
                    Freshness = new FreshnessConfig { MaxAgeDays = 60 }
                }
            ]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/status.md", 100, false)],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new FreshnessRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().ContainSingle();
        var remediation = diagnostics[0].Remediation!;
        remediation.Should().Contain("last_updated",
            because: "remediation must tell users what to update");
        remediation.Should().Contain("steward check --fix",
            because: "remediation should mention the auto-fix command");
        remediation.Should().Contain(DateTime.UtcNow.ToString("yyyy-MM-dd"),
            because: "remediation should include the current date as the value to set");
    }

    [Fact]
    public async Task ComputeFixesAsync_StaleFile_ProducesLastUpdatedFix()
    {
        var staleDate = DateTime.UtcNow.AddDays(-90);
        var content = "---\ntitle: Status\n---\n# Status\nOld content.";
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/docs/status.md", content, staleDate);

        var policy = new RepositoryPolicy
        {
            Artifacts =
            [
                new ArtifactDefinition
                {
                    Path = "docs/status.md",
                    Freshness = new FreshnessConfig { MaxAgeDays = 60 }
                }
            ]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/status.md", 100, false)],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new FreshnessRule();
        var fixes = await rule.ComputeFixesAsync(context);

        fixes.Should().ContainSingle();
        fixes[0].RuleId.Should().Be("STWD-012");
        fixes[0].FilePath.Should().Be("docs/status.md");
        fixes[0].NewContent.Should().Contain($"last_updated: {DateTime.UtcNow:yyyy-MM-dd}");
    }

    [Fact]
    public async Task ComputeFixesAsync_FileWithNoFrontmatter_CreatesNewFrontmatterWithLastUpdated()
    {
        var staleDate = DateTime.UtcNow.AddDays(-90);
        var content = "# Status\nNo frontmatter here.";
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/docs/status.md", content, staleDate);

        var policy = new RepositoryPolicy
        {
            Artifacts =
            [
                new ArtifactDefinition
                {
                    Path = "docs/status.md",
                    Freshness = new FreshnessConfig { MaxAgeDays = 60 }
                }
            ]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/status.md", 100, false)],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new FreshnessRule();
        var fixes = await rule.ComputeFixesAsync(context);

        fixes.Should().ContainSingle();
        fixes[0].NewContent.Should().Contain("---");
        fixes[0].NewContent.Should().Contain("last_updated");
        fixes[0].NewContent.Should().Contain(DateTime.UtcNow.ToString("yyyy-MM-dd"));
    }

    [Fact]
    public async Task Evaluate_FutureDatedLastUpdated_ReportsWarning()
    {
        var futureDateStr = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd");
        var content = $"---\ntitle: Status\nlast_updated: {futureDateStr}\n---\n# Status";
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/docs/status.md", content);

        var policy = new RepositoryPolicy
        {
            Artifacts =
            [
                new ArtifactDefinition
                {
                    Path = "docs/status.md",
                    Freshness = new FreshnessConfig { MaxAgeDays = 60 }
                }
            ]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/status.md", 100, false)],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new FreshnessRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().ContainSingle();
        diagnostics[0].RuleId.Should().Be("STWD-012");
        diagnostics[0].Message.Should().Contain("future date");
    }

    [Fact]
    public async Task ComputeFixesAsync_FreshFile_NoFixes()
    {
        var recentDate = DateTime.UtcNow.AddDays(-10);
        var content = "# Status\nRecent content.";
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/docs/status.md", content, recentDate);

        var policy = new RepositoryPolicy
        {
            Artifacts =
            [
                new ArtifactDefinition
                {
                    Path = "docs/status.md",
                    Freshness = new FreshnessConfig { MaxAgeDays = 60 }
                }
            ]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/status.md", 100, false)],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new FreshnessRule();
        var fixes = await rule.ComputeFixesAsync(context);

        fixes.Should().BeEmpty();
    }

    [Fact]
    public async Task ImplementsIFixableRule()
    {
        var rule = new FreshnessRule();
        rule.Should().BeAssignableTo<IFixableRule>(
            because: "STWD-012 should implement IFixableRule for last_updated auto-fix");
    }
}
