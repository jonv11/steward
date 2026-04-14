using FluentAssertions;
using Steward.Cli.Tests.Helpers;
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
}
