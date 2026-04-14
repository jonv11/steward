using System.CommandLine;
using Steward.Core;
using Steward.Core.Abstractions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Formatting;
using Steward.Core.Maintenance;
using Steward.Cli.Formatting;

namespace Steward.Cli.Commands;

public static class StatusCommand
{
    public static Command Create()
    {
        var command = new Command("status", "Show current repository state at a glance");

        command.SetAction(parseResult =>
        {
            var output = parseResult.GetValue(GlobalOptionsSetup.OutputOption);
            var noColor = parseResult.GetValue(GlobalOptionsSetup.NoColorOption);
            var configPath = parseResult.GetValue(GlobalOptionsSetup.ConfigOption);

            var formatter = CreateFormatter(output, noColor);
            var fileSystem = new PhysicalFileSystem();
            var rootPath = Directory.GetCurrentDirectory();

            // Load config
            var configLoader = new ConfigLoader(fileSystem);
            var configDir = configLoader.FindConfigDirectory(rootPath, configPath);

            if (configDir == null)
            {
                formatter.WriteError("No .steward configuration directory found. Run 'steward init' first.");
                return ExitCodes.UsageError;
            }

            var policy = configLoader.LoadPolicy(configDir);

            // Discover files
            var ignoreFilter = GitIgnoreFilter.Load(rootPath, fileSystem);
            var discoveryService = new FileDiscoveryService(fileSystem, ignoreFilter);
            var files = discoveryService.Discover(rootPath);

            // Cheap status checks
            var status = ComputeStatus(policy, fileSystem, rootPath, files);

            if (output == OutputFormat.Json)
            {
                formatter.WriteObject(status);
            }
            else
            {
                formatter.WriteMessage($"Repository: {status.RepositoryName ?? "(unnamed)"}");
                formatter.WriteMessage($"Files: {status.FileCount}");
                formatter.WriteMessage("");

                // Required artifacts
                if (status.RequiredArtifacts.Count > 0)
                {
                    formatter.WriteMessage("Required Artifacts:");
                    foreach (var a in status.RequiredArtifacts)
                    {
                        var icon = a.Present ? "OK" : "MISSING";
                        formatter.WriteMessage($"  [{icon}] {a.Path} ({a.Role})");
                    }
                    formatter.WriteMessage("");
                }

                // Maintenance status
                if (status.MaintenanceArtifacts.Count > 0)
                {
                    formatter.WriteMessage("Maintained Artifacts:");
                    foreach (var m in status.MaintenanceArtifacts)
                    {
                        var icon = m.Stale ? "STALE" : "OK   ";
                        formatter.WriteMessage($"  [{icon}] {m.Id}: {m.Path}");
                    }
                    formatter.WriteMessage("");
                }

                // Completeness
                formatter.WriteMessage($"Completeness: {status.PresentCount}/{status.RequiredCount} required artifacts present");
                if (status.StaleCount > 0)
                    formatter.WriteMessage($"Stale artifacts: {status.StaleCount}");
            }

            return ExitCodes.Success;
        });

        return command;
    }

    internal static RepositoryStatus ComputeStatus(
        RepositoryPolicy? policy,
        IFileSystem fileSystem,
        string rootPath,
        IReadOnlyList<DiscoveredFile> files)
    {
        var existingPaths = new HashSet<string>(
            files.Select(f => f.RelativePath),
            StringComparer.OrdinalIgnoreCase);

        // Required artifacts
        var requiredArtifacts = new List<ArtifactStatus>();
        if (policy?.Artifacts != null)
        {
            foreach (var artifact in policy.Artifacts.Where(a => a.Required))
            {
                requiredArtifacts.Add(new ArtifactStatus
                {
                    Path = artifact.Path ?? "",
                    Role = artifact.Role ?? "",
                    Present = artifact.Path != null && existingPaths.Contains(artifact.Path)
                });
            }
        }

        // Maintenance status (cheap check)
        var maintenanceArtifacts = new List<MaintenanceStatus>();
        if (policy?.Maintenance?.Artifacts != null)
        {
            var context = new MaintenanceContext
            {
                RepositoryRoot = rootPath,
                FileSystem = fileSystem,
                Files = files
            };

            var engine = new MaintenanceEngine();
            var plan = engine.Evaluate(policy, context);

            foreach (var action in plan.Actions)
            {
                maintenanceArtifacts.Add(new MaintenanceStatus
                {
                    Id = action.ArtifactId,
                    Path = action.ArtifactPath,
                    Stale = action.HasChanges
                });
            }
        }

        return new RepositoryStatus
        {
            RepositoryName = policy?.Repository?.Name,
            FileCount = files.Count,
            RequiredArtifacts = requiredArtifacts,
            MaintenanceArtifacts = maintenanceArtifacts,
            PresentCount = requiredArtifacts.Count(a => a.Present),
            RequiredCount = requiredArtifacts.Count,
            StaleCount = maintenanceArtifacts.Count(m => m.Stale)
        };
    }

    private static IOutputFormatter CreateFormatter(OutputFormat format, bool noColor)
    {
        return format switch
        {
            OutputFormat.Json => new JsonOutputFormatter(Console.Out),
            _ => new TextOutputFormatter(Console.Out, !noColor && !Console.IsOutputRedirected)
        };
    }

    internal sealed class RepositoryStatus
    {
        public string? RepositoryName { get; init; }
        public int FileCount { get; init; }
        public List<ArtifactStatus> RequiredArtifacts { get; init; } = [];
        public List<MaintenanceStatus> MaintenanceArtifacts { get; init; } = [];
        public int PresentCount { get; init; }
        public int RequiredCount { get; init; }
        public int StaleCount { get; init; }
    }

    internal sealed class ArtifactStatus
    {
        public required string Path { get; init; }
        public required string Role { get; init; }
        public required bool Present { get; init; }
    }

    internal sealed class MaintenanceStatus
    {
        public required string Id { get; init; }
        public required string Path { get; init; }
        public required bool Stale { get; init; }
    }
}
