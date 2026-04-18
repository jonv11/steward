using FluentAssertions;
using Steward.Core.Abstractions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class FamilyMinCountRuleTests
{
    private static RepositoryPolicy MakePolicyWithMinCount(int minCount, string? description = null) => new()
    {
        ArtifactFamilies =
        [
            new ArtifactFamilyDefinition
            {
                Family = "adr",
                DisplayName = "Architecture Decision Record",
                Match = new ArtifactFamilyMatch { PathPattern = "docs/adrs/ADR-*.md" },
                DirectoryExpectations = new DirectoryExpectations
                {
                    MinCount = minCount,
                    Description = description
                }
            }
        ]
    };

    [Fact]
    public async Task Evaluate_SufficientFiles_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();
        var files = new[]
        {
            new DiscoveredFile("docs/adrs/ADR-001.md", 50, false),
            new DiscoveredFile("docs/adrs/ADR-002.md", 50, false),
            new DiscoveredFile("docs/adrs/ADR-003.md", 50, false)
        };

        var context = new ValidationContext
        {
            Policy = MakePolicyWithMinCount(3),
            PathPolicy = null,
            TargetFiles = files,
            AllDiscoveredFiles = files,
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyMinCountRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_InsufficientFiles_ReportsDiagnostic()
    {
        var fs = new InMemoryFileSystem();
        var files = new[]
        {
            new DiscoveredFile("docs/adrs/ADR-001.md", 50, false)
        };

        var context = new ValidationContext
        {
            Policy = MakePolicyWithMinCount(3),
            PathPolicy = null,
            TargetFiles = files,
            AllDiscoveredFiles = files,
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyMinCountRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].RuleId.Should().Be("STWD-015");
        diagnostics[0].Message.Should().Contain("1");
        diagnostics[0].Message.Should().Contain("3");
        diagnostics[0].Message.Should().Contain("adr");
    }

    [Fact]
    public async Task Evaluate_ZeroFiles_ReportsDiagnostic()
    {
        var fs = new InMemoryFileSystem();
        DiscoveredFile[] allFiles = [];

        var context = new ValidationContext
        {
            Policy = MakePolicyWithMinCount(1),
            PathPolicy = null,
            TargetFiles = [],
            AllDiscoveredFiles = allFiles,
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyMinCountRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].RuleId.Should().Be("STWD-015");
    }

    [Fact]
    public async Task Evaluate_UsesAllDiscoveredFiles_NotJustTargetFiles()
    {
        // Scoped validation: only 1 file in scope (TargetFiles), but 3 exist in repo (AllDiscoveredFiles)
        var fs = new InMemoryFileSystem();
        var allFiles = new[]
        {
            new DiscoveredFile("docs/adrs/ADR-001.md", 50, false),
            new DiscoveredFile("docs/adrs/ADR-002.md", 50, false),
            new DiscoveredFile("docs/adrs/ADR-003.md", 50, false)
        };

        var context = new ValidationContext
        {
            Policy = MakePolicyWithMinCount(3),
            PathPolicy = null,
            TargetFiles = [allFiles[0]], // only one in scope
            AllDiscoveredFiles = allFiles, // all three in repo
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyMinCountRule();
        var diagnostics = await rule.EvaluateAsync(context);

        // Should use AllDiscoveredFiles (3 files meet min_count of 3)
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_FallsBackToTargetFilesWhenAllDiscoveredFilesIsNull()
    {
        var fs = new InMemoryFileSystem();
        var files = new[]
        {
            new DiscoveredFile("docs/adrs/ADR-001.md", 50, false),
            new DiscoveredFile("docs/adrs/ADR-002.md", 50, false)
        };

        var context = new ValidationContext
        {
            Policy = MakePolicyWithMinCount(2),
            PathPolicy = null,
            TargetFiles = files,
            AllDiscoveredFiles = null, // not set
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyMinCountRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_DescriptionIncludedInMessage()
    {
        var fs = new InMemoryFileSystem();

        var context = new ValidationContext
        {
            Policy = MakePolicyWithMinCount(2, "At least two ADRs document major decisions."),
            PathPolicy = null,
            TargetFiles = [],
            AllDiscoveredFiles = [],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyMinCountRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].Message.Should().Contain("At least two ADRs document major decisions.");
    }

    [Fact]
    public async Task Evaluate_DisplayNameUsedInMessage()
    {
        var fs = new InMemoryFileSystem();

        var context = new ValidationContext
        {
            Policy = MakePolicyWithMinCount(2),
            PathPolicy = null,
            TargetFiles = [],
            AllDiscoveredFiles = [],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyMinCountRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].Message.Should().Contain("Architecture Decision Record");
    }

    [Fact]
    public async Task Evaluate_ExplicitArtifact_CountedInFamily()
    {
        var fs = new InMemoryFileSystem();
        var allFiles = new[]
        {
            new DiscoveredFile("docs/adrs/ADR-001.md", 50, false),
            new DiscoveredFile("docs/adrs/ADR-002.md", 50, false)
        };

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
                    DirectoryExpectations = new DirectoryExpectations { MinCount = 2 }
                }
            ]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = allFiles,
            AllDiscoveredFiles = allFiles,
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyMinCountRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_FrontmatterMatchedFamily_UsesFrontmatterForCount()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/records/doc-1.md", "---\ndoc_type: adr\n---\n# ADR\n");
        fs.AddFile("root/docs/records/doc-2.md", "---\ndoc_type: note\n---\n# Note\n");

        var policy = new RepositoryPolicy
        {
            ArtifactFamilies =
            [
                new ArtifactFamilyDefinition
                {
                    Family = "adr",
                    Match = new ArtifactFamilyMatch
                    {
                        PathPattern = "docs/records/*.md",
                        Frontmatter = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["doc_type"] = "adr"
                        }
                    },
                    DirectoryExpectations = new DirectoryExpectations { MinCount = 2 }
                }
            ]
        };

        var allFiles = new[]
        {
            new DiscoveredFile("docs/records/doc-1.md", 50, false),
            new DiscoveredFile("docs/records/doc-2.md", 50, false)
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = allFiles,
            AllDiscoveredFiles = allFiles,
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyMinCountRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].RuleId.Should().Be("STWD-015");
    }

    [Fact]
    public async Task Evaluate_NoFamiliesWithMinCount_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();

        var policy = new RepositoryPolicy
        {
            ArtifactFamilies =
            [
                new ArtifactFamilyDefinition
                {
                    Family = "adr",
                    Match = new ArtifactFamilyMatch { PathPattern = "docs/adrs/ADR-*.md" }
                    // No DirectoryExpectations
                }
            ]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [],
            AllDiscoveredFiles = [],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new FamilyMinCountRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }
}
