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
            Verbosity = Steward.Core.Verbosity.Normal,
            NoColor = true,
            Policy = policy,
            PathPolicy = pathPolicy,
            Files = files
        };
    }
}
