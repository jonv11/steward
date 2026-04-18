namespace Steward.Core.Markdown;

/// <summary>
/// Parses and evaluates MdPath selector expressions against a StructuredDocument.
/// Supported selectors:
///   frontmatter            — entire frontmatter block
///   frontmatter.field      — specific frontmatter field
///   #anchor-slug          — heading resolved through Markdown anchor normalization
///   README.md#anchor      — Markdown link-style heading selector
///   heading[Name]          — section by heading text
///   heading[Parent/Child]  — nested heading path
///   heading[#N]            — Nth heading (1-based)
///   managed[id]            — managed region by id
///   managed[*]             — all managed regions
///   heading[Name].lists    — list blocks in section
///   heading[Name].tables   — table blocks in section
///   heading[Name].codeblocks — code blocks in section
/// </summary>
public static class MdPathSelector
{
    public static bool IsAnchorSelector(string selector)
    {
        if (string.IsNullOrWhiteSpace(selector))
            return false;

        if (selector[0] == '#')
            return selector.Length > 1;

        if (selector.StartsWith("heading[", StringComparison.OrdinalIgnoreCase) ||
            selector.StartsWith("frontmatter", StringComparison.OrdinalIgnoreCase) ||
            selector.StartsWith("managed[", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var fragmentIndex = selector.IndexOf('#');
        return fragmentIndex > 0 && fragmentIndex < selector.Length - 1;
    }

    public static SelectorResult Evaluate(StructuredDocument doc, string selector)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);

        if (selector.Equals("frontmatter", StringComparison.OrdinalIgnoreCase))
            return EvaluateFrontmatter(doc);

        if (selector.StartsWith("frontmatter.", StringComparison.OrdinalIgnoreCase))
            return EvaluateFrontmatterField(doc, selector[12..]);

        if (TryGetAnchorSlug(selector, out var anchorSlug))
            return EvaluateAnchor(doc, selector, anchorSlug);

        if (selector.StartsWith("heading[", StringComparison.OrdinalIgnoreCase) && selector.EndsWith(']'))
            return EvaluateHeading(doc, selector[8..^1]);

        // heading[Name].lists / .tables / .codeblocks sub-selectors
        if (selector.StartsWith("heading[", StringComparison.OrdinalIgnoreCase))
        {
            var closeBracket = selector.IndexOf(']');
            if (closeBracket > 8 && closeBracket < selector.Length - 1 && selector[closeBracket + 1] == '.')
            {
                var headingPath = selector[8..closeBracket];
                var subSelector = selector[(closeBracket + 2)..];
                return EvaluateHeadingSubSelector(doc, headingPath, subSelector);
            }
        }

        if (selector.StartsWith("managed[", StringComparison.OrdinalIgnoreCase) && selector.EndsWith(']'))
            return EvaluateManaged(doc, selector[8..^1]);

        return SelectorResult.Error($"Unrecognized selector syntax: '{selector}'");
    }

    private static SelectorResult EvaluateFrontmatter(StructuredDocument doc)
    {
        if (doc.Frontmatter == null)
            return SelectorResult.Empty("frontmatter");

        var content = doc.Frontmatter.RawYaml;
        return new SelectorResult
        {
            Selector = "frontmatter",
            Matches =
            [
                new SelectorMatch
                {
                    Kind = MatchKind.Frontmatter,
                    Label = "frontmatter",
                    Content = content,
                    Range = doc.Frontmatter.Range
                }
            ]
        };
    }

    private static SelectorResult EvaluateFrontmatterField(StructuredDocument doc, string field)
    {
        if (doc.Frontmatter == null)
            return SelectorResult.Empty($"frontmatter.{field}");

        if (!doc.Frontmatter.Fields.TryGetValue(field, out var value))
            return SelectorResult.Empty($"frontmatter.{field}");

        return new SelectorResult
        {
            Selector = $"frontmatter.{field}",
            Matches =
            [
                new SelectorMatch
                {
                    Kind = MatchKind.FrontmatterField,
                    Label = field,
                    Content = value?.ToString() ?? "",
                    Range = doc.Frontmatter.Range
                }
            ]
        };
    }

