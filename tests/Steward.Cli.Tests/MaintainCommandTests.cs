using FluentAssertions;
using Steward.Cli.Commands;
using System.CommandLine;
using Xunit;

namespace Steward.Cli.Tests;

[Collection("Console")]
public class MaintainCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _origDir;

    public MaintainCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "steward-maint-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
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

    private void WritePolicyYaml(string yaml)
    {
        File.WriteAllText(Path.Combine(_tempDir, ".steward", "policy.yaml"), yaml);
    }

    private void WriteFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(_tempDir, relativePath);
        var dir = Path.GetDirectoryName(fullPath);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(fullPath, content);
    }

    private (int ExitCode, string Output, string Error) InvokeMaintain(params string[] args)
    {
        var rootCommand = new RootCommand("Repository Steward");
        GlobalOptionsSetup.AddGlobalOptions(rootCommand);
        rootCommand.Add(MaintainCommand.Create());

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
    public void Preview_ShowsActions()
    {
        WritePolicyYaml(@"
maintenance:
  artifacts:
    - id: structure
      path: STRUCTURE.md
      type: structure-document
");
        WriteFile("README.md", "# Hello");

        var (exitCode, output, _) = InvokeMaintain("maintain");

        exitCode.Should().Be(0);
        output.Should().Contain("structure");
        output.Should().Contain("STRUCTURE.md");

        // File should NOT be created in preview mode
        File.Exists(Path.Combine(_tempDir, "STRUCTURE.md")).Should().BeFalse();
    }

    [Fact]
    public void Apply_CreatesArtifact()
    {
        WritePolicyYaml(@"
maintenance:
  artifacts:
    - id: structure
      path: STRUCTURE.md
      type: structure-document
");
        WriteFile("README.md", "# Hello");

        var (exitCode, output, _) = InvokeMaintain("maintain", "--apply");

        exitCode.Should().Be(0);
        output.Should().Contain("Changes applied");

        var structurePath = Path.Combine(_tempDir, "STRUCTURE.md");
        File.Exists(structurePath).Should().BeTrue();
        var content = File.ReadAllText(structurePath);
        content.Should().Contain("# Repository Structure");
    }

    [Fact]
    public void Apply_Idempotent()
    {
        WritePolicyYaml(@"
maintenance:
  artifacts:
    - id: structure
      path: STRUCTURE.md
      type: structure-document
");
        WriteFile("README.md", "# Hello");

        // First apply — creates STRUCTURE.md
        InvokeMaintain("maintain", "--apply");

        // Second apply — STRUCTURE.md now in file list, so tree changes; apply to converge
        InvokeMaintain("maintain", "--apply");

        // Third run (preview) — should report no changes at fixed point
        var (exitCode, output, _) = InvokeMaintain("maintain");

        exitCode.Should().Be(0);
        output.Should().Contain("OK");
    }

    [Fact]
    public void Artifact_FiltersToSingleArtifact()
    {
        WritePolicyYaml(@"
maintenance:
  artifacts:
    - id: struct1
      path: S1.md
      type: structure-document
    - id: struct2
      path: S2.md
      type: structure-document
");
        WriteFile("README.md", "# Hello");

        var (exitCode, output, _) = InvokeMaintain("maintain", "--artifact", "struct2");

        exitCode.Should().Be(0);
        output.Should().Contain("struct2");
        output.Should().NotContain("struct1");
    }

    [Fact]
    public void Apply_WithJsonOutput_ProducesSingleJsonObject()
    {
        WritePolicyYaml(@"
maintenance:
  artifacts:
    - id: structure
      path: STRUCTURE.md
      type: structure-document
");
        WriteFile("README.md", "# Hello");

        var (exitCode, output, _) = InvokeMaintain("maintain", "--apply", "--output", "json");

        exitCode.Should().Be(0);

        // Should be parseable as a single JSON object — not two concatenated objects
        var trimmed = output.Trim();
        trimmed.Should().StartWith("{");
        trimmed.Should().EndWith("}");

        // Single object contains both plan and apply result
        output.Should().Contain("\"applied\"");
        output.Should().Contain("\"hasChanges\"");
        output.Should().Contain("\"actions\"");
    }

    [Fact]
    public void Diff_ShowsChanges_WhenCombinedWithApply()
    {
        WritePolicyYaml(@"
maintenance:
  artifacts:
    - id: structure
      path: STRUCTURE.md
      type: structure-document
");
        WriteFile("README.md", "# Hello");
        WriteFile("STRUCTURE.md", "# Repository Structure\n\n```\nold tree\n```\n");

        var (exitCode, output, _) = InvokeMaintain("maintain", "--apply", "--diff");

        exitCode.Should().Be(0);
        // --diff should work even with --apply
        output.Should().Contain("+");
    }

    [Fact]
    public void Json_OutputFormat()
    {
        WritePolicyYaml(@"
maintenance:
  artifacts:
    - id: structure
      path: STRUCTURE.md
      type: structure-document
");
        WriteFile("README.md", "# Hello");

        var (exitCode, output, _) = InvokeMaintain("maintain", "--output", "json");

        exitCode.Should().Be(0);
        output.Should().Contain("\"hasChanges\"");
        output.Should().Contain("\"artifactId\"");
    }

    [Fact]
    public void NoConfig_ReportsError()
    {
        // Delete the .steward directory
        Directory.Delete(Path.Combine(_tempDir, ".steward"), true);

        var (exitCode, output, _) = InvokeMaintain("maintain");

        exitCode.Should().Be(2); // UsageError
    }

    [Fact]
    public void Diff_ShowsChanges()
    {
        WritePolicyYaml(@"
maintenance:
  artifacts:
    - id: structure
      path: STRUCTURE.md
      type: structure-document
");
        WriteFile("README.md", "# Hello");
        // Create an existing (stale) structure document so diff has both sides
        WriteFile("STRUCTURE.md", "# Repository Structure\n\n```\nold tree\n```\n");

        var (exitCode, output, _) = InvokeMaintain("maintain", "--diff");

        exitCode.Should().Be(0);
        // --diff should display lines with + or - prefix for changes
        output.Should().Contain("+");
    }
}
