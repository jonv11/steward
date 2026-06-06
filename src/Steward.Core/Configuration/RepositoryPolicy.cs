using YamlDotNet.Serialization;

namespace Steward.Core.Configuration;

public sealed class RepositoryPolicy
{
    [YamlMember(Alias = "repository")]
    public RepositoryInfo? Repository { get; set; }

    [YamlMember(Alias = "artifacts")]
    public List<ArtifactDefinition>? Artifacts { get; set; }

    [YamlMember(Alias = "artifact_families")]
    public List<ArtifactFamilyDefinition>? ArtifactFamilies { get; set; }

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

    /// <summary>
    /// Returns the resolved importance for this artifact, applying the precedence chain:
    /// explicit <c>importance:</c> field → <c>required: true</c> flag → role-linked default → "optional".
    /// </summary>
    public string ResolveImportance()
    {
        if (!string.IsNullOrWhiteSpace(Importance))
            return Importance.ToLowerInvariant();

        if (Required)
            return "required";

        return RoleDefaults.GetDefaultImportance(Role) ?? "optional";
    }
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

public sealed class ArtifactFamilyDefinition
{
    /// <summary>Unique identifier for this artifact family (e.g., "adr", "rfc").</summary>
    [YamlMember(Alias = "family")]
    public string? Family { get; set; }

    /// <summary>Human-readable label shown in status, orient, and explain surfaces.</summary>
    [YamlMember(Alias = "display_name")]
    public string? DisplayName { get; set; }

    /// <summary>Matching criteria. At least one of PathPattern or Frontmatter must be specified.</summary>
    [YamlMember(Alias = "match")]
    public ArtifactFamilyMatch? Match { get; set; }

    /// <summary>Role assigned to files matched by this family (informational; surfaced in orient/status).</summary>
    [YamlMember(Alias = "role")]
    public string? Role { get; set; }

    /// <summary>Importance level (required/recommended/optional) for family-matched files.</summary>
    [YamlMember(Alias = "importance")]
    public string? Importance { get; set; }

    /// <summary>Frontmatter schema enforced on files matched by this family.</summary>
    [YamlMember(Alias = "frontmatter_schema")]
    public ArtifactFamilyFrontmatterSchema? FrontmatterSchema { get; set; }

    /// <summary>Naming pattern regex enforced against the filename of matched files (e.g. "ADR-\\d{3}-.+\\.md").</summary>
    [YamlMember(Alias = "naming_pattern")]
    public string? NamingPattern { get; set; }

    /// <summary>Regex enforced against the H1 heading text of matched files. Enforced by STWD-019.</summary>
    [YamlMember(Alias = "title_pattern")]
    public string? TitlePattern { get; set; }

    /// <summary>Required heading sections that must be present in matched files.</summary>
    [YamlMember(Alias = "required_sections")]
    public List<string>? RequiredSections { get; set; }

    /// <summary>Directory-level expectations for this family (e.g. min_count).</summary>
    [YamlMember(Alias = "directory_expectations")]
    public DirectoryExpectations? DirectoryExpectations { get; set; }
}

public sealed class DirectoryExpectations
{
    /// <summary>Minimum number of files that must match this family.</summary>
    [YamlMember(Alias = "min_count")]
    public int MinCount { get; set; }

    /// <summary>Optional description shown in diagnostic messages.</summary>
    [YamlMember(Alias = "description")]
    public string? Description { get; set; }
}

public sealed class ArtifactFamilyMatch
{
    /// <summary>Glob pattern matched against the file's relative path.</summary>
    [YamlMember(Alias = "path_pattern")]
    public string? PathPattern { get; set; }

    /// <summary>
    /// Frontmatter field conditions (field → required value). All specified
    /// conditions must be satisfied (AND semantics). Case-insensitive comparison.
    /// </summary>
    [YamlMember(Alias = "frontmatter")]
    public Dictionary<string, string>? Frontmatter { get; set; }
}

public sealed class ArtifactFamilyFrontmatterSchema
{
    /// <summary>Field names that must be present in matched files' frontmatter.</summary>
    [YamlMember(Alias = "required")]
    public List<string>? Required { get; set; }

    /// <summary>Controlled vocabularies: each field maps to its allowed string values.</summary>
    [YamlMember(Alias = "allowed_values")]
    public Dictionary<string, List<string>>? AllowedValues { get; set; }

    /// <summary>
    /// Closed-schema field list. When present, any frontmatter key not in this list (and not in
    /// governance.frontmatter.auto_fields) emits a STWD-003 Warning. Absent = open schema.
    /// </summary>
    [YamlMember(Alias = "allowed_fields")]
    public List<string>? AllowedFields { get; set; }

    /// <summary>
    /// Deprecated field names mapped to their canonical replacements (or null for removal-only).
    /// STWD-003 emits a Warning when a deprecated field is found; Error when both the deprecated
    /// field and its non-null replacement coexist. Fixable via --fix --apply.
    /// </summary>
    [YamlMember(Alias = "deprecated_fields")]
    public Dictionary<string, string?>? DeprecatedFields { get; set; }
}
