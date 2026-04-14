using FluentAssertions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Search;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class SearchEngineTests
{
    private readonly InMemoryFileSystem _fs = new();

    private SearchEngine CreateEngine() => new(_fs, "root");

    [Fact]
    public void Search_ContentMode_FindsTextMatches()
    {
        _fs.AddFile("root/doc.txt", "Hello world\nAnother line\nHello again\n");
        var files = new[] { new DiscoveredFile("doc.txt", 40, false) };

        var result = CreateEngine().Search("Hello", files, SearchMode.Content);

        result.Matches.Should().HaveCount(2);
        result.Matches.Should().OnlyContain(m => m.Kind == SearchMatchKind.Content);
    }

    [Fact]
    public void Search_HeadingMode_FindsOnlyHeadings()
    {
        _fs.AddFile("root/doc.md", "# Hello\nSome text with Hello\n## World\n");
        var files = new[] { new DiscoveredFile("doc.md", 40, false) };

        var result = CreateEngine().Search("Hello", files, SearchMode.Headings);

        result.Matches.Should().HaveCount(1);
        result.Matches[0].Kind.Should().Be(SearchMatchKind.Heading);
        result.Matches[0].HeadingContext.Should().Be("Hello");
    }

    [Fact]
    public void Search_AllMode_FindsBothContentAndHeadings()
    {
        _fs.AddFile("root/doc.md", "# Goals\nWe have goals here\n## Sub\n");
        var files = new[] { new DiscoveredFile("doc.md", 40, false) };

        var result = CreateEngine().Search("Goals", files, SearchMode.All);

        result.Matches.Should().Contain(m => m.Kind == SearchMatchKind.Heading);
        result.Matches.Should().Contain(m => m.Kind == SearchMatchKind.Content);
    }

    [Fact]
    public void Search_CaseInsensitive()
    {
        _fs.AddFile("root/doc.txt", "Hello World\n");
        var files = new[] { new DiscoveredFile("doc.txt", 15, false) };

        var result = CreateEngine().Search("hello", files, SearchMode.Content);

        result.Matches.Should().HaveCount(1);
    }

    [Fact]
    public void Search_HeadingContext_IncludesNearestHeading()
    {
        _fs.AddFile("root/doc.md", "# Title\nIntro text\n## Details\nImportant detail here\n");
        var files = new[] { new DiscoveredFile("doc.md", 50, false) };

        var result = CreateEngine().Search("Important", files, SearchMode.Content);

        result.Matches.Should().HaveCount(1);
        result.Matches[0].HeadingContext.Should().Be("Details");
    }

    [Fact]
    public void Search_MaxResults_LimitsOutput()
    {
        var lines = string.Concat(Enumerable.Range(1, 200).Select(i => $"match line {i}\n"));
        _fs.AddFile("root/big.txt", lines);
        var files = new[] { new DiscoveredFile("big.txt", 5000, false) };

        var result = CreateEngine().Search("match", files, SearchMode.Content, maxResults: 5);

        result.Matches.Should().HaveCount(5);
        result.TotalMatches.Should().BeGreaterThan(5);
        result.Truncated.Should().BeTrue();
    }

    [Fact]
    public void Search_ScopeFilter_LimitsToRole()
    {
        _fs.AddFile("root/README.md", "# Hello\nContent\n");
        _fs.AddFile("root/notes.md", "# Hello\nMore content\n");
        var files = new[]
        {
            new DiscoveredFile("README.md", 20, false),
            new DiscoveredFile("notes.md", 20, false)
        };

        var policy = new RepositoryPolicy
        {
            Artifacts =
            [
                new ArtifactDefinition { Path = "README.md", Role = "authoritative", Required = true }
            ]
        };

        var result = CreateEngine().Search("Hello", files, SearchMode.Content,
            scopeRole: "authoritative", policy: policy);

        result.Matches.Should().OnlyContain(m => m.Path == "README.md");
    }

    [Fact]
    public void Search_NoMatches_EmptyResult()
    {
        _fs.AddFile("root/doc.txt", "Nothing relevant here\n");
        var files = new[] { new DiscoveredFile("doc.txt", 25, false) };

        var result = CreateEngine().Search("nonexistent", files, SearchMode.Content);

        result.Matches.Should().BeEmpty();
        result.TotalMatches.Should().Be(0);
    }

    [Fact]
    public void Search_SkipsDirectories()
    {
        var files = new[] { new DiscoveredFile("src", 0, true) };

        var result = CreateEngine().Search("anything", files, SearchMode.Content);

        result.Matches.Should().BeEmpty();
    }

    [Fact]
    public void Search_NonMdFiles_NoHeadingContext()
    {
        _fs.AddFile("root/code.cs", "// Hello world\n");
        var files = new[] { new DiscoveredFile("code.cs", 20, false) };

        var result = CreateEngine().Search("Hello", files, SearchMode.Content);

        result.Matches.Should().HaveCount(1);
        result.Matches[0].HeadingContext.Should().BeNull();
    }

    [Fact]
    public void Search_LineAndColumnCorrect()
    {
        _fs.AddFile("root/doc.txt", "first\nsecond target here\nthird\n");
        var files = new[] { new DiscoveredFile("doc.txt", 30, false) };

        var result = CreateEngine().Search("target", files, SearchMode.Content);

        result.Matches.Should().HaveCount(1);
        result.Matches[0].Line.Should().Be(2);
        result.Matches[0].Column.Should().Be(8); // "second " = 7 chars, column 8
    }
}
