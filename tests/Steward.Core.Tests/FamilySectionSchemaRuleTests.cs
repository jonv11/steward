using FluentAssertions;
using Steward.Core.Abstractions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class FamilySectionSchemaRuleTests
{
    private static ArtifactFamilyDefinition MakeFamily(
        List<SectionSchemaEntry> sections,
        string? headingMatch = null,
        bool enforceOrder = false,
        bool allowExtra = true) => new()
    {
        Family = "adr",
        Match = new ArtifactFamilyMatch { PathPattern = "docs/adrs/*.md" },
        SectionSchema = new SectionSchemaConfig
        {
            Sections = sections,
            HeadingMatch = headingMatch,
            EnforceOrder = enforceOrder,
            AllowExtra = allowExtra
        }
    };

    private static RepositoryPolicy MakePolicy(ArtifactFamilyDefinition family) => new()
    {
        ArtifactFamilies = [family]
    };

    private static ValidationContext MakeContext(
        RepositoryPolicy policy,
        InMemoryFileSystem fs,
        string filePath) => new()
    {
        Policy = policy,
        PathPolicy = null,
        TargetFiles = [new DiscoveredFile(filePath, 100, false)],
        FileSystem = fs,
        RepositoryRoot = "root"
    };

    // ── Required section presence ──────────────────────────────────────────────

    [Fact]
    public async Task Evaluate_RequiredSectionPresent_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "# Title\n\n## Context\n\n## Decision\n\n## Consequences\n");

        var family = MakeFamily([
            new SectionSchemaEntry { Heading = "Context", Required = true },
            new SectionSchemaEntry { Heading = "Decision", Required = true },
            new SectionSchemaEntry { Heading = "Consequences", Required = true }
        ]);

        var diagnostics = await new FamilySectionSchemaRule().EvaluateAsync(
            MakeContext(MakePolicy(family), fs, "docs/adrs/ADR-001.md"));

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_RequiredSectionMissing_EmitsDiagnostic()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "# Title\n\n## Context\n\n## Consequences\n");

        var family = MakeFamily([
            new SectionSchemaEntry { Heading = "Context", Required = true },
            new SectionSchemaEntry { Heading = "Decision", Required = true },
            new SectionSchemaEntry { Heading = "Consequences", Required = true }
        ]);

        var diagnostics = await new FamilySectionSchemaRule().EvaluateAsync(
            MakeContext(MakePolicy(family), fs, "docs/adrs/ADR-001.md"));

        diagnostics.Should().HaveCount(1);
        diagnostics[0].RuleId.Should().Be("STWD-021");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostics[0].Message.Should().Contain("Decision");
        diagnostics[0].Message.Should().Contain("adr");
    }

    [Fact]
    public async Task Evaluate_MultipleRequiredMissing_EmitsOnePerMissing()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "# Title\n\n## Context\n");

        var family = MakeFamily([
            new SectionSchemaEntry { Heading = "Context", Required = true },
            new SectionSchemaEntry { Heading = "Decision", Required = true },
            new SectionSchemaEntry { Heading = "Consequences", Required = true }
        ]);

        var diagnostics = await new FamilySectionSchemaRule().EvaluateAsync(
            MakeContext(MakePolicy(family), fs, "docs/adrs/ADR-001.md"));

        diagnostics.Should().HaveCount(2);
        diagnostics.All(d => d.RuleId == "STWD-021").Should().BeTrue();
        diagnostics.Select(d => d.Message).Should().Contain(m => m.Contains("Decision"));
        diagnostics.Select(d => d.Message).Should().Contain(m => m.Contains("Consequences"));
    }

    // ── Optional sections ──────────────────────────────────────────────────────

    [Fact]
    public async Task Evaluate_OptionalSectionMissing_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "# Title\n\n## Context\n\n## Decision\n");

        var family = MakeFamily([
            new SectionSchemaEntry { Heading = "Context", Required = true },
            new SectionSchemaEntry { Heading = "Decision", Required = true },
            new SectionSchemaEntry { Heading = "Consequences", Required = false }
        ]);

        var diagnostics = await new FamilySectionSchemaRule().EvaluateAsync(
            MakeContext(MakePolicy(family), fs, "docs/adrs/ADR-001.md"));

        diagnostics.Should().BeEmpty();
    }

    // ── allow_extra ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Evaluate_AllowExtraFalse_UnlistedSectionEmitsDiagnostic()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "# Title\n\n## Context\n\n## Decision\n\n## Appendix\n");

        var family = MakeFamily(
            sections: [
                new SectionSchemaEntry { Heading = "Context", Required = true },
                new SectionSchemaEntry { Heading = "Decision", Required = true }
            ],
            allowExtra: false);

        var diagnostics = await new FamilySectionSchemaRule().EvaluateAsync(
            MakeContext(MakePolicy(family), fs, "docs/adrs/ADR-001.md"));

        diagnostics.Should().HaveCount(1);
        diagnostics[0].RuleId.Should().Be("STWD-021");
        diagnostics[0].Message.Should().Contain("Appendix");
        diagnostics[0].Message.Should().Contain("not defined");
    }

    [Fact]
    public async Task Evaluate_AllowExtraTrue_UnlistedSectionNoDiagnostic()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "# Title\n\n## Context\n\n## Decision\n\n## Appendix\n");

        var family = MakeFamily(
            sections: [
                new SectionSchemaEntry { Heading = "Context", Required = true },
                new SectionSchemaEntry { Heading = "Decision", Required = true }
            ],
            allowExtra: true);

        var diagnostics = await new FamilySectionSchemaRule().EvaluateAsync(
            MakeContext(MakePolicy(family), fs, "docs/adrs/ADR-001.md"));

        diagnostics.Should().BeEmpty();
    }

    // ── enforce_order ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Evaluate_EnforceOrderCorrectOrder_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "# Title\n\n## Context\n\n## Decision\n\n## Consequences\n");

        var family = MakeFamily(
            sections: [
                new SectionSchemaEntry { Heading = "Context", Required = true },
                new SectionSchemaEntry { Heading = "Decision", Required = true },
                new SectionSchemaEntry { Heading = "Consequences", Required = true }
            ],
            enforceOrder: true);

        var diagnostics = await new FamilySectionSchemaRule().EvaluateAsync(
            MakeContext(MakePolicy(family), fs, "docs/adrs/ADR-001.md"));

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_EnforceOrderWrongOrder_EmitsDiagnostic()
    {
        var fs = new InMemoryFileSystem();
        // Decision before Context violates schema order
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "# Title\n\n## Decision\n\n## Context\n\n## Consequences\n");

        var family = MakeFamily(
            sections: [
                new SectionSchemaEntry { Heading = "Context", Required = true },
                new SectionSchemaEntry { Heading = "Decision", Required = true },
                new SectionSchemaEntry { Heading = "Consequences", Required = true }
            ],
            enforceOrder: true);

        var diagnostics = await new FamilySectionSchemaRule().EvaluateAsync(
            MakeContext(MakePolicy(family), fs, "docs/adrs/ADR-001.md"));

        diagnostics.Should().HaveCount(1);
        diagnostics[0].RuleId.Should().Be("STWD-021");
        diagnostics[0].Message.Should().Contain("Context");
        diagnostics[0].Message.Should().Contain("out of order");
    }

    [Fact]
    public async Task Evaluate_EnforceOrderFalseWrongOrder_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "# Title\n\n## Decision\n\n## Context\n\n## Consequences\n");

        var family = MakeFamily(
            sections: [
                new SectionSchemaEntry { Heading = "Context", Required = true },
                new SectionSchemaEntry { Heading = "Decision", Required = true },
                new SectionSchemaEntry { Heading = "Consequences", Required = true }
            ],
            enforceOrder: false);

        var diagnostics = await new FamilySectionSchemaRule().EvaluateAsync(
            MakeContext(MakePolicy(family), fs, "docs/adrs/ADR-001.md"));

        diagnostics.Should().BeEmpty();
    }

    // ── heading_match ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Evaluate_ContainsMatch_NumberedSectionMatches()
    {
        var fs = new InMemoryFileSystem();
        // RFC-style numbered sections — "1. Context" should match schema "Context" with contains
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "# Title\n\n## 1. Context\n\n## 2. Decision\n\n## 3. Consequences\n");

        var family = MakeFamily(
            sections: [
                new SectionSchemaEntry { Heading = "Context", Required = true },
                new SectionSchemaEntry { Heading = "Decision", Required = true },
                new SectionSchemaEntry { Heading = "Consequences", Required = true }
            ],
            headingMatch: "contains");

        var diagnostics = await new FamilySectionSchemaRule().EvaluateAsync(
            MakeContext(MakePolicy(family), fs, "docs/adrs/ADR-001.md"));

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_ExactMatch_NumberedSectionDoesNotMatch()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "# Title\n\n## 1. Context\n\n## 2. Decision\n");

        var family = MakeFamily(
            sections: [
                new SectionSchemaEntry { Heading = "Context", Required = true },
                new SectionSchemaEntry { Heading = "Decision", Required = true }
            ],
            headingMatch: "exact");

        var diagnostics = await new FamilySectionSchemaRule().EvaluateAsync(
            MakeContext(MakePolicy(family), fs, "docs/adrs/ADR-001.md"));

        // "1. Context" != "Context" with exact match → both required sections flagged as missing
        diagnostics.Should().HaveCount(2);
        diagnostics.All(d => d.RuleId == "STWD-021").Should().BeTrue();
    }

    [Fact]
    public async Task Evaluate_DefaultMatchIsContains()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "# Title\n\n## 1. Context\n\n## 2. Decision\n");

        // No heading_match specified — should default to contains
        var family = MakeFamily(
            sections: [
                new SectionSchemaEntry { Heading = "Context", Required = true },
                new SectionSchemaEntry { Heading = "Decision", Required = true }
            ]);

        var diagnostics = await new FamilySectionSchemaRule().EvaluateAsync(
            MakeContext(MakePolicy(family), fs, "docs/adrs/ADR-001.md"));

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_ContainsMatchIsCaseInsensitive()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "# Title\n\n## CONTEXT\n\n## decision\n");

        var family = MakeFamily(
            sections: [
                new SectionSchemaEntry { Heading = "Context", Required = true },
                new SectionSchemaEntry { Heading = "Decision", Required = true }
            ],
            headingMatch: "contains");

        var diagnostics = await new FamilySectionSchemaRule().EvaluateAsync(
            MakeContext(MakePolicy(family), fs, "docs/adrs/ADR-001.md"));

        diagnostics.Should().BeEmpty();
    }

    // ── Edge cases ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Evaluate_NoH2sInDocument_NoDiagnosticsForMissingSections()
    {
        var fs = new InMemoryFileSystem();
        // No H2 headings — required section checks skip (document skeleton not yet written)
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "# Title\n\nJust prose.\n");

        var family = MakeFamily([
            new SectionSchemaEntry { Heading = "Context", Required = true }
        ]);

        var diagnostics = await new FamilySectionSchemaRule().EvaluateAsync(
            MakeContext(MakePolicy(family), fs, "docs/adrs/ADR-001.md"));

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_NoSectionSchema_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "# Title\n\n## anything goes\n");

        var policy = new RepositoryPolicy
        {
            ArtifactFamilies =
            [
                new ArtifactFamilyDefinition
                {
                    Family = "adr",
                    Match = new ArtifactFamilyMatch { PathPattern = "docs/adrs/*.md" }
                    // No SectionSchema
                }
            ]
        };

        var diagnostics = await new FamilySectionSchemaRule().EvaluateAsync(
            MakeContext(policy, fs, "docs/adrs/ADR-001.md"));

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_FileOutsideFamily_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/other/readme.md",
            "# Title\n\n## anything\n");

        var family = MakeFamily([
            new SectionSchemaEntry { Heading = "Context", Required = true }
        ]);

        var diagnostics = await new FamilySectionSchemaRule().EvaluateAsync(
            MakeContext(MakePolicy(family), fs, "docs/other/readme.md"));

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_DirectoryEntry_Skipped()
    {
        var fs = new InMemoryFileSystem();

        var policy = MakePolicy(MakeFamily([
            new SectionSchemaEntry { Heading = "Context", Required = true }
        ]));

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/something", 0, true)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var diagnostics = await new FamilySectionSchemaRule().EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_MissingRequiredSection_DiagnosticHasNoLineNumber()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "# Title\n\n## Context\n");

        var family = MakeFamily([
            new SectionSchemaEntry { Heading = "Context", Required = true },
            new SectionSchemaEntry { Heading = "Decision", Required = true }
        ]);

        var diagnostics = await new FamilySectionSchemaRule().EvaluateAsync(
            MakeContext(MakePolicy(family), fs, "docs/adrs/ADR-001.md"));

        diagnostics.Should().HaveCount(1);
        diagnostics[0].Line.Should().BeNull();
    }

    [Fact]
    public async Task Evaluate_AllowExtraFalseDiagnosticIncludesLineNumber()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "# Title\n\n## Context\n\n## Appendix\n");

        var family = MakeFamily(
            sections: [new SectionSchemaEntry { Heading = "Context", Required = true }],
            allowExtra: false);

        var diagnostics = await new FamilySectionSchemaRule().EvaluateAsync(
            MakeContext(MakePolicy(family), fs, "docs/adrs/ADR-001.md"));

        diagnostics.Should().HaveCount(1);
        diagnostics[0].Line.Should().NotBeNull();
    }

    [Fact]
    public async Task Evaluate_EnforceOrderDiagnosticIncludesLineNumber()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "# Title\n\n## Decision\n\n## Context\n");

        var family = MakeFamily(
            sections: [
                new SectionSchemaEntry { Heading = "Context", Required = true },
                new SectionSchemaEntry { Heading = "Decision", Required = true }
            ],
            enforceOrder: true);

        var diagnostics = await new FamilySectionSchemaRule().EvaluateAsync(
            MakeContext(MakePolicy(family), fs, "docs/adrs/ADR-001.md"));

        // "Context" appears after "Decision" but schema says Context comes first
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Line.Should().NotBeNull();
    }
}
