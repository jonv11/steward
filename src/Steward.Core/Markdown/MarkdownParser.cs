using Markdig;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Steward.Core.Markdown;

public static class MarkdownParser
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseYamlFrontMatter()
        .UsePreciseSourceLocation()
        .Build();

    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static StructuredDocument Parse(string filePath, string content)
    {
        var document = Markdig.Markdown.Parse(content, Pipeline);
        var lines = content.Split('\n');
        var totalLines = lines.Length;

        var frontmatter = ExtractFrontmatter(document, lines);
        var sections = BuildSectionHierarchy(document, lines, totalLines);
        var managedRegions = ExtractManagedRegions(lines);

        return new StructuredDocument
        {
            FilePath = filePath,
            RawContent = content,
            Frontmatter = frontmatter,
            Sections = sections,
            ManagedRegions = managedRegions,
            TotalLines = totalLines
        };
    }

    private static FrontmatterBlock? ExtractFrontmatter(MarkdownDocument document, string[] lines)
    {
        var yamlBlock = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();
        if (yamlBlock == null) return null;

        var startLine = yamlBlock.Line; // 0-based
        var endLine = startLine;

        // Find the closing --- line
        for (var i = startLine + 1; i < lines.Length; i++)
        {
            if (lines[i].TrimEnd('\r') == "---")
            {
                endLine = i;
                break;
            }
        }

        // Extract the YAML content between the --- markers
        var yamlLines = new List<string>();
        for (var i = startLine + 1; i < endLine; i++)
        {
            yamlLines.Add(lines[i].TrimEnd('\r'));
        }

        var rawYaml = string.Join('\n', yamlLines);
        Dictionary<string, object?> fields;

        try
        {
            fields = YamlDeserializer.Deserialize<Dictionary<string, object?>>(rawYaml)
                     ?? new Dictionary<string, object?>();
        }
        catch
        {
            fields = new Dictionary<string, object?>();
        }

        return new FrontmatterBlock
        {
            RawYaml = rawYaml,
            Fields = fields,
            Range = new LineRange(startLine + 1, endLine + 1) // 1-based
        };
    }

    private static IReadOnlyList<Section> BuildSectionHierarchy(
        MarkdownDocument document, string[] lines, int totalLines)
    {
        var headings = new List<(HeadingBlock Block, int LineNumber)>();

        foreach (var block in document)
        {
            if (block is HeadingBlock heading)
            {
                headings.Add((heading, heading.Line)); // 0-based
            }
        }

        if (headings.Count == 0) return [];

        // Build flat list of sections with ranges
        var flatSections = new List<(string Heading, int Level, int StartLine, int EndLine)>();

        for (var i = 0; i < headings.Count; i++)
        {
            var (block, line) = headings[i];
            var heading = block.Inline?.FirstChild?.ToString() ?? "";
            var level = block.Level;
            var startLine = line; // 0-based
            var endLine = i + 1 < headings.Count
                ? headings[i + 1].LineNumber - 1
                : totalLines - 1;

            flatSections.Add((heading, level, startLine, endLine));
        }

        // Build hierarchy
        return BuildChildren(flatSections, 0, flatSections.Count, document, lines);
    }

    private static IReadOnlyList<Section> BuildChildren(
        List<(string Heading, int Level, int StartLine, int EndLine)> sections,
        int start, int end,
        MarkdownDocument document, string[] lines)
    {
        var result = new List<Section>();
        var i = start;

        while (i < end)
        {
            var (heading, level, startLine, endLine) = sections[i];

            // Find range of children: all sections after this one that are deeper
            var childStart = i + 1;
            var childEnd = childStart;
            while (childEnd < end && sections[childEnd].Level > level)
            {
                childEnd++;
            }

            var children = BuildChildren(sections, childStart, childEnd, document, lines);
            var contentBlocks = ExtractContentBlocks(document, startLine, endLine, lines);

            result.Add(new Section
            {
                Heading = heading,
                Level = level,
                Range = new LineRange(startLine + 1, endLine + 1), // 1-based
                ContentBlocks = contentBlocks,
                Children = children
            });

            i = childEnd;
        }

        return result;
    }

    private static IReadOnlyList<ContentBlock> ExtractContentBlocks(
        MarkdownDocument document, int startLine, int endLine, string[] lines)
    {
        var blocks = new List<ContentBlock>();

        foreach (var block in document)
        {
            if (block is HeadingBlock || block is YamlFrontMatterBlock) continue;
            if (block.Line < startLine || block.Line > endLine) continue;

            var type = block switch
            {
                ListBlock => ContentBlockType.List,
                Markdig.Extensions.Tables.Table => ContentBlockType.Table,
                FencedCodeBlock or CodeBlock => ContentBlockType.CodeBlock,
                ThematicBreakBlock => ContentBlockType.ThematicBreak,
                HtmlBlock => ContentBlockType.HtmlBlock,
                ParagraphBlock => ContentBlockType.Paragraph,
                _ => ContentBlockType.Other
            };

            // Convert span end (char offset) to line number
            var endLineNum = block.Line;
            var charCount = 0;
            for (var lineIdx = 0; lineIdx < lines.Length; lineIdx++)
            {
                charCount += lines[lineIdx].Length + 1; // +1 for \n
                if (charCount > block.Span.End)
                {
                    endLineNum = lineIdx;
                    break;
                }
            }
            if (endLineNum < block.Line) endLineNum = block.Line;

            blocks.Add(new ContentBlock
            {
                Type = type,
                Range = new LineRange(block.Line + 1, endLineNum + 1) // 1-based
            });
        }

        return blocks;
    }

    private static IReadOnlyList<ManagedRegion> ExtractManagedRegions(string[] lines)
    {
        var regions = new List<ManagedRegion>();
        var openRegions = new Stack<(string Id, string? Owner, int StartLine)>();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');

            if (line.StartsWith("<!-- steward:begin ", StringComparison.OrdinalIgnoreCase))
            {
                var id = ExtractAttribute(line, "id");
                var owner = ExtractAttribute(line, "owner");
                if (id != null)
                {
                    openRegions.Push((id, owner, i));
                }
            }
            else if (line.StartsWith("<!-- steward:end", StringComparison.OrdinalIgnoreCase) && openRegions.Count > 0)
            {
                var (id, owner, startLine) = openRegions.Pop();
                regions.Add(new ManagedRegion(id, owner, new LineRange(startLine + 1, i + 1))); // 1-based
            }
        }

        return regions;
    }

    private static string? ExtractAttribute(string line, string name)
    {
        var search = $"{name}=\"";
        var idx = line.IndexOf(search, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;

        var start = idx + search.Length;
        var end = line.IndexOf('"', start);
        if (end < 0) return null;

        return line[start..end];
    }
}
