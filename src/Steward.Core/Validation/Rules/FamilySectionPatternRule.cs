using System.Text.RegularExpressions;
using Steward.Core.Configuration;
using Steward.Core.Markdown;

namespace Steward.Core.Validation.Rules;

/// <summary>
/// STWD-020: Enforces <c>section_pattern</c> regex declared on artifact families.
/// The regex is matched case-sensitively against every H2 heading text in matched files.
/// One diagnostic is emitted per violating H2, including its line number.
/// Skips files with no H2 headings — absent H2s are outside this rule's scope.
/// Not auto-fixable; section heading text requires human judgment to correct.
/// </summary>
public sealed class FamilySectionPatternRule : IValidationRule
{
    public string RuleId => "STWD-020";
    public string Category => "naming";
    public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Warning;
    public string Description => "Files matched by an artifact family must satisfy the family's section_pattern applied to all H2 headings.";

    public Task<IReadOnlyList<Diagnostic>> EvaluateAsync(ValidationContext context)
    {
        var diagnostics = new List<Diagnostic>();

        var families = context.Policy?.ArtifactFamilies;
        if (families == null || families.Count == 0)
            return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);

        var compiled = CompileFamilies(families, diagnostics);
        if (compiled.Count == 0)
            return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);

        var classifier = new ArtifactFamilyClassifier(compiled.Select(c => c.Definition).ToList());

        foreach (var file in context.TargetFiles)
        {
            if (file.IsDirectory) continue;
            if (!file.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) continue;

            var matched = classifier.ClassifyFile(file.RelativePath, context.FileSystem, context.RepositoryRoot);
            if (matched == null) continue;

            var rule = compiled.FirstOrDefault(c =>
                string.Equals(c.Definition.Family, matched.Family, StringComparison.OrdinalIgnoreCase));
            if (rule == null) continue;

            StructuredDocument? doc = null;
            try
            {
                doc = context.DocumentCache?.GetOrParse(file.RelativePath)
                    ?? MarkdownParser.Parse(file.RelativePath,
                        context.FileSystem.ReadAllText(
                            Path.Combine(context.RepositoryRoot, file.RelativePath)));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            // Flatten all sections and filter to H2. Per RFC-015, if no H2s are present the rule does not fire.
            var h2Sections = MarkdownHeadings.Flatten(doc.Sections)
                .Where(s => s.Level == 2)
                .ToList();
            if (h2Sections.Count == 0) continue;

            foreach (var section in h2Sections)
            {
                bool isMatch;
                try
                {
                    isMatch = rule.Pattern.IsMatch(section.Heading);
                }
                catch (RegexMatchTimeoutException)
                {
                    diagnostics.Add(new Diagnostic(
                        RuleId,
                        DiagnosticSeverity.Warning,
                        "config-error",
                        file.RelativePath,
                        section.Range.Start,
                        $"Family section_pattern regex '{rule.RawPattern}' timed out evaluating an H2 heading in '{file.RelativePath}'. This may indicate catastrophic backtracking.",
                        "Simplify the section_pattern regex in policy.yaml to avoid excessive backtracking.",
                        "policy.yaml"));
                    break;
                }

                if (!isMatch)
                {
                    diagnostics.Add(new Diagnostic(
                        RuleId,
                        DefaultSeverity,
                        Category,
                        file.RelativePath,
                        section.Range.Start,
                        $"H2 heading '{section.Heading}' (line {section.Range.Start}) does not match the section_pattern '{rule.RawPattern}' required for family '{matched.Family}'.",
                        $"Update the heading to match the pattern: {rule.RawPattern}",
                        "policy.yaml",
                        new Dictionary<string, object> { ["expectedPattern"] = rule.RawPattern }));
                }
            }
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }

    private static List<CompiledFamilySection> CompileFamilies(
        IReadOnlyList<ArtifactFamilyDefinition> families,
        List<Diagnostic> diagnostics)
    {
        var result = new List<CompiledFamilySection>();
        foreach (var family in families)
        {
            if (string.IsNullOrWhiteSpace(family.SectionPattern) || family.Match == null)
                continue;

            try
            {
                // Case-sensitive per RFC-015 — no RegexOptions.IgnoreCase
                var regex = new Regex(
                    family.SectionPattern,
                    RegexOptions.Compiled | RegexOptions.CultureInvariant,
                    TimeSpan.FromSeconds(1));
                result.Add(new CompiledFamilySection(family, regex, family.SectionPattern));
            }
            catch (RegexParseException ex)
            {
                diagnostics.Add(new Diagnostic(
                    RuleId: "STWD-020",
                    Severity: DiagnosticSeverity.Warning,
                    Category: "config-error",
                    Path: null,
                    Line: null,
                    Message: $"policy.yaml: section_pattern '{family.SectionPattern}' for family '{family.Family}' is an invalid regex — section enforcement is disabled for this family.",
                    Remediation: $"Fix or remove the section_pattern regex in policy.yaml. Parse error: {ex.Message}",
                    Source: "policy.yaml"));
            }
        }
        return result;
    }

    private sealed record CompiledFamilySection(ArtifactFamilyDefinition Definition, Regex Pattern, string RawPattern);
}
