using FluentAssertions;
using Steward.Core.Discovery;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class FileDiscoveryServiceTests
{
    [Fact]
    public void Discover_FindsAllFiles()
    {
        var fs = new InMemoryFileSystem()
            .AddDirectory("root")
            .AddFile("root/readme.md", "# Hello")
            .AddFile("root/src/app.cs", "class App {}");

        var filter = GitIgnoreFilter.Load("root", fs);
        var service = new FileDiscoveryService(fs, filter);

        var files = service.Discover("root");

        files.Should().Contain(f => f.RelativePath == "readme.md" && !f.IsDirectory);
        files.Should().Contain(f => f.RelativePath == "src" && f.IsDirectory);
        files.Should().Contain(f => f.RelativePath == "src/app.cs" && !f.IsDirectory);
    }

    [Fact]
    public void Discover_PrunesIgnoredDirectories()
    {
        var fs = new InMemoryFileSystem()
            .AddDirectory("root")
            .AddFile("root/.gitignore", "bin/\nobj/\n")
            .AddDirectory("root/bin")
            .AddFile("root/bin/output.dll", "binary")
            .AddDirectory("root/obj")
            .AddFile("root/obj/temp.cs", "temp")
            .AddFile("root/src/app.cs", "class App {}");

        var filter = GitIgnoreFilter.Load("root", fs);
        var service = new FileDiscoveryService(fs, filter);

        var files = service.Discover("root");

        files.Should().NotContain(f => f.RelativePath.StartsWith("bin"));
        files.Should().NotContain(f => f.RelativePath.StartsWith("obj"));
        files.Should().Contain(f => f.RelativePath == "src/app.cs");
    }

    [Fact]
    public void Discover_ResultsSortedAlphabetically()
    {
        var fs = new InMemoryFileSystem()
            .AddDirectory("root")
            .AddFile("root/zebra.txt", "z")
            .AddFile("root/alpha.txt", "a")
            .AddFile("root/middle.txt", "m");

        var filter = GitIgnoreFilter.Load("root", fs);
        var service = new FileDiscoveryService(fs, filter);

        var files = service.Discover("root");
        var filePaths = files.Where(f => !f.IsDirectory).Select(f => f.RelativePath).ToList();

        filePaths.Should().BeInAscendingOrder();
    }

    [Fact]
    public void Discover_IncludesFileSize()
    {
        var fs = new InMemoryFileSystem()
            .AddDirectory("root")
            .AddFile("root/data.txt", "hello world");

        var filter = GitIgnoreFilter.Load("root", fs);
        var service = new FileDiscoveryService(fs, filter);

        var files = service.Discover("root");
        var dataFile = files.First(f => f.RelativePath == "data.txt");

        dataFile.Size.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Discover_ExcludesGitDirectory()
    {
        var fs = new InMemoryFileSystem()
            .AddDirectory("root")
            .AddDirectory("root/.git")
            .AddFile("root/.git/config", "git config")
            .AddFile("root/readme.md", "hello");

        var filter = GitIgnoreFilter.Load("root", fs);
        var service = new FileDiscoveryService(fs, filter);

        var files = service.Discover("root");

        files.Should().NotContain(f => f.RelativePath.StartsWith(".git"));
        files.Should().Contain(f => f.RelativePath == "readme.md");
    }
}
