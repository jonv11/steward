using FluentAssertions;
using Steward.Cli.Commands;
using System.CommandLine;
using Xunit;

namespace Steward.Cli.Tests;

/// <summary>
/// Tests that settings in config.yaml (output format, no-color, discovery excludes) are
/// correctly applied as defaults, with CLI flags taking precedence when explicitly provided.
/// </summary>
[Collection("Console")]
public class ConfigSettingsTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _origDir;

    public ConfigSettingsTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "steward-cfg-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(Path.Combine(_tempDir, ".steward"));
        Directory.CreateDirectory(Path.Combine(_tempDir, ".git")); // mark as repo root

        _origDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_tempDir);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_origDir);
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private void WriteConfig(string yaml)
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "config.yaml"), yaml);
    }

    private void WriteFile(string relativePath, string content = "")
    {
        var fullPath = Path.Combine(_tempDir, relativePath);
        var dir = Path.GetDirectoryName(fullPath);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(fullPath, content);
    }

    private (int ExitCode, string Output, string Error) InvokeOrient(params string[] args)
    {
        var rootCommand = new RootCommand("Repository Steward");
        GlobalOptionsSetup.AddGlobalOptions(rootCommand);
        rootCommand.Add(OrientCommand.Create());

        var stdOut = new StringWriter();
        var stdErr = new StringWriter();
        var origOut = Console.Out;
        var origErr = Console.Error;
        Console.SetOut(stdOut);
        Console.SetError(stdErr);

        try
        {
            var exitCode = rootCommand.Parse(args).Invoke();
            return (exitCode, stdOut.ToString(), stdErr.ToString());
        }
        finally
        {
            Console.SetOut(origOut);
            Console.SetError(origErr);
        }
    }

    [Fact]
    public void DiscoveryExclude_FromConfig_FiltersDiscoveredFiles()
    {
        // "node_modules/" (trailing slash) matches the directory itself, following gitignore semantics.
        WriteConfig("""
            discovery:
              exclude:
                - "node_modules/"
            """);

        // Create files in both normal and excluded directories
        WriteFile("README.md", "# Hello");
        WriteFile("node_modules/lodash/index.js", "module.exports = {}");

        var (exitCode, output, _) = InvokeOrient("orient");

        exitCode.Should().Be(0);
        output.Should().Contain("README.md");
        // node_modules directory should be excluded from discovery
        output.Should().NotContain("node_modules");
        output.Should().NotContain("lodash");
    }

    [Fact]
    public void DiscoveryExclude_NotSet_IncludesAllFiles()
    {
        // No discovery config — no excludes beyond .gitignore
        WriteFile("README.md", "# Hello");
        WriteFile("vendor/lib.js", "/* lib */");

        var (exitCode, output, _) = InvokeOrient("orient");

        exitCode.Should().Be(0);
        output.Should().Contain("vendor");
    }

    [Fact]
    public void OutputFormat_FromConfig_UsedAsDefault()
    {
        // config.yaml sets json output — should be honored without --output flag
        WriteConfig("output:\n  format: json\n");
        WriteFile("README.md", "# Hello");

        var (exitCode, output, _) = InvokeOrient("orient");

        exitCode.Should().Be(0);
        // JSON output contains structured data
        output.Should().Contain("{");
    }

    [Fact]
    public void OutputFormat_CliFlagOverridesConfig()
    {
        // config.yaml wants json, but explicit --output text should win
        WriteConfig("output:\n  format: json\n");
        WriteFile("README.md", "# Hello");

        var (exitCode, output, _) = InvokeOrient("orient", "--output", "text");

        exitCode.Should().Be(0);
        // Plain text output should not start with {
        output.TrimStart().Should().NotStartWith("{");
    }

    [Fact]
    public void MultipleExcludes_AllApplied()
    {
        // Trailing-slash patterns match directories; bare names also match directories.
        WriteConfig("""
            discovery:
              exclude:
                - "node_modules/"
                - "dist/"
                - "*.log"
            """);

        WriteFile("src/main.ts", "export {}");
        WriteFile("node_modules/dep/index.js", "");
        WriteFile("dist/bundle.js", "");
        WriteFile("debug.log", "log output");

        var (exitCode, output, _) = InvokeOrient("orient");

        exitCode.Should().Be(0);
        output.Should().Contain("src");
        output.Should().NotContain("node_modules");
        output.Should().NotContain("dist");
        output.Should().NotContain("debug.log");
    }
}
