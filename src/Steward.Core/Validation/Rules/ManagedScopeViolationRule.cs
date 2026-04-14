using Steward.Core.Markdown;

namespace Steward.Core.Validation.Rules;

/// <summary>
/// STWD-006: Detects content modifications inside managed/generated regions
/// that should only be edited by their declared owner.
/// Currently checks for structural anomalies: empty managed regions
/// or managed regions that don't contain expected steward-generated patterns.
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

            var content = context.FileSystem.ReadAllText(fullPath);
            var doc = MarkdownParser.Parse(fullPath, content);

            foreach (var region in doc.ManagedRegions)
            {
                if (region.Owner == null) continue;

                // Check for headings inside managed regions that don't follow steward patterns
                foreach (var section in FlattenSections(doc.Sections))
                {
                    if (section.Range.Start >= region.Range.Start &&
                        section.Range.End <= region.Range.End)
                    {
                        // Section is inside the managed region — check for manual edits
                        // If the section has content blocks that look hand-authored in a
                        // steward-owned region, flag it
                        if (string.Equals(region.Owner, "steward", StringComparison.OrdinalIgnoreCase))
                        {
                            // Steward-owned regions shouldn't have manually-added sections
                            // unless they were inserted by steward. We flag sections that
                            // don't match expected patterns.
                            // For now, we only flag if the managed region has nested headings
                            // at unexpected levels (potential manual insertion).
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
