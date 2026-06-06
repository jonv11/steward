using System.Text.RegularExpressions;
using Steward.Core.Configuration;
using Steward.Core.Markdown;

namespace Steward.Core.Validation.Rules;

/// <summary>
/// STWD-019: Enforces <c>title_pattern</c> regex declared on artifact families.
/// The regex is matched case-sensitively against the H1 heading text of matched files.
/// Skips files with no H1 heading — absent H1 is outside this rule's scope.
/// Not auto-fixable; H1 text requires human judgment to correct.
/// </summary>
public sealed class FamilyTitlePatternRule : IValidationRule
{
    public string RuleId => "STWD-019";
    public string Category => "naming";
    public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Warning;
    public string Description => "Files matched by an artifact family must satisfy the family's title_pattern applied to the H1 heading.";

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

            // Find the H1 heading. Per RFC-014, if no H1 is present the rule does not fire.
            var h1 = doc.Sections.FirstOrDefault(s => s.Level == 1)?.Heading;
            if (h1 == null) continue;

            bool isMatch;
            try
            {
                isMatch = rule.Pattern.IsMatch(h1);
            }
            catch (RegexMatchTimeoutException)
            {
                diagnostics.Add(new Diagnostic(
                    RuleId,
                    DiagnosticSeverity.Warning,
                    "config-error",
                    file.RelativePath,
                    null,
                    $"Family title_pattern regex '{rule.RawPattern}' timed out evaluating the H1 heading in '{file.RelativePath}'. This may indicate catastrophic backtracking.",
                    "Simplify the title_pattern regex in policy.yaml to avoid excessive backtracking.",
                    "policy.yaml"));
                continue;
            }

            if (!isMatch)
            {
                diagnostics.Add(new Diagnostic(
                    RuleId,
                    DefaultSeverity,
                    Category,
                    file.RelativePath,
                    null,
                    $"H1 heading '{h1}' does not match the title_pattern '{rule.RawPattern}' required for family '{matched.Family}'.",
                    $"Update the H1 heading to match the pattern: {rule.RawPattern}",
                    "policy.yaml",
                    new Dictionary<string, object> { ["expectedPattern"] = rule.RawPattern }));
            }
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }

    private static List<CompiledFamilyTitle> CompileFamilies(
        IReadOnlyList<ArtifactFamilyDefinition> families,
        List<Diagnostic> diagnostics)
    {
        var result = new List<CompiledFamilyTitle>();
        foreach (var family in families)
        {
            if (string.IsNullOrWhiteSpace(family.TitlePattern) || family.Match == null)
                continue;

            try
            {
                // Case-sensitive per RFC-014 — no RegexOptions.IgnoreCase
                var regex = new Regex(
                    family.TitlePattern,
                    RegexOptions.Compiled | RegexOptions.CultureInvariant,
                    TimeSpan.FromSeconds(1));
                result.Add(new CompiledFamilyTitle(family, regex, family.TitlePattern));
            }
            catch (RegexParseException ex)
            {
                diagnostics.Add(new Diagnostic(
                    RuleId: "STWD-019",
                    Severity: DiagnosticSeverity.Warning,
                    Category: "config-error",
                    Path: null,
                    Line: null,
                    Message: $"policy.yaml: title_pattern '{family.TitlePattern}' for family '{family.Family}' is an invalid regex — title enforcement is disabled for this family.",
                    Remediation: $"Fix or remove the title_pattern regex in policy.yaml. Parse error: {ex.Message}",
                    Source: "policy.yaml"));
            }
        }
        return result;
    }

    private sealed record CompiledFamilyTitle(ArtifactFamilyDefinition Definition, Regex Pattern, string RawPattern);
}
