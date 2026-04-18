using System.Text.RegularExpressions;
using Steward.Core.Configuration;

namespace Steward.Core.Validation.Rules;

/// <summary>
/// STWD-016: Enforces <c>naming_pattern</c> regex declared on artifact families.
/// The regex is matched against the filename (not the full path). Case-insensitive.
/// Emits a Warning diagnostic when a naming_pattern regex is invalid so the user
/// knows enforcement is silently disabled for that family.
/// </summary>
public sealed class FamilyNamingPatternRule : IValidationRule
{
    public string RuleId => "STWD-016";
    public string Category => "naming";
    public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Warning;
    public string Description => "Files matched by an artifact family must satisfy the family's naming_pattern.";

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

            var matched = classifier.ClassifyFile(file.RelativePath, context.FileSystem, context.RepositoryRoot);
            if (matched == null) continue;

            var rule = compiled.FirstOrDefault(c => string.Equals(c.Definition.Family, matched.Family, StringComparison.OrdinalIgnoreCase));
            if (rule == null) continue;

            var fileName = Path.GetFileName(file.RelativePath);
            bool isMatch;
            try
            {
                isMatch = rule.Pattern.IsMatch(fileName);
            }
            catch (RegexMatchTimeoutException)
            {
                diagnostics.Add(new Diagnostic(
                    RuleId,
                    DiagnosticSeverity.Warning,
                    "config-error",
                    file.RelativePath,
                    null,
                    $"Family naming_pattern regex '{rule.RawPattern}' timed out evaluating '{fileName}'. This may indicate catastrophic backtracking.",
                    "Simplify the naming_pattern regex in policy.yaml to avoid excessive backtracking.",
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
                    $"File '{fileName}' does not match the naming pattern '{rule.RawPattern}' required for family '{matched.Family}'.",
                    $"Rename the file to match the pattern: {rule.RawPattern}",
                    "policy.yaml",
                    new Dictionary<string, object> { ["expectedPattern"] = rule.RawPattern }));
            }
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }

    private static List<CompiledFamilyNaming> CompileFamilies(IReadOnlyList<ArtifactFamilyDefinition> families, List<Diagnostic> diagnostics)
    {
        var result = new List<CompiledFamilyNaming>();
        foreach (var family in families)
        {
            if (string.IsNullOrWhiteSpace(family.NamingPattern) || family.Match == null)
                continue;

            try
            {
                var regex = new Regex(
                    family.NamingPattern,
                    RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
                    TimeSpan.FromSeconds(1));
                result.Add(new CompiledFamilyNaming(family, regex, family.NamingPattern));
            }
            catch (RegexParseException ex)
            {
                // Surface invalid regex as a warning so the user knows enforcement is disabled.
                diagnostics.Add(new Diagnostic(
                    RuleId: "STWD-016",
                    Severity: DiagnosticSeverity.Warning,
                    Category: "config-error",
                    Path: null,
                    Line: null,
                    Message: $"policy.yaml: naming_pattern '{family.NamingPattern}' for family '{family.Family}' is an invalid regex — naming enforcement is disabled for this family.",
                    Remediation: $"Fix or remove the naming_pattern regex in policy.yaml. Parse error: {ex.Message}",
                    Source: "policy.yaml"));
            }
        }
        return result;
    }
    private sealed record CompiledFamilyNaming(ArtifactFamilyDefinition Definition, Regex Pattern, string RawPattern);
}
