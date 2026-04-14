namespace Steward.Core.Validation.Rules;

/// <summary>
/// STWD-009: Policy-declared artifact paths that do not resolve to existing files.
/// Checks the 'artifacts' list in policy.yaml — regardless of whether required —
/// to detect references to paths that no longer exist.
/// </summary>
public sealed class BrokenArtifactReferenceRule : IValidationRule
{
    public string RuleId => "STWD-009";
    public string Category => "broken-reference";
    public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Warning;
    public string Description => "Policy-declared artifact paths should resolve to existing files.";

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

            // Required artifacts are already reported by STWD-001 as errors.
            // This rule only fires for non-required artifacts that don't resolve.
            if (!found && !artifact.Required)
            {
                diagnostics.Add(new Diagnostic(
                    RuleId: RuleId,
                    Severity: DefaultSeverity,
                    Category: Category,
                    Path: artifact.Path,
                    Line: null,
                    Message: $"Policy artifact '{artifact.Path}' (role: {artifact.Role ?? "unspecified"}) does not exist.",
                    Remediation: "Create the artifact, remove it from policy.yaml, or mark it as required if it is mandatory.",
                    Source: "policy.yaml"));
            }
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }
}
