using FluentAssertions;
using Steward.Cli.Tests.Helpers;
using Xunit;

namespace Steward.Cli.Tests;

/// <summary>
/// Verifies that running steward from a subdirectory uses the repo root (where .steward lives)
/// rather than the current working directory for file discovery.
/// </summary>
[Collection("Console")]
public class CheckSubdirectoryTests : IDisposable
{
    private readonly string _repoRoot;
    private readonly string _origDir;

    public CheckSubdirectoryTests()
    {
        _repoRoot = Path.Combine(Path.GetTempPath(), "steward-subdir-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_repoRoot);
        Directory.CreateDirectory(Path.Combine(_repoRoot, ".git"));
        Directory.CreateDirectory(Path.Combine(_repoRoot, ".steward"));
        _origDir = Directory.GetCurrentDirectory();
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_origDir);
        if (Directory.Exists(_repoRoot))
            Directory.Delete(_repoRoot, true);
    }

    [Fact]
    public void Check_FromSubdirectory_DiscoverFilesFromRepoRoot()
    {
        // Place README.md at the repo root (not inside the subdirectory).
        File.WriteAllText(Path.Combine(_repoRoot, ".steward", "policy.yaml"), """
            artifacts:
              - path: README.md
                role: authoritative
                required: true
            """);
        File.WriteAllText(Path.Combine(_repoRoot, "README.md"), "# Hello");

        // Create a subdirectory and run steward from it.
        var subDir = Path.Combine(_repoRoot, "src");
        Directory.CreateDirectory(subDir);
        Directory.SetCurrentDirectory(subDir);

        var (exitCode, _, _) = CliTestHelper.InvokeCapture("check");

        // README.md lives at the repo root. If discovery correctly starts from the repo root
        // (parent of .steward), the required artifact is found and the check passes.
        // If discovery used CWD (src/), README.md would be missing → exit code 1.
        exitCode.Should().Be(0, "discovery should start from the repo root, not the CWD subdirectory");
    }

    [Fact]
    public void Check_FromDeepSubdirectory_DiscoverFilesFromRepoRoot()
    {
        File.WriteAllText(Path.Combine(_repoRoot, ".steward", "policy.yaml"), """
            artifacts:
              - path: README.md
                role: authoritative
                required: true
            """);
        File.WriteAllText(Path.Combine(_repoRoot, "README.md"), "# Hello");

        // Create a two-level deep subdirectory.
        var deepDir = Path.Combine(_repoRoot, "src", "deep");
        Directory.CreateDirectory(deepDir);
        Directory.SetCurrentDirectory(deepDir);

        var (exitCode, _, _) = CliTestHelper.InvokeCapture("check");

        exitCode.Should().Be(0, "discovery should start from the repo root even two levels deep");
    }
}
