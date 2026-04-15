using FluentAssertions;
using Steward.Cli.Tests.Helpers;
using Xunit;

namespace Steward.Cli.Tests;

[Collection("Console")]
public class CheckCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _origDir;

    public CheckCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "steward-check-" + Guid.NewGuid().ToString("N")[..8]);
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
    public void Check_DisabledRule_IsHonored()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            validation:
              disabled_rules:
                - STWD-001
            artifacts:
              - path: README.md
                role: readme
                required: true
            """);

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check", "--output", "json");

        exitCode.Should().Be(0);
        output.Should().Contain("\"diagnostics\": []");
    }

    [Fact]
    public void Check_Quiet_SuppressesOutput_Pass()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            artifacts:
              - path: README.md
                role: readme
                required: true
            """);
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Hello");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check", "--quiet");

        exitCode.Should().Be(0);
        output.Trim().Should().BeEmpty();
    }

    [Fact]
    public void Check_Quiet_SuppressesOutput_Fail()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            artifacts:
              - path: MISSING.md
                role: readme
                required: true
            """);

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check", "--quiet");

        exitCode.Should().NotBe(0);
        output.Trim().Should().BeEmpty();
    }

    [Fact]
    public void Check_JsonOutput_UsesStringSeverity()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            artifacts:
              - path: README.md
                role: readme
                required: true
            """);

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check", "--output", "json");

        exitCode.Should().Be(1);
        output.Should().Contain("\"severity\": \"error\"");
        output.Should().NotContain("\"severity\": 2");
    }

    [Fact]
    public void Check_InvalidScope_ReturnsUsageError()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), "");

        var (exitCode, _, _) = CliTestHelper.InvokeCapture("check", "--scope", "badvalue");

        // System.CommandLine rejects invalid scope values (AcceptOnlyFromAmong); exit code 2.
        exitCode.Should().Be(2);
    }

    [Fact]
    public void Check_CompletionSummary_UsesConfiguredCompletionPolicy()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            governance:
              completion_policy:
                rules:
                  - id: STWD-001
                    description: required artifact(s) missing before release
            artifacts:
              - path: README.md
                role: readme
                required: true
            """);

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check");

        exitCode.Should().Be(1);
        output.Should().Contain("Completion:");
        output.Should().Contain("required artifact(s) missing before release");
    }
}
