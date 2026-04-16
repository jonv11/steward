using FluentAssertions;
using Steward.Cli.Tests.Helpers;
using Steward.Core;
using Xunit;

namespace Steward.Cli.Tests;

/// <summary>
/// Contract tests for stable user-facing CLI surfaces.
/// These tests lock the JSON shape and key text-mode behaviors
/// that users and agents depend on across versions.
/// </summary>
[Collection("Console")]
public class StableSurfaceContractTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _origDir;

    public StableSurfaceContractTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "steward-contract-" + Guid.NewGuid().ToString("N")[..8]);
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
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "config.yaml"), """
            profile: software
            """);
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
              - path: LICENSE
                role: authoritative
                required: true
            """);
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Contract Test Repo\n\nA test repository.");
        File.WriteAllText(Path.Combine(_tempDir, "LICENSE"), "MIT License");
    }

    // --- check JSON contract ---

    [Fact]
    public void CheckJson_ContractShape_HasSummaryAndDiagnostics()
    {
        SetupGovernedRepo();

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check", "--output", "json");

        exitCode.Should().Be(ExitCodes.Success);
        // Contract: JSON must contain summary with these fields
        output.Should().Contain("\"summary\"");
        output.Should().Contain("\"scope\"");
        output.Should().Contain("\"filesChecked\"");
        output.Should().Contain("\"errors\"");
        output.Should().Contain("\"warnings\"");
        output.Should().Contain("\"infos\"");
        output.Should().Contain("\"pass\"");
        // Contract: diagnostics array must be present
        output.Should().Contain("\"diagnostics\"");
        // Contract: completion section must be present
        output.Should().Contain("\"completion\"");
    }

    [Fact]
    public void CheckJson_DiagnosticShape_HasRequiredFields()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            artifacts:
              - path: MISSING.md
                role: readme
                required: true
            """);

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check", "--output", "json");

        exitCode.Should().Be(ExitCodes.ValidationFailure);
        // Contract: each diagnostic must have these fields
        output.Should().Contain("\"ruleId\"");
        output.Should().Contain("\"severity\"");
        output.Should().Contain("\"category\"");
        output.Should().Contain("\"message\"");
        // Contract: severity is string, not int
        output.Should().Contain("\"severity\": \"error\"");
    }

    // --- status JSON contract ---

    [Fact]
    public void StatusJson_ContractShape_HasRequiredFields()
    {
        SetupGovernedRepo();

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("status", "--output", "json");

        exitCode.Should().Be(ExitCodes.Success);
        output.Should().Contain("\"repositoryName\"");
        output.Should().Contain("\"fileCount\"");
        output.Should().Contain("\"requiredArtifacts\"");
        output.Should().Contain("\"presentCount\"");
        output.Should().Contain("\"requiredCount\"");
    }

    [Fact]
    public void StatusCoverageJson_ContractShape_HasCoverageFields()
    {
        SetupGovernedRepo();

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("status", "--coverage", "--output", "json");

        exitCode.Should().Be(ExitCodes.Success);
        output.Should().Contain("\"coverage\"");
        output.Should().Contain("\"governedCount\"");
        output.Should().Contain("\"totalMarkdownFiles\"");
        output.Should().Contain("\"percentage\"");
        output.Should().Contain("\"ungoverned\"");
    }

    // --- check text mode contract ---

    [Fact]
    public void CheckText_PassingRepo_ShowsResultPass()
    {
        SetupGovernedRepo();

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check");

        exitCode.Should().Be(ExitCodes.Success);
        output.Should().Contain("Result: PASS");
        output.Should().Contain("Files checked:");
        output.Should().Contain("Errors: 0");
    }

    [Fact]
    public void CheckText_FailingRepo_ShowsResultFail()
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            artifacts:
              - path: MISSING.md
                role: readme
                required: true
            """);

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check");

        exitCode.Should().Be(ExitCodes.ValidationFailure);
        output.Should().Contain("Result: FAIL");
        output.Should().Contain("STWD-001");
    }

    // --- status text mode contract ---

    [Fact]
    public void StatusText_ShowsCompleteness()
    {
        SetupGovernedRepo();

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("status");

        exitCode.Should().Be(ExitCodes.Success);
        output.Should().Contain("contract-test");
        output.Should().Contain("Completeness:");
        output.Should().Contain("2/2 required artifacts present");
    }

    // --- orient JSON contract ---

    [Fact]
    public void OrientJson_ContractShape_HasRequiredFields()
    {
        SetupGovernedRepo();

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("orient", "--output", "json");

        exitCode.Should().Be(ExitCodes.Success);
        output.Should().Contain("\"repositoryName\"");
        output.Should().Contain("\"repositoryType\"");
        output.Should().Contain("\"entries\"");
    }

    // --- version contract ---

    [Fact]
    public void Version_ReportsVersionNumber()
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("version");

        exitCode.Should().Be(ExitCodes.Success);
        output.Should().Contain("0.10.0");
        // Version output includes "steward X.Y.Z" prefix
        output.Should().MatchRegex(@"steward \d+\.\d+\.\d+");
    }

    // --- B6 regression: scoped check on clean tree ---

    [Fact]
    public void Check_ScopeChanged_CleanTree_NoFalsePositives()
    {
        SetupGovernedRepo();

        // On a clean tree with no changes, --scope changed should produce no diagnostics.
        // (Git may not be available in test env, in which case scope falls back to full —
        // either way, no false positives should appear for present artifacts.)
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check", "--scope", "changed");

        // When git is available and tree is clean: 0 files, PASS
        // When git is unavailable: falls back to full scope, should still PASS
        exitCode.Should().Be(ExitCodes.Success);
        output.Should().Contain("Result: PASS");
    }
}
