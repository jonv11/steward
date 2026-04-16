using Steward.Core.Maintenance;

namespace Steward.Core.Validation.Rules;

/// <summary>
/// STWD-007: Detects maintained artifacts that are stale (differ from what maintenance would produce).
/// Implements IFixableRule to support 'steward check --fix' for stale artifacts.
/// </summary>
public sealed class StaleArtifactRule : IValidationRule, IFixableRule
{
    public string RuleId => "STWD-007";
    public string Category => "stale-artifact";
    public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Warning;
    public string Description => "Maintained artifacts should match what 'steward maintain' would produce.";

    public Task<IReadOnlyList<Diagnostic>> EvaluateAsync(ValidationContext context)
    {
        var diagnostics = new List<Diagnostic>();
        var plan = EvaluatePlan(context);
        if (plan == null)
            return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);

        foreach (var action in plan.Actions.Where(a => a.HasChanges))
        {
            diagnostics.Add(new Diagnostic(
                RuleId,
                DefaultSeverity,
                Category,
                action.ArtifactPath,
                null,
                $"Maintained artifact '{action.ArtifactId}' is stale. {action.Description}",
                "Run 'steward maintain --apply' or 'steward check --fix' to update.",
                null));
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }

    public Task<IReadOnlyList<Fix>> ComputeFixesAsync(ValidationContext context)
    {
        var fixes = new List<Fix>();
        var plan = EvaluatePlan(context);
        if (plan == null)
            return Task.FromResult<IReadOnlyList<Fix>>(fixes);

        foreach (var action in plan.Actions.Where(a => a.HasChanges && a.ExpectedContent != null))
        {
            fixes.Add(new Fix(
                RuleId,
                action.ArtifactPath,
                $"Update stale artifact '{action.ArtifactId}'",
                action.ExpectedContent!));
        }

        return Task.FromResult<IReadOnlyList<Fix>>(fixes);
    }

    private static MaintenancePlan? EvaluatePlan(ValidationContext context)
    {
        if (context.Policy?.Maintenance?.Artifacts == null)
            return null;

        // Use AllDiscoveredFiles so that maintenance evaluation sees the full repo
        // even during scoped validation (--scope changed/staged).
        var maintenanceContext = new MaintenanceContext
        {
            RepositoryRoot = context.RepositoryRoot,
            FileSystem = context.FileSystem,
            Files = context.AllDiscoveredFiles ?? context.TargetFiles
        };

        var engine = new MaintenanceEngine();
        return engine.Evaluate(context.Policy, maintenanceContext);
    }
}
