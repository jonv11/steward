using System.Text.Json;
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
        RunGit("init");
        RunGit("config user.email test@test.com");
        RunGit("config user.name Test");

        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            artifact_families:
              - family: note
                match:
                  path_pattern: "docs/*.md"
                frontmatter_schema:
                  required: [type]
            """);
        Directory.CreateDirectory(Path.Combine(_tempDir, "docs"));
        File.WriteAllText(Path.Combine(_tempDir, "docs", "unchanged.md"), "# Missing type\n");
        File.WriteAllText(Path.Combine(_tempDir, "docs", "changed.md"), "---\ntype: note\n---\n# Note\n");
        RunGit("add .");
        RunGit("commit -m initial");
        var firstCommit = GetGitOutput("rev-parse HEAD").Trim();

        File.WriteAllText(Path.Combine(_tempDir, "docs", "changed.md"), "# Now missing type\n");
        RunGit("add docs/changed.md");
        RunGit("commit -m change-note");

        // Prove both violations exist before testing that --since excludes the unchanged one.
        var (fullExitCode, fullOutput, _) = CliTestHelper.InvokeCapture("check", "--output", "json");
        fullExitCode.Should().Be(ExitCodes.ValidationFailure);
        using var full = JsonDocument.Parse(fullOutput);
        full.RootElement.GetProperty("data").GetProperty("diagnostics").EnumerateArray()
            .Where(d => d.GetProperty("ruleId").GetString() == "STWD-003")
            .Select(d => d.GetProperty("path").GetString())
            .Should().BeEquivalentTo("docs/changed.md", "docs/unchanged.md");

        var (exitCode, output, error) = CliTestHelper.InvokeCapture("check", "--since", firstCommit, "--output", "json");

        exitCode.Should().Be(ExitCodes.ValidationFailure);
        error.Should().BeEmpty();
        using var scoped = JsonDocument.Parse(output);
        var data = scoped.RootElement.GetProperty("data");
        data.GetProperty("summary").GetProperty("scope").GetString().Should().Be($"since:{firstCommit}");
        data.GetProperty("summary").GetProperty("filesChecked").GetInt32().Should().Be(1);
        data.GetProperty("diagnostics").EnumerateArray()
            .Where(d => d.GetProperty("ruleId").GetString() == "STWD-003")
            .Should().ContainSingle()
            .Which.GetProperty("path").GetString().Should().Be("docs/changed.md");
    }

    private void RunGit(string args)
    {
        _ = GetGitOutput(args);
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
        var output = p.StandardOutput.ReadToEndAsync();
        var error = p.StandardError.ReadToEndAsync();
        if (!p.WaitForExit(10_000))
        {
            p.Kill(entireProcessTree: true);
            throw new TimeoutException($"git {args} did not finish within 10 seconds.");
        }
        p.ExitCode.Should().Be(0, $"git {args} must succeed: {error.GetAwaiter().GetResult()}");
        return output.GetAwaiter().GetResult();
    }
}
