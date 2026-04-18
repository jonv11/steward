using FluentAssertions;
using Steward.Cli.Commands;
using System.CommandLine;
using Xunit;

namespace Steward.Cli.Tests;

[Collection("Console")]
public class MdQueryCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _origDir;

    public MdQueryCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "steward-mdq-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(Path.Combine(_tempDir, "docs"));
        _origDir = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_tempDir);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_origDir);
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private static (int ExitCode, string Output, string Error) InvokeQuery(params string[] args)
    {
        var rootCommand = new RootCommand("Repository Steward");
        GlobalOptionsSetup.AddGlobalOptions(rootCommand);
        rootCommand.Add(MdCommand.Create());

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
    public void BatchQuery_PatternWithSelector_Works()
    {
        File.WriteAllText(Path.Combine(_tempDir, "docs", "guide.md"), "# Guide\n## Status\nActive\n");
        File.WriteAllText(Path.Combine(_tempDir, "docs", "faq.md"), "# FAQ\n## Status\nDraft\n");

        var (exitCode, output, _) = InvokeQuery("md", "query", "--pattern", "docs/*.md", "heading[Status]");

        exitCode.Should().Be(0);
        output.Should().Contain("Active");
        output.Should().Contain("Draft");
    }

    [Fact]
    public void SingleFileQuery_Works()
    {
        var testFile = Path.Combine(_tempDir, "test.md");
        File.WriteAllText(testFile, "# Title\n## Status\nDone\n");

        var (exitCode, output, _) = InvokeQuery("md", "query", testFile, "heading[Status]");

        exitCode.Should().Be(0);
        output.Should().Contain("Done");
    }

    [Fact]
    public void SingleArgumentAnchorSelector_Works()
    {
        var testFile = Path.Combine(_tempDir, "README.md");
        File.WriteAllText(testFile, "# Intro\n## Who Is Steward For?\nMaintainers and contributors.\n");

        var (exitCode, output, _) = InvokeQuery("md", "query", "README.md#who-is-steward-for");

        exitCode.Should().Be(0);
        output.Should().Contain("Maintainers and contributors.");
    }

    [Fact]
    public void NoSelectorProvided_ReturnsError()
    {
        var (exitCode, _, error) = InvokeQuery("md", "query");

        exitCode.Should().Be(2);
    }
}
