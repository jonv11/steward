using FluentAssertions;
using Steward.Core.Discovery;
using Steward.Core.Orientation;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class OutlineEngineTests
{
    [Fact]
    public void BuildOutline_IncludesSizes_WhenRequested()
    {
        var fs = new InMemoryFileSystem()
            .AddDirectory("root")
            .AddFile("root/data.txt", "hello world");

        var files = new List<DiscoveredFile>
        {
            new("data.txt", 11, false)
        };

        var engine = new OutlineEngine(fs);
        var result = engine.BuildOutline("root", files, includeSizes: true);

        result.Entries.Should().HaveCount(1);
        result.Entries[0].Size.Should().Be(11);
    }

    [Fact]
    public void BuildOutline_ExcludesSizes_WhenNotRequested()
    {
        var fs = new InMemoryFileSystem()
            .AddDirectory("root")
            .AddFile("root/data.txt", "content");

        var files = new List<DiscoveredFile> { new("data.txt", 7, false) };

        var engine = new OutlineEngine(fs);
        var result = engine.BuildOutline("root", files, includeSizes: false);

        result.Entries[0].Size.Should().BeNull();
    }

    [Fact]
    public void BuildOutline_IncludesLineCount_WhenRequested()
    {
        var fs = new InMemoryFileSystem()
            .AddDirectory("root")
            .AddFile("root/data.txt", "line1\nline2\nline3");

        var files = new List<DiscoveredFile> { new("data.txt", 18, false) };

        var engine = new OutlineEngine(fs);
        var result = engine.BuildOutline("root", files, includeLines: true);

        result.Entries[0].LineCount.Should().Be(3);
    }

    [Fact]
    public void BuildOutline_RespectsMaxDepth()
    {
        var fs = new InMemoryFileSystem()
            .AddDirectory("root");

        var files = new List<DiscoveredFile>
        {
            new("a", 0, true),
            new("a/b", 0, true),
            new("a/b/c", 0, true),
        };

        var engine = new OutlineEngine(fs);
        var result = engine.BuildOutline("root", files, maxDepth: 2);

        result.Entries.Should().HaveCount(2);
    }

    [Fact]
    public void FormatSize_FormatsCorrectly()
    {
        OutlineEngine.FormatSize(500).Should().Be("500 B");
        OutlineEngine.FormatSize(1536).Should().Be("1.5 KB");
        OutlineEngine.FormatSize(2 * 1024 * 1024).Should().Be("2.0 MB");
    }
}
