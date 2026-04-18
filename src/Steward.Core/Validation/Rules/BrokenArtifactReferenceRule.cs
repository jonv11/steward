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

        // Use AllDiscoveredFiles for existence checks so that scoped validation
        // (--scope changed/staged) does not produce false broken-reference diagnostics.
        var allFiles = context.AllDiscoveredFiles ?? context.TargetFiles;

        var existingPaths = new HashSet<string>(
            allFiles.Select(f => f.RelativePath),
            StringComparer.OrdinalIgnoreCase);

        foreach (var artifact in context.Policy.Artifacts.Where(a => a.Path != null))
        {
            var path = artifact.Path!.TrimEnd('/');
            var isDir = artifact.Path.EndsWith('/');
            var importance = artifact.ResolveImportance();

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

            if (found) continue;

            // Required artifacts are reported as errors by STWD-001 — skip them here.
            if (importance == "required") continue;

            var roleLabel = !string.IsNullOrWhiteSpace(artifact.Role)
                ? $" (role: {artifact.Role})" : "";
            var descriptionLabel = !string.IsNullOrWhiteSpace(artifact.Description)
                ? $" — {artifact.Description}" : "";

            // Optional artifacts that are missing get Info severity (silent previously).
            // Recommended artifacts get Warning severity (same as before).
            var severity = importance == "optional" ? DiagnosticSeverity.Info : DefaultSeverity;

            var importanceNote = importance == "optional"
                ? " This is an optional artifact; consider removing it from policy.yaml if it is no longer needed."
                : " Required artifacts that are missing are reported as errors by STWD-001.";

            diagnostics.Add(new Diagnostic(
                RuleId: RuleId,
                Severity: severity,
                Category: Category,
                Path: artifact.Path,
                Line: null,
                Message: $"Policy artifact '{artifact.Path}'{roleLabel} does not exist.{descriptionLabel}",
                Remediation: $"Create the artifact, remove it from policy.yaml, or update the path.{importanceNote}",
                Source: "policy.yaml"));
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }
}
