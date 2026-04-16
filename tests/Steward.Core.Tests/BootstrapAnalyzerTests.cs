using FluentAssertions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class BootstrapAnalyzerTests
{
    [Fact]
    public void Analyze_DetectsReadme()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/root/README.md", "# Hello");

        var files = new List<DiscoveredFile>
        {
            new("README.md", 10, false)
        };

        var result = BootstrapAnalyzer.Analyze(files, fs, "/root");

        result.StartHere.Should().Contain("README.md");
        result.Artifacts.Should().Contain(a => a.Path == "README.md" && a.Role == "authoritative");
    }

    [Fact]
    public void Analyze_DetectsDocsIndex()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/root/docs/index.md", "# Docs");
        fs.AddFile("/root/docs/guide.md", "# Guide");

        var files = new List<DiscoveredFile>
        {
            new("docs/index.md", 20, false),
            new("docs/guide.md", 15, false)
        };

        var result = BootstrapAnalyzer.Analyze(files, fs, "/root");

        result.StartHere.Should().Contain("docs/index.md");
        result.Artifacts.Should().Contain(a => a.Path == "docs/index.md");
    }

    [Fact]
    public void Analyze_DetectsRequirements()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/root/docs/PRD.md", "# Product Requirements");

        var files = new List<DiscoveredFile>
        {
            new("docs/PRD.md", 50, false)
        };

        var result = BootstrapAnalyzer.Analyze(files, fs, "/root");

        result.Artifacts.Should().Contain(a => a.Path == "docs/PRD.md" && a.Role == "requirements");
    }

    [Fact]
    public void Analyze_SuggestsExcludePatterns()
    {
        var fs = new InMemoryFileSystem();

        var files = new List<DiscoveredFile>
        {
            new("README.md", 10, false),
            new("node_modules/package/index.js", 200, false),
            new("bin/Debug/app.dll", 500, false)
        };

        var result = BootstrapAnalyzer.Analyze(files, fs, "/root");

        result.ExcludePatterns.Should().Contain("node_modules/");
        result.ExcludePatterns.Should().Contain("bin/");
    }

    [Fact]
    public void Analyze_EmptyRepo_NoSuggestions()
    {
        var fs = new InMemoryFileSystem();
        var files = new List<DiscoveredFile>();

        var result = BootstrapAnalyzer.Analyze(files, fs, "/root");

        result.StartHere.Should().BeEmpty();
        result.Artifacts.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_DetectsDecisionDirectory()
    {
        var fs = new InMemoryFileSystem();

        var files = new List<DiscoveredFile>
        {
            new("docs/decisions/adrs/adr-001.md", 30, false),
            new("docs/decisions/adrs/adr-002.md", 30, false),
            new("docs/decisions/decision-index.md", 20, false),
        };

        var result = BootstrapAnalyzer.Analyze(files, fs, "/root");

        result.Artifacts.Should().Contain(a => a.Path == "docs/decisions/decision-index.md" && a.Role == "index");
    }

    [Fact]
    public void Analyze_DetectsPlanningDocuments()
    {
        var fs = new InMemoryFileSystem();

        var files = new List<DiscoveredFile>
        {
            new("docs/planning/milestone-plan.md", 40, false),
            new("docs/planning/delivery-strategy.md", 30, false),
        };

        var result = BootstrapAnalyzer.Analyze(files, fs, "/root");

        result.Artifacts.Should().Contain(a => a.Path == "docs/planning/milestone-plan.md" && a.Role == "state-document");
        result.Artifacts.Should().Contain(a => a.Path == "docs/planning/delivery-strategy.md" && a.Role == "authoritative");
    }

    [Fact]
    public void Analyze_DetectsStateDocuments()
    {
        var fs = new InMemoryFileSystem();

        var files = new List<DiscoveredFile>
        {
            new("docs/implementation-status.md", 50, false),
            new("docs/release-tracker.md", 30, false),
        };

        var result = BootstrapAnalyzer.Analyze(files, fs, "/root");

        result.Artifacts.Should().Contain(a => a.Path == "docs/implementation-status.md" && a.Role == "state-document");
        result.Artifacts.Should().Contain(a => a.Path == "docs/release-tracker.md" && a.Role == "state-document");
    }

    [Fact]
    public void Analyze_DetectsSubdirectoryIndexFiles()
    {
        var fs = new InMemoryFileSystem();

        var files = new List<DiscoveredFile>
        {
            new("guides/index.md", 10, false),
            new("guides/getting-started.md", 20, false),
        };

        var result = BootstrapAnalyzer.Analyze(files, fs, "/root");

        result.Artifacts.Should().Contain(a => a.Path == "guides/index.md" && a.Role == "index");
    }
}