    private static SelectorResult EvaluateHeading(StructuredDocument doc, string path)
    {
        // heading[#N] — index-based
        if (path.StartsWith('#'))
        {
            if (!int.TryParse(path[1..], out var index) || index < 1)
                return SelectorResult.Error($"Invalid heading index: '{path}'");

            var allSections = MarkdownHeadings.Flatten(doc.Sections);
            if (index > allSections.Count)
                return SelectorResult.Empty($"heading[{path}]");

            var section = allSections[index - 1];
            return MakeSectionResult($"heading[{path}]", section, doc);
        }

        // heading[Parent/Child] or heading[Name]
        var parts = path.Split('/');
        var matches = FindSectionsByPath(doc.Sections, parts, 0);

        if (matches.Count == 0)
            return SelectorResult.Empty($"heading[{path}]");

        if (matches.Count > 1)
            return SelectorResult.Ambiguous($"heading[{path}]", matches.Count);

        return MakeSectionResult($"heading[{path}]", matches[0], doc);
    }

    private static SelectorResult EvaluateHeadingSubSelector(StructuredDocument doc, string headingPath, string subSelector)
    {
        var selectorStr = $"heading[{headingPath}].{subSelector}";
        var blockType = subSelector.ToLowerInvariant() switch
        {
            "lists" => ContentBlockType.List,
            "tables" => ContentBlockType.Table,
            "codeblocks" => ContentBlockType.CodeBlock,
            _ => (ContentBlockType?)null
        };

        if (blockType == null)
            return SelectorResult.Error($"Unknown sub-selector '.{subSelector}'. Supported: .lists, .tables, .codeblocks");

        // Find the section
        var parts = headingPath.Split('/');
        var sections = parts[0].StartsWith('#')
            ? [MarkdownHeadings.Flatten(doc.Sections).ElementAtOrDefault(int.Parse(parts[0][1..]) - 1)!]
            : FindSectionsByPath(doc.Sections, parts, 0);

        if (sections.Count == 0 || sections[0] == null)
            return SelectorResult.Empty(selectorStr);

        if (sections.Count > 1)
            return SelectorResult.Ambiguous(selectorStr, sections.Count);

        var section = sections[0];
        var blocks = section.ContentBlocks
            .Where(b => b.Type == blockType)
            .Select(b => new SelectorMatch
            {
                Kind = MatchKind.ContentBlock,
                Label = $"{subSelector}@L{b.Range.Start}",
                Content = ExtractLines(doc.RawContent, b.Range),
                Range = b.Range
            })
            .ToList();

        if (blocks.Count == 0)
            return SelectorResult.Empty(selectorStr);

        return new SelectorResult
        {
            Selector = selectorStr,
            Matches = blocks
        };
    }

