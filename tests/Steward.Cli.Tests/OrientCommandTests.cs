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
}
