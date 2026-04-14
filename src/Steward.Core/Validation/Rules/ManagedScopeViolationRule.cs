using Steward.Core.Markdown;

namespace Steward.Core.Validation.Rules;

/// <summary>
/// STWD-006: Detects content modifications inside managed/generated regions
/// that should only be edited by their declared owner.
/// Checks for structural anomalies: headings inside managed regions that
/// were not placed there by the declared owner.
/// </summary>
public sealed class ManagedScopeViolationRule : IValidationRule
{
    public string RuleId => "STWD-006";
    public string Category => "ownership";
    public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Warning;
    public string Description => "Content in managed regions must only be modified by the declared owner.";

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

                // Check for empty managed regions (markers exist but no content between them)
                if (region.Range.End - region.Range.Start <= 1)
                {
                    diagnostics.Add(new Diagnostic(
                        RuleId: RuleId,
                        Severity: DefaultSeverity,
                        Category: Category,
                        Path: file.RelativePath,
                        Line: region.Range.Start,
                        Message: $"Managed region '{region.Id}' (owner: '{region.Owner}') is empty.",
                        Remediation: $"Run 'steward maintain' to regenerate content for this region.",
                        Source: null));
                    continue;
                }

                // Check for headings inside steward-owned managed regions
                // These indicate manual insertion into a machine-managed section
                if (string.Equals(region.Owner, "steward", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var section in FlattenSections(doc.Sections))
                    {
                        if (section.Range.Start > region.Range.Start &&
                            section.Range.Start < region.Range.End)
                        {
                            diagnostics.Add(new Diagnostic(
                                RuleId: RuleId,
                                Severity: DefaultSeverity,
                                Category: Category,
                                Path: file.RelativePath,
                                Line: section.Range.Start,
                                Message: $"Heading '{section.Heading}' inside managed region '{region.Id}' " +
                                         $"(owner: '{region.Owner}') may have been manually inserted.",
                                Remediation: "Avoid manually editing content inside steward-managed regions. " +
                                             "Run 'steward maintain --apply' to regenerate.",
                                Source: null));
                        }
                    }
                }
            }
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }

    private static IEnumerable<Section> FlattenSections(IReadOnlyList<Section> sections)
    {
        foreach (var section in sections)
        {
            yield return section;
            foreach (var child in FlattenSections(section.Children))
            {
                yield return child;
            }
        }
    }
}
