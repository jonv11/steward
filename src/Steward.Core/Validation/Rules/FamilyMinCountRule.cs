using Steward.Core.Configuration;

namespace Steward.Core.Validation.Rules;

/// <summary>
/// STWD-015: Checks that each artifact family with a <c>directory_expectations.min_count</c>
/// declaration has at least that many matching files in the repository.
/// Uses the full file set (AllDiscoveredFiles) so scoped validation does not produce false positives.
/// </summary>
public sealed class FamilyMinCountRule : IValidationRule
{
    public string RuleId => "STWD-015";
    public string Category => "family-completeness";
    public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Warning;
    public string Description => "Artifact families with directory_expectations.min_count must have at least the declared number of matched files.";

    public Task<IReadOnlyList<Diagnostic>> EvaluateAsync(ValidationContext context)
    {
        var diagnostics = new List<Diagnostic>();

        var families = context.Policy?.ArtifactFamilies;
        if (families == null || families.Count == 0)
            return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);

        var applicableFamilies = families
            .Where(f => f.DirectoryExpectations?.MinCount > 0 && f.Match != null && !string.IsNullOrWhiteSpace(f.Family))
            .ToList();

        if (applicableFamilies.Count == 0)
            return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);

        // Use the full repository file set for this repo-wide obligation check
        var allFiles = context.AllDiscoveredFiles ?? context.TargetFiles;

        var explicitPaths = BuildExplicitPaths(context.Policy);
        var classifier = new ArtifactFamilyClassifier(applicableFamilies);

        // Count matched files per family (path-pattern only — no frontmatter read needed for count)
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in applicableFamilies)
            counts[f.Family!] = 0;

        foreach (var file in allFiles)
        {
            if (file.IsDirectory) continue;
            if (explicitPaths.Contains(PathHelper.NormalizeAndTrim(file.RelativePath))) continue;

            var matched = classifier.Classify(file.RelativePath, frontmatterFields: null);
            if (matched?.Family != null && counts.ContainsKey(matched.Family))
                counts[matched.Family]++;
        }

        foreach (var family in applicableFamilies)
        {
            var minCount = family.DirectoryExpectations!.MinCount;
            var actual = counts[family.Family!];

            if (actual < minCount)
            {
                var label = family.DisplayName != null
                    ? $"{family.Family} ({family.DisplayName})"
                    : family.Family!;

                var description = !string.IsNullOrWhiteSpace(family.DirectoryExpectations.Description)
                    ? $" {family.DirectoryExpectations.Description}"
                    : string.Empty;

                diagnostics.Add(new Diagnostic(
                    RuleId,
                    DefaultSeverity,
                    Category,
                    null,
                    null,
                    $"Artifact family '{label}' has {actual} matched file(s) but requires at least {minCount}.{description}",
                    $"Add at least {minCount - actual} more file(s) matching the '{family.Family}' family pattern.",
                    "policy.yaml"));
            }
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
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
