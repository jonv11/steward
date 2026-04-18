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

        // Use AllDiscoveredFiles for existence checks so that scoped validation
        // (--scope changed/staged) does not produce false missing-artifact diagnostics.
        var allFiles = context.AllDiscoveredFiles ?? context.TargetFiles;

        var existingPaths = new HashSet<string>(
            allFiles.Select(f => f.RelativePath),
            StringComparer.OrdinalIgnoreCase);

        foreach (var artifact in context.Policy.Artifacts.Where(a => a.Path != null))
        {
            var importance = artifact.ResolveImportance();
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
                found = allFiles.Any(f => f.IsDirectory &&
                    f.RelativePath.Equals(path, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                found = existingPaths.Contains(path);
            }

            if (!found)
            {
                var roleLabel = !string.IsNullOrWhiteSpace(artifact.Role) ? $" (role: {artifact.Role})" : "";
                var descriptionLabel = !string.IsNullOrWhiteSpace(artifact.Description)
                    ? $" — {artifact.Description}" : "";
                diagnostics.Add(new Diagnostic(
                    RuleId: RuleId,
                    Severity: severity,
                    Category: Category,
                    Path: artifact.Path,
                    Line: null,
                    Message: $"{(importance == "recommended" ? "Recommended" : "Required")} artifact '{artifact.Path}'{roleLabel} is missing.{descriptionLabel}",
                    Remediation: $"Create the file '{artifact.Path}' as specified in the repository policy.",
                    Source: "policy.yaml"));
            }
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }
}
