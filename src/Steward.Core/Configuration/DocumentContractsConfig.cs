using YamlDotNet.Serialization;

namespace Steward.Core.Configuration;

/// <summary>Opt-in, repository-local contracts; list entries replace profile lists.</summary>
public sealed class DocumentContractsConfig
{
    [YamlMember(Alias = "budgets")]
    public List<DocumentBudget>? Budgets { get; set; }

    [YamlMember(Alias = "identities")]
    public List<DocumentIdentity>? Identities { get; set; }

    [YamlMember(Alias = "relations")]
    public List<DocumentRelation>? Relations { get; set; }

    [YamlMember(Alias = "source_collections")]
    public List<SourceCollection>? SourceCollections { get; set; }
}

public sealed class DocumentBudget
{
    [YamlMember(Alias = "pattern")]
    public string? Pattern { get; set; }
    [YamlMember(Alias = "max_words")]
    public int? MaxWords { get; set; }
    [YamlMember(Alias = "max_section_words")]
    public int? MaxSectionWords { get; set; }
    [YamlMember(Alias = "max_table_cell_words")]
    public int? MaxTableCellWords { get; set; }
    [YamlMember(Alias = "max_index_entries")]
    public int? MaxIndexEntries { get; set; }
}

public sealed class DocumentIdentity
{
    [YamlMember(Alias = "pattern")]
    public string? Pattern { get; set; }
    [YamlMember(Alias = "field")]
    public string? Field { get; set; }
    [YamlMember(Alias = "required")]
    public bool Required { get; set; } = true;
}

public sealed class DocumentRelation
{
    [YamlMember(Alias = "pattern")]
    public string? Pattern { get; set; }
    [YamlMember(Alias = "field")]
    public string? Field { get; set; }
    [YamlMember(Alias = "target_pattern")]
    public string? TargetPattern { get; set; }
}

public sealed class SourceCollection
{
    [YamlMember(Alias = "id")]
    public string? Id { get; set; }
    [YamlMember(Alias = "parts_pattern")]
    public string? PartsPattern { get; set; }
    [YamlMember(Alias = "parts")]
    public List<string>? Parts { get; set; }
    [YamlMember(Alias = "sha256")]
    public string? Sha256 { get; set; }
}
