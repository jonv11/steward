using System.Text.RegularExpressions;
using DotNet.Globbing;
using Steward.Core.Configuration;

namespace Steward.Core.Validation.Rules;

/// <summary>
/// STWD-010: Enforces naming conventions declared via must_match regex in path-policy rules.
/// </summary>
public sealed class NamingConventionRule : IValidationRule
{
    public string RuleId => "STWD-010";
    public string Category => "path-policy";
    public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Warning;
    public string Description => "Files in governed directories must match declared naming conventions.";

    public Task<IReadOnlyList<Diagnostic>> EvaluateAsync(ValidationContext context)
    {
        var diagnostics = new List<Diagnostic>();

        if (context.PathPolicy?.Rulesets == null)
            return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);

        var namingRules = CompileNamingRules(context.PathPolicy, diagnostics);
        if (namingRules.Count == 0)
            return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);

        foreach (var file in context.TargetFiles)
        {
            foreach (var rule in namingRules)
            {
                if (!rule.Glob.IsMatch(file.RelativePath))
                    continue;

                var fileName = Path.GetFileName(file.RelativePath);
                bool isMatch;
                try
                {
                    isMatch = rule.MustMatch.IsMatch(fileName);
                }
                catch (RegexMatchTimeoutException)
                {
                    diagnostics.Add(new Diagnostic(
                        RuleId: RuleId,
                        Severity: DiagnosticSeverity.Warning,
                        Category: "config-error",
                        Path: file.RelativePath,
                        Line: null,
                        Message: $"Naming convention regex '{rule.MustMatchPattern}' timed out evaluating '{fileName}'. This may indicate catastrophic backtracking.",
                        Remediation: "Simplify the must_match regex in path-policy.yaml to avoid excessive backtracking.",
                        Source: "path-policy.yaml"));
                    continue;
                }

                if (!isMatch)
                {
                    diagnostics.Add(new Diagnostic(
                        RuleId: RuleId,
                        Severity: DefaultSeverity,
                        Category: Category,
                        Path: file.RelativePath,
                        Line: null,
                        Message: $"File '{fileName}' does not match naming convention '{rule.MustMatchPattern}' required for pattern '{rule.Pattern}'.",
                        Remediation: $"Rename the file to match the pattern: {rule.MustMatchPattern}",
                        Source: "path-policy.yaml",
                        Details: new Dictionary<string, object> { ["expectedPattern"] = rule.MustMatchPattern }));
                }
            }
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }

    private static List<CompiledNamingRule> CompileNamingRules(PathPolicyDocument pathPolicy, List<Diagnostic> diagnostics)
    {
        var rules = new List<CompiledNamingRule>();

        foreach (var ruleset in pathPolicy.Rulesets ?? [])
        {
            foreach (var rule in ruleset.Rules ?? [])
            {
                if (string.IsNullOrWhiteSpace(rule.Pattern) || string.IsNullOrWhiteSpace(rule.MustMatch))
                    continue;

                try
                {
                    var regex = new Regex(rule.MustMatch, RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
                    var glob = Glob.Parse(rule.Pattern);
                    rules.Add(new CompiledNamingRule(rule.Pattern, glob, regex, rule.MustMatch));
                }
                catch (RegexParseException ex)
                {
                    // Surface invalid regex as a warning so the user knows enforcement is disabled.
                    diagnostics.Add(new Diagnostic(
                        RuleId: "STWD-010",
                        Severity: DiagnosticSeverity.Warning,
                        Category: "config-error",
                        Path: null,
                        Line: null,
                        Message: $"path-policy.yaml: must_match pattern '{rule.MustMatch}' for glob '{rule.Pattern}' is an invalid regex — naming convention enforcement is disabled for this pattern.",
                        Remediation: $"Fix or remove the must_match regex in path-policy.yaml. Parse error: {ex.Message}",
                        Source: "path-policy.yaml"));
                }
            }
        }

        return rules;
    }

    private sealed record CompiledNamingRule(string Pattern, Glob Glob, Regex MustMatch, string MustMatchPattern);
}
