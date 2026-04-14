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

        foreach (var artifact in context.Policy.Artifacts.Where(a => a.Required && a.Path != null))
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

            if (!found)
            {
                diagnostics.Add(new Diagnostic(
                    RuleId: RuleId,
                    Severity: DefaultSeverity,
                    Category: Category,
                    Path: artifact.Path,
                    Line: null,
                    Message: $"Required artifact '{artifact.Path}' is missing.",
                    Remediation: $"Create the file '{artifact.Path}' as specified in the repository policy.",
                    Source: "policy.yaml"));
            }
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }
}
