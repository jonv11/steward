namespace Steward.Core.Validation;

public sealed class ValidationEngine
{
    private readonly List<IValidationRule> _rules;

    public ValidationEngine(IEnumerable<IValidationRule> rules)
    {
        _rules = rules.ToList();
    }

    public async Task<ValidationResult> ValidateAsync(ValidationContext context)
    {
        var diagnostics = new List<Diagnostic>();
        var disabledRules = new HashSet<string>(
            context.Policy?.Validation?.DisabledRules ?? [],
            StringComparer.OrdinalIgnoreCase);

        foreach (var rule in _rules)
        {
            if (disabledRules.Contains(rule.RuleId))
                continue;

            context.CancellationToken.ThrowIfCancellationRequested();

            var results = await rule.EvaluateAsync(context);
            diagnostics.AddRange(results);
        }

        var errors = diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error);
        var warnings = diagnostics.Count(d => d.Severity == DiagnosticSeverity.Warning);
        var infos = diagnostics.Count(d => d.Severity == DiagnosticSeverity.Info);

        return new ValidationResult
        {
            Summary = new ValidationSummary
            {
                Scope = "full",
                FilesChecked = context.TargetFiles.Count,
                Errors = errors,
                Warnings = warnings,
                Infos = infos,
                Pass = errors == 0
            },
            Diagnostics = diagnostics
        };
    }
}
