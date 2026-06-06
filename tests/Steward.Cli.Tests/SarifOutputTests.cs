using System.Text.Json;
using FluentAssertions;
using Steward.Cli.Tests.Helpers;
using Xunit;

namespace Steward.Cli.Tests;

[Collection("Console")]
public class SarifOutputTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _origDir;

    public SarifOutputTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "steward-sarif-" + Guid.NewGuid().ToString("N")[..8]);
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
    public void Check_SarifOutput_ProducesValidSchema()
    {
        // Repo with a required artifact missing → STWD-001 fires
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            artifacts:
              - path: README.md
                role: readme
                required: true
            """);

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check", "--output", "sarif");

        exitCode.Should().Be(1); // Validation failure

        var doc = JsonDocument.Parse(output);
        var root = doc.RootElement;

        root.GetProperty("version").GetString().Should().Be("2.1.0");

        var runs = root.GetProperty("runs");
        runs.GetArrayLength().Should().Be(1);

        var run = runs[0];
        run.GetProperty("tool").GetProperty("driver").GetProperty("name").GetString().Should().Be("Steward");

        var results = run.GetProperty("results");
        results.GetArrayLength().Should().BeGreaterThan(0);

        // Each result must have ruleId, level, message
        foreach (var result in results.EnumerateArray())
        {
            result.GetProperty("ruleId").GetString().Should().NotBeNullOrEmpty();
            result.GetProperty("level").GetString().Should().BeOneOf("error", "warning", "note");
            result.GetProperty("message").GetProperty("text").GetString().Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void Check_SarifOutput_IncludesRulesArray()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            artifacts:
              - path: README.md
                role: readme
                required: true
            """);

        var (_, output, _) = CliTestHelper.InvokeCapture("check", "--output", "sarif");
        var doc = JsonDocument.Parse(output);
        var driver = doc.RootElement.GetProperty("runs")[0].GetProperty("tool").GetProperty("driver");

        var rules = driver.GetProperty("rules");
        rules.GetArrayLength().Should().BeGreaterThan(0);

        foreach (var rule in rules.EnumerateArray())
        {
            rule.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
            rule.GetProperty("shortDescription").GetProperty("text").GetString().Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void Check_SarifOutput_SeverityMapping_ErrorMapsToError()
    {
        // STWD-001 is an error (missing required artifact)
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            artifacts:
              - path: README.md
                role: readme
                required: true
            """);

        var (_, output, _) = CliTestHelper.InvokeCapture("check", "--output", "sarif");
        var doc = JsonDocument.Parse(output);
        var results = doc.RootElement.GetProperty("runs")[0].GetProperty("results");

        var stwd001 = results.EnumerateArray()
            .FirstOrDefault(r => r.GetProperty("ruleId").GetString() == "STWD-001");

        stwd001.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        stwd001.GetProperty("level").GetString().Should().Be("error");
    }

    [Fact]
    public void Check_SarifOutput_LocationIncludesPath_WhenDiagnosticHasPath()
    {
        // STWD-018 fires on a broken fragment anchor — has a path
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            validation:
              disabled_rules:
                - STWD-001
                - STWD-009
            """);
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Hello\n\nSee [guide](guide.md#nonexistent).");
        File.WriteAllText(Path.Combine(_tempDir, "guide.md"), "# Guide\n\n## Overview\n\nContent.");

        var (_, output, _) = CliTestHelper.InvokeCapture("check", "--output", "sarif");
        var doc = JsonDocument.Parse(output);
        var results = doc.RootElement.GetProperty("runs")[0].GetProperty("results");

        var stwd018 = results.EnumerateArray()
            .FirstOrDefault(r => r.GetProperty("ruleId").GetString() == "STWD-018");

        stwd018.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        var locations = stwd018.GetProperty("locations");
        locations.GetArrayLength().Should().BeGreaterThan(0);
        locations[0].GetProperty("physicalLocation")
            .GetProperty("artifactLocation")
            .GetProperty("uri").GetString()
            .Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Check_SarifOutput_CleanRepo_ProducesEmptyResults()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            artifacts:
              - path: README.md
                role: readme
                required: true
            """);
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Hello");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check", "--output", "sarif");

        exitCode.Should().Be(0);
        var doc = JsonDocument.Parse(output);
        var results = doc.RootElement.GetProperty("runs")[0].GetProperty("results");
        results.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public void NonCheckCommand_SarifOutput_ReturnsUsageError()
    {
        var (exitCode, output, error) = CliTestHelper.InvokeCapture("orient", "--output", "sarif");

        exitCode.Should().Be(2);
        output.Should().BeEmpty();
        error.Should().Contain("SARIF output is supported only by 'steward check'.");
        error.Should().Contain("--output text");
        error.Should().Contain("--output json");
    }
}
