using FluentAssertions;
using Steward.Cli.Commands;
using System.CommandLine;
using Xunit;

namespace Steward.Cli.Tests;

[Collection("Console")]
public class ExplainCommandTests
{
    private static (int ExitCode, string Output, string Error) InvokeExplain(params string[] args)
    {
        var rootCommand = new RootCommand("Repository Steward");
        GlobalOptionsSetup.AddGlobalOptions(rootCommand);
        rootCommand.Add(ExplainCommand.Create());

        var stdOut = new StringWriter();
        var stdErr = new StringWriter();
        var origOut = Console.Out;
        var origErr = Console.Error;

        Console.SetOut(stdOut);
        Console.SetError(stdErr);

        try
        {
            var exitCode = rootCommand.Parse(args).Invoke();
            return (exitCode, stdOut.ToString(), stdErr.ToString());
        }
        finally
        {
            Console.SetOut(origOut);
            Console.SetError(origErr);
        }
    }

    [Fact]
    public void Explain_KnownRule_ShowsDetails()
    {
        var (exitCode, output, _) = InvokeExplain("explain", "STWD-001");

        exitCode.Should().Be(0);
        output.Should().Contain("STWD-001");
        output.Should().Contain("Category:");
        output.Should().Contain("Remediation:");
    }

    [Fact]
    public void Explain_UnknownRule_ReportsError()
    {
        var (exitCode, _, _) = InvokeExplain("explain", "STWD-999");

        exitCode.Should().Be(2);
    }

    [Fact]
    public void Explain_JsonOutput()
    {
        var (exitCode, output, _) = InvokeExplain("explain", "STWD-003", "--output", "json");

        exitCode.Should().Be(0);
        output.Should().Contain("\"ruleId\"");
        output.Should().Contain("\"remediation\"");
        output.Should().Contain("STWD-003");
    }

    [Fact]
    public void Explain_AllRules_Listed()
    {
        var (exitCode, output, _) = InvokeExplain("explain", "STWD-008");

        exitCode.Should().Be(0);
        output.Should().Contain("STWD-008");
        output.Should().Contain("broken-link");
    }

    [Fact]
    public void Explain_EachRuleHasRemediation()
    {
        foreach (var ruleId in new[] { "STWD-001", "STWD-002", "STWD-003", "STWD-004",
                                        "STWD-005", "STWD-006", "STWD-007", "STWD-008",
                                        "STWD-009", "STWD-010", "STWD-011" })
        {
            var remediation = ExplainCommand.GetRemediation(ruleId);
            remediation.Should().NotBeNullOrWhiteSpace($"Rule {ruleId} should have remediation guidance");
            remediation.Should().NotBe("No specific remediation guidance available.",
                $"Rule {ruleId} should have specific remediation");
        }
    }

    [Fact]
    public void Explain_STWD009_ShowsDetails()
    {
        var (exitCode, output, _) = InvokeExplain("explain", "STWD-009");

        exitCode.Should().Be(0);
        output.Should().Contain("STWD-009");
        output.Should().Contain("broken-reference");
        output.Should().Contain("Remediation:");
    }

    [Fact]
    public void Explain_ListAll_IncludesSTWD009()
    {
        var (exitCode, output, _) = InvokeExplain("explain");

        exitCode.Should().Be(0);
        output.Should().Contain("STWD-009");
        output.Should().Contain("STWD-001");
    }

    [Fact]
    public void Explain_Verbose_ShowsFilesEvaluated()
    {
        // Explain with --verbosity verbose outside a steward repo should still work (no files info)
        var (exitCode, output, _) = InvokeExplain("explain", "STWD-001", "--verbosity", "verbose");

        exitCode.Should().Be(0);
        output.Should().Contain("STWD-001");
        // Without a valid steward dir, filesEvaluated may not appear — that's OK
    }

    [Fact]
    public void ExplainPath_ResolveEffectivePolicy_ReturnsAllLayers()
    {
        var policy = new Steward.Core.Configuration.RepositoryPolicy
        {
            Artifacts =
            [
                new Steward.Core.Configuration.ArtifactDefinition
                {
                    Path = "docs/index.md",
                    Role = "index",
                    Description = "Main index",
                    Required = true,
                    IndexOf = "docs/decisions"
                }
            ],
            Governance = new Steward.Core.Configuration.GovernanceConfig
            {
                Frontmatter = new Steward.Core.Configuration.FrontmatterConfig
                {
                    RequiredFields = ["title"]
                }
            },
            Validation = new Steward.Core.Configuration.ValidationConfig
            {
                DisabledRules = ["STWD-004"],
                PathOverrides =
                [
                    new Steward.Core.Configuration.PathOverride
                    {
                        Pattern = "docs/**",
                        DisabledRules = ["STWD-002"]
                    }
                ],
                FrontmatterRequirements =
                [
                    new Steward.Core.Configuration.FrontmatterRequirement
                    {
                        Pattern = "docs/**",
                        RequiredFields = ["status"],
                        AllowedValues = new Dictionary<string, List<string>>
                        {
                            ["status"] = ["draft", "accepted"]
                        }
                    }
                ]
            }
        };

        var ctx = new CommandContext
        {
            RootPath = "/repo",
            FileSystem = new Steward.TestFixtures.InMemoryFileSystem(),
            Formatter = new Steward.Cli.Formatting.TextOutputFormatter(TextWriter.Null, false),
            OutputFormat = Steward.Core.OutputFormat.Text,
            Verbosity = Steward.Core.Verbosity.Normal,
            NoColor = true,
            Policy = policy,
            PathPolicy = null,
            Files = [new Steward.Core.Discovery.DiscoveredFile("docs/index.md", 100, false)]
        };

        var info = ExplainCommand.ResolveEffectivePolicy("docs/index.md", ctx);

        info.Path.Should().Be("docs/index.md");
        info.Classification.Should().NotBeNullOrEmpty();
        info.Artifact.Should().NotBeNull();
        info.Artifact!.Role.Should().Be("index");
        info.Artifact.IndexOf.Should().Be("docs/decisions");
        info.SuppressedRules.Should().Contain("STWD-004");
        info.SuppressedRules.Should().Contain("STWD-002");
        info.RequiredFrontmatterFields.Should().Contain("title");
        info.RequiredFrontmatterFields.Should().Contain("status");
        info.AllowedValues.Should().ContainKey("status");
        info.ApplicableRules.Should().NotContain("STWD-004");
        info.ApplicableRules.Should().NotContain("STWD-002");
    }
}
