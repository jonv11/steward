using FluentAssertions;
using Steward.Core.Abstractions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class FamilyNamingPatternRuleTests
{
    private static RepositoryPolicy MakePolicyWithPattern(string namingPattern) => new()
    {
        ArtifactFamilies =
        [
            new ArtifactFamilyDefinition
            {
                Family = "adr",
                Match = new ArtifactFamilyMatch { PathPattern = "docs/adrs/*.md" },
                NamingPattern = namingPattern
            }
        ]
    };

    [Fact]
    public async Task Evaluate_FileMatchesPattern_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();

        var context = new ValidationContext
        {
            Policy = MakePolicyWithPattern(@"^ADR-\d{3}-"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001-use-postgres.md", 50, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyNamingPatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_FileViolatesPattern_ReportsDiagnostic()
    {
        var fs = new InMemoryFileSystem();

        var context = new ValidationContext
        {
            Policy = MakePolicyWithPattern(@"^ADR-\d{3}-"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/decision-use-postgres.md", 50, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyNamingPatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].RuleId.Should().Be("STWD-016");
        diagnostics[0].Message.Should().Contain("decision-use-postgres.md");
        diagnostics[0].Message.Should().Contain("adr");
    }

    [Fact]
    public async Task Evaluate_PatternMatchIsCaseInsensitive()
    {
        var fs = new InMemoryFileSystem();

        var context = new ValidationContext
        {
            Policy = MakePolicyWithPattern(@"^adr-\d{3}-"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001-decision.md", 50, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyNamingPatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_PatternAppliedToFilenameNotPath()
    {
        var fs = new InMemoryFileSystem();
        // Pattern would match the directory name "adrs" but not the filename "decision.md"
        var context = new ValidationContext
        {
            Policy = MakePolicyWithPattern(@"^ADR"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/decision.md", 50, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyNamingPatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        // Pattern is matched against filename only, not the full path
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Message.Should().Contain("decision.md");
    }

    [Fact]
    public async Task Evaluate_InvalidRegexPattern_Skipped()
    {
        var fs = new InMemoryFileSystem();

        var context = new ValidationContext
        {
            Policy = MakePolicyWithPattern(@"[invalid(regex"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001.md", 50, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyNamingPatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        // Invalid pattern is silently skipped — config validate catches it
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_ExplicitArtifact_PatternNotApplied()
    {
        var fs = new InMemoryFileSystem();

        var policy = new RepositoryPolicy
        {
            Artifacts =
            [
                new ArtifactDefinition { Path = "docs/adrs/legacy.md", Role = "governance", Importance = "required" }
            ],
            ArtifactFamilies =
            [
                new ArtifactFamilyDefinition
                {
                    Family = "adr",
                    Match = new ArtifactFamilyMatch { PathPattern = "docs/adrs/*.md" },
                    NamingPattern = @"^ADR-\d{3}-"
                }
            ]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/legacy.md", 50, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyNamingPatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_FileOutsideFamily_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();

        var context = new ValidationContext
        {
            Policy = MakePolicyWithPattern(@"^ADR-\d{3}-"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/other/readme.md", 20, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyNamingPatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_FamilyWithoutNamingPattern_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();

        var policy = new RepositoryPolicy
        {
            ArtifactFamilies =
            [
                new ArtifactFamilyDefinition
                {
                    Family = "adr",
                    Match = new ArtifactFamilyMatch { PathPattern = "docs/adrs/*.md" }
                    // No NamingPattern
                }
            ]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/whatever.md", 20, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyNamingPatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_MultipleFiles_ReportsEachViolation()
    {
        var fs = new InMemoryFileSystem();

        var context = new ValidationContext
        {
            Policy = MakePolicyWithPattern(@"^ADR-\d{3}-"),
            PathPolicy = null,
            TargetFiles =
            [
                new DiscoveredFile("docs/adrs/ADR-001-good.md", 50, false),
                new DiscoveredFile("docs/adrs/bad-name.md", 50, false),
                new DiscoveredFile("docs/adrs/also-bad.md", 50, false)
            ],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyNamingPatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(2);
        diagnostics.All(d => d.RuleId == "STWD-016").Should().BeTrue();
    }

    [Fact]
    public async Task Evaluate_DirectoryEntry_Skipped()
    {
        var fs = new InMemoryFileSystem();

        var context = new ValidationContext
        {
            Policy = MakePolicyWithPattern(@"^ADR-\d{3}-"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/bad-name", 0, true)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyNamingPatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }
}
