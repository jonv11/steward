using FluentAssertions;
using Steward.Core.Abstractions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class FamilyTitlePatternRuleTests
{
    private static RepositoryPolicy MakePolicyWithTitlePattern(string titlePattern) => new()
    {
        ArtifactFamilies =
        [
            new ArtifactFamilyDefinition
            {
                Family = "adr",
                Match = new ArtifactFamilyMatch { PathPattern = "docs/adrs/*.md" },
                TitlePattern = titlePattern
            }
        ]
    };

    [Fact]
    public async Task Evaluate_H1MatchesPattern_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001-test.md",
            "---\ntype: adr\n---\n# ADR-001: Use PostgreSQL\n\nContent.\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithTitlePattern(@"^ADR-[0-9]{3}: .+"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001-test.md", 50, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyTitlePatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_H1ViolatesPattern_ReportsDiagnostic()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001-test.md",
            "---\ntype: adr\n---\n# Use PostgreSQL\n\nContent.\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithTitlePattern(@"^ADR-[0-9]{3}: .+"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001-test.md", 50, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyTitlePatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].RuleId.Should().Be("STWD-019");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostics[0].Message.Should().Contain("Use PostgreSQL");
        diagnostics[0].Message.Should().Contain("adr");
    }

    [Fact]
    public async Task Evaluate_PatternIsCaseSensitive()
    {
        var fs = new InMemoryFileSystem();
        // H1 uses uppercase "ADR" but pattern requires lowercase "adr"
        fs.AddFile("root/docs/adrs/ADR-001-test.md",
            "---\ntype: adr\n---\n# ADR-001: Title\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithTitlePattern(@"^adr-[0-9]{3}: .+"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001-test.md", 50, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyTitlePatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        // Should fail because matching is case-sensitive (unlike STWD-016 naming pattern)
        diagnostics.Should().HaveCount(1);
        diagnostics[0].RuleId.Should().Be("STWD-019");
    }

    [Fact]
    public async Task Evaluate_NoH1InDocument_Skipped()
    {
        var fs = new InMemoryFileSystem();
        // Document has only H2/H3 — no H1 at all
        fs.AddFile("root/docs/adrs/ADR-001-test.md",
            "---\ntype: adr\n---\n## Section\n\nContent.\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithTitlePattern(@"^ADR-[0-9]{3}: .+"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001-test.md", 50, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyTitlePatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_OnlyFrontmatterNoHeadings_Skipped()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001-test.md",
            "---\ntype: adr\n---\nJust prose, no headings.\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithTitlePattern(@"^ADR-[0-9]{3}: .+"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001-test.md", 50, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyTitlePatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_InvalidRegexPattern_EmitsConfigError()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001-test.md",
            "---\ntype: adr\n---\n# ADR-001: Title\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithTitlePattern(@"[invalid(regex"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001-test.md", 50, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyTitlePatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].RuleId.Should().Be("STWD-019");
        diagnostics[0].Category.Should().Be("config-error");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostics[0].Message.Should().Contain("[invalid(regex");
    }

    [Fact]
    public async Task Evaluate_FamilyWithoutTitlePattern_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001-test.md",
            "---\ntype: adr\n---\n# Anything goes here\n");

        var policy = new RepositoryPolicy
        {
            ArtifactFamilies =
            [
                new ArtifactFamilyDefinition
                {
                    Family = "adr",
                    Match = new ArtifactFamilyMatch { PathPattern = "docs/adrs/*.md" }
                    // No TitlePattern
                }
            ]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001-test.md", 50, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyTitlePatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_FileOutsideFamily_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/other/readme.md",
            "# readme\nContent.\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithTitlePattern(@"^ADR-[0-9]{3}: .+"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/other/readme.md", 20, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyTitlePatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_DirectoryEntry_Skipped()
    {
        var fs = new InMemoryFileSystem();

        var context = new ValidationContext
        {
            Policy = MakePolicyWithTitlePattern(@"^ADR-[0-9]{3}: .+"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/bad-name", 0, true)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyTitlePatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_MultipleFiles_ReportsEachViolation()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001-test.md",
            "---\ntype: adr\n---\n# ADR-001: Correct\n");
        fs.AddFile("root/docs/adrs/ADR-002-test.md",
            "---\ntype: adr\n---\n# Wrong Title\n");
        fs.AddFile("root/docs/adrs/ADR-003-test.md",
            "---\ntype: adr\n---\n# Also Wrong\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithTitlePattern(@"^ADR-[0-9]{3}: .+"),
            PathPolicy = null,
            TargetFiles =
            [
                new DiscoveredFile("docs/adrs/ADR-001-test.md", 50, false),
                new DiscoveredFile("docs/adrs/ADR-002-test.md", 50, false),
                new DiscoveredFile("docs/adrs/ADR-003-test.md", 50, false)
            ],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyTitlePatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(2);
        diagnostics.All(d => d.RuleId == "STWD-019").Should().BeTrue();
    }

    [Fact]
    public async Task Evaluate_PatternAppliedToH1NotH2()
    {
        var fs = new InMemoryFileSystem();
        // H2 matches the pattern but H1 does not — should still fail
        fs.AddFile("root/docs/adrs/ADR-001-test.md",
            "---\ntype: adr\n---\n# Wrong Title\n\n## ADR-001: Section\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithTitlePattern(@"^ADR-[0-9]{3}: .+"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001-test.md", 50, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyTitlePatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        // Violation because H1 "Wrong Title" doesn't match, even though H2 does
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Message.Should().Contain("Wrong Title");
    }

    [Fact]
    public async Task Evaluate_DiagnosticIncludesExpectedPatternInDetails()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001-test.md",
            "---\ntype: adr\n---\n# Bad Title\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithTitlePattern(@"^ADR-[0-9]{3}: .+"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001-test.md", 50, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyTitlePatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].Details.Should().ContainKey("expectedPattern");
        diagnostics[0].Details!["expectedPattern"].Should().Be(@"^ADR-[0-9]{3}: .+");
    }
}
