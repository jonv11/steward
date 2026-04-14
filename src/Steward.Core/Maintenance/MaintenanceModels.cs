namespace Steward.Core.Maintenance;

/// <summary>
/// A plan describing all maintenance actions to be performed.
/// </summary>
public sealed class MaintenancePlan
{
    public required IReadOnlyList<MaintenanceAction> Actions { get; init; }
    public bool HasChanges => Actions.Any(a => a.HasChanges);
}

/// <summary>
/// A single maintenance action for one artifact.
/// </summary>
public sealed class MaintenanceAction
{
    public required string ArtifactId { get; init; }
    public required string ArtifactPath { get; init; }
    public required string Type { get; init; }
    public required string Description { get; init; }
    public required bool HasChanges { get; init; }
    public string? ExpectedContent { get; init; }
    public string? CurrentContent { get; init; }
    public string? Diff { get; init; }
}

/// <summary>
/// Configuration for a maintained artifact, loaded from policy.yaml.
/// </summary>
public sealed class MaintenanceArtifactConfig
{
    public required string Id { get; init; }
    public required string Path { get; init; }
    public required string Type { get; init; }
    public string? Source { get; init; }
    public string? ManagedSection { get; init; }
    public string? Sort { get; init; }
    public string? Targets { get; init; }
    public Dictionary<string, string>? Fields { get; init; }
    public MaintenanceOptions? Options { get; init; }
}

public sealed class MaintenanceOptions
{
    public int Depth { get; init; } = 3;
    public List<string>? Exclude { get; init; }
}

/// <summary>
/// Interface for artifact maintainers.
/// </summary>
public interface IArtifactMaintainer
{
    string Type { get; }
    MaintenanceAction Evaluate(MaintenanceArtifactConfig config, MaintenanceContext context);
}

/// <summary>
/// Context provided to maintainers during evaluation.
/// </summary>
public sealed class MaintenanceContext
{
    public required string RepositoryRoot { get; init; }
    public required Abstractions.IFileSystem FileSystem { get; init; }
    public required IReadOnlyList<Discovery.DiscoveredFile> Files { get; init; }
}
