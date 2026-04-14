using DotNet.Globbing;

namespace Steward.Core.Configuration;

public sealed class PathPolicyEngine
{
    private readonly List<CompiledPathRule> _rules;

    public PathPolicyEngine(PathPolicyDocument? document)
    {
        _rules = [];
        if (document?.Rulesets == null) return;

        foreach (var ruleset in document.Rulesets)
        {
            if (ruleset.Rules == null) continue;
            foreach (var rule in ruleset.Rules)
            {
                if (rule.Pattern == null || rule.Category == null) continue;
                _rules.Add(new CompiledPathRule(
                    Pattern: rule.Pattern,
                    Category: rule.Category.ToLowerInvariant(),
                    Priority: rule.Priority,
                    IsExact: rule.Exact,
                    Glob: rule.Exact ? null : Glob.Parse(rule.Pattern)));
            }
        }
    }

    public PathEvaluation Evaluate(string relativePath)
    {
        CompiledPathRule? winner = null;

        foreach (var rule in _rules)
        {
            var matches = rule.IsExact
                ? string.Equals(rule.Pattern, relativePath, StringComparison.OrdinalIgnoreCase)
                : rule.Glob!.IsMatch(relativePath);

            if (!matches) continue;

            if (winner == null || ShouldReplace(winner, rule))
                winner = rule;
        }

        return new PathEvaluation(
            relativePath,
            winner?.Category ?? "unclassified",
            winner?.Pattern);
    }

    public bool IsIgnored(string relativePath)
    {
        var eval = Evaluate(relativePath);
        return eval.Category == "ignored";
    }

    private static bool ShouldReplace(CompiledPathRule current, CompiledPathRule candidate)
    {
        // Higher priority wins
        if (candidate.Priority != current.Priority)
            return candidate.Priority > current.Priority;

        // Exact over glob
        if (candidate.IsExact && !current.IsExact) return true;
        if (!candidate.IsExact && current.IsExact) return false;

        // More specific pattern (longer) wins
        if (candidate.Pattern.Length != current.Pattern.Length)
            return candidate.Pattern.Length > current.Pattern.Length;

        // Stricter category wins
        return GetCategoryStrictness(candidate.Category) > GetCategoryStrictness(current.Category);
    }

    private static int GetCategoryStrictness(string category) => category switch
    {
        "forbidden" => 7,
        "required" => 6,
        "reserved" => 5,
        "deprecated" => 4,
        "discouraged" => 3,
        "recommended" => 2,
        "optional" => 1,
        "ignored" => 0,
        _ => -1
    };

    private sealed record CompiledPathRule(
        string Pattern,
        string Category,
        int Priority,
        bool IsExact,
        Glob? Glob);
}

public sealed record PathEvaluation(
    string Path,
    string Category,
    string? MatchedPattern);
