using FluentAssertions;
using Steward.Cli.Commands;
using Steward.Cli.Tests.Helpers;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Xunit;

namespace Steward.Cli.Tests;

[Collection("Console")]
public class ConfigCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _origDir;

    public ConfigCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "steward-config-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(Path.Combine(_tempDir, ".git"));
        Directory.CreateDirectory(Path.Combine(_tempDir, ".steward"));
        _origDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_tempDir);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_origDir);
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void ConfigValidate_InvalidProfile_ReturnsUsageError()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "config.yaml"), "profile: default\n");

        var (exitCode, _, error) = CliTestHelper.InvokeCapture("config", "validate");

        exitCode.Should().Be(2);
        error.Should().Contain("Invalid profile");
    }

    [Fact]
    public void ConfigValidate_UnknownField_ReturnsUsageError()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "config.yaml"), "profile: software\nextra: nope\n");

        var (exitCode, _, error) = CliTestHelper.InvokeCapture("config", "validate");

        exitCode.Should().Be(2);
        error.Should().Contain("Configuration is invalid");
        error.Should().Contain("config.yaml");
    }

    [Fact]
    public void ConfigValidate_UnknownRuleId_ReturnsUsageError()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            validation:
              disabled_rules:
                - STWD-999
            """);

        var (exitCode, _, error) = CliTestHelper.InvokeCapture("config", "validate");

        exitCode.Should().Be(2);
        error.Should().Contain("Unknown rule id");
        error.Should().Contain("STWD-999");
    }

    [Fact]
    public void ConfigValidate_InvalidMaintenanceType_ReturnsUsageError()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            maintenance:
              artifacts:
                - id: demo
                  path: STRUCTURE.md
                  type: not-a-maintainer
            """);

        var (exitCode, _, error) = CliTestHelper.InvokeCapture("config", "validate");

        exitCode.Should().Be(2);
        error.Should().Contain("Invalid maintenance artifact type");
        error.Should().Contain("not-a-maintainer");
    }

    [Fact]
    public void ConfigValidate_UnknownDependsOn_ReturnsUsageError()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            maintenance:
              artifacts:
                - id: structure
                  path: STRUCTURE.md
                  type: structure-document
                  depends_on:
                    - missing-artifact
            """);

        var (exitCode, _, error) = CliTestHelper.InvokeCapture("config", "validate");

        exitCode.Should().Be(2);
        error.Should().Contain("depends_on");
        error.Should().Contain("missing-artifact");
    }

    [Fact]
    public void ConfigValidate_InvalidPathPolicyRegex_ReturnsUsageError()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "path-policy.yaml"), """
            rulesets:
              - name: naming
                rules:
                  - pattern: "docs/*.md"
                    must_match: "["
            """);

        var (exitCode, _, error) = CliTestHelper.InvokeCapture("config", "validate");

        exitCode.Should().Be(2);
        error.Should().Contain("must_match regex");
    }

    [Fact]
    public void ConfigShow_Effective_IncludesResolvedRuntimeAndRawFiles()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "config.yaml"), """
            profile: software
            output:
              format: json
            discovery:
              exclude:
                - dist/
            """);
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            repository:
              name: demo
              type: software
            """);

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("config", "show", "--effective", "--output", "json");

        exitCode.Should().Be(0);
        output.Should().Contain("\"configDirectory\"");
        output.Should().Contain("\"rawFiles\"");
        output.Should().Contain("\"effectiveRuntime\"");
        output.Should().Contain("\"format\": \"json\"");
        output.Should().Contain("\"exclude\": [");
        output.Should().Contain("\"path\": \"CHANGELOG.md\"");
    }

    [Fact]
    public void ConfigShow_Effective_Text_IncludesMergedPolicy()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "config.yaml"), """
            profile: software
            """);
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            repository:
              name: demo
            """);

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("config", "show", "--effective");

        exitCode.Should().Be(0);
        output.Should().Contain("Effective runtime defaults:");
        output.Should().Contain("Effective policy (profile defaults merged):");
        output.Should().Contain("path: CHANGELOG.md");
        output.Should().Contain("path: CONTRIBUTING.md");
    }

    [Fact]
    public void ConfigSuggest_JsonOutput_ProducesSuggestionObject()
    {
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Demo");
        Directory.CreateDirectory(Path.Combine(_tempDir, "docs", "requirements"));
        File.WriteAllText(Path.Combine(_tempDir, "docs", "requirements", "PRD.md"), "# PRD");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("config", "suggest", "--output", "json");

        exitCode.Should().Be(0);
        output.Should().Contain("\"startHere\"");
        output.Should().Contain("\"artifacts\"");
        output.Should().Contain("\"path\": \"README.md\"");
        output.Should().Contain("\"path\": \"docs/requirements/PRD.md\"");
    }

    [Fact]
    public void Orient_WithInvalidConfiguration_ReturnsUsageError()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "config.yaml"), "profile: default\n");

        var (exitCode, _, error) = CliTestHelper.InvokeCapture("orient");

        exitCode.Should().Be(2);
        error.Should().Contain("Run 'steward config validate' for details.");
    }

    [Fact]
    public void ConfigDoctor_DeadStartHere_ReportsIssue()
    {
        var policy = new RepositoryPolicy
        {
            Governance = new GovernanceConfig
            {
                StartHere = ["docs/getting-started.md"]
            }
        };

        var ctx = CreateDoctorContext(policy, pathPolicy: null,
            files: [new DiscoveredFile("README.md", 100, false)]);

        var findings = ConfigCommand.RunDoctor(ctx);

        findings.Should().ContainSingle();
        findings[0].Category.Should().Be("dead-start-here");
    }

    [Fact]
    public void ConfigDoctor_MissingArtifact_ReportsIssue()
    {
        var policy = new RepositoryPolicy
        {
            Artifacts = [new ArtifactDefinition { Path = "docs/guide.md", Required = true }]
        };

        var ctx = CreateDoctorContext(policy, pathPolicy: null,
            files: [new DiscoveredFile("README.md", 100, false)]);

        var findings = ConfigCommand.RunDoctor(ctx);

        findings.Should().ContainSingle();
        findings[0].Category.Should().Be("missing-artifact");
    }

    [Fact]
    public void ConfigDoctor_OverlappingGlobalFrontmatterKeys_ReportsIssue()
    {
        var policy = new RepositoryPolicy
        {
            Governance = new GovernanceConfig
            {
                Frontmatter = new FrontmatterConfig
                {
                    RequiredFields = ["status"]
                }
            },
            Validation = new ValidationConfig
            {
                RequiredFrontmatterFields = ["owner"]
            }
        };

        var ctx = CreateDoctorContext(policy, pathPolicy: null,
            files: [new DiscoveredFile("README.md", 100, false)]);

        var findings = ConfigCommand.RunDoctor(ctx);

        findings.Should().ContainSingle();
        findings[0].Category.Should().Be("overlapping-frontmatter-globals");
    }

    [Fact]
    public void ConfigDoctor_UnmatchedPathRule_ReportsIssue()
    {
        var pathPolicy = new PathPolicyDocument
        {
            Rulesets = [new PathRuleSet
            {
                Rules = [new PathRule { Pattern = "archive/**", Category = "deprecated" }]
            }]
        };

        var ctx = CreateDoctorContext(policy: null, pathPolicy: pathPolicy,
            files: [new DiscoveredFile("docs/readme.md", 100, false)]);

        var findings = ConfigCommand.RunDoctor(ctx);

        findings.Should().ContainSingle();
        findings[0].Category.Should().Be("unmatched-path-rule");
    }

    [Fact]
    public void ConfigDoctor_NoIssues_ReturnsEmpty()
    {
        var policy = new RepositoryPolicy
        {
            Artifacts = [new ArtifactDefinition { Path = "README.md" }],
            Governance = new GovernanceConfig { StartHere = ["README.md"] }
        };

        var ctx = CreateDoctorContext(policy, pathPolicy: null,
            files: [new DiscoveredFile("README.md", 100, false)]);

        var findings = ConfigCommand.RunDoctor(ctx);

        findings.Should().BeEmpty();
    }

    [Fact]
    public void ConfigDoctor_DeadSuppression_UnknownRuleId()
    {
        var policy = new RepositoryPolicy
        {
            Validation = new ValidationConfig
            {
                DisabledRules = ["STWD-999"]
            }
        };

        var ctx = CreateDoctorContext(policy, pathPolicy: null,
            files: [new DiscoveredFile("README.md", 100, false)]);

        var findings = ConfigCommand.RunDoctor(ctx);

        findings.Should().Contain(f => f.Category == "dead-suppression" && f.Message.Contains("STWD-999"));
    }

    [Fact]
    public void ConfigDoctor_UnreachablePathOverride()
    {
        var policy = new RepositoryPolicy
        {
            Validation = new ValidationConfig
            {
                PathOverrides =
                [
                    new PathOverride { Pattern = "nonexistent/**", DisabledRules = ["STWD-001"] }
                ]
            }
        };

        var ctx = CreateDoctorContext(policy, pathPolicy: null,
            files: [new DiscoveredFile("README.md", 100, false)]);

        var findings = ConfigCommand.RunDoctor(ctx);

        findings.Should().Contain(f => f.Category == "unreachable-path-override" && f.Message.Contains("nonexistent/**"));
    }

    [Fact]
    public void ConfigDoctor_UnreachableFrontmatterPattern()
    {
        var policy = new RepositoryPolicy
        {
            Validation = new ValidationConfig
            {
                FrontmatterRequirements =
                [
                    new FrontmatterRequirement { Pattern = "archive/**", RequiredFields = ["status"] }
                ]
            }
        };

        var ctx = CreateDoctorContext(policy, pathPolicy: null,
            files: [new DiscoveredFile("README.md", 100, false)]);

        var findings = ConfigCommand.RunDoctor(ctx);

        findings.Should().Contain(f => f.Category == "unreachable-frontmatter-pattern" && f.Message.Contains("archive/**"));
    }

    [Fact]
    public void ConfigDoctor_MaintenanceDirectorySource_DoesNotReportUnmatchedSource()
    {
        var policy = new RepositoryPolicy
        {
            Maintenance = new MaintenanceConfig
            {
                Artifacts =
                [
                    new MaintenanceArtifactDef
                    {
                        Id = "structure",
                        Path = "STRUCTURE.md",
                        Type = "structure-document",
                        Source = "src/"
                    }
                ]
            }
        };

        var ctx = CreateDoctorContext(policy, pathPolicy: null,
            files:
            [
                new DiscoveredFile("src", 0, true),
                new DiscoveredFile("src/Program.cs", 100, false)
            ]);

        var findings = ConfigCommand.RunDoctor(ctx);

        findings.Should().NotContain(f => f.Category == "unmatched-maintenance-source");
    }

    [Fact]
    public void ConfigDoctor_ConflictingAllowedValues_NonOverlappingPatterns_NoIssue()
    {
        var policy = new RepositoryPolicy
        {
            ArtifactFamilies =
            [
                new ArtifactFamilyDefinition
                {
                    Family = "rfc",
                    Match = new ArtifactFamilyMatch { PathPattern = "docs/decisions/rfcs/*.md" },
                    FrontmatterSchema = new ArtifactFamilyFrontmatterSchema
                    {
                        AllowedValues = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["type"] = ["rfc"]
                        }
                    }
                }
            ],
            Validation = new ValidationConfig
            {
                FrontmatterRequirements =
                [
                    new FrontmatterRequirement
                    {
                        Pattern = "docs/planning/*.md",
                        AllowedValues = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["type"] = ["planning"]
                        }
                    }
                ]
            }
        };

        var ctx = CreateDoctorContext(policy, pathPolicy: null,
            files:
            [
                new DiscoveredFile("docs/decisions/rfcs/RFC-001-proposal.md", 100, false),
                new DiscoveredFile("docs/planning/implementation-plan.md", 100, false)
            ]);

        var findings = ConfigCommand.RunDoctor(ctx);

        findings.Should().NotContain(f => f.Category == "conflicting-allowed-values");
    }

    [Fact]
    public void ConfigDoctor_ConflictingAllowedValues_OverlappingPatterns_ReportsIssue()
    {
        var policy = new RepositoryPolicy
        {
            ArtifactFamilies =
            [
                new ArtifactFamilyDefinition
                {
                    Family = "planning",
                    Match = new ArtifactFamilyMatch { PathPattern = "docs/planning/*.md" },
                    FrontmatterSchema = new ArtifactFamilyFrontmatterSchema
                    {
                        AllowedValues = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["status"] = ["Active"]
                        }
                    }
                }
            ],
            Validation = new ValidationConfig
            {
                FrontmatterRequirements =
                [
                    new FrontmatterRequirement
                    {
                        Pattern = "docs/planning/*.md",
                        AllowedValues = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["status"] = ["Draft"]
                        }
                    }
                ]
            }
        };

        var ctx = CreateDoctorContext(policy, pathPolicy: null,
            files: [new DiscoveredFile("docs/planning/implementation-plan.md", 100, false)]);

        var findings = ConfigCommand.RunDoctor(ctx);

        findings.Should().Contain(f => f.Category == "conflicting-allowed-values");
    }

    private static CommandContext CreateDoctorContext(
        RepositoryPolicy? policy,
        PathPolicyDocument? pathPolicy,
        IReadOnlyList<DiscoveredFile> files)
    {
        return new CommandContext
        {
            RootPath = "/repo",
            FileSystem = new Steward.TestFixtures.InMemoryFileSystem(),
            Formatter = new Steward.Cli.Formatting.TextOutputFormatter(TextWriter.Null, false),
            OutputFormat = Steward.Core.OutputFormat.Text,
            JsonEnvelope = Steward.Core.JsonEnvelopeMode.Legacy,
            Verbosity = Steward.Core.Verbosity.Normal,
            NoColor = true,
            Policy = policy,
            PathPolicy = pathPolicy,
            Files = files
        };
    }
}
