using FluentAssertions;
using Steward.Core.Abstractions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class FamilySectionPatternRuleTests
{
    private static RepositoryPolicy MakePolicyWithSectionPattern(string sectionPattern) => new()
    {
        ArtifactFamilies =
        [
            new ArtifactFamilyDefinition
            {
                Family = "adr",
                Match = new ArtifactFamilyMatch { PathPattern = "docs/adrs/*.md" },
                SectionPattern = sectionPattern
            }
        ]
    };

    [Fact]
    public async Task Evaluate_AllH2sMatchPattern_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001-test.md",
            "---\ntype: adr\n---\n# ADR-001: Title\n\n## Context\n\n## Decision\n\n## Status\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithSectionPattern(@"^[A-Z][A-Za-z ]+"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001-test.md", 100, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilySectionPatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_OneH2ViolatesPattern_ReportsDiagnostic()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001-test.md",
            "---\ntype: adr\n---\n# ADR-001: Title\n\n## context\n\n## Decision\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithSectionPattern(@"^[A-Z][A-Za-z ]+"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001-test.md", 80, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilySectionPatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].RuleId.Should().Be("STWD-020");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostics[0].Message.Should().Contain("context");
        diagnostics[0].Message.Should().Contain("adr");
    }

    [Fact]
    public async Task Evaluate_MultipleH2Violations_ReportsOnePerViolation()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001-test.md",
            "---\ntype: adr\n---\n# ADR-001: Title\n\n## context\n\n## decision\n\n## Status\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithSectionPattern(@"^[A-Z][A-Za-z ]+"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001-test.md", 100, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilySectionPatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(2);
        diagnostics.All(d => d.RuleId == "STWD-020").Should().BeTrue();
        diagnostics.Select(d => d.Message).Should().Contain(m => m.Contains("context"));
        diagnostics.Select(d => d.Message).Should().Contain(m => m.Contains("decision"));
    }

    [Fact]
    public async Task Evaluate_NoH2sInDocument_Skipped()
    {
        var fs = new InMemoryFileSystem();
        // Document has only an H1 — no H2 headings
        fs.AddFile("root/docs/adrs/ADR-001-test.md",
            "---\ntype: adr\n---\n# ADR-001: Title\n\nJust prose.\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithSectionPattern(@"^[A-Z][A-Za-z ]+"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001-test.md", 50, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilySectionPatternRule();
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
            Policy = MakePolicyWithSectionPattern(@"^[A-Z][A-Za-z ]+"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001-test.md", 40, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilySectionPatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_PatternIsCaseSensitive()
    {
        var fs = new InMemoryFileSystem();
        // H2 uses uppercase "Context" but pattern requires all-lowercase
        fs.AddFile("root/docs/adrs/ADR-001-test.md",
            "---\ntype: adr\n---\n# ADR-001: Title\n\n## Context\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithSectionPattern(@"^[a-z]+"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001-test.md", 50, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilySectionPatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].RuleId.Should().Be("STWD-020");
    }

    [Fact]
    public async Task Evaluate_InvalidRegexPattern_EmitsConfigError()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001-test.md",
            "---\ntype: adr\n---\n# ADR-001: Title\n\n## Context\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithSectionPattern(@"[invalid(regex"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001-test.md", 50, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilySectionPatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].RuleId.Should().Be("STWD-020");
        diagnostics[0].Category.Should().Be("config-error");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostics[0].Message.Should().Contain("[invalid(regex");
    }

    [Fact]
    public async Task Evaluate_FamilyWithoutSectionPattern_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001-test.md",
            "---\ntype: adr\n---\n# ADR-001: Title\n\n## anything goes\n");

        var policy = new RepositoryPolicy
        {
            ArtifactFamilies =
            [
                new ArtifactFamilyDefinition
                {
                    Family = "adr",
                    Match = new ArtifactFamilyMatch { PathPattern = "docs/adrs/*.md" }
                    // No SectionPattern
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

        var rule = new FamilySectionPatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_FileOutsideFamily_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/other/readme.md",
            "# readme\n\n## anything\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithSectionPattern(@"^[A-Z][A-Za-z ]+"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/other/readme.md", 30, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilySectionPatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_DirectoryEntry_Skipped()
    {
        var fs = new InMemoryFileSystem();

        var context = new ValidationContext
        {
            Policy = MakePolicyWithSectionPattern(@"^[A-Z][A-Za-z ]+"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/bad-name", 0, true)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilySectionPatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_DiagnosticIncludesLineNumber()
    {
        var fs = new InMemoryFileSystem();
        // H2 "bad heading" is on line 3 (no frontmatter, H1 on line 1, blank line 2, H2 on line 3)
        fs.AddFile("root/docs/adrs/ADR-001-test.md",
            "# ADR-001: Title\n\n## bad heading\n\nContent.\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithSectionPattern(@"^[A-Z][A-Za-z ]+"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001-test.md", 50, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilySectionPatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].Line.Should().NotBeNull();
        diagnostics[0].Line.Should().Be(3);
    }

    [Fact]
    public async Task Evaluate_DiagnosticIncludesExpectedPatternInDetails()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001-test.md",
            "---\ntype: adr\n---\n# ADR-001: Title\n\n## bad heading\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithSectionPattern(@"^[A-Z][A-Za-z ]+"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001-test.md", 50, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilySectionPatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].Details.Should().ContainKey("expectedPattern");
        diagnostics[0].Details!["expectedPattern"].Should().Be(@"^[A-Z][A-Za-z ]+");
    }

    [Fact]
    public async Task Evaluate_H1ViolationIgnored_OnlyH2sChecked()
    {
        var fs = new InMemoryFileSystem();
        // H1 "wrong title" violates the pattern, but all H2s match — no diagnostics expected
        fs.AddFile("root/docs/adrs/ADR-001-test.md",
            "---\ntype: adr\n---\n# wrong title\n\n## Context\n\n## Decision\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithSectionPattern(@"^[A-Z][A-Za-z ]+"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001-test.md", 80, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilySectionPatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_MultipleFiles_ReportsViolationsPerFile()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001-test.md",
            "---\ntype: adr\n---\n# ADR-001: Title\n\n## Context\n");
        fs.AddFile("root/docs/adrs/ADR-002-test.md",
            "---\ntype: adr\n---\n# ADR-002: Title\n\n## context\n");
        fs.AddFile("root/docs/adrs/ADR-003-test.md",
            "---\ntype: adr\n---\n# ADR-003: Title\n\n## decision\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithSectionPattern(@"^[A-Z][A-Za-z ]+"),
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

        var rule = new FamilySectionPatternRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(2);
        diagnostics.All(d => d.RuleId == "STWD-020").Should().BeTrue();
    }
}
