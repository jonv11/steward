using FluentAssertions;
using Steward.Cli.Tests.Helpers;
using Xunit;

namespace Steward.Cli.Tests;

[Collection("Console")]
public class CheckFixTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _origDir;

    public CheckFixTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "steward-chkfix-" + Guid.NewGuid().ToString("N")[..8]);
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
    public void Check_DryRun_ShowsFixPlan_DoesNotApply()
    {
        // Setup: stale structure document — maintainer will detect drift
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            maintenance:
              artifacts:
                - id: structure
                  path: STRUCTURE.md
                  type: structure-document
            """);
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Hello");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check", "--dry-run");

        // STWD-007 fires, so exit code is validation failure (warnings present)
        output.Should().Contain("Dry run:");
        output.Should().Contain("[fix]");
        output.Should().Contain("STWD-007");

        // File should NOT be written
        File.Exists(Path.Combine(_tempDir, "STRUCTURE.md")).Should().BeFalse();
    }

    [Fact]
    public void Check_Fix_AppliesStaleArtifactFix()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            maintenance:
              artifacts:
                - id: structure
                  path: STRUCTURE.md
                  type: structure-document
            """);
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Hello");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check", "--fix");

        output.Should().Contain("Applied");
        output.Should().Contain("[fix]");

        // The file should now exist
        File.Exists(Path.Combine(_tempDir, "STRUCTURE.md")).Should().BeTrue();
        var content = File.ReadAllText(Path.Combine(_tempDir, "STRUCTURE.md"));
        content.Should().Contain("# Repository Structure");
    }

    [Fact]
    public void Check_NoFixableIssues_ReportsNoFixes()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            artifacts:
              - path: README.md
                role: readme
                required: true
            """);
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Hello");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check", "--fix");

        exitCode.Should().Be(0);
        output.Should().Contain("No automatic fixes available.");
    }

    [Fact]
    public void Check_Scope_Full_IsDefault()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            artifacts:
              - path: README.md
                role: readme
                required: true
            """);
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Hello");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check");

        exitCode.Should().Be(0);
        output.Should().Contain("No issues found.");
        output.Should().Contain("Result: PASS");
    }

    [Fact]
    public void Check_Scope_Invalid_FallsBackToFull()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            artifacts:
              - path: README.md
                role: readme
                required: true
            """);
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Hello");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check", "--scope", "invalid");

        exitCode.Should().Be(0);
        output.Should().Contain("Result: PASS");
    }

    [Fact]
    public void Check_Paths_LimitsScope()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            artifacts:
              - path: README.md
                role: readme
                required: true
              - path: CHANGELOG.md
                role: changelog
                required: true
            """);
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Hello");
        // CHANGELOG.md is missing — but we only check README.md via --paths

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check", "--paths", "README.md");

        // With paths scope, only README.md is validated — but required artifact check
        // looks at policy regardless of file scope
        output.Should().Contain("Scope: paths");
    }

    [Fact]
    public void Check_CompletionSummary_ShowsMissingArtifacts()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            artifacts:
              - path: README.md
                role: readme
                required: true
              - path: CHANGELOG.md
                role: changelog
                required: true
            """);
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Hello");
        // CHANGELOG.md missing

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check");

        exitCode.Should().Be(1); // Validation failure
        output.Should().Contain("Completion:");
        output.Should().Contain("required artifact(s) missing");
        output.Should().Contain("Result: FAIL");
    }

    [Fact]
    public void Check_CompletionSummary_StaleArtifacts()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            maintenance:
              artifacts:
                - id: structure
                  path: STRUCTURE.md
                  type: structure-document
            """);
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Hello");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check");

        output.Should().Contain("Completion:");
        output.Should().Contain("maintained artifact(s) stale");
    }

    [Fact]
    public void Check_Json_IncludesSTWD009()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            artifacts:
              - path: CHANGELOG.md
                role: changelog
                required: false
            """);
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Hello");

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check", "--output", "json");

        output.Should().Contain("STWD-009");
        output.Should().Contain("broken-reference");
    }

    [Fact]
    public void Check_STWD009_RegisteredInAllRules()
    {
        var rules = Steward.Cli.Commands.CheckCommand.AllRules;
        rules.Should().Contain(r => r.RuleId == "STWD-009");
    }
}
