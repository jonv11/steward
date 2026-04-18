using FluentAssertions;
using Steward.Core.Abstractions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class RequiredSectionsRuleTests
{
    private static RepositoryPolicy MakePolicyWithSections(params string[] sections) => new()
    {
        ArtifactFamilies =
        [
            new ArtifactFamilyDefinition
            {
                Family = "adr",
                Match = new ArtifactFamilyMatch { PathPattern = "docs/adrs/ADR-*.md" },
                RequiredSections = [.. sections]
            }
        ]
    };

    [Fact]
    public async Task Evaluate_AllSectionsPresent_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md", "# ADR-001\n\n## Context\n\nSome context.\n\n## Decision\n\nThe decision.\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithSections("Context", "Decision"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001.md", 80, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new RequiredSectionsRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_MissingSection_ReportsDiagnostic()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md", "# ADR-001\n\n## Context\n\nSome context.\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithSections("Context", "Decision"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001.md", 50, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new RequiredSectionsRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].RuleId.Should().Be("STWD-014");
        diagnostics[0].Message.Should().Contain("Decision");
        diagnostics[0].Message.Should().Contain("adr");
    }

    [Fact]
    public async Task Evaluate_SectionMatchIsCaseInsensitive()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md", "# ADR-001\n\n## CONTEXT\n\nSome context.\n\n## decision\n\nDecided.\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithSections("Context", "Decision"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001.md", 70, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new RequiredSectionsRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_SectionInNestedLevel_CountsAsPresent()
    {
        var fs = new InMemoryFileSystem();
        // "Decision" is a level-3 heading nested under "Context"
        fs.AddFile("root/docs/adrs/ADR-001.md", "# ADR-001\n\n## Context\n\n### Decision\n\nDecided.\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithSections("Context", "Decision"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001.md", 60, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new RequiredSectionsRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_NonMarkdownFile_Skipped()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.txt", "Context\nDecision\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithSections("Context", "Decision"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001.txt", 20, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new RequiredSectionsRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_ExplicitArtifact_FamilySectionsApplied()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md", "# ADR-001\n\n## Context\n\nSome context.\n");

        var policy = new RepositoryPolicy
        {
            Artifacts =
            [
                new ArtifactDefinition { Path = "docs/adrs/ADR-001.md", Role = "governance", Importance = "required" }
            ],
            ArtifactFamilies =
            [
                new ArtifactFamilyDefinition
                {
                    Family = "adr",
                    Match = new ArtifactFamilyMatch { PathPattern = "docs/adrs/ADR-*.md" },
                    RequiredSections = ["Context", "Decision"]
                }
            ]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001.md", 50, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new RequiredSectionsRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].RuleId.Should().Be("STWD-014");
        diagnostics[0].Message.Should().Contain("Decision");
    }

    [Fact]
    public async Task Evaluate_FileOutsideFamily_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/other.md", "# Other Doc\n\nNo sections.\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithSections("Context", "Decision"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/other.md", 30, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new RequiredSectionsRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_NoFamiliesDefined_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md", "# ADR-001\n");

        var context = new ValidationContext
        {
            Policy = new RepositoryPolicy(),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001.md", 20, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new RequiredSectionsRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_MultipleMissingSections_ReportsOneDiagnosticPerSection()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md", "# ADR-001\n\nNo sections here.\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithSections("Context", "Decision", "Consequences"),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001.md", 30, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new RequiredSectionsRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(3);
        diagnostics.All(d => d.RuleId == "STWD-014").Should().BeTrue();
    }
}
