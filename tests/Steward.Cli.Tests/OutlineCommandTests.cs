using FluentAssertions;
using Steward.Cli.Commands;
using System.CommandLine;
using Xunit;

namespace Steward.Cli.Tests;

[Collection("Console")]
public class OutlineCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _origDir;

    public OutlineCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "steward-outline-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(Path.Combine(_tempDir, "src"));
        Directory.CreateDirectory(Path.Combine(_tempDir, "docs"));
        File.WriteAllText(Path.Combine(_tempDir, "README.md"), "# Hello");
        File.WriteAllText(Path.Combine(_tempDir, "src", "main.cs"), "// code");

        _origDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_tempDir);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_origDir);
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private (int ExitCode, string Output, string Error) InvokeOutline(params string[] args)
    {
        var rootCommand = new RootCommand("Repository Steward");
        GlobalOptionsSetup.AddGlobalOptions(rootCommand);
        rootCommand.Add(OutlineCommand.Create());

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
    public void Outline_IsRegisteredAsOutline()
    {
        var (exitCode, _, _) = InvokeOutline("outline");
        exitCode.Should().Be(0);
    }

    [Fact]
    public void Outline_ListsFiles()
    {
        var (exitCode, output, _) = InvokeOutline("outline");

        exitCode.Should().Be(0);
        output.Should().Contain("README.md");
        output.Should().Contain("src");
        output.Should().Contain("├──");
    }

    [Fact]
    public void Outline_RespectsDepthLimit()
    {
        var (exitCode, output, _) = InvokeOutline("outline", "--depth", "1");

        exitCode.Should().Be(0);
        output.Should().Contain("README.md");
        output.Should().NotContain("main.cs");
    }

    [Fact]
    public void Outline_JsonOutput_ContainsEntries()
    {
        var (exitCode, output, _) = InvokeOutline("outline", "--output", "json");

        exitCode.Should().Be(0);
        output.Should().Contain("\"entries\"");
        output.Should().Contain("README.md");
    }

    [Fact]
    public void Outline_WithSizes_ShowsSizeInfo()
    {
        var (exitCode, output, _) = InvokeOutline("outline", "--sizes");

        exitCode.Should().Be(0);
        output.Should().MatchRegex(@"\d+ [BKMG]?[Bb]?");
    }

    [Fact]
    public void Outline_WithCounts_ShowsDirectoryCounts()
    {
        var (exitCode, output, _) = InvokeOutline("outline", "--counts");

        exitCode.Should().Be(0);
        output.Should().Contain("src/ (1 file)");
    }
}
