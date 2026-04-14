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

        foreach (var file in context.TargetFiles)
        {
            var eval = engine.Evaluate(file.RelativePath);
            if (eval.Category == "forbidden")
            {
                diagnostics.Add(new Diagnostic(
                    RuleId: RuleId,
                    Severity: DefaultSeverity,
                    Category: Category,
                    Path: file.RelativePath,
                    Line: null,
                    Message: $"Path '{file.RelativePath}' matches a forbidden pattern '{eval.MatchedPattern}'.",
                    Remediation: "Remove or rename the file to comply with repository policy.",
                    Source: "path-policy.yaml"));
            }
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }
}
