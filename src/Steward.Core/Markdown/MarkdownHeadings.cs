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
}
