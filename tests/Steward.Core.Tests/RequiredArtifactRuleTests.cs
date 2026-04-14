using FluentAssertions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class RequiredArtifactRuleTests
{
    [Fact]
    public async Task Evaluate_MissingRequired_ReportsError()
    {
        var policy = new RepositoryPolicy
        {
            Artifacts =
            [
                new() { Path = "README.md", Role = "authoritative", Required = true }
            ]
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
        diagnostics[0].RuleId.Should().Be("STWD-001");
        diagnostics[0].Message.Should().Contain("README.md");
    }

    [Fact]
    public async Task Evaluate_PresentRequired_NoDiagnostics()
    {
        var policy = new RepositoryPolicy
        {
            Artifacts =
            [
                new() { Path = "README.md", Role = "authoritative", Required = true }
            ]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("README.md", 100, false)],
            FileSystem = new InMemoryFileSystem(),
            RepositoryRoot = "root"
        };

        var rule = new RequiredArtifactRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_OptionalMissing_NoDiagnostics()
    {
        var policy = new RepositoryPolicy
        {
            Artifacts =
            [
                new() { Path = "CHANGELOG.md", Role = "changelog", Required = false }
            ]
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
    public async Task Evaluate_NullArtifacts_NoDiagnostics()
    {
        var policy = new RepositoryPolicy { Artifacts = null };

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
    public async Task Evaluate_CaseInsensitiveLookup()
    {
        var policy = new RepositoryPolicy
        {
            Artifacts =
            [
                new() { Path = "readme.md", Role = "authoritative", Required = true }
            ]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("README.md", 100, false)],
            FileSystem = new InMemoryFileSystem(),
            RepositoryRoot = "root"
        };

        var rule = new RequiredArtifactRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }
}
