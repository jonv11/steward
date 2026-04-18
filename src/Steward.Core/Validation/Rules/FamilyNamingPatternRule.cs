using System.Text.RegularExpressions;
using Steward.Core.Configuration;

namespace Steward.Core.Validation.Rules;

/// <summary>
/// STWD-016: Enforces <c>naming_pattern</c> regex declared on artifact families.
/// The regex is matched against the filename (not the full path). Case-insensitive.
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

        var compiled = CompileFamilies(families);
        if (compiled.Count == 0)
            return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);

        var explicitPaths = BuildExplicitPaths(context.Policy);
        var classifier = new ArtifactFamilyClassifier(compiled.Select(c => c.Definition).ToList());

        foreach (var file in context.TargetFiles)
        {
            if (file.IsDirectory) continue;
            if (explicitPaths.Contains(PathHelper.NormalizeAndTrim(file.RelativePath))) continue;

            var matched = classifier.Classify(file.RelativePath, frontmatterFields: null);
            if (matched == null) continue;

            var rule = compiled.FirstOrDefault(c => string.Equals(c.Definition.Family, matched.Family, StringComparison.OrdinalIgnoreCase));
            if (rule == null) continue;

            var fileName = Path.GetFileName(file.RelativePath);
            if (!rule.Pattern.IsMatch(fileName))
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

    private static List<CompiledFamilyNaming> CompileFamilies(IReadOnlyList<ArtifactFamilyDefinition> families)
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
            catch (RegexParseException)
            {
                // Config validate should catch invalid patterns; skip silently here
            }
        }
        return result;
    }

    private static HashSet<string> BuildExplicitPaths(RepositoryPolicy? policy)
    {
        if (policy?.Artifacts == null)
            return [];

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in policy.Artifacts)
        {
            if (!string.IsNullOrWhiteSpace(a.Path))
                set.Add(PathHelper.NormalizeAndTrim(a.Path!));
        }
        return set;
    }

    private sealed record CompiledFamilyNaming(ArtifactFamilyDefinition Definition, Regex Pattern, string RawPattern);
}
