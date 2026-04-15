using Steward.Core.Abstractions;

namespace Steward.Core.Validation.Rules;

/// <summary>
/// STWD-012: Checks that artifacts with freshness declarations have been
/// modified within the declared max_age_days window.
/// Uses filesystem last-write time as the primary source, with optional
/// frontmatter 'last_updated' override.
/// </summary>
public sealed class FreshnessRule : IValidationRule
{
    public string RuleId => "STWD-012";
    public string Category => "freshness";
    public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Warning;
    public string Description => "State documents with freshness declarations should be updated within the declared window.";

    public Task<IReadOnlyList<Diagnostic>> EvaluateAsync(ValidationContext context)
    {
        var diagnostics = new List<Diagnostic>();

        if (context.Policy?.Artifacts is null)
            return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);

        var now = DateTime.UtcNow;

        foreach (var artifact in context.Policy.Artifacts)
        {
            if (artifact.Freshness == null || artifact.Freshness.MaxAgeDays <= 0)
                continue;
            if (string.IsNullOrWhiteSpace(artifact.Path))
                continue;

            var artifactPath = artifact.Path!.Replace('\\', '/');
            var fullPath = Path.Combine(context.RepositoryRoot, artifactPath);

            if (!context.FileSystem.FileExists(fullPath))
                continue;

            // Try frontmatter 'last_updated' override first
            DateTime? lastModified = TryGetFrontmatterDate(context, fullPath);

            // Fall back to filesystem timestamp
            lastModified ??= context.FileSystem.GetLastWriteTimeUtc(fullPath);

            var age = now - lastModified.Value;
            if (age.TotalDays > artifact.Freshness.MaxAgeDays)
            {
                diagnostics.Add(new Diagnostic(
                    RuleId,
                    DefaultSeverity,
                    Category,
                    artifactPath,
                    null,
                    $"File is {(int)age.TotalDays} days old (max: {artifact.Freshness.MaxAgeDays} days).",
                    $"Update the document content and its 'last_updated' frontmatter field.",
                    null));
            }
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }

    private static DateTime? TryGetFrontmatterDate(ValidationContext context, string fullPath)
    {
        try
        {
            var content = context.FileSystem.ReadAllText(fullPath);
            if (!content.StartsWith("---"))
                return null;

            var endIdx = content.IndexOf("---", 3, StringComparison.Ordinal);
            if (endIdx < 0)
                return null;

            var yaml = content[3..endIdx];
            foreach (var line in yaml.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("last_updated:", StringComparison.OrdinalIgnoreCase))
                {
                    var value = trimmed["last_updated:".Length..].Trim().Trim('"', '\'');
                    if (DateTime.TryParse(value, System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.AssumeUniversal |
                        System.Globalization.DateTimeStyles.AdjustToUniversal, out var dt))
                    {
                        return dt;
                    }
                }
            }
        }
        catch
        {
            // If we can't parse frontmatter, fall through
        }

        return null;
    }
}
