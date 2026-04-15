using YamlDotNet.Serialization;

namespace Steward.Core.Configuration;

public sealed class RepositoryPolicy
{
    [YamlMember(Alias = "repository")]
    public RepositoryInfo? Repository { get; set; }

    [YamlMember(Alias = "artifacts")]
    public List<ArtifactDefinition>? Artifacts { get; set; }

    [YamlMember(Alias = "governance")]
    public GovernanceConfig? Governance { get; set; }

    [YamlMember(Alias = "validation")]
    public ValidationConfig? Validation { get; set; }

    [YamlMember(Alias = "maintenance")]
    public MaintenanceConfig? Maintenance { get; set; }
}

public sealed class RepositoryInfo
{
    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    [YamlMember(Alias = "type")]
    public string? Type { get; set; }

    [YamlMember(Alias = "terminology")]
    public Dictionary<string, string>? Terminology { get; set; }
}

public sealed class ArtifactDefinition
{
    [YamlMember(Alias = "path")]
    public string? Path { get; set; }

    [YamlMember(Alias = "role")]
    public string? Role { get; set; }

    [YamlMember(Alias = "description")]
    public string? Description { get; set; }

    [YamlMember(Alias = "required")]
    public bool Required { get; set; }

    [YamlMember(Alias = "importance")]
    public string? Importance { get; set; }

    [YamlMember(Alias = "index_of")]
    public string? IndexOf { get; set; }

    [YamlMember(Alias = "freshness")]
    public FreshnessConfig? Freshness { get; set; }
}

public sealed class FreshnessConfig
{
    [YamlMember(Alias = "max_age_days")]
    public int MaxAgeDays { get; set; }
}

public sealed class GovernanceConfig
{
    [YamlMember(Alias = "section_size_warning_threshold")]
    public int? SectionSizeWarningThreshold { get; set; }

    [YamlMember(Alias = "start_here")]
    public List<string>? StartHere { get; set; }

    [YamlMember(Alias = "frontmatter")]
    public FrontmatterConfig? Frontmatter { get; set; }

    [YamlMember(Alias = "managed_regions")]
    public ManagedRegionsConfig? ManagedRegions { get; set; }

    [YamlMember(Alias = "completion_policy")]
    public CompletionPolicyConfig? CompletionPolicy { get; set; }
}

public sealed class FrontmatterConfig
{
    [YamlMember(Alias = "required_fields")]
    public List<string>? RequiredFields { get; set; }

    [YamlMember(Alias = "auto_fields")]
    public Dictionary<string, bool>? AutoFields { get; set; }
}

public sealed class ManagedRegionsConfig
{
    [YamlMember(Alias = "marker")]
    public string? Marker { get; set; }

    [YamlMember(Alias = "enforce_ownership")]
    public bool EnforceOwnership { get; set; }
}

public sealed class CompletionPolicyConfig
{
    [YamlMember(Alias = "rules")]
    public List<CompletionRule>? Rules { get; set; }
}

public sealed class CompletionRule
{
    [YamlMember(Alias = "id")]
    public string? Id { get; set; }

    [YamlMember(Alias = "description")]
    public string? Description { get; set; }
}

public sealed class ValidationConfig
{
    [YamlMember(Alias = "severity_overrides")]
    public Dictionary<string, string>? SeverityOverrides { get; set; }

    [YamlMember(Alias = "disabled_rules")]
    public List<string>? DisabledRules { get; set; }

    [YamlMember(Alias = "required_frontmatter_fields")]
    public List<string>? RequiredFrontmatterFields { get; set; }

    [YamlMember(Alias = "path_overrides")]
    public List<PathOverride>? PathOverrides { get; set; }

    [YamlMember(Alias = "frontmatter_requirements")]
    public List<FrontmatterRequirement>? FrontmatterRequirements { get; set; }
}

public sealed class PathOverride
{
    [YamlMember(Alias = "pattern")]
    public string? Pattern { get; set; }

    [YamlMember(Alias = "disabled_rules")]
    public List<string>? DisabledRules { get; set; }
}

public sealed class FrontmatterRequirement
{
    [YamlMember(Alias = "pattern")]
    public string? Pattern { get; set; }

    [YamlMember(Alias = "required_fields")]
    public List<string>? RequiredFields { get; set; }

    [YamlMember(Alias = "allowed_values")]
    public Dictionary<string, List<string>>? AllowedValues { get; set; }
}

public sealed class MaintenanceConfig
{
    [YamlMember(Alias = "artifacts")]
    public List<MaintenanceArtifactDef>? Artifacts { get; set; }
}

public sealed class MaintenanceArtifactDef
{
    [YamlMember(Alias = "id")]
    public string? Id { get; set; }

    [YamlMember(Alias = "path")]
    public string? Path { get; set; }

    [YamlMember(Alias = "type")]
    public string? Type { get; set; }

    [YamlMember(Alias = "source")]
    public string? Source { get; set; }

    [YamlMember(Alias = "managed_section")]
    public string? ManagedSection { get; set; }

    [YamlMember(Alias = "sort")]
    public string? Sort { get; set; }

    [YamlMember(Alias = "targets")]
    public string? Targets { get; set; }

    [YamlMember(Alias = "fields")]
    public Dictionary<string, string>? Fields { get; set; }

    [YamlMember(Alias = "options")]
    public MaintenanceOptionsDef? Options { get; set; }

    [YamlMember(Alias = "depends_on")]
    public List<string>? DependsOn { get; set; }
}

public sealed class MaintenanceOptionsDef
{
    [YamlMember(Alias = "depth")]
    public int Depth { get; set; } = 3;

    [YamlMember(Alias = "exclude")]
    public List<string>? Exclude { get; set; }
}
