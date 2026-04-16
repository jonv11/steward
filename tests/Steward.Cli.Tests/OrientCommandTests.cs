using FluentAssertions;
using Steward.Cli.Tests.Helpers;
using Xunit;

namespace Steward.Cli.Tests;

[Collection("Console")]
public class OrientCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _origDir;

    public OrientCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "steward-orient-" + Guid.NewGuid().ToString("N")[..8]);
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
    public void Orient_UsesPolicyRoleAndStartHere()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "config.yaml"), "profile: software\n");
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            repository:
              name: demo
              type: software
            artifacts:
              - path: AGENT_GUIDE.txt
                role: authoritative
                required: false
            governance:
              start_here:
                - AGENT_GUIDE.txt
            """);
        File.WriteAllText(Path.Combine(_tempDir, "AGENT_GUIDE.txt"), "hello");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("orient", "--output", "json");

        exitCode.Should().Be(0);
        output.Should().Contain("\"repositoryName\": \"demo\"");
        output.Should().Contain("\"profile\": \"software\"");
        output.Should().Contain("\"startHere\": [");
        output.Should().Contain("\"classification\": \"authoritative\"");
        output.Should().Contain("\"isStartHere\": true");
    }

    [Fact]
    public void Orient_Signals_IncludeMissingRequiredArtifacts()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            artifacts:
              - path: README.md
                role: readme
                required: true
            """);

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("orient", "--signals", "--output", "json");

        exitCode.Should().Be(0);
        output.Should().Contain("\"signals\": [");
        output.Should().Contain("missing-required-artifact");
    }

    [Fact]
    public void Orient_Compact_LimitsEntries()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "config.yaml"), "profile: software\n");
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            repository:
              name: demo
            """);
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Hello");
        // Create many files to exceed 15
        for (int i = 0; i < 20; i++)
        {
            File.WriteAllText(Path.Combine(_tempDir, $"doc{i}.md"), $"# Doc {i}");
        }

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("orient", "--compact", "--output", "json");

        exitCode.Should().Be(0);
        // In compact mode, at most 15 entries
        var entryCount = output.Split("\"path\"").Length - 1;
        entryCount.Should().BeLessThanOrEqualTo(15);
    }

    [Fact]
    public void Orient_Compact_PrioritizesStartHere()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            repository:
              name: demo
            governance:
              start_here:
                - README.md
            """);
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Hello");
        for (int i = 0; i < 20; i++)
        {
            File.WriteAllText(Path.Combine(_tempDir, $"file{i}.txt"), "content");
        }

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("orient", "--compact", "--output", "json");

        exitCode.Should().Be(0);
        output.Should().Contain("README.md");
        output.Should().Contain("\"isStartHere\": true");
    }

    [Fact]
    public void Orient_DefaultTextOutput_UsesCompactCuratedView()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "config.yaml"), "profile: software\n");
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Hello");
        for (int i = 0; i < 20; i++)
        {
            File.WriteAllText(Path.Combine(_tempDir, $"doc{i}.md"), $"# Doc {i}");
        }

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("orient");

        exitCode.Should().Be(0);
        output.Should().Contain("Orientation");
        var entryCount = output.Split('[').Count(part => part.StartsWith("documentation]") || part.StartsWith("authoritative]") || part.StartsWith("configuration]"));
        entryCount.Should().BeLessThanOrEqualTo(15);
    }

    [Fact]
    public void Orient_TreeMode_PreservesActualAncestors()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "config.yaml"), "profile: software\n");
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            repository:
              name: demo
            artifacts:
              - path: .steward/policy.yaml
                role: governance
                required: false
            """);
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Hello");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("orient", "--tree");

        exitCode.Should().Be(0);
        output.Should().Contain("├── .steward/");
        output.Should().Contain("│   └── policy.yaml");
    }

    [Fact]
    public void Orient_Signals_ShowsNoneWhenHealthy()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            artifacts:
              - path: README.md
                role: authoritative
                required: true
            """);
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Hello");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("orient", "--signals");

        exitCode.Should().Be(0);
        output.Should().Contain("Signals");
        output.Should().Contain("none");
    }
}
