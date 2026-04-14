namespace Steward.Core.Markdown;

using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

/// <summary>
/// Structural editor: performs raw-text edits guided by structural model positions.
/// All operations return EditResult without modifying the input.
/// </summary>
public static class StructuralEditor
{
    public static EditResult EnsureSection(StructuredDocument doc, string heading, string? under = null, string? content = null)
    {
        // Check if section already exists
        var existing = FindSection(doc.Sections, heading);
        if (existing != null)
            return EditResult.NoOp(doc.RawContent, $"Section '{heading}' already exists.");

        return InsertSection(doc, heading, under, content);
    }

    public static EditResult SetSection(StructuredDocument doc, string heading, string newContent)
    {
        var section = FindSection(doc.Sections, heading);
        if (section == null)
            return EditResult.Error($"Section '{heading}' not found.");

        // Check ownership
        var ownershipError = CheckOwnership(doc, section, "steward");
        if (ownershipError != null) return ownershipError;

        var lines = doc.RawContent.Split('\n').ToList();
        var startIdx = section.Range.Start - 1; // 0-based
        var endIdx = section.Range.End - 1;

        // Find content start (line after heading)
        var contentStart = startIdx + 1;

        // Find content end (before next heading or section end)
        var contentEnd = endIdx;
        for (var i = contentStart; i <= endIdx; i++)
        {
            if (i < lines.Count && lines[i].StartsWith('#'))
            {
                contentEnd = i - 1;
                break;
            }
        }

        // Replace content lines
        var newLines = newContent.Split('\n');
        var replacementLines = new List<string>();
        replacementLines.Add(lines[startIdx]); // keep heading
        replacementLines.AddRange(newLines);

        lines.RemoveRange(startIdx, contentEnd - startIdx + 1);
        lines.InsertRange(startIdx, replacementLines);

        var newRawContent = string.Join('\n', lines);
        return EditResult.Changed(doc.RawContent, newRawContent,
            $"Set content of section '{heading}'.");
    }

    public static EditResult InsertSection(StructuredDocument doc, string heading, string? under = null, string? content = null)
    {
        var lines = doc.RawContent.Split('\n').ToList();
        int level;
        int insertIdx;

        if (under != null)
        {
            var parent = FindSection(doc.Sections, under);
            if (parent == null)
                return EditResult.Error($"Parent section '{under}' not found.");

            level = parent.Level + 1;
            insertIdx = parent.Range.End; // Insert at end of parent (0-based = End since End is 1-based)
        }
        else
        {
            // Insert at end of document as top-level
            level = 1;
            insertIdx = lines.Count;
        }

        var hashes = new string('#', level);
        var newLines = new List<string> { "", $"{hashes} {heading}" };
        if (!string.IsNullOrEmpty(content))
        {
            newLines.Add(content);
        }
        newLines.Add("");

        lines.InsertRange(insertIdx, newLines);
        var newRawContent = string.Join('\n', lines);
        return EditResult.Changed(doc.RawContent, newRawContent,
            $"Inserted section '{heading}'" + (under != null ? $" under '{under}'." : "."));
    }

    public static EditResult AppendBlock(StructuredDocument doc, string heading, string block)
    {
        var section = FindSection(doc.Sections, heading);
        if (section == null)
            return EditResult.Error($"Section '{heading}' not found.");

        var ownershipError = CheckOwnership(doc, section, "steward");
        if (ownershipError != null) return ownershipError;

        var lines = doc.RawContent.Split('\n').ToList();

        // Find end of section content (before children or section end)
        var insertIdx = section.Range.End; // 1-based → insert after
        // If there are children, insert before first child
        if (section.Children.Count > 0)
        {
            insertIdx = section.Children[0].Range.Start - 1; // just before first child
        }

        lines.InsertRange(insertIdx, [block, ""]);
        var newRawContent = string.Join('\n', lines);
        return EditResult.Changed(doc.RawContent, newRawContent,
            $"Appended block to section '{heading}'.");
    }

    public static EditResult PrependBlock(StructuredDocument doc, string heading, string block)
    {
        var section = FindSection(doc.Sections, heading);
        if (section == null)
            return EditResult.Error($"Section '{heading}' not found.");

        var ownershipError = CheckOwnership(doc, section, "steward");
        if (ownershipError != null) return ownershipError;

        var lines = doc.RawContent.Split('\n').ToList();
        var insertIdx = section.Range.Start; // After heading (1-based, so this is line after heading in 0-based)

        lines.InsertRange(insertIdx, ["", block]);
        var newRawContent = string.Join('\n', lines);
        return EditResult.Changed(doc.RawContent, newRawContent,
            $"Prepended block to section '{heading}'.");
    }

    private static Section? FindSection(IReadOnlyList<Section> sections, string heading)
    {
        foreach (var section in sections)
        {
            if (string.Equals(section.Heading, heading, StringComparison.OrdinalIgnoreCase))
                return section;

            var child = FindSection(section.Children, heading);
            if (child != null) return child;
        }
        return null;
    }

    private static EditResult? CheckOwnership(StructuredDocument doc, Section section, string requestedOwner)
    {
        foreach (var region in doc.ManagedRegions)
        {
            // A section is in a managed region if its heading line falls within the region
            if (section.Range.Start >= region.Range.Start && section.Range.Start <= region.Range.End)
            {
                if (region.Owner != null && !string.Equals(region.Owner, requestedOwner, StringComparison.OrdinalIgnoreCase))
                {
                    return EditResult.Error(
                        $"Section '{section.Heading}' is within managed region '{region.Id}' owned by '{region.Owner}'. " +
                        $"Requested owner '{requestedOwner}' does not match.");
                }
            }
        }
        return null;
    }
}

public sealed class EditResult
{
    public required string OriginalContent { get; init; }
    public required string NewContent { get; init; }
    public required string Message { get; init; }
    public bool HasChanges { get; init; }
    public bool IsError { get; init; }

    public static EditResult NoOp(string content, string message) => new()
    {
        OriginalContent = content,
        NewContent = content,
        Message = message,
        HasChanges = false,
        IsError = false
    };

    public static EditResult Changed(string original, string newContent, string message) => new()
    {
        OriginalContent = original,
        NewContent = newContent,
        Message = message,
        HasChanges = true,
        IsError = false
    };

    public static EditResult Error(string message) => new()
    {
        OriginalContent = "",
        NewContent = "",
        Message = message,
        HasChanges = false,
        IsError = true
    };

    public string GetUnifiedDiff()
    {
        if (!HasChanges) return "";

        var diffBuilder = new InlineDiffBuilder(new Differ());
        var diff = diffBuilder.BuildDiffModel(OriginalContent, NewContent);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("--- a/file");
        sb.AppendLine("+++ b/file");

        foreach (var line in diff.Lines)
        {
            switch (line.Type)
            {
                case ChangeType.Inserted:
                    sb.AppendLine($"+{line.Text}");
                    break;
                case ChangeType.Deleted:
                    sb.AppendLine($"-{line.Text}");
                    break;
                case ChangeType.Unchanged:
                    sb.AppendLine($" {line.Text}");
                    break;
            }
        }

        return sb.ToString();
    }
}
