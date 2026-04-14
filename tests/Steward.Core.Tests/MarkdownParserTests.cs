using FluentAssertions;
using Steward.Core.Markdown;
using Xunit;

namespace Steward.Core.Tests;

public class MarkdownParserTests
{
    [Fact]
    public void Parse_ExtractsFrontmatter()
    {
        var content = """
            ---
            title: Test
            version: 1
            ---
            # Hello
            """.Replace("            ", "");

        var doc = MarkdownParser.Parse("test.md", content);

        doc.Frontmatter.Should().NotBeNull();
        doc.Frontmatter!.Fields.Should().ContainKey("title");
        doc.Frontmatter.Fields["title"].Should().Be("Test");
    }

    [Fact]
    public void Parse_NoFrontmatter_ReturnsNull()
    {
        var content = "# Hello\nSome text\n";

        var doc = MarkdownParser.Parse("test.md", content);

        doc.Frontmatter.Should().BeNull();
    }

    [Fact]
    public void Parse_ExtractsHeadings()
    {
        var content = "# Title\n## Section A\n## Section B\n### Subsection\n";

        var doc = MarkdownParser.Parse("test.md", content);

        doc.Sections.Should().HaveCount(1);
        doc.Sections[0].Heading.Should().Be("Title");
        doc.Sections[0].Children.Should().HaveCount(2);
        doc.Sections[0].Children[0].Heading.Should().Be("Section A");
        doc.Sections[0].Children[1].Heading.Should().Be("Section B");
        doc.Sections[0].Children[1].Children.Should().HaveCount(1);
        doc.Sections[0].Children[1].Children[0].Heading.Should().Be("Subsection");
    }

    [Fact]
    public void Parse_SectionLevels()
    {
        var content = "# H1\n## H2\n### H3\n";

        var doc = MarkdownParser.Parse("test.md", content);

        doc.Sections[0].Level.Should().Be(1);
        doc.Sections[0].Children[0].Level.Should().Be(2);
        doc.Sections[0].Children[0].Children[0].Level.Should().Be(3);
    }

    [Fact]
    public void Parse_SectionLineRanges()
    {
        var content = "# Title\nLine 1\nLine 2\n## Next\nLine 3\n";

        var doc = MarkdownParser.Parse("test.md", content);

        doc.Sections[0].Heading.Should().Be("Title");
        doc.Sections[0].Range.Start.Should().Be(1); // 1-based
        doc.Sections[0].Children[0].Heading.Should().Be("Next");
    }

    [Fact]
    public void Parse_DetectsCodeBlocks()
    {
        var content = "# Title\n```csharp\nvar x = 1;\n```\n";

        var doc = MarkdownParser.Parse("test.md", content);

        doc.Sections[0].ContentBlocks.Should().Contain(b => b.Type == ContentBlockType.CodeBlock);
    }

    [Fact]
    public void Parse_DetectsLists()
    {
        var content = "# Title\n- item 1\n- item 2\n";

        var doc = MarkdownParser.Parse("test.md", content);

        doc.Sections[0].ContentBlocks.Should().Contain(b => b.Type == ContentBlockType.List);
    }

    [Fact]
    public void Parse_ManagedRegions()
    {
        var content = "# Title\n<!-- steward:begin id=\"toc\" owner=\"steward\" -->\nTOC here\n<!-- steward:end -->\n";

        var doc = MarkdownParser.Parse("test.md", content);

        doc.ManagedRegions.Should().HaveCount(1);
        doc.ManagedRegions[0].Id.Should().Be("toc");
        doc.ManagedRegions[0].Owner.Should().Be("steward");
    }

    [Fact]
    public void Parse_TotalLines()
    {
        var content = "Line 1\nLine 2\nLine 3\n";

        var doc = MarkdownParser.Parse("test.md", content);

        doc.TotalLines.Should().Be(4); // trailing newline creates empty 4th line
    }

    [Fact]
    public void Parse_EmptyDocument()
    {
        var doc = MarkdownParser.Parse("empty.md", "");

        doc.Sections.Should().BeEmpty();
        doc.Frontmatter.Should().BeNull();
        doc.ManagedRegions.Should().BeEmpty();
    }

    [Fact]
    public void Parse_MultipleTopLevelHeadings()
    {
        var content = "# First\nText\n# Second\nMore text\n";

        var doc = MarkdownParser.Parse("test.md", content);

        doc.Sections.Should().HaveCount(2);
        doc.Sections[0].Heading.Should().Be("First");
        doc.Sections[1].Heading.Should().Be("Second");
    }
}
