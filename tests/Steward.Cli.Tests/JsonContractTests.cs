using System.Text.Json;
using FluentAssertions;
using Steward.Cli.Tests.Helpers;
using Steward.Core;
using Xunit;

namespace Steward.Cli.Tests;

/// <summary>
/// Contract tests for the standard JSON envelope, structured errors,
/// and enriched output shapes introduced for CC-01 through CC-10.
/// </summary>
[Collection("Console")]
public class JsonContractTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _origDir;

    public JsonContractTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "steward-json-contract-" + Guid.NewGuid().ToString("N")[..8]);
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

    private void SetupGovernedRepo()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "config.yaml"), "profile: software\n");
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            repository:
              name: contract-test
              type: software
            governance:
              start_here:
                - README.md
            artifacts:
              - path: README.md
                role: authoritative
                required: true
            """);
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Contract Test\n\nHello.");
    }

    private static JsonElement ParseJson(string json)
    {
        return JsonDocument.Parse(json).RootElement;
    }

    // --- CC-02: Standard envelope on every JSON command ---

    [Theory]
    [InlineData("version")]
    [InlineData("check")]
    [InlineData("status")]
    [InlineData("orient")]
    public void StandardEnvelope_HasRequiredFields(string command)
    {
        SetupGovernedRepo();
        var (_, output, _) = CliTestHelper.InvokeCapture(command, "--output", "json");
        var root = ParseJson(output);

        root.TryGetProperty("schemaVersion", out _).Should().BeTrue($"'{command}' envelope must have schemaVersion");
        root.TryGetProperty("command", out _).Should().BeTrue($"'{command}' envelope must have command");
        root.TryGetProperty("success", out var success).Should().BeTrue($"'{command}' envelope must have success");
        root.TryGetProperty("exitCode", out _).Should().BeTrue($"'{command}' envelope must have exitCode");
        root.TryGetProperty("data", out _).Should().BeTrue($"'{command}' envelope must have data");
        success.GetBoolean().Should().BeTrue($"'{command}' should succeed on valid repo");
    }

    [Fact]
    public void StandardEnvelope_SchemaVersion_IsStewardJsonV1()
    {
        SetupGovernedRepo();
        var (_, output, _) = CliTestHelper.InvokeCapture("version", "--output", "json");
        var root = ParseJson(output);

        root.GetProperty("schemaVersion").GetString().Should().Be("steward-json/v1");
    }

    [Fact]
    public void RootParseFailure_WithJsonOutput_ReturnsStructuredError()
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("not-a-real-command", "--output", "json");

        exitCode.Should().Be(ExitCodes.UsageError);
        var root = ParseJson(output);
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("data").GetProperty("error").GetProperty("kind").GetString().Should().Be("usage-error");
        root.GetProperty("data").GetProperty("error").GetProperty("details").GetProperty("errors").GetArrayLength().Should().BeGreaterThan(0);
    }

    // --- CC-03: Process success vs domain result ---

    [Fact]
    public void Check_WithViolations_StandardEnvelope_SuccessIsTrue()
    {
        // Policy requires MISSING.md which doesn't exist → violations
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            artifacts:
              - path: MISSING.md
                role: readme
                required: true
            """);

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check", "--output", "json");

        exitCode.Should().Be(ExitCodes.ValidationFailure);
        var root = ParseJson(output);
        // CC-03: success=true means process executed correctly; violations are domain results
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        root.GetProperty("exitCode").GetInt32().Should().Be(ExitCodes.ValidationFailure);
        root.GetProperty("data").GetProperty("summary").GetProperty("pass").GetBoolean().Should().BeFalse();
    }

    // --- CC-01: JSON error contract for explain command ---

    [Fact]
    public void Explain_UnknownRule_JsonError_HasStructuredFields()
    {
        SetupGovernedRepo();
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("explain", "STWD-999", "--output", "json");

        exitCode.Should().Be(ExitCodes.UsageError);
        var root = ParseJson(output);
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("data").GetProperty("error").GetProperty("kind").GetString().Should().Be("unknown-rule");
    }

    // --- CC-01: JSON error contract for search command ---

    [Fact]
    public void Search_InvalidRegex_JsonError_HasStructuredFields()
    {
        SetupGovernedRepo();
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("search", "[invalid", "--regex", "--output", "json");

        exitCode.Should().Be(ExitCodes.UsageError);
        var root = ParseJson(output);
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("data").GetProperty("error").GetProperty("kind").GetString().Should().Be("invalid-query");
    }

    // --- CC-05: explain path has exists field ---

    [Fact]
    public void ExplainPath_ExistingFile_HasExistsTrue()
    {
        SetupGovernedRepo();
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("explain", "path", "README.md", "--output", "json");

        exitCode.Should().Be(ExitCodes.Success);
        output.Should().Contain("\"exists\": true");
    }

    [Fact]
    public void ExplainPath_MissingFile_HasExistsFalse()
    {
        SetupGovernedRepo();
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("explain", "path", "nonexistent.md", "--output", "json");

        exitCode.Should().Be(ExitCodes.Success);
        output.Should().Contain("\"exists\": false");
    }

    // --- CC-06: Diagnostic details field ---

    [Fact]
    public void Check_DiagnosticDetails_IncludedInJsonOutput()
    {
        // Set up a policy with a naming convention that will be violated
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            artifacts:
              - path: MISSING.md
                role: readme
                required: true
            """);

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check", "--output", "json");

        exitCode.Should().Be(ExitCodes.ValidationFailure);
        // Diagnostics array should exist in the data
        var root = ParseJson(output);
        root.GetProperty("data").GetProperty("diagnostics").GetArrayLength().Should().BeGreaterThan(0);
    }

    // --- CC-07: md query normalized shape ---

    [Fact]
    public void MdQuery_SingleFile_HasResultsArray()
    {
        SetupGovernedRepo();
        var mdFile = Path.Combine(_tempDir, "test.md");
        File.WriteAllText(mdFile, "# Hello\n\nContent here.\n\n## World\n\nMore content.");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("md", "query", "test.md", "heading[Hello]", "--output", "json");

        exitCode.Should().Be(ExitCodes.Success);
        // CC-07: single-file query must have results[] wrapper just like batch
        output.Should().Contain("\"results\"");
        output.Should().Contain("\"matchCount\"");
        var root = ParseJson(output);
        var data = root.GetProperty("data");
        data.GetProperty("results").GetArrayLength().Should().Be(1);
        data.GetProperty("results")[0].GetProperty("file").GetString().Should().Be("test.md");
        data.GetProperty("results")[0].GetProperty("matchCount").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public void MdQuery_SingleFile_MatchesHaveRange()
    {
        var mdFile = Path.Combine(_tempDir, "test.md");
        File.WriteAllText(mdFile, "# Hello\n\nContent here.\n");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("md", "query", "test.md", "heading[Hello]", "--output", "json");

        exitCode.Should().Be(ExitCodes.Success);
        output.Should().Contain("\"range\"");
        output.Should().Contain("\"start\"");
        output.Should().Contain("\"end\"");
    }

    // --- CC-08: Structured config validate errors ---

    [Fact]
    public void ConfigValidate_InvalidConfig_HasStructuredErrors()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "config.yaml"), "profile: nonexistent-profile\n");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("config", "validate", "--output", "json");

        exitCode.Should().Be(ExitCodes.UsageError);
        // CC-08: errors should be structured objects with file + message, not plain strings
        output.Should().Contain("\"file\"");
        output.Should().Contain("\"message\"");
    }

    [Fact]
    public void SetupFailure_WithJsonOutput_ReturnsStructuredError()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "config.yaml"), "profile: definitely-not-valid\n");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("orient", "--output", "json");

        exitCode.Should().Be(ExitCodes.UsageError);
        var root = ParseJson(output);
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("data").GetProperty("error").GetProperty("kind").GetString().Should().Be("configuration-error");
    }

    [Fact]
    public void Init_Success_JsonOutput_HasStructuredFields()
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("init", "--profile", "minimal", "--output", "json");

        exitCode.Should().Be(ExitCodes.Success);
        var root = ParseJson(output);
        var data = root.GetProperty("data");
        data.GetProperty("profile").GetString().Should().Be("minimal");
        data.GetProperty("configDirectory").GetString().Should().Be(".steward");
        data.GetProperty("filesWritten").GetArrayLength().Should().BeGreaterThan(0);
        data.GetProperty("scaffoldedArtifacts").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public void Init_AlreadyInitialized_JsonOutput_ReturnsStructuredError()
    {
        SetupGovernedRepo();

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("init", "--output", "json");

        exitCode.Should().Be(ExitCodes.UsageError);
        var root = ParseJson(output);
        root.GetProperty("data").GetProperty("error").GetProperty("kind").GetString().Should().Be("already-initialized");
    }

    [Fact]
    public void MdSplitPlan_MissingFile_JsonOutput_ReturnsStructuredError()
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("md", "split", "plan", "missing.md", "--output", "json");

        exitCode.Should().Be(ExitCodes.UsageError);
        var root = ParseJson(output);
        root.GetProperty("data").GetProperty("error").GetProperty("kind").GetString().Should().Be("file-not-found");
    }

    // --- CC-10: Refactor move preview enrichment ---

    [Fact]
    public void RefactorMove_Preview_HasEnrichedFields()
    {
        SetupGovernedRepo();
        File.WriteAllText(Path.Combine(_tempDir, "source.md"), "# Source\n\nContent.");
        File.WriteAllText(Path.Combine(_tempDir, "linking.md"), "# Links\n\nSee [source](source.md).");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("refactor", "move", "source.md", "dest.md",
            "--preview", "--output", "json");

        exitCode.Should().Be(ExitCodes.Success);
        var root = ParseJson(output);
        var data = root.GetProperty("data");
        data.GetProperty("sourceExists").GetBoolean().Should().BeTrue();
        data.GetProperty("collision").GetBoolean().Should().BeFalse();
        data.GetProperty("applied").GetBoolean().Should().BeFalse();
        data.GetProperty("affectedFileCount").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public void RefactorMove_NoMode_JsonError()
    {
        SetupGovernedRepo();
        File.WriteAllText(Path.Combine(_tempDir, "source.md"), "# Source");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("refactor", "move", "source.md", "dest.md",
            "--output", "json");

        exitCode.Should().Be(ExitCodes.UsageError);
        var root = ParseJson(output);
        root.GetProperty("data").GetProperty("error").GetProperty("kind").GetString().Should().Be("missing-mode");
    }

    [Fact]
    public void Search_JsonOutput_IncludesHandoffFields()
    {
        SetupGovernedRepo();
        File.WriteAllText(Path.Combine(_tempDir, "notes.md"), "# Intro\n\nSearchable content.\n");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("search", "Searchable", "--output", "json");

        exitCode.Should().Be(ExitCodes.Success);
        var match = ParseJson(output).GetProperty("data").GetProperty("matches")[0];
        match.TryGetProperty("sectionHeading", out _).Should().BeTrue();
        match.TryGetProperty("sectionRange", out _).Should().BeTrue();
        match.TryGetProperty("mdQuerySelector", out _).Should().BeTrue();
    }

    [Fact]
    public void Refs_JsonOutput_IncludesConcreteLinkInstances()
    {
        SetupGovernedRepo();
        File.WriteAllText(Path.Combine(_tempDir, "source.md"), "# Intro\n\nSee [Guide](README.md#contract-test).\n");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("refs", "source.md", "--output", "json");

        exitCode.Should().Be(ExitCodes.Success);
        var data = ParseJson(output).GetProperty("data");
        data.TryGetProperty("outboundLinks", out var outboundLinks).Should().BeTrue();
        outboundLinks.GetArrayLength().Should().Be(1);
        outboundLinks[0].TryGetProperty("sourcePath", out _).Should().BeTrue();
        outboundLinks[0].TryGetProperty("sourceLine", out _).Should().BeTrue();
        outboundLinks[0].TryGetProperty("rawTarget", out _).Should().BeTrue();
        outboundLinks[0].TryGetProperty("mdQuerySelector", out _).Should().BeTrue();
    }

    [Fact]
    public void ExplainPath_JsonOutput_IncludesProvenanceFields()
    {
        SetupGovernedRepo();

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("explain", "path", "README.md", "--output", "json");

        exitCode.Should().Be(ExitCodes.Success);
        var data = ParseJson(output).GetProperty("data");
        data.TryGetProperty("discovered", out _).Should().BeTrue();
        data.TryGetProperty("isExplicitArtifact", out _).Should().BeTrue();
        data.TryGetProperty("isFamilyMatch", out _).Should().BeTrue();
        data.TryGetProperty("governanceSources", out var governanceSources).Should().BeTrue();
        governanceSources.TryGetProperty("explicitArtifactPath", out _).Should().BeTrue();
        governanceSources.TryGetProperty("frontmatterRequirementPatterns", out _).Should().BeTrue();
        governanceSources.TryGetProperty("maintenanceArtifactIds", out _).Should().BeTrue();
    }
}
