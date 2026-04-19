using System.Text;

namespace Steward.Core.Markdown;

/// <summary>
/// Shared helpers for working with Markdown heading text, traversal, and anchor slugs.
/// </summary>
public static class MarkdownHeadings
{
    public static IReadOnlyList<Section> Flatten(IReadOnlyList<Section> sections)
    {
        var result = new List<Section>();
        Collect(sections, result);
        return result;
    }

    public static bool TryFindSectionAtLine(
        IReadOnlyList<Section> sections,
        int lineNumber,
        out Section? section,
        out IReadOnlyList<string> headingPath)
    {
        var path = new List<string>();
        if (TryFindSectionAtLine(sections, lineNumber, path, out section))
        {
            headingPath = path;
            return true;
        }

        headingPath = [];
        return false;
    }

    public static string? TryCreateSafeSelector(StructuredDocument doc, IReadOnlyList<string> headingPath, Section section)
    {
        var anchorSlug = ToAnchorSlug(section.Heading);
        if (!string.IsNullOrWhiteSpace(anchorSlug))
        {
            var anchorSelector = $"#{anchorSlug}";
            var anchorResult = MdPathSelector.Evaluate(doc, anchorSelector);
            if (!anchorResult.IsError && anchorResult.Matches.Count == 1)
                return anchorSelector;
        }

        if (headingPath.Count == 0 || headingPath.Any(static segment => segment.Contains('/') || segment.Contains(']')))
            return null;

        var headingSelector = $"heading[{string.Join('/', headingPath)}]";
        var headingResult = MdPathSelector.Evaluate(doc, headingSelector);
        return !headingResult.IsError && headingResult.Matches.Count == 1
            ? headingSelector
            : null;
    }

    public static string ToAnchorSlug(string heading)
    {
        if (string.IsNullOrWhiteSpace(heading))
            return string.Empty;

        var normalized = heading.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
        var builder = new StringBuilder();
        var lastWasHyphen = false;

        foreach (var character in normalized)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                lastWasHyphen = false;
            }
            else if (char.IsWhiteSpace(character) || character is '-' or '_')
            {
                if (!lastWasHyphen && builder.Length > 0)
                {
                    builder.Append('-');
                    lastWasHyphen = true;
                }
            }
        }

        return builder.ToString().Trim('-');
    }

    private static void Collect(IReadOnlyList<Section> sections, List<Section> result)
    {
        foreach (var section in sections)
        {
            result.Add(section);
            Collect(section.Children, result);
        }
    }

    private static bool TryFindSectionAtLine(
        IReadOnlyList<Section> sections,
        int lineNumber,
        List<string> headingPath,
        out Section? section)
    {
        foreach (var candidate in sections)
        {
            headingPath.Add(candidate.Heading);

            if (lineNumber >= candidate.Range.Start && lineNumber <= candidate.Range.End)
            {
                if (TryFindSectionAtLine(candidate.Children, lineNumber, headingPath, out section))
                    return true;

                section = candidate;
                return true;
            }

            if (TryFindSectionAtLine(candidate.Children, lineNumber, headingPath, out section))
                return true;

            headingPath.RemoveAt(headingPath.Count - 1);
        }

        section = null;
        return false;
    }
}
