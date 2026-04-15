using FluentAssertions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class RoleDefaultsTests
{
    [Theory]
    [InlineData("authoritative", "required")]
    [InlineData("requirements", "required")]
    [InlineData("state-document", "required")]
    [InlineData("generated", "recommended")]
    [InlineData("guide", "recommended")]
    [InlineData("changelog", "optional")]
    [InlineData("audit", "optional")]
    public void GetDefaultImportance_KnownRoles(string role, string expected)
    {
        RoleDefaults.GetDefaultImportance(role).Should().Be(expected);
    }

    [Fact]
    public void GetDefaultImportance_UnknownRole_ReturnsNull()
    {
        RoleDefaults.GetDefaultImportance("custom").Should().BeNull();
    }

    [Fact]
    public void GetDefaultFreshnessDays_StateDocument_Returns30()
    {
        RoleDefaults.GetDefaultFreshnessDays("state-document").Should().Be(30);
    }

    [Fact]
    public void GetDefaultFreshnessDays_NonFreshnessRole_ReturnsNull()
    {
        RoleDefaults.GetDefaultFreshnessDays("guide").Should().BeNull();
    }

    [Fact]
    public async Task RequiredArtifactRule_RoleLinkedImportance_AuthoritativeDefaultsToRequired()
    {
        // No explicit required or importance — role "authoritative" → required → Error
        var policy = new RepositoryPolicy
        {
            Artifacts = [new() { Path = "README.md", Role = "authoritative" }]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [],
            FileSystem = new InMemoryFileSystem(),
            RepositoryRoot = "root"
        };

        var rule = new RequiredArtifactRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task RequiredArtifactRule_RoleLinkedImportance_GeneratedDefaultsToRecommended()
    {
        var policy = new RepositoryPolicy
        {
            Artifacts = [new() { Path = "STRUCTURE.md", Role = "generated" }]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [],
            FileSystem = new InMemoryFileSystem(),
            RepositoryRoot = "root"
        };

        var rule = new RequiredArtifactRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Warning);
    }

    [Fact]
    public async Task RequiredArtifactRule_ExplicitImportanceOverridesRole()
    {
        // Role "authoritative" defaults to required, but explicit importance=optional overrides
        var policy = new RepositoryPolicy
        {
            Artifacts = [new() { Path = "README.md", Role = "authoritative", Importance = "optional" }]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [],
            FileSystem = new InMemoryFileSystem(),
            RepositoryRoot = "root"
        };

        var rule = new RequiredArtifactRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task FreshnessRule_StateDocumentRole_DefaultFreshness()
    {
        var fs = new InMemoryFileSystem();
        var now = DateTime.UtcNow;
        // File is 45 days old, role-linked default is 30 days
        fs.AddFile("/root/docs/status.md", "# Status", now.AddDays(-45));

        var policy = new RepositoryPolicy
        {
            Artifacts = [new() { Path = "docs/status.md", Role = "state-document" }]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/status.md", 50, false)],
            FileSystem = fs,
            RepositoryRoot = "/root"
        };

        var rule = new FreshnessRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].Message.Should().Contain("30 days");
    }

    [Fact]
    public async Task FreshnessRule_ExplicitFreshnessOverridesRoleDefault()
    {
        var fs = new InMemoryFileSystem();
        var now = DateTime.UtcNow;
        // File is 45 days old, explicit freshness set to 60 days → should pass
        fs.AddFile("/root/docs/status.md", "# Status", now.AddDays(-45));

        var policy = new RepositoryPolicy
        {
            Artifacts = [new()
            {
                Path = "docs/status.md",
                Role = "state-document",
                Freshness = new FreshnessConfig { MaxAgeDays = 60 }
            }]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/status.md", 50, false)],
            FileSystem = fs,
            RepositoryRoot = "/root"
        };

        var rule = new FreshnessRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }
}
