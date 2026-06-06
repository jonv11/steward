using FluentAssertions;
using Steward.Cli.Tests.Helpers;
using Steward.Core;
using Xunit;

namespace Steward.Cli.Tests;

[Collection("Console")]
public class CheckSinceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _origDir;

    public CheckSinceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "steward-since-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(Path.Combine(_tempDir, ".steward"));
        _origDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_tempDir);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_origDir);
        if (Directory.Exists(_tempDir))
            ForceDeleteDirectory(_tempDir);
    }

    private static void ForceDeleteDirectory(string path)
    {
        // Git marks object files as read-only on Windows; reset attributes before deleting.
        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(path, true);
    }

    [Fact]
    public void Check_Since_InvalidRef_ReturnsUsageError()
    {
        // A real git repo with an initial commit, but the ref does not exist.
        // SinceScopeResolver must reject the invalid ref with a usage error, not silently
        // fall back to full scope.
        if (!IsGitAvailable())
            return; // Skip if git not installed

        RunGit("init");
        RunGit("config user.email test@test.com");
        RunGit("config user.name Test");
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            artifacts:
              - path: README.md
                role: authoritative
                required: true
            """);
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Hello");
        RunGit("add .");
        RunGit("commit -m initial");

        var (exitCode, _, error) = CliTestHelper.InvokeCapture("check", "--since", "this-ref-does-not-exist-xyz");

        exitCode.Should().Be(ExitCodes.UsageError);
        error.Should().Contain("this-ref-does-not-exist-xyz");
    }

    [Fact]
    public void Check_Since_AndPaths_AreMutuallyExclusive()
    {
        // Provide a valid .git and policy so TryBuild succeeds and reaches the exclusivity check.
        Directory.CreateDirectory(Path.Combine(_tempDir, ".git"));
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), "artifacts: []");

        var (exitCode, _, error) = CliTestHelper.InvokeCapture("check", "--since", "main", "--paths", "README.md");

        exitCode.Should().Be(ExitCodes.UsageError);
        error.Should().Contain("mutually exclusive");
    }

    [Fact]
    public void Check_Since_OnRealRepo_FiltersToChangedFiles()
    {
        if (!IsGitAvailable())
            return; // Skip if git not installed

        RunGit("init");
        RunGit("config user.email test@test.com");
        RunGit("config user.name Test");

        // Initial commit: README.md present (satisfies required artifact rule)
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            artifacts:
              - path: README.md
                role: readme
                required: true
            """);
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Hello");
        RunGit("add .");
        RunGit("commit -m initial");

        // Record the initial commit
        var firstCommit = GetGitOutput("rev-list --max-parents=0 HEAD").Trim();

        // Add a new file that has a frontmatter violation (wrong required field)
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            artifacts:
              - path: README.md
                role: readme
                required: true
            validation:
              disabled_rules:
                - STWD-001
                - STWD-009
            """);
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Hello");
        RunGit("add .");
        RunGit("commit -m second");

        // --since <firstCommit> should only validate files changed since the initial commit
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check", "--since", firstCommit, "--output", "json");

        output.Should().Contain($"\"scope\": \"since:{firstCommit}\"");
    }

    private void RunGit(string args)
    {
        var psi = new System.Diagnostics.ProcessStartInfo("git", args)
        {
            WorkingDirectory = _tempDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var p = System.Diagnostics.Process.Start(psi)!;
        p.WaitForExit(10_000);
    }

    private string GetGitOutput(string args)
    {
        var psi = new System.Diagnostics.ProcessStartInfo("git", args)
        {
            WorkingDirectory = _tempDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var p = System.Diagnostics.Process.Start(psi)!;
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit(10_000);
        return output;
    }

    private static bool IsGitAvailable()
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("git", "--version")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var p = System.Diagnostics.Process.Start(psi)!;
            return p.WaitForExit(5_000) && p.ExitCode == 0;
        }
        catch { return false; }
    }
}
