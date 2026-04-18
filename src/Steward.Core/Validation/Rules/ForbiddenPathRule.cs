using Steward.Core.Configuration;

namespace Steward.Core.Validation.Rules;

public sealed class ForbiddenPathRule : IValidationRule
{
    public string RuleId => "STWD-002";
    public string Category => "path-policy";
    public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Error;
    public string Description => "Files matching forbidden path patterns must not exist.";

    public Task<IReadOnlyList<Diagnostic>> EvaluateAsync(ValidationContext context)
    {
        var diagnostics = new List<Diagnostic>();

        if (context.PathPolicy == null) return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);

        var engine = new PathPolicyEngine(context.PathPolicy);
        var patternDescriptions = BuildPatternDescriptions(context.PathPolicy);

        foreach (var file in context.TargetFiles)
        {
            var eval = engine.Evaluate(file.RelativePath);
            if (eval.Category == "forbidden")
            {
                var reasonSuffix = eval.MatchedPattern != null &&
                    patternDescriptions.TryGetValue(eval.MatchedPattern, out var desc) &&
                    !string.IsNullOrWhiteSpace(desc)
                    ? $" Reason: {desc}"
                    : "";

                diagnostics.Add(new Diagnostic(
                    RuleId: RuleId,
                    Severity: DefaultSeverity,
                    Category: Category,
                    Path: file.RelativePath,
                    Line: null,
                    Message: $"Path '{file.RelativePath}' matches a forbidden pattern '{eval.MatchedPattern}'.{reasonSuffix}",
                    Remediation: $"Remove or rename the file to comply with repository policy.{reasonSuffix}",
                    Source: "path-policy.yaml"));
            }
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }

    private static Dictionary<string, string> BuildPatternDescriptions(PathPolicyDocument policy)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var ruleset in policy.Rulesets ?? [])
        {
            foreach (var rule in ruleset.Rules ?? [])
            {
                if (!string.IsNullOrWhiteSpace(rule.Pattern))
                {
                    // Prefer rule-level description, fall back to ruleset-level description
                    var desc = !string.IsNullOrWhiteSpace(rule.Description)
                        ? rule.Description
                        : ruleset.Description;
                    if (!string.IsNullOrWhiteSpace(desc))
                        result.TryAdd(rule.Pattern, desc!);
                }
            }
        }
        return result;
    }
}
