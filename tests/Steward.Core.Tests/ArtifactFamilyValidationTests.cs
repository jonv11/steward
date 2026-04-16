using FluentAssertions;
using Steward.Core.Abstractions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class ArtifactFamilyValidationTests
{
    private static RepositoryPolicy MakePolicyWithAdrFamily() => new()
    {
        ArtifactFamilies =
        [
            new ArtifactFamilyDefinition
            {
                Family = "adr",
                DisplayName = "Architecture Decision Record",
                Match = new ArtifactFamilyMatch
                {
                    PathPattern = "docs/adrs/ADR-*.md"
                },
                FrontmatterSchema = new ArtifactFamilyFrontmatterSchema
                {
                    Required = ["type", "status"],
                    AllowedValues = new()
                    {
                        ["status"] = ["Draft", "Accepted", "Deprecated"]
                    }
                }
            }
        ]
    };

    [Fact]
    public async Task Evaluate_MissingRequiredFamilyField_ReportsDiagnosticWithFamilyName()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001-test.md", "---\ntype: adr\n---\n# ADR-001\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithAdrFamily(),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001-test.md", 50, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new RequiredFrontmatterFieldRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].RuleId.Should().Be("STWD-003");
        diagnostics[0].Message.Should().Contain("status");
        diagnostics[0].Message.Should().Contain("[family: adr]");
    }

    [Fact]
    public async Task Evaluate_AllowedValuesViolation_ReportsDiagnosticWithFamilyName()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001-test.md", "---\ntype: adr\nstatus: InvalidStatus\n---\n# ADR-001\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithAdrFamily(),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001-test.md", 60, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new RequiredFrontmatterFieldRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].RuleId.Should().Be("STWD-003");
        diagnostics[0].Message.Should().Contain("InvalidStatus");
        diagnostics[0].Message.Should().Contain("[family: adr]");
    }

    [Fact]
    public async Task Evaluate_ValidFamilyFile_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001-test.md", "---\ntype: adr\nstatus: Accepted\n---\n# ADR-001\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithAdrFamily(),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001-test.md", 60, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new RequiredFrontmatterFieldRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_FileOutsideAnyFamily_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/other.md", "# Just a doc\n");

        var context = new ValidationContext
        {
            Policy = MakePolicyWithAdrFamily(),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/other.md", 20, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new RequiredFrontmatterFieldRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_ExplicitArtifact_FamilySchemaNotApplied()
    {
        var fs = new InMemoryFileSystem();
        // File matches the family path pattern but is declared as explicit artifact
        fs.AddFile("root/docs/adrs/ADR-001-test.md", "---\ntype: adr\n---\n# ADR-001\n");

        var policy = new RepositoryPolicy
        {
            Artifacts =
            [
                new ArtifactDefinition
                {
                    Path = "docs/adrs/ADR-001-test.md",
                    Role = "governance",
                    Importance = "required"
                }
            ],
            ArtifactFamilies =
            [
                new ArtifactFamilyDefinition
                {
                    Family = "adr",
                    Match = new ArtifactFamilyMatch { PathPattern = "docs/adrs/ADR-*.md" },
                    FrontmatterSchema = new ArtifactFamilyFrontmatterSchema
                    {
                        Required = ["type", "status"]
                    }
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

        var rule = new RequiredFrontmatterFieldRule();
        var diagnostics = await rule.EvaluateAsync(context);

        // Explicit artifact: no family schema applied, so missing 'status' is not reported
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_PathPatternFamilyOnly_EnforcesRequiredFields()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/rfcs/RFC-001.md", "---\nstatus: Draft\n---\n# RFC-001\n");

        var policy = new RepositoryPolicy
        {
            ArtifactFamilies =
            [
                new ArtifactFamilyDefinition
                {
                    Family = "rfc",
                    Match = new ArtifactFamilyMatch { PathPattern = "docs/rfcs/RFC-*.md" },
                    FrontmatterSchema = new ArtifactFamilyFrontmatterSchema
                    {
                        Required = ["type", "status"]
                    }
                }
            ]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/rfcs/RFC-001.md", 40, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new RequiredFrontmatterFieldRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].Message.Should().Contain("type");
        diagnostics[0].Message.Should().Contain("[family: rfc]");
    }

    [Fact]
    public async Task Evaluate_FrontmatterOnlyFamilyMatch_EnforcesSchema()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/misc/doc.md", "---\ndoc_type: chapter\n---\n# Chapter\n");

        var policy = new RepositoryPolicy
        {
            ArtifactFamilies =
            [
                new ArtifactFamilyDefinition
                {
                    Family = "chapter",
                    Match = new ArtifactFamilyMatch
                    {
                        Frontmatter = new() { ["doc_type"] = "chapter" }
                    },
                    FrontmatterSchema = new ArtifactFamilyFrontmatterSchema
                    {
                        Required = ["doc_type", "title"]
                    }
                }
            ]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/misc/doc.md", 40, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new RequiredFrontmatterFieldRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].Message.Should().Contain("title");
        diagnostics[0].Message.Should().Contain("[family: chapter]");
    }

    [Fact]
    public async Task Evaluate_NoFamiliesDefined_BackwardCompatible()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001-test.md", "# ADR-001\n");

        var context = new ValidationContext
        {
            Policy = new RepositoryPolicy(), // no families, no global requirements
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/adrs/ADR-001-test.md", 20, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new RequiredFrontmatterFieldRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_FixtureRepo_ReportsExpectedDiagnostics()
    {
        var fixturePath = RepositoryFixture.GetFixturePath("artifact-families");
        var tempDir = Path.Combine(Path.GetTempPath(), "steward-test-af-" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            RepositoryFixture.CopyFixtureTo("artifact-families", tempDir);

            var loader = new ConfigLoader(new PhysicalFileSystem());
            var configDir = loader.FindConfigDirectory(tempDir)!;
            var policy = loader.LoadPolicy(configDir);

            var discovery = new FileDiscoveryService(
                new PhysicalFileSystem(),
                GitIgnoreFilter.Load(tempDir, new PhysicalFileSystem()));
            var files = discovery.Discover(tempDir);

            var targetFiles = files.Where(f => !f.IsDirectory).ToList();

            var context = new ValidationContext
            {
                Policy = policy,
                PathPolicy = null,
                TargetFiles = targetFiles,
                AllDiscoveredFiles = files,
                FileSystem = new PhysicalFileSystem(),
                RepositoryRoot = tempDir
            };

            var rule = new RequiredFrontmatterFieldRule();
            var diagnostics = await rule.EvaluateAsync(context);

            // ADR-002 is missing 'status', ADR-003 has bad status, RFC-002 is missing 'type' and 'resolves'
            var familyDiags = diagnostics.Where(d => d.Message.Contains("[family:")).ToList();
            familyDiags.Should().NotBeEmpty();

            // ADR-002 missing 'status'
            familyDiags.Should().Contain(d =>
                (d.Path ?? "").Contains("ADR-002") && (d.Message ?? "").Contains("status") && (d.Message ?? "").Contains("[family: adr]"));

            // ADR-003 has bad status value 'Invalid'
            familyDiags.Should().Contain(d =>
                (d.Path ?? "").Contains("ADR-003") && (d.Message ?? "").Contains("Invalid") && (d.Message ?? "").Contains("[family: adr]"));

            // RFC-002 missing 'type'
            familyDiags.Should().Contain(d =>
                (d.Path ?? "").Contains("RFC-002") && (d.Message ?? "").Contains("type") && (d.Message ?? "").Contains("[family: rfc]"));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }
}
