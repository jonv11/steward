using FluentAssertions;
using Steward.Cli.Tests.Helpers;
using Xunit;

namespace Steward.Cli.Tests;

[Collection("Console")]
public class CliSnapshotTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _origDir;

    public CliSnapshotTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "steward-snap-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(Path.Combine(_tempDir, ".git"));
        Directory.CreateDirectory(Path.Combine(_tempDir, ".steward"));
        _origDir = Directory.GetCurrentDirectory();
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_origDir);
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, true);
            }
            catch (IOException)
            {
                // Verify may still be finalizing file handles when a snapshot fails.
            }
        }
    }

    [Fact]
    public Task RootHelp_IsStable()
    {
        var (_, output, _) = CliTestHelper.InvokeCapture("--help");
        return Verify(output.TrimEnd());
    }

    [Fact]
    public Task CheckJson_IsStable()
    {
        Directory.SetCurrentDirectory(_tempDir);
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), """
            artifacts:
              - path: README.md
                role: readme
                required: true
            """);

        var (exitCode, output, _) = CliTestHelper.InvokeCapture("check", "--output", "json");

        exitCode.Should().Be(1);
        return Verify(output.TrimEnd());
    }
}
