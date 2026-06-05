using FluentAssertions;
using Steward.Core.Discovery;
using Steward.Core.Validation;
using Xunit;

namespace Steward.Core.Tests;

public class SinceScopeResolverTests
{
    private static readonly IReadOnlyList<DiscoveredFile> AllFiles =
    [
        new DiscoveredFile("README.md", 100, false),
        new DiscoveredFile("docs/guide.md", 200, false),
        new DiscoveredFile("src/main.cs", 300, false),
    ];

    [Fact]
    public void Resolve_WhenGitUnavailable_FallsBackToAllFiles()
    {
        // Pass an invalid path so git fails → should return allFiles unchanged
        var resolver = new SinceScopeResolver("origin/main");
        var result = resolver.Resolve(AllFiles, "Z:\\nonexistent-path");
        result.Should().BeEquivalentTo(AllFiles);
    }

    [Fact]
    public void Resolve_WhenGitReturnsChanges_FiltersToChangedFiles()
    {
        // Use a real initialized git repo to exercise the happy path
        var tempDir = Path.Combine(Path.GetTempPath(), "steward-since-" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            Directory.CreateDirectory(tempDir);
            RunGit(tempDir, "init");
            RunGit(tempDir, "config user.email test@test.com");
            RunGit(tempDir, "config user.name Test");

            // Initial commit
            File.WriteAllText(Path.Combine(tempDir, "README.md"), "# Hello");
            RunGit(tempDir, "add README.md");
            RunGit(tempDir, "commit -m initial");

            // Add a new file in a second commit
            Directory.CreateDirectory(Path.Combine(tempDir, "docs"));
            File.WriteAllText(Path.Combine(tempDir, "docs", "guide.md"), "# Guide");
            RunGit(tempDir, "add docs/guide.md");
            RunGit(tempDir, "commit -m add-guide");

            var allFiles = new List<DiscoveredFile>
            {
                new("README.md", 100, false),
                new("docs/guide.md", 200, false),
            };

            // Get the initial commit hash
            var firstCommit = GetGitOutput(tempDir, "rev-list --max-parents=0 HEAD").Trim();

            var resolver = new SinceScopeResolver(firstCommit);
            var result = resolver.Resolve(allFiles, tempDir);

            result.Should().ContainSingle(f => f.RelativePath == "docs/guide.md");
            result.Should().NotContain(f => f.RelativePath == "README.md");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                // Git marks object files as read-only on Windows.
                foreach (var file in Directory.EnumerateFiles(tempDir, "*", SearchOption.AllDirectories))
                    File.SetAttributes(file, FileAttributes.Normal);
                Directory.Delete(tempDir, true);
            }
        }
    }

    private static void RunGit(string workingDir, string args)
    {
        var psi = new System.Diagnostics.ProcessStartInfo("git", args)
        {
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var p = System.Diagnostics.Process.Start(psi)!;
        p.WaitForExit(10_000);
    }

    private static string GetGitOutput(string workingDir, string args)
    {
        var psi = new System.Diagnostics.ProcessStartInfo("git", args)
        {
            WorkingDirectory = workingDir,
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
}
