using Steward.Core.Markdown;

namespace Steward.Core.Validation.Rules;

/// <summary>
/// STWD-006: Detects structural anomalies in managed regions — specifically,
/// regions whose begin/end markers are present but contain no content.
/// An empty managed region usually means 'steward maintain --apply' has not
/// been run since the region was declared, or that the maintenance source
/// produced no output. This is a proxy signal; STWD-007 detects stale content
/// once a region has been populated.
/// </summary>
public sealed class ManagedScopeViolationRule : IValidationRule
{
    public string RuleId => "STWD-006";
    public string Category => "managed-region";
    public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Warning;
    public string Description => "Managed regions should not be empty. An empty managed region indicates 'steward maintain --apply' has not been run.";

    public Task<IReadOnlyList<Diagnostic>> EvaluateAsync(ValidationContext context)
    {
        var diagnostics = new List<Diagnostic>();

        foreach (var file in context.TargetFiles)
        {
            if (!file.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                continue;

            var fullPath = Path.Combine(context.RepositoryRoot, file.RelativePath);
            if (!context.FileSystem.FileExists(fullPath))
                continue;

            var doc = context.DocumentCache?.GetOrParse(file.RelativePath)
                ?? MarkdownParser.Parse(fullPath, context.FileSystem.ReadAllText(fullPath));

            foreach (var region in doc.ManagedRegions)
            {
                if (region.Owner == null) continue;

                // An empty managed region (markers present but no content) is a structural anomaly.
                // STWD-007 handles stale content in populated regions.
                if (region.Range.End - region.Range.Start <= 1)
                {
                    diagnostics.Add(new Diagnostic(
                        RuleId: RuleId,
                        Severity: DefaultSeverity,
                        Category: Category,
                        Path: file.RelativePath,
                        Line: region.Range.Start,
                        Message: $"Managed region '{region.Id}' in '{file.RelativePath}' (owner: '{region.Owner}') is empty. " +
                                 $"Run 'steward maintain --apply' to populate it.",
                        Remediation: "Run 'steward maintain --apply' to generate content for this region, " +
                                     "or remove the managed region markers if the region is no longer needed.",
                        Source: file.RelativePath));
                }
            }
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }
}
