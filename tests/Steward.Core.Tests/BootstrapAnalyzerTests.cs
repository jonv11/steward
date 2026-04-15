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
}
