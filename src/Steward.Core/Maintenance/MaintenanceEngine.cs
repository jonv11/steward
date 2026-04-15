using Steward.Core.Configuration;

namespace Steward.Core.Maintenance;

/// <summary>
/// Orchestrates artifact maintainers to produce a maintenance plan.
/// </summary>
public sealed class MaintenanceEngine
{
    private static readonly string[] SupportedArtifactTypes =
    [
        "structure-document",
        "index",
        "directory-index",
        "managed-section",
        "frontmatter-auto",
        "manifest"
    ];

    private readonly Dictionary<string, IArtifactMaintainer> _maintainers;

    public MaintenanceEngine(IEnumerable<IArtifactMaintainer> maintainers)
    {
        _maintainers = maintainers.ToDictionary(m => m.Type, StringComparer.OrdinalIgnoreCase);
    }

    public MaintenanceEngine() : this(DefaultMaintainers()) { }

    public static IReadOnlyCollection<string> GetSupportedArtifactTypes() => SupportedArtifactTypes;

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
                    : null,
                DependsOn = a.DependsOn
            })
            .ToList();

        if (scope != null)
        {
            configs = configs.Where(c => string.Equals(c.Id, scope, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var actions = new List<MaintenanceAction>();

        // Topological sort by depends_on
        var ordered = TopologicalSort(configs, out var cyclicIds);

        foreach (var id in cyclicIds)
        {
            actions.Add(new MaintenanceAction
            {
                ArtifactId = id,
                ArtifactPath = "",
                Type = "error",
                Description = $"Circular dependency detected involving '{id}'.",
                HasChanges = false
            });
        }

        foreach (var config in ordered)
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
        new DirectoryIndexMaintainer(),
        new ManagedSectionMaintainer(),
        new FrontmatterAutoMaintainer(),
        new ManifestMaintainer()
    ];

    internal static List<MaintenanceArtifactConfig> TopologicalSort(
        List<MaintenanceArtifactConfig> configs, out List<string> cyclicIds)
    {
        var byId = configs.ToDictionary(c => c.Id, StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var inStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<MaintenanceArtifactConfig>();
        var cycles = new List<string>();

        void Visit(string id)
        {
            if (visited.Contains(id)) return;
            if (inStack.Contains(id))
            {
                cycles.Add(id);
                return;
            }

            inStack.Add(id);

            if (byId.TryGetValue(id, out var config) && config.DependsOn != null)
            {
                foreach (var dep in config.DependsOn)
                {
                    if (byId.ContainsKey(dep))
                        Visit(dep);
                }
            }

            inStack.Remove(id);
            visited.Add(id);
            if (byId.TryGetValue(id, out var c))
                result.Add(c);
        }

        foreach (var config in configs)
            Visit(config.Id);

        cyclicIds = cycles;
        return result;
    }
}
