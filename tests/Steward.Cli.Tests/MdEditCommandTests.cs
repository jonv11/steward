using FluentAssertions;
using Steward.Cli.Commands;
using Steward.Cli.Tests.Helpers;
using System.CommandLine;
using Xunit;

namespace Steward.Cli.Tests;

[Collection("Console")]
public class MdEditCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _testFile;

    public MdEditCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "steward-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        _testFile = Path.Combine(_tempDir, "test.md");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private void WriteTestFile(string content)
    {
        File.WriteAllText(_testFile, content);
    }

    private (int ExitCode, string Output, string Error) InvokeEdit(params string[] args)
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
    public void EnsureSection_Preview_ShowsDiff()
    {
        WriteTestFile("# Introduction\nSome text.\n");

        var (exitCode, output, _) = InvokeEdit(
            "md", "edit", "ensure-section", _testFile,
            "--heading", "Goals");

        exitCode.Should().Be(0);
        output.Should().Contain("Goals");

        // File should NOT be modified in preview mode
        File.ReadAllText(_testFile).Should().Be("# Introduction\nSome text.\n");
    }

    [Fact]
    public void EnsureSection_Apply_ModifiesFile()
    {
        WriteTestFile("# Introduction\nSome text.\n");

        var (exitCode, output, _) = InvokeEdit(
            "md", "edit", "ensure-section", _testFile,
            "--heading", "Goals", "--apply");

        exitCode.Should().Be(0);
        output.Should().Contain("applied");

        var modified = File.ReadAllText(_testFile);
        modified.Should().Contain("# Goals");
    }

    [Fact]
    public void FmSet_Apply_UpdatesFrontmatter()
    {
        WriteTestFile("---\ntitle: Test\n---\n# Title\n");

        var (exitCode, _, _) = InvokeEdit(
            "md", "edit", "fm-set", _testFile,
            "--key", "status", "--value", "draft", "--apply");

        exitCode.Should().Be(0);

        var modified = File.ReadAllText(_testFile);
        modified.Should().Contain("status: draft");
        modified.Should().Contain("title: Test");
    }

    [Fact]
    public void SetSection_FileNotFound_ReturnsError()
    {
        var (exitCode, _, _) = InvokeEdit(
            "md", "edit", "set-section", Path.Combine(_tempDir, "missing.md"),
            "--heading", "X", "--content", "Y");

        exitCode.Should().Be(2);
    }

    [Fact]
    public void EnsureSection_UnderParent_CreatesChild()
    {
        WriteTestFile("# Parent\nParent text.\n");

        var (exitCode, output, _) = InvokeEdit(
            "md", "edit", "ensure-section", _testFile,
            "--heading", "Child", "--under", "Parent", "--apply");

        exitCode.Should().Be(0);

        var modified = File.ReadAllText(_testFile);
        modified.Should().Contain("## Child");
    }

    [Fact]
    public void JsonOutput_ReturnsStructuredResult()
    {
        WriteTestFile("# Title\nText.\n");

        var (exitCode, output, _) = InvokeEdit(
            "md", "edit", "ensure-section", _testFile,
            "--heading", "New", "--output", "json");

        exitCode.Should().Be(0);
        output.Should().Contain("\"hasChanges\"");
        output.Should().Contain("\"message\"");
    }

    [Fact]
    public void InsertSection_After_PlacesCorrectly()
    {
        WriteTestFile("# Intro\nIntro text.\n# Goals\nGoal text.\n# Appendix\nEnd.\n");

        var (exitCode, _, _) = InvokeEdit(
            "md", "edit", "insert-section", _testFile,
            "--heading", "Details", "--after", "Goals", "--apply");

        exitCode.Should().Be(0);

        var modified = File.ReadAllText(_testFile);
        var goalsIdx = modified.IndexOf("# Goals", StringComparison.Ordinal);
        var detailsIdx = modified.IndexOf("# Details", StringComparison.Ordinal);
        var appendixIdx = modified.IndexOf("# Appendix", StringComparison.Ordinal);
        detailsIdx.Should().BeGreaterThan(goalsIdx);
        detailsIdx.Should().BeLessThan(appendixIdx);
    }

    [Fact]
    public void InsertSection_Before_PlacesCorrectly()
    {
        WriteTestFile("# Intro\nIntro text.\n# Goals\nGoal text.\n");

        var (exitCode, _, _) = InvokeEdit(
            "md", "edit", "insert-section", _testFile,
            "--heading", "Preamble", "--before", "Goals", "--apply");

        exitCode.Should().Be(0);

        var modified = File.ReadAllText(_testFile);
        var preambleIdx = modified.IndexOf("# Preamble", StringComparison.Ordinal);
        var goalsIdx = modified.IndexOf("# Goals", StringComparison.Ordinal);
        preambleIdx.Should().BeGreaterThan(-1);
        preambleIdx.Should().BeLessThan(goalsIdx);
    }

    [Fact]
    public void InsertSection_Level_OverridesDefault()
    {
        WriteTestFile("# Intro\nSome text.\n");

        var (exitCode, _, _) = InvokeEdit(
            "md", "edit", "insert-section", _testFile,
            "--heading", "Deep", "--level", "3", "--apply");

        exitCode.Should().Be(0);

        var modified = File.ReadAllText(_testFile);
        modified.Should().Contain("### Deep");
    }
}
