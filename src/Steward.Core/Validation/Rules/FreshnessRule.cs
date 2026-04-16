using Steward.Core.Abstractions;
using Steward.Core.Markdown;

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
            int maxAgeDays;
            if (artifact.Freshness != null && artifact.Freshness.MaxAgeDays > 0)
            {
                maxAgeDays = artifact.Freshness.MaxAgeDays;
            }
            else
            {
                // Fall back to role-linked default freshness
                var roleDefault = Configuration.RoleDefaults.GetDefaultFreshnessDays(artifact.Role);
                if (roleDefault is null or <= 0)
                    continue;
                maxAgeDays = roleDefault.Value;
            }

            if (string.IsNullOrWhiteSpace(artifact.Path))
                continue;

            var artifactPath = artifact.Path!.Replace('\\', '/');
            var fullPath = Path.Combine(context.RepositoryRoot, artifactPath);

            if (!context.FileSystem.FileExists(fullPath))
                continue;

            // Try frontmatter 'last_updated' override first
            DateTime? lastModified = FrontmatterEditor.TryGetLastUpdatedDate(context.FileSystem, fullPath);

            // Fall back to filesystem timestamp
            lastModified ??= context.FileSystem.GetLastWriteTimeUtc(fullPath);

            var age = now - lastModified.Value;
            if (age.TotalDays > maxAgeDays)
            {
                diagnostics.Add(new Diagnostic(
                    RuleId,
                    DefaultSeverity,
                    Category,
                    artifactPath,
                    null,
                    $"File is {(int)age.TotalDays} days old (max: {maxAgeDays} days).",
                    $"Update the document content and its 'last_updated' frontmatter field.",
                    null));
            }
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }
}
