using FluentAssertions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class NamingConventionRuleTests
{
    [Fact]
    public async Task Evaluate_MatchingName_NoDiagnostics()
    {
        var pathPolicy = new PathPolicyDocument
        {
            Rulesets =
            [
                new PathRuleSet
                {
                    Name = "rfcs",
                    Rules =
                    [
                        new PathRule
                        {
                            Pattern = "docs/rfcs/**/*.md",
                            Category = "recommended",
                            Priority = 10,
                            MustMatch = @"^RFC-\d{3}-.+\.md$"
                        }
                    ]
                }
            ]
        };

        var context = new ValidationContext
        {
            Policy = new RepositoryPolicy(),
            PathPolicy = pathPolicy,
            TargetFiles = [new DiscoveredFile("docs/rfcs/RFC-001-cli-structure.md", 100, false)],
            FileSystem = new InMemoryFileSystem(),
            RepositoryRoot = "root"
        };

        var rule = new NamingConventionRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_NonMatchingName_ReportsWarning()
    {
        var pathPolicy = new PathPolicyDocument
        {
            Rulesets =
            [
                new PathRuleSet
                {
                    Name = "rfcs",
                    Rules =
                    [
                        new PathRule
                        {
                            Pattern = "docs/rfcs/**/*.md",
                            Category = "recommended",
                            Priority = 10,
                            MustMatch = @"^RFC-\d{3}-.+\.md$"
                        }
                    ]
                }
            ]
        };

        var context = new ValidationContext
        {
            Policy = new RepositoryPolicy(),
            PathPolicy = pathPolicy,
            TargetFiles = [new DiscoveredFile("docs/rfcs/bad-name.md", 100, false)],
            FileSystem = new InMemoryFileSystem(),
            RepositoryRoot = "root"
        };

        var rule = new NamingConventionRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].RuleId.Should().Be("STWD-010");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostics[0].Message.Should().Contain("bad-name.md");
    }

    [Fact]
    public async Task Evaluate_FileOutsidePattern_NotChecked()
    {
        var pathPolicy = new PathPolicyDocument
        {
            Rulesets =
            [
                new PathRuleSet
                {
                    Name = "rfcs",
                    Rules =
                    [
                        new PathRule
                        {
                            Pattern = "docs/rfcs/**/*.md",
                            Category = "recommended",
                            Priority = 10,
                            MustMatch = @"^RFC-\d{3}-.+\.md$"
                        }
                    ]
                }
            ]
        };

        var context = new ValidationContext
        {
            Policy = new RepositoryPolicy(),
            PathPolicy = pathPolicy,
            TargetFiles = [new DiscoveredFile("docs/readme.md", 100, false)],
            FileSystem = new InMemoryFileSystem(),
            RepositoryRoot = "root"
        };

        var rule = new NamingConventionRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_NoMustMatch_NoDiagnostics()
    {
        var pathPolicy = new PathPolicyDocument
        {
            Rulesets =
            [
                new PathRuleSet
                {
                    Name = "test",
                    Rules =
                    [
                        new PathRule { Pattern = "**/*.md", Category = "recommended", Priority = 10 }
                    ]
                }
            ]
        };

        var context = new ValidationContext
        {
            Policy = new RepositoryPolicy(),
            PathPolicy = pathPolicy,
            TargetFiles = [new DiscoveredFile("any-name.md", 100, false)],
            FileSystem = new InMemoryFileSystem(),
            RepositoryRoot = "root"
        };

        var rule = new NamingConventionRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_NoPathPolicy_NoDiagnostics()
    {
        var context = new ValidationContext
        {
            Policy = new RepositoryPolicy(),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("any.md", 100, false)],
            FileSystem = new InMemoryFileSystem(),
            RepositoryRoot = "root"
        };

        var rule = new NamingConventionRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }
}