    private static SelectorResult EvaluateAnchor(StructuredDocument doc, string selector, string anchorSlug)
    {
        var matches = MarkdownHeadings.Flatten(doc.Sections)
            .Where(section => string.Equals(
                MarkdownHeadings.ToAnchorSlug(section.Heading),
                anchorSlug,
                StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
            return SelectorResult.Empty(selector);

        if (matches.Count > 1)
            return SelectorResult.Ambiguous(selector, matches.Count);

        return MakeSectionResult(selector, matches[0], doc);
    }

    private static SelectorResult EvaluateManaged(StructuredDocument doc, string id)
    {
        // managed[*] — return all managed regions
        if (id == "*")
        {
            if (doc.ManagedRegions.Count == 0)
                return SelectorResult.Empty("managed[*]");

            var allMatches = doc.ManagedRegions.Select(r => new SelectorMatch
            {
                Kind = MatchKind.ManagedRegion,
                Label = r.Id,
                Content = ExtractLines(doc.RawContent, r.Range),
                Range = r.Range
            }).ToList();

            return new SelectorResult
            {
                Selector = "managed[*]",
                Matches = allMatches
            };
        }

        var matches = doc.ManagedRegions.Where(r => r.Id == id).ToList();

        if (matches.Count == 0)
            return SelectorResult.Empty($"managed[{id}]");

        if (matches.Count > 1)
            return SelectorResult.Ambiguous($"managed[{id}]", matches.Count);

        var region = matches[0];
        var content = ExtractLines(doc.RawContent, region.Range);

        return new SelectorResult
        {
            Selector = $"managed[{id}]",
            Matches =
            [
                new SelectorMatch
                {
                    Kind = MatchKind.ManagedRegion,
                    Label = id,
                    Content = content,
                    Range = region.Range
                }
            ]
        };
    }

    private static List<Section> FindSectionsByPath(
        IReadOnlyList<Section> sections, string[] parts, int depth)
    {
        var results = new List<Section>();

        foreach (var section in sections)
        {
            if (string.Equals(section.Heading, parts[depth], StringComparison.OrdinalIgnoreCase))
            {
                if (depth == parts.Length - 1)
                {
                    results.Add(section);
                }
                else
                {
                    results.AddRange(FindSectionsByPath(section.Children, parts, depth + 1));
                }
            }

            // For single-name selectors, also search within children recursively
            if (depth == 0 && parts.Length == 1)
            {
                results.AddRange(FindSectionsByPath(section.Children, parts, 0));
            }
        }

        return results;
    }

    private static SelectorResult MakeSectionResult(string selector, Section section, StructuredDocument doc)
    {
        var content = ExtractLines(doc.RawContent, section.Range);

        return new SelectorResult
        {
            Selector = selector,
            Matches =
            [
                new SelectorMatch
                {
                    Kind = MatchKind.Section,
                    Label = section.Heading,
                    Content = content,
                    Range = section.Range
                }
            ]
        };
    }

    private static string ExtractLines(string content, LineRange range)
    {
        var lines = content.Split('\n');
        var start = Math.Max(0, range.Start - 1); // to 0-based
        var end = Math.Min(lines.Length, range.End); // exclusive
        return string.Join('\n', lines[start..end]);
    }

    private static bool TryGetAnchorSlug(string selector, out string anchorSlug)
    {
        anchorSlug = string.Empty;

        if (string.IsNullOrWhiteSpace(selector))
            return false;

        if (selector[0] == '#')
        {
            anchorSlug = selector[1..].Trim();
            return anchorSlug.Length > 0;
        }

        if (!IsAnchorSelector(selector))
            return false;

        var fragmentIndex = selector.IndexOf('#');
        anchorSlug = selector[(fragmentIndex + 1)..].Trim();
        return anchorSlug.Length > 0;
    }
}

public sealed class SelectorResult
{
    public required string Selector { get; init; }
    public IReadOnlyList<SelectorMatch> Matches { get; init; } = [];
    public string? ErrorMessage { get; init; }
    public bool IsError => ErrorMessage != null;
    public bool IsEmpty => !IsError && Matches.Count == 0;
    public bool HasMatches => Matches.Count > 0;

    public static SelectorResult Empty(string selector) => new()
    {
        Selector = selector,
        Matches = []
    };

    public static SelectorResult Error(string message) => new()
    {
        Selector = "",
        ErrorMessage = message
    };

    public static SelectorResult Ambiguous(string selector, int count) => new()
    {
        Selector = selector,
        ErrorMessage = $"Ambiguous selector '{selector}': matched {count} elements. Use an index or more specific path."
    };
}

public sealed class SelectorMatch
{
    public required MatchKind Kind { get; init; }
    public required string Label { get; init; }
    public required string Content { get; init; }
    public required LineRange Range { get; init; }
}

public enum MatchKind
{
    Frontmatter,
    FrontmatterField,
    Section,
    ManagedRegion,
    ContentBlock
}
