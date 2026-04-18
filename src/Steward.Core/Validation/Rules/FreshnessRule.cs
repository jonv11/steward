using Steward.Core;
using Steward.Core.Abstractions;
using Steward.Core.Markdown;

namespace Steward.Core.Validation.Rules;

/// <summary>
/// STWD-012: State documents with freshness declarations should be updated
/// within the declared max_age_days window.
/// Prefers the 'last_updated' frontmatter field over filesystem mtime.
/// Implements IFixableRule to update 'last_updated' to today's date.
/// </summary>
public sealed class FreshnessRule : IValidationRule, IFixableRule
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
                var roleDefault = Configuration.RoleDefaults.GetDefaultFreshnessDays(artifact.Role);
                if (roleDefault is null or <= 0)
                    continue;
                maxAgeDays = roleDefault.Value;
            }

            if (string.IsNullOrWhiteSpace(artifact.Path))
                continue;

            var artifactPath = PathHelper.NormalizeSeparators(artifact.Path!);
            var fullPath = Path.Combine(context.RepositoryRoot, artifactPath);

            if (!context.FileSystem.FileExists(fullPath))
                continue;

            // Prefer frontmatter 'last_updated' over filesystem mtime
            DateTime? lastModified = FrontmatterEditor.TryGetLastUpdatedDate(context.FileSystem, fullPath);
            lastModified ??= context.FileSystem.GetLastWriteTimeUtc(fullPath);

            var age = now - lastModified.Value;

            // Warn if last_updated is set to a future date — it will never trigger freshness
            if (lastModified.Value > now.AddDays(1))
            {
                var futureDateStr = lastModified.Value.ToString("yyyy-MM-dd");
                diagnostics.Add(new Diagnostic(
                    RuleId,
                    DiagnosticSeverity.Warning,
                    Category,
                    artifactPath,
                    null,
                    $"Artifact '{artifact.Path}'{RoleLabel(artifact.Role)} has 'last_updated' set to a future date ({futureDateStr}). " +
                    $"This will prevent freshness checks from ever triggering.",
                    "Correct the 'last_updated' frontmatter field to the actual last-updated date (YYYY-MM-DD).",
                    null));
                continue;
            }

            if (age.TotalDays > maxAgeDays)
            {
                diagnostics.Add(new Diagnostic(
                    RuleId,
                    DefaultSeverity,
                    Category,
                    artifactPath,
                    null,
                    $"Artifact '{artifact.Path}'{RoleLabel(artifact.Role)} is {(int)age.TotalDays} days old (max: {maxAgeDays} days).",
                    $"Review and update the document to reflect current state, then set " +
                    $"'last_updated: {now:yyyy-MM-dd}' in frontmatter. " +
                    $"To update the timestamp only, run 'steward check --fix'.",
                    null));
            }
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }

    public async Task<IReadOnlyList<Fix>> ComputeFixesAsync(ValidationContext context)
    {
        // Evaluate to find stale artifacts, then produce a fix for each that updates last_updated.
        var diagnostics = await EvaluateAsync(context);
        var fixes = new List<Fix>();
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        foreach (var diag in diagnostics)
        {
            // Only fix the staleness diagnostic, not the future-date warning
            if (diag.Path == null || diag.Message.Contains("future date"))
                continue;

            var fullPath = Path.Combine(context.RepositoryRoot, diag.Path);
            if (!context.FileSystem.FileExists(fullPath))
                continue;

            var content = context.FileSystem.ReadAllText(fullPath);
            var doc = context.DocumentCache?.GetOrParse(diag.Path)
                ?? MarkdownParser.Parse(diag.Path, content);

            var result = FrontmatterEditor.SetField(doc, "last_updated", today);
            if (result.HasChanges)
            {
                fixes.Add(new Fix(
                    RuleId,
                    diag.Path,
                    $"Update 'last_updated' to {today} in '{diag.Path}'.",
                    result.NewContent));
            }
        }

        return fixes;
    }

    private static string RoleLabel(string? role) =>
        string.IsNullOrWhiteSpace(role) ? "" : $" (role: {role})";
}
