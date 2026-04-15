using FluentAssertions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class FreshnessRuleTests
{
    [Fact]
    public async Task Evaluate_StaleFile_ReportsWarning()
    {
        var staleDate = DateTime.UtcNow.AddDays(-90);
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/docs/status.md", "# Status\nOld content.", staleDate);

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
        diagnostics[0].Path.Should().Be("docs/status.md");
        diagnostics[0].Message.Should().Contain("90 days old");
    }

    [Fact]
    public async Task Evaluate_FreshFile_NoDiagnostics()
    {
        var recentDate = DateTime.UtcNow.AddDays(-10);
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/docs/status.md", "# Status\nRecent content.", recentDate);

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

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_FrontmatterOverride_UsesLastUpdated()
    {
        // File system says stale, but frontmatter says recent
        var staleDate = DateTime.UtcNow.AddDays(-90);
        var recentDateStr = DateTime.UtcNow.AddDays(-5).ToString("yyyy-MM-dd");
        var content = $"---\ntitle: Status\nlast_updated: {recentDateStr}\n---\n# Status";

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
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_NoFreshnessConfig_NoDiagnostics()
    {
        var staleDate = DateTime.UtcNow.AddDays(-90);
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/docs/status.md", "# Status", staleDate);

        var policy = new RepositoryPolicy
        {
            Artifacts =
            [
                new ArtifactDefinition { Path = "docs/status.md" }
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

        diagnostics.Should().BeEmpty();
    }
}
