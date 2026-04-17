using DotNet.Globbing;
using Steward.Core.Configuration;

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

        var pathOverrides = CompilePathOverrides(context.Policy?.Validation?.PathOverrides);

        foreach (var rule in _rules)
        {
            if (disabledRules.Contains(rule.RuleId))
                continue;

            context.CancellationToken.ThrowIfCancellationRequested();

            var results = await rule.EvaluateAsync(context);

            // Filter out diagnostics suppressed by per-path overrides
            foreach (var diag in results)
            {
                if (diag.Path != null && IsPathSuppressed(diag.Path, diag.RuleId, pathOverrides))
                    continue;
                diagnostics.Add(diag);
            }
        }

        var severityOverrides = context.Policy?.Validation?.SeverityOverrides;
        if (severityOverrides != null && severityOverrides.Count > 0)
        {
            for (var i = 0; i < diagnostics.Count; i++)
            {
                var diag = diagnostics[i];
                if (severityOverrides.TryGetValue(diag.RuleId, out var overrideSeverity) &&
                    Enum.TryParse<DiagnosticSeverity>(overrideSeverity, ignoreCase: true, out var newSeverity))
                {
                    diagnostics[i] = diag with { Severity = newSeverity };
                }
            }
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

    private static List<CompiledPathOverride> CompilePathOverrides(List<PathOverride>? overrides)
    {
        if (overrides == null || overrides.Count == 0)
            return [];

        var compiled = new List<CompiledPathOverride>();
        foreach (var ov in overrides)
        {
            if (string.IsNullOrWhiteSpace(ov.Pattern) || ov.DisabledRules == null || ov.DisabledRules.Count == 0)
                continue;

            compiled.Add(new CompiledPathOverride(
                Glob.Parse(ov.Pattern),
                new HashSet<string>(ov.DisabledRules, StringComparer.OrdinalIgnoreCase)));
        }
        return compiled;
    }

    private static bool IsPathSuppressed(string path, string ruleId, List<CompiledPathOverride> overrides)
    {
        foreach (var ov in overrides)
        {
            if (ov.Glob.IsMatch(path) && ov.DisabledRules.Contains(ruleId))
                return true;
        }
        return false;
    }

    private sealed record CompiledPathOverride(Glob Glob, HashSet<string> DisabledRules);
}
