using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Steward.Core.Configuration;
using Steward.Core.Validation;

namespace Steward.Core.Baseline;

/// <summary>
/// One grandfathered violation in <c>.steward/baseline.json</c>. Mirrors
/// <see cref="DiagnosticIdentity"/>: rule, repository-relative path and selector.
/// </summary>
public sealed record BaselineEntry(
    [property: JsonPropertyName("rule")] string Rule,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("selector")] string Selector)
{
    [JsonIgnore]
    public DiagnosticIdentity Identity => new(Rule.ToUpperInvariant(), PathHelper.NormalizeSeparators(Path), Selector);

    public static BaselineEntry From(DiagnosticIdentity identity) => new(identity.RuleId, identity.Path, identity.Selector);
}

/// <summary>
/// The generated baseline file (RFC-017). Serialization is deterministic: entries are sorted by
/// path, rule and selector, so regenerating an unchanged repository produces no diff.
/// </summary>
public sealed record BaselineDocument(
    [property: JsonPropertyName("generated_at")] string GeneratedAt,
    [property: JsonPropertyName("steward_version")] string? StewardVersion,
    [property: JsonPropertyName("entries")] IReadOnlyList<BaselineEntry> Entries)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    /// <summary>Builds a baseline holding every diagnostic's identity, sorted.</summary>
    public static BaselineDocument FromDiagnostics(
        IEnumerable<Diagnostic> diagnostics,
        DateOnly generatedAt,
        string? stewardVersion)
    {
        var entries = diagnostics
            .Select(DiagnosticIdentity.Of)
            .Order()
            .Select(BaselineEntry.From)
            .ToList();
        return new BaselineDocument(generatedAt.ToString("yyyy-MM-dd"), stewardVersion, entries);
    }

    public string Serialize()
    {
        var sorted = this with { Entries = Entries.OrderBy(e => e.Identity).ToList() };
        return JsonSerializer.Serialize(sorted, Options).ReplaceLineEndings("\n") + "\n";
    }

    /// <summary>Parses a baseline file, raising <see cref="StewardConfigException"/> naming the file on any defect.</summary>
    public static BaselineDocument Parse(string json, string path)
    {
        BaselineDocument? document;
        try
        {
            document = JsonSerializer.Deserialize<BaselineDocument>(json, Options);
        }
        catch (JsonException ex)
        {
            throw new StewardConfigException($"Failed to parse baseline '{path}': {ex.Message}", path, ex);
        }

        if (document?.Entries == null || string.IsNullOrWhiteSpace(document.GeneratedAt))
            throw new StewardConfigException(
                $"Baseline '{path}' must contain 'generated_at' and an 'entries' array.", path);

        for (var i = 0; i < document.Entries.Count; i++)
        {
            var entry = document.Entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.Rule) || entry.Path == null || string.IsNullOrEmpty(entry.Selector))
                throw new StewardConfigException(
                    $"Baseline '{path}' entry {i} must have non-empty 'rule' and 'selector' and a 'path'.", path);
        }

        return document;
    }
}
