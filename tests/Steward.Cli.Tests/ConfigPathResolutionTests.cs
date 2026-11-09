using FluentAssertions;
using Steward.Cli.Tests.Helpers;
using Xunit;

namespace Steward.Cli.Tests;

/// <summary>
/// Regression coverage for bare relative <c>--config</c> directories, which used to crash
/// discovery because the repository root was derived as an empty parent path.
/// </summary>
[Collection("Console")]
public class ConfigPathResolutionTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _origDir;

    public ConfigPathResolutionTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "steward-cfgpath-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(Path.Combine(_tempDir, ".git"));
        Directory.CreateDirectory(Path.Combine(_tempDir, ".steward-draft"));
        File.WriteAllText(Path.Combine(_tempDir, ".steward-draft", "policy.yaml"), """
            artifacts:
              - path: README.md
                role: readme
                required: true
            """);
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Hello\n");
        _origDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_tempDir);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_origDir);
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Theory]
    [InlineData(".steward-draft")]
    [InlineData("./.steward-draft")]
    public void Check_BareRelativeConfig_RunsAgainstCurrentDirectory(string configArg)
    {
        var (exitCode, output, error) = CliTestHelper.InvokeCapture("check", "-c", configArg, "--output", "json");

        error.Should().NotContain("ArgumentException");
        exitCode.Should().Be(0, because: output + error);
        output.Should().Contain("\"diagnostics\": []");
    }

    [Fact]
    public void ConfigDoctor_BareRelativeConfig_RunsNormally()
    {
        var (exitCode, output, error) = CliTestHelper.InvokeCapture("config", "doctor", "-c", ".steward-draft", "--output", "json");

        error.Should().NotContain("ArgumentException");
        exitCode.Should().BeOneOf([0, 1], because: output + error);
        output.Should().Contain("\"command\": \"config doctor\"");
    }

    [Fact]
    public void Init_WithConfig_WritesScaffoldIntoThatDirectory()
    {
        var (exitCode, output, error) = CliTestHelper.InvokeCapture("init", "-c", ".steward-new", "--profile", "minimal");

        exitCode.Should().Be(0, because: output + error);
        File.Exists(Path.Combine(_tempDir, ".steward-new", "config.yaml")).Should().BeTrue();
        Directory.Exists(Path.Combine(_tempDir, ".steward")).Should().BeFalse();
    }

    [Fact]
    public void Init_WithoutConfig_WritesDefaultStewardDirectory()
    {
        var (exitCode, output, error) = CliTestHelper.InvokeCapture("init", "--profile", "minimal");

        exitCode.Should().Be(0, because: output + error);
        File.Exists(Path.Combine(_tempDir, ".steward", "config.yaml")).Should().BeTrue();
    }

    [Fact]
    public void Init_WithExistingConfigDirectory_RefusesToOverwrite()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward-draft", "config.yaml"), "profile: minimal\n");

        var (exitCode, _, _) = CliTestHelper.InvokeCapture("init", "-c", ".steward-draft");

        exitCode.Should().Be(2);
    }
}
