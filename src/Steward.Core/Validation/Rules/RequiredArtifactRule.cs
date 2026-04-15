namespace Steward.Core.Validation.Rules;

public sealed class RequiredArtifactRule : IValidationRule
{
    public string RuleId => "STWD-001";
    public string Category => "path-policy";
    public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Error;
    public string Description => "Required artifacts defined in policy must exist in the repository.";

    public Task<IReadOnlyList<Diagnostic>> EvaluateAsync(ValidationContext context)
    {
        var diagnostics = new List<Diagnostic>();

        if (context.Policy?.Artifacts == null)
            return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);

        var existingPaths = new HashSet<string>(
            context.TargetFiles.Select(f => f.RelativePath),
            StringComparer.OrdinalIgnoreCase);

        foreach (var artifact in context.Policy.Artifacts.Where(a => a.Path != null))
        {
            var importance = ResolveImportance(artifact);
            if (importance == "optional")
                continue;

            var severity = importance == "recommended"
                ? DiagnosticSeverity.Warning
                : DiagnosticSeverity.Error;

            var path = artifact.Path!.TrimEnd('/');
            var isDir = artifact.Path.EndsWith('/');

            bool found;
            if (isDir)
            {
                found = context.TargetFiles.Any(f => f.IsDirectory &&
                    f.RelativePath.Equals(path, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                found = existingPaths.Contains(path);
            }

            if (!found)
            {
                diagnostics.Add(new Diagnostic(
                    RuleId: RuleId,
                    Severity: severity,
                    Category: Category,
                    Path: artifact.Path,
                    Line: null,
                    Message: $"{(importance == "recommended" ? "Recommended" : "Required")} artifact '{artifact.Path}' is missing.",
                    Remediation: $"Create the file '{artifact.Path}' as specified in the repository policy.",
                    Source: "policy.yaml"));
            }
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }

    internal static string ResolveImportance(Configuration.ArtifactDefinition artifact)
    {
        if (!string.IsNullOrEmpty(artifact.Importance))
            return artifact.Importance.ToLowerInvariant();

        return artifact.Required ? "required" : "optional";
    }
}
