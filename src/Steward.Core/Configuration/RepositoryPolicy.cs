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
}

public sealed class GovernanceConfig
{
    [YamlMember(Alias = "section_size_warning_threshold")]
    public int SectionSizeWarningThreshold { get; set; } = 500;

    [YamlMember(Alias = "start_here")]
    public List<string>? StartHere { get; set; }
}

public sealed class ValidationConfig
{
    [YamlMember(Alias = "severity_overrides")]
    public Dictionary<string, string>? SeverityOverrides { get; set; }

    [YamlMember(Alias = "disabled_rules")]
    public List<string>? DisabledRules { get; set; }

    [YamlMember(Alias = "required_frontmatter_fields")]
    public List<string>? RequiredFrontmatterFields { get; set; }
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
}

public sealed class MaintenanceOptionsDef
{
    [YamlMember(Alias = "depth")]
    public int Depth { get; set; } = 3;

    [YamlMember(Alias = "exclude")]
    public List<string>? Exclude { get; set; }
}
