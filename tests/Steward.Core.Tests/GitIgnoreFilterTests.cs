using FluentAssertions;
using Steward.Core.Discovery;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class GitIgnoreFilterTests
{
    [Fact]
    public void IsIgnored_GitDirectory_AlwaysIgnored()
    {
        var fs = new InMemoryFileSystem()
            .AddDirectory("root");

        var filter = GitIgnoreFilter.Load("root", fs);

        filter.IsIgnored(".git", isDirectory: true).Should().BeTrue();
        filter.IsIgnored(".git/config", isDirectory: false).Should().BeTrue();
    }

    [Fact]
    public void IsIgnored_NoGitignore_NothingIgnored()
    {
        var fs = new InMemoryFileSystem()
            .AddDirectory("root")
            .AddFile("root/file.txt", "content");

        var filter = GitIgnoreFilter.Load("root", fs);

        filter.IsIgnored("file.txt", isDirectory: false).Should().BeFalse();
    }

    [Fact]
    public void IsIgnored_SimplePattern_MatchesFile()
    {
        var fs = new InMemoryFileSystem()
            .AddDirectory("root")
            .AddFile("root/.gitignore", "*.log\n");

        var filter = GitIgnoreFilter.Load("root", fs);

        filter.IsIgnored("error.log", isDirectory: false).Should().BeTrue();
        filter.IsIgnored("debug.log", isDirectory: false).Should().BeTrue();
        filter.IsIgnored("readme.md", isDirectory: false).Should().BeFalse();
    }

    [Fact]
    public void IsIgnored_DirectoryPattern_OnlyMatchesDirectories()
    {
        var fs = new InMemoryFileSystem()
            .AddDirectory("root")
            .AddFile("root/.gitignore", "build/\n");

        var filter = GitIgnoreFilter.Load("root", fs);

        filter.IsIgnored("build", isDirectory: true).Should().BeTrue();
        filter.IsIgnored("build", isDirectory: false).Should().BeFalse();
    }

    [Fact]
    public void IsIgnored_NegationPattern_UnIgnoresFile()
    {
        var fs = new InMemoryFileSystem()
            .AddDirectory("root")
            .AddFile("root/.gitignore", "*.log\n!important.log\n");

        var filter = GitIgnoreFilter.Load("root", fs);

        filter.IsIgnored("error.log", isDirectory: false).Should().BeTrue();
        filter.IsIgnored("important.log", isDirectory: false).Should().BeFalse();
    }

    [Fact]
    public void IsIgnored_DoubleStarPattern_MatchesNestedPaths()
    {
        var fs = new InMemoryFileSystem()
            .AddDirectory("root")
            .AddFile("root/.gitignore", "**/bin/\n");

        var filter = GitIgnoreFilter.Load("root", fs);

        filter.IsIgnored("bin", isDirectory: true).Should().BeTrue();
        filter.IsIgnored("src/Steward/bin", isDirectory: true).Should().BeTrue();
        filter.IsIgnored("deep/nested/bin", isDirectory: true).Should().BeTrue();
    }

    [Fact]
    public void IsIgnored_AnchoredPattern_OnlyMatchesFromRoot()
    {
        var fs = new InMemoryFileSystem()
            .AddDirectory("root")
            .AddFile("root/.gitignore", "/build.log\n");

        var filter = GitIgnoreFilter.Load("root", fs);

        filter.IsIgnored("build.log", isDirectory: false).Should().BeTrue();
        filter.IsIgnored("sub/build.log", isDirectory: false).Should().BeFalse();
    }

    [Fact]
    public void IsIgnored_NestedGitignore_AddsRules()
    {
        var fs = new InMemoryFileSystem()
            .AddDirectory("root")
            .AddDirectory("root/sub")
            .AddFile("root/.gitignore", "*.log\n")
            .AddFile("root/sub/.gitignore", "*.tmp\n");

        var filter = GitIgnoreFilter.Load("root", fs);

        filter.IsIgnored("error.log", isDirectory: false).Should().BeTrue();
        filter.IsIgnored("sub/data.tmp", isDirectory: false).Should().BeTrue();
        filter.IsIgnored("data.tmp", isDirectory: false).Should().BeFalse();
    }

    [Fact]
    public void IsIgnored_AdditionalExcludes_Applied()
    {
        var fs = new InMemoryFileSystem()
            .AddDirectory("root");

        var filter = GitIgnoreFilter.Load("root", fs, additionalExcludes: ["*.bak"]);

        filter.IsIgnored("file.bak", isDirectory: false).Should().BeTrue();
        filter.IsIgnored("file.txt", isDirectory: false).Should().BeFalse();
    }

    [Fact]
    public void IsIgnored_CommentsAndBlankLines_Skipped()
    {
        var fs = new InMemoryFileSystem()
            .AddDirectory("root")
            .AddFile("root/.gitignore", "# comment\n\n*.log\n# another comment\n");

        var filter = GitIgnoreFilter.Load("root", fs);

        filter.IsIgnored("error.log", isDirectory: false).Should().BeTrue();
        filter.IsIgnored("# comment", isDirectory: false).Should().BeFalse();
    }
}
