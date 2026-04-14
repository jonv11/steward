using FluentAssertions;
using Steward.Core.Markdown;
using Xunit;

namespace Steward.Core.Tests;

public class MdPathSelectorTests
{
    private static StructuredDocument ParseSample()
    {
        var content = """
            ---
            title: Sample
            author: Test
            ---
            # Introduction
            Some intro text.
            ## Goals
            Goal content here.
            ## Non-Goals
            Non-goal content.
            # Implementation
            Impl text.
            ## Phase 1
            Phase 1 details.
            <!-- steward:begin id="toc" owner="steward" -->
            TOC placeholder
            <!-- steward:end -->
            """.Replace("            ", "");

        return MarkdownParser.Parse("sample.md", content);
    }

    [Fact]
    public void Evaluate_Frontmatter_ReturnsBlock()
    {
        var doc = ParseSample();
        var result = MdPathSelector.Evaluate(doc, "frontmatter");

        result.HasMatches.Should().BeTrue();
        result.Matches.Should().HaveCount(1);
        result.Matches[0].Kind.Should().Be(MatchKind.Frontmatter);
        result.Matches[0].Content.Should().Contain("title: Sample");
    }

    [Fact]
    public void Evaluate_FrontmatterField_ReturnsValue()
    {
        var doc = ParseSample();
        var result = MdPathSelector.Evaluate(doc, "frontmatter.title");

        result.HasMatches.Should().BeTrue();
        result.Matches[0].Content.Should().Be("Sample");
    }

    [Fact]
    public void Evaluate_FrontmatterField_Missing_ReturnsEmpty()
    {
        var doc = ParseSample();
        var result = MdPathSelector.Evaluate(doc, "frontmatter.nonexistent");

        result.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_HeadingByName_ReturnsSection()
    {
        var doc = ParseSample();
        var result = MdPathSelector.Evaluate(doc, "heading[Goals]");

        result.HasMatches.Should().BeTrue();
        result.Matches[0].Kind.Should().Be(MatchKind.Section);
        result.Matches[0].Content.Should().Contain("Goal content");
    }

    [Fact]
    public void Evaluate_HeadingByPath_ReturnsNestedSection()
    {
        var doc = ParseSample();
        var result = MdPathSelector.Evaluate(doc, "heading[Implementation/Phase 1]");

        result.HasMatches.Should().BeTrue();
        result.Matches[0].Label.Should().Be("Phase 1");
    }

    [Fact]
    public void Evaluate_HeadingByIndex_ReturnsNthHeading()
    {
        var doc = ParseSample();
        var result = MdPathSelector.Evaluate(doc, "heading[#1]");

        result.HasMatches.Should().BeTrue();
        result.Matches[0].Label.Should().Be("Introduction");
    }

    [Fact]
    public void Evaluate_HeadingNotFound_ReturnsEmpty()
    {
        var doc = ParseSample();
        var result = MdPathSelector.Evaluate(doc, "heading[DoesNotExist]");

        result.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_ManagedRegion_ReturnsContent()
    {
        var doc = ParseSample();
        var result = MdPathSelector.Evaluate(doc, "managed[toc]");

        result.HasMatches.Should().BeTrue();
        result.Matches[0].Kind.Should().Be(MatchKind.ManagedRegion);
        result.Matches[0].Content.Should().Contain("TOC placeholder");
    }

    [Fact]
    public void Evaluate_ManagedRegionNotFound_ReturnsEmpty()
    {
        var doc = ParseSample();
        var result = MdPathSelector.Evaluate(doc, "managed[nonexistent]");

        result.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_InvalidSyntax_ReturnsError()
    {
        var doc = ParseSample();
        var result = MdPathSelector.Evaluate(doc, "invalid-selector");

        result.IsError.Should().BeTrue();
        result.ErrorMessage.Should().Contain("Unrecognized");
    }

    [Fact]
    public void Evaluate_HeadingCaseInsensitive()
    {
        var doc = ParseSample();
        var result = MdPathSelector.Evaluate(doc, "heading[goals]");

        result.HasMatches.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_FrontmatterOnDocWithoutIt_ReturnsEmpty()
    {
        var doc = MarkdownParser.Parse("test.md", "# Title\nContent\n");
        var result = MdPathSelector.Evaluate(doc, "frontmatter");

        result.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_InvalidHeadingIndex_ReturnsError()
    {
        var doc = ParseSample();
        var result = MdPathSelector.Evaluate(doc, "heading[#0]");

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_HeadingIndexOutOfRange_ReturnsEmpty()
    {
        var doc = ParseSample();
        var result = MdPathSelector.Evaluate(doc, "heading[#999]");

        result.IsEmpty.Should().BeTrue();
    }
}
