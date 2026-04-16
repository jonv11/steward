using FluentAssertions;
using Steward.Cli.Tests.Helpers;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Cli.Tests;

[Collection("Console")]
public class ArtifactFamiliesCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _origDir;

    public ArtifactFamiliesCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "steward-af-" + Guid.NewGuid().ToString("N")[..8]);
        RepositoryFixture.CopyFixtureTo("artifact-families", _tempDir);
        Directory.CreateDirectory(Path.Combine(_tempDir, ".git"));
        _origDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_tempDir);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_origDir);
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    // ── check ────────────────────────────────────────────────────────────────

    [Fact]
    public void Check_FixtureRepo_ReportsFamilySchemaDiagnostics()
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check", "--output", "json");

        exitCode.Should().Be(1, "fixture has files violating family schemas");

        // ADR-002 is missing 'status' field
        output.Should().Contain("ADR-002");
        output.Should().Contain("[family: adr]");

        // ADR-003 has invalid status value
        output.Should().Contain("ADR-003");
        output.Should().Contain("Invalid");
    }

    [Fact]
    public void Check_ValidFamilyFile_NoDiagnosticsForThatFile()
    {
        var (_, output, _) = CliTestHelper.InvokeCapture("check", "--output", "json");

        // ADR-001 is fully valid — should not appear in diagnostics
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var diagnosticLines = lines.Where(l => l.Contains("ADR-001-initial")).ToList();
        diagnosticLines.Should().BeEmpty("ADR-001 is a valid ADR and should produce no diagnostics");
    }

    [Fact]
    public void Check_ConfigValidate_PassesOnFixturePolicy()
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("config", "validate");

        exitCode.Should().Be(0, "artifact-families fixture has a valid policy");
        output.ToLowerInvariant().Should().Contain("valid");
    }

    // ── explain path ─────────────────────────────────────────────────────────

    [Fact]
    public void ExplainPath_ValidAdr_ShowsFamilyName()
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture(
            "explain", "path", "docs/decisions/adrs/ADR-001-initial.md");

        exitCode.Should().Be(0);
        output.Should().Contain("Family:");
        output.Should().Contain("adr");
    }

    [Fact]
    public void ExplainPath_ValidAdr_ShowsFamilyDisplayName()
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture(
            "explain", "path", "docs/decisions/adrs/ADR-001-initial.md");

        exitCode.Should().Be(0);
        output.Should().Contain("Architecture Decision Record");
    }

    [Fact]
    public void ExplainPath_ValidAdr_ShowsFamilyRequiredFields()
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture(
            "explain", "path", "docs/decisions/adrs/ADR-001-initial.md");

        exitCode.Should().Be(0);
        output.Should().Contain("Required frontmatter:");
        output.Should().Contain("status");
    }

    [Fact]
    public void ExplainPath_Json_IncludesFamilyFields()
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture(
            "explain", "path", "docs/decisions/adrs/ADR-001-initial.md",
            "--output", "json");

        exitCode.Should().Be(0);
        output.Should().Contain("matchedFamily");
        output.Should().Contain("adr");
        output.Should().Contain("familyDisplayName");
        output.Should().Contain("Architecture Decision Record");
    }

    [Fact]
    public void ExplainPath_FileOutsideAnyFamily_NoFamilyShown()
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture(
            "explain", "path", "README.md");

        exitCode.Should().Be(0);
        output.Should().NotContain("Family:");
    }

    // ── status ───────────────────────────────────────────────────────────────

    [Fact]
    public void Status_FixtureRepo_ShowsFamilySummary()
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("status");

        exitCode.Should().Be(0);
        output.Should().Contain("Artifact Families:");
        output.Should().Contain("adr");
        output.Should().Contain("rfc");
    }

    [Fact]
    public void Status_Json_IncludesArtifactFamiliesField()
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("status", "--output", "json");

        exitCode.Should().Be(0);
        output.Should().Contain("artifactFamilies");
        output.Should().Contain("\"family\":");
        output.Should().Contain("matchedCount");
    }

    // ── orient ───────────────────────────────────────────────────────────────

    [Fact]
    public void Orient_FixtureRepo_ClassifiesAdrFilesAsFamily()
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("orient", "--depth", "4");

        exitCode.Should().Be(0);
        output.Should().Contain("family:adr");
    }

    // ── config doctor ─────────────────────────────────────────────────────────

    [Fact]
    public void ConfigDoctor_FixtureRepo_NoUnreachableFamilyPatterns()
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("config", "doctor");

        exitCode.Should().Be(0);
        // The fixture has ADR and RFC files, so the family patterns should be reachable
        output.Should().NotContain("unreachable-family-pattern");
    }

    [Fact]
    public void ConfigDoctor_UnreachableFamilyPattern_ReportsIssue()
    {
        // Replace policy with one that has a family pattern matching nothing
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            repository:
              name: test
              type: software
            artifact_families:
              - family: nonexistent
                match:
                  path_pattern: "nonexistent/path/**"
            """);

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("config", "doctor");

        output.Should().Contain("unreachable-family-pattern");
        output.Should().Contain("nonexistent");
    }

    // ── config validate ───────────────────────────────────────────────────────

    [Fact]
    public void ConfigValidate_InvalidFamilyDefinition_RejectsWithClearError()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            artifact_families:
              - family: bad
                match:
                  path_pattern: "[invalid-glob"
            """);

        var (exitCode, _, error) = CliTestHelper.InvokeCapture("config", "validate");

        exitCode.Should().NotBe(0);
        error.Should().Contain("path_pattern");
    }

    [Fact]
    public void ConfigValidate_DuplicateFamilyNames_RejectsWithClearError()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            artifact_families:
              - family: adr
                match:
                  path_pattern: "docs/adrs/**"
              - family: adr
                match:
                  path_pattern: "docs/adrs2/**"
            """);

        var (exitCode, _, error) = CliTestHelper.InvokeCapture("config", "validate");

        exitCode.Should().NotBe(0);
        error.Should().Contain("Duplicate");
    }
}
