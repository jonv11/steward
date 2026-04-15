using YamlDotNet.Serialization;

namespace Steward.Core.Configuration;

public sealed class PathPolicyDocument
{
    [YamlMember(Alias = "rulesets")]
    public List<PathRuleSet>? Rulesets { get; set; }
}

public sealed class PathRuleSet
{
    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    [YamlMember(Alias = "rules")]
    public List<PathRule>? Rules { get; set; }
}

public sealed class PathRule
{
    [YamlMember(Alias = "pattern")]
    public string? Pattern { get; set; }

    [YamlMember(Alias = "category")]
    public string? Category { get; set; }

    [YamlMember(Alias = "priority")]
    public int Priority { get; set; }

    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    [YamlMember(Alias = "exact")]
    public bool Exact { get; set; }

    [YamlMember(Alias = "kind")]
    public string? Kind { get; set; }

    [YamlMember(Alias = "must_match")]
    public string? MustMatch { get; set; }
}
