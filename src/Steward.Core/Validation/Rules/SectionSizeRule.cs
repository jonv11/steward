using Steward.Core.Markdown;
using Steward.Core.Validation;

namespace Steward.Core.Validation.Rules;

/// <summary>
/// STWD-004: Reports sections that exceed the configured line count threshold.
/// </summary>
public sealed class SectionSizeRule : IValidationRule
{
    public string RuleId => "STWD-004";
    public string Category => "governance";
    public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Info;
    public string Description => "Sections exceeding the size threshold should be split.";

    public Task<IReadOnlyList<Diagnostic>> EvaluateAsync(ValidationContext context)
    {
        var diagnostics = new List<Diagnostic>();
        var threshold = context.Policy?.Governance?.SectionSizeWarningThreshold ?? 500;

        foreach (var file in context.TargetFiles.Where(f =>
            f.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var doc = context.DocumentCache?.GetOrParse(file.RelativePath)
                    ?? MarkdownParser.Parse(file.RelativePath,
                        context.FileSystem.ReadAllText(Path.Combine(context.RepositoryRoot, file.RelativePath)));

                CheckSections(doc.Sections, file.RelativePath, threshold, diagnostics);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                diagnostics.Add(new Diagnostic(
                    RuleId: RuleId,
                    Severity: DiagnosticSeverity.Warning,
                    Category: Category,
                    Path: file.RelativePath,
                    Line: null,
                    Message: $"Could not read file '{file.RelativePath}': {ex.Message}",
                    Remediation: "Check file permissions and encoding.",
                    Source: null));
            }
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }

    private void CheckSections(IReadOnlyList<Section> sections, string filePath,
        int threshold, List<Diagnostic> diagnostics)
    {
        foreach (var section in sections)
        {
            if (section.LineCount > threshold)
            {
                diagnostics.Add(new Diagnostic(
                    RuleId: RuleId,
                    Severity: DefaultSeverity,
                    Category: Category,
                    Path: filePath,
                    Line: section.Range.Start,
                    Message: $"Section '{section.Heading}' has {section.LineCount} lines (threshold: {threshold}).",
                    Remediation: "Consider splitting this section into smaller subsections.",
                    Source: "policy.yaml"));
            }

            CheckSections(section.Children, filePath, threshold, diagnostics);
        }
    }
}
