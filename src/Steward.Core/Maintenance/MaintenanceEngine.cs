using Steward.Core.Configuration;

namespace Steward.Core.Maintenance;

/// <summary>
/// Orchestrates artifact maintainers to produce a maintenance plan.
/// </summary>
public sealed class MaintenanceEngine
{
    private readonly Dictionary<string, IArtifactMaintainer> _maintainers;

    public MaintenanceEngine(IEnumerable<IArtifactMaintainer> maintainers)
    {
        _maintainers = maintainers.ToDictionary(m => m.Type, StringComparer.OrdinalIgnoreCase);
    }

    public MaintenanceEngine() : this(DefaultMaintainers()) { }

    public MaintenancePlan Evaluate(RepositoryPolicy? policy, MaintenanceContext context, string? scope = null)
    {
        var artifacts = policy?.Maintenance?.Artifacts;
        if (artifacts == null || artifacts.Count == 0)
        {
            return new MaintenancePlan { Actions = [] };
        }

        var configs = artifacts
            .Where(a => a.Id != null && a.Type != null)
            .Select(a => new MaintenanceArtifactConfig
            {
                Id = a.Id!,
                Path = a.Path ?? "",
                Type = a.Type!,
                Source = a.Source,
                ManagedSection = a.ManagedSection,
                Sort = a.Sort,
                Targets = a.Targets,
                Fields = a.Fields,
                Options = a.Options != null
                    ? new MaintenanceOptions { Depth = a.Options.Depth, Exclude = a.Options.Exclude }
                    : null
            })
            .ToList();

        if (scope != null)
        {
            configs = configs.Where(c => string.Equals(c.Id, scope, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var actions = new List<MaintenanceAction>();

        foreach (var config in configs)
        {
            if (_maintainers.TryGetValue(config.Type, out var maintainer))
            {
                actions.Add(maintainer.Evaluate(config, context));
            }
        }

        return new MaintenancePlan { Actions = actions };
    }

    private static IArtifactMaintainer[] DefaultMaintainers() =>
    [
        new StructureDocumentMaintainer(),
        new IndexMaintainer(),
        new ManagedSectionMaintainer(),
        new FrontmatterAutoMaintainer(),
        new ManifestMaintainer()
    ];
}
