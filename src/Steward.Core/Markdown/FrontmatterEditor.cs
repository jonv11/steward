using Steward.Core.Abstractions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Steward.Core.Markdown;

/// <summary>
/// Edits YAML frontmatter within Markdown documents.
/// </summary>
public static class FrontmatterEditor
{
    private static readonly ISerializer YamlSerializer = new SerializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();

    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static EditResult SetField(StructuredDocument doc, string key, string value)
    {
        var lines = doc.RawContent.Split('\n').ToList();

        if (doc.Frontmatter == null)
        {
            // Create new frontmatter
            lines.InsertRange(0, ["---", $"{key}: {value}", "---", ""]);
            var newContent = string.Join('\n', lines);
            return EditResult.Changed(doc.RawContent, newContent,
                $"Created frontmatter with '{key}' = '{value}'.");
        }

        // Modify existing frontmatter
        var fields = new Dictionary<string, object?>(doc.Frontmatter.Fields);
        fields[key] = value;

        return ReplaceFrontmatter(doc, lines, fields,
            $"Set frontmatter field '{key}' = '{value}'.");
    }

    public static EditResult MergeFields(StructuredDocument doc, string yamlInput)
    {
        Dictionary<string, object?> newFields;
        try
        {
            newFields = YamlDeserializer.Deserialize<Dictionary<string, object?>>(yamlInput)
                        ?? new Dictionary<string, object?>();
        }
        catch (Exception ex)
        {
            return EditResult.Error($"Invalid YAML input: {ex.Message}");
        }

        var lines = doc.RawContent.Split('\n').ToList();

        if (doc.Frontmatter == null)
        {
            var yaml = YamlSerializer.Serialize(newFields).TrimEnd('\n', '\r');
            lines.InsertRange(0, ["---", yaml, "---", ""]);
            var newContent = string.Join('\n', lines);
            return EditResult.Changed(doc.RawContent, newContent, "Created frontmatter from merged fields.");
        }

        var merged = new Dictionary<string, object?>(doc.Frontmatter.Fields);
        foreach (var kv in newFields)
        {
            merged[kv.Key] = kv.Value;
        }

        return ReplaceFrontmatter(doc, lines, merged, "Merged fields into frontmatter.");
    }

    public static EditResult SetFields(StructuredDocument doc, IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Count == 0)
            return EditResult.NoOp(doc.RawContent, "No frontmatter fields required updates.");

        var merged = doc.Frontmatter != null
            ? new Dictionary<string, object?>(doc.Frontmatter.Fields)
            : new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in values)
            merged[key] = value;

        var lines = doc.RawContent.Split('\n').ToList();

        if (doc.Frontmatter == null)
        {
            var yaml = YamlSerializer.Serialize(merged).TrimEnd('\n', '\r');
            lines.InsertRange(0, ["---", yaml, "---", ""]);
            var newContent = string.Join('\n', lines);
            return EditResult.Changed(doc.RawContent, newContent,
                $"Created frontmatter with fields: {string.Join(", ", values.Keys)}.");
        }

        return ReplaceFrontmatter(doc, lines, merged,
            $"Set frontmatter field(s): {string.Join(", ", values.Keys)}.");
    }

    /// <summary>
    /// Reads the <c>last_updated</c> frontmatter field from a Markdown file and returns it as a UTC DateTime.
    /// Returns null when the file has no frontmatter, the field is absent, or the value cannot be parsed.
    /// </summary>
    public static DateTime? TryGetLastUpdatedDate(IFileSystem fileSystem, string fullPath)
    {
        try
        {
            var content = fileSystem.ReadAllText(fullPath);
            if (!content.StartsWith("---", StringComparison.Ordinal))
                return null;

            var endIdx = content.IndexOf("---", 3, StringComparison.Ordinal);
            if (endIdx < 0)
                return null;

            var yaml = content[3..endIdx];
            foreach (var line in yaml.Split('\n'))
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("last_updated:", StringComparison.OrdinalIgnoreCase))
                    continue;

                var value = trimmed["last_updated:".Length..].Trim().Trim('"', '\'');
                if (DateTime.TryParse(
                    value,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                    out var parsed))
                {
                    return parsed;
                }
            }
        }
        catch
        {
            // Best-effort; fall through to return null.
        }

        return null;
    }

    private static EditResult ReplaceFrontmatter(
        StructuredDocument doc, List<string> lines,
        Dictionary<string, object?> fields, string message)
    {
        var yaml = YamlSerializer.Serialize(fields).TrimEnd('\n', '\r');
        var startIdx = doc.Frontmatter!.Range.Start - 1; // 0-based
        var endIdx = doc.Frontmatter.Range.End - 1;

        // Remove old frontmatter block (including --- markers)
        lines.RemoveRange(startIdx, endIdx - startIdx + 1);

        // Insert new frontmatter
        var newFm = new List<string> { "---" };
        newFm.AddRange(yaml.Split('\n'));
        newFm.Add("---");
        lines.InsertRange(startIdx, newFm);

        var newContent = string.Join('\n', lines);
        return EditResult.Changed(doc.RawContent, newContent, message);
    }
}
