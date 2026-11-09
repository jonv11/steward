using System.Text.Json;
using FluentAssertions;
using Steward.Cli.Tests.Helpers;
using Xunit;

namespace Steward.Cli.Tests;

[Collection("Console")]
public class BaselineCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _origDir;

    public BaselineCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "steward-baseline-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(Path.Combine(_tempDir, ".git"));
        Directory.CreateDirectory(Path.Combine(_tempDir, ".steward"));
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "config.yaml"), "profile: minimal\n");
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Readme\n\n[gone](missing.md)\n\n[also gone](other.md)\n");
        _origDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_tempDir);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_origDir);
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private string BaselinePath => Path.Combine(_tempDir, ".steward", "baseline.json");

    [Fact]
    public void Generate_WritesEveryCurrentDiagnostic()
    {
        var (exitCode, output, error) = CliTestHelper.InvokeCapture("baseline", "generate");

        exitCode.Should().Be(0, because: output + error);
        output.Should().Contain("Wrote .steward/baseline.json with 2 entries.").And.Contain("STWD-008  2");
        var json = File.ReadAllText(BaselinePath);
        json.Should().Contain("missing.md").And.Contain("other.md");
    }

    [Fact]
    public void Generate_Twice_LeavesFileIdentical()
    {
        CliTestHelper.InvokeCapture("baseline", "generate");
        var first = File.ReadAllText(BaselinePath);

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("baseline", "generate");

        exitCode.Should().Be(0);
        output.Should().Contain("already current");
        File.ReadAllText(BaselinePath).Should().Be(first);
    }

    [Fact]
    public void Generate_Json_UsesStandardEnvelope()
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("baseline", "generate", "--output", "json");

        exitCode.Should().Be(0);
        using var doc = JsonDocument.Parse(output);
        doc.RootElement.GetProperty("command").GetString().Should().Be("baseline generate");
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("path").GetString().Should().Be(".steward/baseline.json");
        data.GetProperty("entries").GetInt32().Should().Be(2);
        data.GetProperty("byRule").GetProperty("STWD-008").GetInt32().Should().Be(2);
    }

    [Fact]
    public void Generate_WithRelativeConfig_WritesBesideThatConfig()
    {
        Directory.CreateDirectory(Path.Combine(_tempDir, ".steward-draft"));
        File.WriteAllText(Path.Combine(_tempDir, ".steward-draft", "config.yaml"), "profile: minimal\n");

        var (exitCode, output, error) = CliTestHelper.InvokeCapture("baseline", "generate", "-c", ".steward-draft");

        exitCode.Should().Be(0, because: output + error);
        File.Exists(Path.Combine(_tempDir, ".steward-draft", "baseline.json")).Should().BeTrue();
        File.Exists(BaselinePath).Should().BeFalse();
    }

    [Fact]
    public void Generate_WithoutConfig_IsUsageError()
    {
        Directory.Delete(Path.Combine(_tempDir, ".steward"), true);

        var (exitCode, _, _) = CliTestHelper.InvokeCapture("baseline", "generate");

        exitCode.Should().Be(2);
    }
}
