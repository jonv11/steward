using FluentAssertions;
using Steward.Cli.Commands;
using System.CommandLine;
using Steward.Core.Abstractions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Xunit;

namespace Steward.Cli.Tests;

[Collection("Console")]
public class StatusCommandTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _origDir;

    public StatusCommandTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "steward-status-" + Guid.NewGuid().ToString("N")[..8]);
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

    private (int ExitCode, string Output, string Error) InvokeStatus(params string[] args)
    {
        var rootCommand = new RootCommand("Repository Steward");
        GlobalOptionsSetup.AddGlobalOptions(rootCommand);
        rootCommand.Add(StatusCommand.Create());

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
    public void Status_ShowsRequiredArtifacts()
    {
        WritePolicyYaml(@"
repository:
  name: test-repo
artifacts:
  - path: README.md
    role: readme
    required: true
");
        WriteFile("README.md", "# Hello");

        var (exitCode, output, _) = InvokeStatus("status");

        exitCode.Should().Be(0);
        output.Should().Contain("test-repo");
        output.Should().Contain("README.md");
        output.Should().Contain("OK");
        output.Should().Contain("1/1");
    }

    [Fact]
    public void Status_ShowsMissingArtifact()
    {
        WritePolicyYaml(@"
artifacts:
  - path: CHANGELOG.md
    role: changelog
    required: true
");

        var (exitCode, output, _) = InvokeStatus("status");

        exitCode.Should().Be(0);
        output.Should().Contain("MISSING");
        output.Should().Contain("CHANGELOG.md");
        output.Should().Contain("0/1");
    }

    [Fact]
    public void Status_SurfacesRecommendedArtifactsFromRoleDefaults()
    {
        WritePolicyYaml(@"
artifacts:
  - path: STRUCTURE.md
    role: generated
");
        WriteFile("STRUCTURE.md", "# Structure");

        var (exitCode, output, _) = InvokeStatus("status");

        exitCode.Should().Be(0);
        output.Should().Contain("Recommended Artifacts:");
        output.Should().Contain("STRUCTURE.md");
        output.Should().Contain("Recommended artifacts: 1/1 present");
    }

    [Fact]
    public void Status_SurfacesStateDocuments()
    {
        WritePolicyYaml(@"
artifacts:
  - path: docs/implementation-status.md
    role: current-state
    freshness:
      max_age_days: 30
");
        WriteFile("docs/implementation-status.md", """
            ---
            last_updated: 2026-04-15
            ---
            # Status
            """);

        var (exitCode, output, _) = InvokeStatus("status");

        exitCode.Should().Be(0);
        output.Should().Contain("State Documents:");
        output.Should().Contain("docs/implementation-status.md");
        output.Should().Contain("current-state");
    }

    [Fact]
    public void Status_JsonOutput()
    {
        WritePolicyYaml(@"
repository:
  name: test-repo
artifacts:
  - path: README.md
    role: readme
    required: true
");
        WriteFile("README.md", "# Hello");

        var (exitCode, output, _) = InvokeStatus("status", "--output", "json");

        exitCode.Should().Be(0);
        output.Should().Contain("\"repositoryName\"");
        output.Should().Contain("\"fileCount\"");
    }

    [Fact]
    public void Status_NoConfig_ReportsError()
    {
        Directory.Delete(Path.Combine(_tempDir, ".steward"), true);

        var (exitCode, _, _) = InvokeStatus("status");

        exitCode.Should().Be(2);
    }

    [Fact]
    public void Status_CoverageJsonOutput_IncludesCoverageObject()
    {
        WritePolicyYaml(@"
repository:
  name: test-repo
artifacts:
  - path: README.md
    role: readme
    required: true
");
        WriteFile("README.md", "# Hello\n[Guide](docs/guide.md)");
        WriteFile("docs/guide.md", "# Guide");
        WriteFile("orphan.md", "# Orphan");

        var (exitCode, output, _) = InvokeStatus("status", "--coverage", "--output", "json");

        exitCode.Should().Be(0);
        output.Should().Contain("\"coverage\"");
        output.Should().Contain("\"governedCount\"");
        output.Should().Contain("\"totalMarkdownFiles\"");
        output.Should().Contain("\"percentage\"");
        output.Should().Contain("\"ungoverned\"");
    }

    [Fact]
    public void Status_JsonOutput_WithoutCoverage_OmitsCoverageObject()
    {
        WritePolicyYaml(@"
repository:
  name: test-repo
artifacts:
  - path: README.md
    role: readme
    required: true
");
        WriteFile("README.md", "# Hello");

        var (exitCode, output, _) = InvokeStatus("status", "--output", "json");

        exitCode.Should().Be(0);
        output.Should().NotContain("\"coverage\"");
    }

    [Fact]
    public void ComputeCoverage_IncludesIndexedAndReachableMarkdownFiles()
    {
        WriteFile("README.md", "# Root\n[Index](docs/planning-index.md)\n[Guide](guides/getting-started.md)");
        WriteFile("docs/planning-index.md", "# Planning Index");
        WriteFile("docs/implementation-status.md", "# Status");
        WriteFile("guides/getting-started.md", "# Guide");

        var policy = new RepositoryPolicy
        {
            Governance = new GovernanceConfig
            {
                StartHere = ["README.md"]
            },
            Artifacts =
            [
                new ArtifactDefinition
                {
                    Path = "docs/planning-index.md",
                    Role = "guide",
                    IndexOf = "docs"
                }
            ]
        };

        IReadOnlyList<DiscoveredFile> files =
        [
            new("README.md", 10, false),
            new("docs/planning-index.md", 10, false),
            new("docs/implementation-status.md", 10, false),
            new("guides/getting-started.md", 10, false)
        ];

        var coverage = StatusCommand.ComputeCoverage(
            policy,
            files,
            new PhysicalFileSystem(),
            _tempDir);

        coverage.TotalMarkdownFiles.Should().Be(4);
        coverage.GovernedCount.Should().Be(4);
        coverage.Ungoverned.Should().BeEmpty();
    }
}
