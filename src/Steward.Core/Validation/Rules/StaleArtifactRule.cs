using Steward.Core.Maintenance;

namespace Steward.Core.Validation.Rules;

/// <summary>
/// STWD-007: Detects maintained artifacts that are stale (differ from what maintenance would produce).
/// </summary>
public sealed class StaleArtifactRule : IValidationRule
{
    public string RuleId => "STWD-007";
    public string Category => "stale-artifact";
    public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Warning;
    public string Description => "Maintained artifacts should match what 'steward maintain' would produce.";

    public Task<IReadOnlyList<Diagnostic>> EvaluateAsync(ValidationContext context)
    {
        var diagnostics = new List<Diagnostic>();

        if (context.Policy?.Maintenance?.Artifacts == null)
            return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);

        var maintenanceContext = new MaintenanceContext
        {
            RepositoryRoot = context.RepositoryRoot,
            FileSystem = context.FileSystem,
            Files = context.TargetFiles
        };

        var engine = new MaintenanceEngine();
        var plan = engine.Evaluate(context.Policy, maintenanceContext);

        foreach (var action in plan.Actions.Where(a => a.HasChanges))
        {
            diagnostics.Add(new Diagnostic(
                RuleId,
                DefaultSeverity,
                Category,
                action.ArtifactPath,
                null,
                $"Maintained artifact '{action.ArtifactId}' is stale. {action.Description}",
                "Run 'steward maintain --apply' to update.",
                null));
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }
}
