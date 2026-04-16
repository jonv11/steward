using FluentAssertions;
using Steward.Cli.Tests.Helpers;
using Steward.Core;
using Xunit;

namespace Steward.Cli.Tests;

[Collection("Console")]
public class ExitCodeTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _origDir;

    public ExitCodeTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "steward-exit-" + Guid.NewGuid().ToString("N")[..8]);
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
    public void ExitCode_Constants_AreStable()
    {
        ExitCodes.Success.Should().Be(0);
        ExitCodes.ValidationFailure.Should().Be(1);
        ExitCodes.UsageError.Should().Be(2);
        ExitCodes.InternalError.Should().Be(3);
    }

    [Fact]
    public void Check_CleanRepo_ReturnsSuccess()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            artifacts:
              - path: README.md
                role: readme
                required: true
            """);
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Hello");

        var (exitCode, _, _) = CliTestHelper.InvokeCapture("check");

        exitCode.Should().Be(ExitCodes.Success);
    }

    [Fact]
    public void Check_MissingRequiredArtifact_ReturnsValidationFailure()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            artifacts:
              - path: MISSING.md
                role: readme
                required: true
            """);

        var (exitCode, _, _) = CliTestHelper.InvokeCapture("check");

        exitCode.Should().Be(ExitCodes.ValidationFailure);
    }

    [Fact]
    public void UnknownCommand_ReturnsUsageErrorExitCode()
    {
        var (exitCode, _, _) = CliTestHelper.InvokeCapture("nonexistent-command");

        exitCode.Should().Be(ExitCodes.UsageError);
    }

    [Fact]
    public void UnknownOption_ReturnsUsageErrorExitCode()
    {
        var (exitCode, _, _) = CliTestHelper.InvokeCapture("check", "--not-a-real-option");

        exitCode.Should().Be(ExitCodes.UsageError);
    }

    [Fact]
    public void Check_InvalidScope_ReturnsUsageError()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), "");

        var (exitCode, _, _) = CliTestHelper.InvokeCapture("check", "--scope", "badvalue");

        exitCode.Should().Be(ExitCodes.UsageError);
    }

    [Fact]
    public void Status_NoConfig_ReturnsUsageError()
    {
        Directory.Delete(Path.Combine(_tempDir, ".steward"), true);

        var (exitCode, _, _) = CliTestHelper.InvokeCapture("status");

        exitCode.Should().Be(ExitCodes.UsageError);
    }
}
