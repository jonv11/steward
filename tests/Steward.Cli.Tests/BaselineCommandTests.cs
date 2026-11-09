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
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), Policy);
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

    private const string Policy = "validation:\n  severity_overrides:\n    STWD-008: error\n";

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

    // ── check applies the baseline (ticket 04) ─────────────────────────────────

    [Fact]
    public void Check_WithoutBaseline_FailsOnExistingDebt()
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check");

        exitCode.Should().Be(1);
        output.Should().NotContain("Baseline debt");
    }

    [Fact]
    public void Check_AfterGenerate_PassesAndReportsSuppressedCount()
    {
        CliTestHelper.InvokeCapture("baseline", "generate");

        var (exitCode, output, error) = CliTestHelper.InvokeCapture("check");

        exitCode.Should().Be(0, because: output + error);
        output.Should().Contain("Baseline debt: 2 grandfathered diagnostic(s) suppressed by .steward/baseline.json");
        output.Should().NotContain("missing.md");
    }

    [Fact]
    public void Check_NewViolationAfterGenerate_FailsWhileBaselinedStaySuppressed()
    {
        CliTestHelper.InvokeCapture("baseline", "generate");
        File.WriteAllText(Path.Combine(_tempDir, "NEW.md"), "# New\n\n[broken](nowhere.md)\n");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check", "--output", "json");

        exitCode.Should().Be(1);
        using var doc = JsonDocument.Parse(output);
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("summary").GetProperty("errors").GetInt32().Should().Be(1);
        data.GetProperty("summary").GetProperty("baseline").GetProperty("suppressed").GetInt32().Should().Be(2);
        data.GetProperty("diagnostics").EnumerateArray()
            .Where(d => d.GetProperty("severity").GetString() == "error")
            .Should().ContainSingle()
            .Which.GetProperty("path").GetString().Should().Be("NEW.md");
    }

    [Fact]
    public void Check_BaselinedViolationMovedToAnotherLine_StaysSuppressed()
    {
        CliTestHelper.InvokeCapture("baseline", "generate");
        File.WriteAllText(Path.Combine(_tempDir, "README.md"),
            "# Readme\n\nIntro paragraph.\n\nMore text.\n\n[also gone](other.md)\n\n[gone](missing.md)\n");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check");

        exitCode.Should().Be(0, because: output);
    }

    [Fact]
    public void Check_BaselineDisabledInPolicy_EnforcesEverything()
    {
        CliTestHelper.InvokeCapture("baseline", "generate");
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), Policy + "baseline:\n  enabled: false\n");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check");

        exitCode.Should().Be(1);
        output.Should().NotContain("Baseline debt");
    }

    [Fact]
    public void Check_WithoutBaseline_JsonSummaryHasNoBaselineField()
    {
        var (_, output, _) = CliTestHelper.InvokeCapture("check", "--output", "json");

        using var doc = JsonDocument.Parse(output);
        doc.RootElement.GetProperty("data").GetProperty("summary").TryGetProperty("baseline", out _).Should().BeFalse();
    }

    [Fact]
    public void Check_Sarif_OmitsBaselinedResults()
    {
        CliTestHelper.InvokeCapture("baseline", "generate");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check", "--output", "sarif");

        exitCode.Should().Be(0);
        output.Should().NotContain("missing.md");
    }

    [Fact]
    public void Check_MalformedBaseline_IsUsageError()
    {
        File.WriteAllText(BaselinePath, "{ not json");

        var (exitCode, _, error) = CliTestHelper.InvokeCapture("check");

        exitCode.Should().Be(2);
        error.Should().Contain("baseline");
    }

    [Fact]
    public void Check_PathsScope_AppliesBaselineToScopedDiagnostics()
    {
        CliTestHelper.InvokeCapture("baseline", "generate");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check", "--paths", "README.md");

        exitCode.Should().Be(0, because: output);
    }

    // ── drift (ticket 05) ──────────────────────────────────────────────────────

    [Fact]
    public void Check_FixedBaselinedViolation_ReportsDriftAndStillPasses()
    {
        CliTestHelper.InvokeCapture("baseline", "generate");
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Readme\n\n[also gone](other.md)\n");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check");

        exitCode.Should().Be(0, because: output);
        output.Should().Contain("Baseline drift").And.Contain("baseline-drift STWD-008 README.md:")
            .And.Contain("missing.md").And.Contain("steward baseline generate");
        output.Should().Contain("Info: 0");
    }

    [Fact]
    public void Check_FixedBaselinedViolation_JsonListsDrift()
    {
        CliTestHelper.InvokeCapture("baseline", "generate");
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Readme\n");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check", "--output", "json");

        exitCode.Should().Be(0);
        using var doc = JsonDocument.Parse(output);
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("summary").GetProperty("baseline").GetProperty("drift").GetInt32().Should().Be(2);
        data.GetProperty("baselineDrift").EnumerateArray().Should().HaveCount(2)
            .And.AllSatisfy(e => e.GetProperty("ruleId").GetString().Should().Be("STWD-008"));
    }

    [Fact]
    public void Check_ScopedRun_ReportsDriftOnlyForInScopeFiles()
    {
        CliTestHelper.InvokeCapture("baseline", "generate");
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Readme\n");
        File.WriteAllText(Path.Combine(_tempDir, "OTHER.md"), "# Other\n");

        var outOfScope = CliTestHelper.InvokeCapture("check", "--paths", "OTHER.md");
        var inScope = CliTestHelper.InvokeCapture("check", "--paths", "README.md");

        outOfScope.Output.Should().NotContain("Baseline drift");
        inScope.Output.Should().Contain("Baseline drift");
    }

    [Fact]
    public void Check_NoDrift_OmitsDriftSection()
    {
        CliTestHelper.InvokeCapture("baseline", "generate");

        var (_, output, _) = CliTestHelper.InvokeCapture("check");

        output.Should().NotContain("Baseline drift");
    }

    // ── baseline status (ticket 06) ────────────────────────────────────────────

    [Fact]
    public void Status_NoBaseline_ReportsInactiveAndSucceeds()
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("baseline", "status");

        exitCode.Should().Be(0);
        output.Should().Contain("No baseline active");
    }

    [Fact]
    public void Status_ReportsEntriesByRuleAndDrift()
    {
        CliTestHelper.InvokeCapture("baseline", "generate");
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Readme\n\n[also gone](other.md)\n");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("baseline", "status");

        exitCode.Should().Be(0, because: output);
        output.Should().Contain("2 entries: 1 still grandfathered, 1 drifted").And.Contain("STWD-008  2  (1 drifted)")
            .And.Contain("0 day(s) old");
    }

    [Fact]
    public void Status_Json_ReportsCounts()
    {
        CliTestHelper.InvokeCapture("baseline", "generate");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("baseline", "status", "--output", "json");

        exitCode.Should().Be(0);
        using var doc = JsonDocument.Parse(output);
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("active").GetBoolean().Should().BeTrue();
        data.GetProperty("entries").GetInt32().Should().Be(2);
        data.GetProperty("matched").GetInt32().Should().Be(2);
        data.GetProperty("drifted").GetInt32().Should().Be(0);
        data.GetProperty("ageDays").GetInt32().Should().Be(0);
        data.GetProperty("byRule")[0].GetProperty("rule").GetString().Should().Be("STWD-008");
    }

    [Fact]
    public void Status_Json_NoBaseline_IsInactive()
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("baseline", "status", "--output", "json");

        exitCode.Should().Be(0);
        using var doc = JsonDocument.Parse(output);
        doc.RootElement.GetProperty("data").GetProperty("active").GetBoolean().Should().BeFalse();
    }

    // ── config validate / doctor (ticket 07) ───────────────────────────────────

    [Fact]
    public void ConfigValidate_EnabledButMissingBaseline_IsUsageError()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), Policy + "baseline:\n  enabled: true\n");

        var (exitCode, _, error) = CliTestHelper.InvokeCapture("config", "validate");

        exitCode.Should().Be(2);
        error.Should().Contain("baseline.json").And.Contain("does not exist");
    }

    [Fact]
    public void ConfigValidate_MalformedBaseline_IsUsageError()
    {
        File.WriteAllText(BaselinePath, "{\"entries\": 3}");

        var (exitCode, _, error) = CliTestHelper.InvokeCapture("config", "validate");

        exitCode.Should().Be(2);
        error.Should().Contain("baseline.json");
    }

    [Fact]
    public void ConfigValidate_ValidBaseline_Passes()
    {
        CliTestHelper.InvokeCapture("baseline", "generate");

        var (exitCode, output, error) = CliTestHelper.InvokeCapture("config", "validate");

        exitCode.Should().Be(0, because: output + error);
    }

    [Fact]
    public void ConfigDoctor_ReportsUnknownRuleAndDeletedFileEntries()
    {
        File.WriteAllText(Path.Combine(_tempDir, "OLD.md"), "# Old\n\n[x](nope.md)\n");
        CliTestHelper.InvokeCapture("baseline", "generate");
        File.Delete(Path.Combine(_tempDir, "OLD.md"));
        var json = File.ReadAllText(BaselinePath).Replace("\"entries\": [",
            "\"entries\": [\n    { \"rule\": \"STWD-999\", \"path\": \"README.md\", \"selector\": \"x\" },");
        File.WriteAllText(BaselinePath, json);

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("config", "doctor");

        exitCode.Should().Be(1);
        output.Should().Contain("[dead-baseline-entry]").And.Contain("STWD-999").And.Contain("'OLD.md'");
        output.Should().NotContain("'README.md' in");
    }

    [Fact]
    public void ConfigDoctor_GrandfatheredMissingRequiredFile_IsNotDead()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"),
            Policy + "artifacts:\n  - path: CONTRIBUTING.md\n    role: guide\n    required: true\n");
        CliTestHelper.InvokeCapture("baseline", "generate");
        File.ReadAllText(BaselinePath).Should().Contain("CONTRIBUTING.md");

        var (_, output, _) = CliTestHelper.InvokeCapture("config", "doctor");

        output.Should().NotContain("dead-baseline-entry");
    }
}
