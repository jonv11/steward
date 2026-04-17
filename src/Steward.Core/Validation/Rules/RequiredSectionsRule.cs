using Steward.Core.Configuration;
using Steward.Core.Markdown;

namespace Steward.Core.Validation.Rules;

/// <summary>
/// STWD-014: Checks that Markdown files matched by an artifact family contain all required headings
/// declared in that family's <c>required_sections</c> list. Heading matching is case-insensitive
/// and checks all headings in the document (any level, flattened).
/// </summary>
public sealed class RequiredSectionsRule : IValidationRule
{
    public string RuleId => "STWD-014";
    public string Category => "structure";
    public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Warning;
    public string Description => "Files in an artifact family must contain all required section headings declared for that family.";

    public Task<IReadOnlyList<Diagnostic>> EvaluateAsync(ValidationContext context)
    {
        var diagnostics = new List<Diagnostic>();

        var families = context.Policy?.ArtifactFamilies;
        if (families == null || families.Count == 0)
            return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);

        // Only families with required_sections need evaluation
        var applicableFamilies = families
            .Where(f => f.RequiredSections is { Count: > 0 } && f.Match != null && !string.IsNullOrWhiteSpace(f.Family))
            .ToList();

        if (applicableFamilies.Count == 0)
            return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);

        var classifier = new ArtifactFamilyClassifier(applicableFamilies);

        // Build set of explicit artifact paths — families don't apply to these
        var explicitPaths = BuildExplicitPaths(context.Policy);

        foreach (var file in context.TargetFiles)
        {
            if (!file.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                continue;

            if (explicitPaths.Contains(PathHelper.NormalizeAndTrim(file.RelativePath)))
                continue;

            StructuredDocument? doc = null;
            try
            {
                doc = context.DocumentCache?.GetOrParse(file.RelativePath)
                    ?? MarkdownParser.Parse(file.RelativePath,
                        context.FileSystem.ReadAllText(Path.Combine(context.RepositoryRoot, file.RelativePath)));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            // Classify against families that have required_sections
            IReadOnlyDictionary<string, object?>? frontmatterFields = doc.Frontmatter?.Fields != null
                ? doc.Frontmatter.Fields.ToDictionary(kv => kv.Key, kv => (object?)kv.Value, StringComparer.OrdinalIgnoreCase)
                : null;

            var matchedFamily = classifier.Classify(file.RelativePath, frontmatterFields);
            if (matchedFamily?.RequiredSections == null)
                continue;

            var presentHeadings = CollectAllHeadings(doc.Sections);

            foreach (var required in matchedFamily.RequiredSections)
            {
                if (!presentHeadings.Any(h => string.Equals(h, required, StringComparison.OrdinalIgnoreCase)))
                {
                    diagnostics.Add(new Diagnostic(
                        RuleId,
                        DefaultSeverity,
                        Category,
                        file.RelativePath,
                        null,
                        $"Required section '{required}' is missing in '{file.RelativePath}' [family: {matchedFamily.Family}].",
                        $"Add a heading '## {required}' (or equivalent level) to the document.",
                        "policy.yaml"));
                }
            }
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }

    private static HashSet<string> CollectAllHeadings(IReadOnlyList<Section> sections)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectRecursive(sections, result);
        return result;
    }

    private static void CollectRecursive(IReadOnlyList<Section> sections, HashSet<string> result)
    {
        foreach (var section in sections)
        {
            result.Add(section.Heading);
            CollectRecursive(section.Children, result);
        }
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
}
