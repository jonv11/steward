using FluentAssertions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class ForbiddenPathRuleTests
{
    [Fact]
    public async Task Evaluate_ForbiddenPathPresent_ReportsError()
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
                        new PathRule { Pattern = "**/node_modules/**", Category = "forbidden", Priority = 100 }
                    ]
                }
            ]
        };

        var context = new ValidationContext
        {
            Policy = new RepositoryPolicy(),
            PathPolicy = pathPolicy,
            TargetFiles =
            [
                new DiscoveredFile("src/app.js", 100, false),
                new DiscoveredFile("node_modules/pkg/index.js", 50, false)
            ],
            FileSystem = new InMemoryFileSystem(),
            RepositoryRoot = "root"
        };

        var rule = new ForbiddenPathRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Error);
        diagnostics[0].Path.Should().Be("node_modules/pkg/index.js");
        diagnostics[0].RuleId.Should().Be("STWD-002");
    }

    [Fact]
    public async Task Evaluate_NoForbiddenPaths_NoDiagnostics()
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
                        new PathRule { Pattern = "**/bin/**", Category = "forbidden", Priority = 100 }
                    ]
                }
            ]
        };

        var context = new ValidationContext
        {
            Policy = new RepositoryPolicy(),
            PathPolicy = pathPolicy,
            TargetFiles =
            [
                new DiscoveredFile("src/app.cs", 100, false)
            ],
            FileSystem = new InMemoryFileSystem(),
            RepositoryRoot = "root"
        };

        var rule = new ForbiddenPathRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_NullPathPolicy_NoDiagnostics()
    {
        var context = new ValidationContext
        {
            Policy = new RepositoryPolicy(),
            PathPolicy = null,
            TargetFiles =
            [
                new DiscoveredFile("anything.txt", 10, false)
            ],
            FileSystem = new InMemoryFileSystem(),
            RepositoryRoot = "root"
        };

        var rule = new ForbiddenPathRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_MultipleForbiddenMatches_ReportsAll()
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
                        new PathRule { Pattern = "**/*.exe", Category = "forbidden", Priority = 100 },
                        new PathRule { Pattern = "**/*.dll", Category = "forbidden", Priority = 100 }
                    ]
                }
            ]
        };

        var context = new ValidationContext
        {
            Policy = new RepositoryPolicy(),
            PathPolicy = pathPolicy,
            TargetFiles =
            [
                new DiscoveredFile("build/app.exe", 1000, false),
                new DiscoveredFile("lib/core.dll", 2000, false),
                new DiscoveredFile("src/main.cs", 500, false)
            ],
            FileSystem = new InMemoryFileSystem(),
            RepositoryRoot = "root"
        };

        var rule = new ForbiddenPathRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(2);
    }
}
