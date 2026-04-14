using System.CommandLine;
using Steward.Core;
using Steward.Core.Abstractions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Formatting;
using Steward.Core.Maintenance;

namespace Steward.Cli.Commands;

public static class StatusCommand
{
    public static Command Create()
    {
        var command = new Command("status", "Show current repository state at a glance");

        command.SetAction(parseResult =>
        {
            if (!CommandSetup.TryBuild(parseResult, out var ctx))
                return ExitCodes.UsageError;

            if (ctx!.ConfigDirectory == null)
            {
                ctx.Formatter.WriteError("No .steward configuration directory found. Run 'steward init' first.");
                return ExitCodes.UsageError;
            }

            // Cheap status checks
            var status = ComputeStatus(ctx.Policy, ctx.Config?.Profile, ctx.FileSystem, ctx.RootPath, ctx.Files!);

            if (ctx.OutputFormat == OutputFormat.Json)
            {
                ctx.Formatter.WriteObject(status);
            }
            else
            {
                ctx.Formatter.WriteMessage($"Repository: {status.RepositoryName ?? "(unnamed)"}");
                if (!string.IsNullOrWhiteSpace(status.RepositoryType) || !string.IsNullOrWhiteSpace(status.Profile))
                {
                    var details = new List<string>();
                    if (!string.IsNullOrWhiteSpace(status.RepositoryType))
                        details.Add($"type={status.RepositoryType}");
                    if (!string.IsNullOrWhiteSpace(status.Profile))
                        details.Add($"profile={status.Profile}");
                    ctx.Formatter.WriteMessage($"Context: {string.Join(", ", details)}");
                }
                ctx.Formatter.WriteMessage($"Files: {status.FileCount}");
                ctx.Formatter.WriteMessage("");

                if (status.StartHere.Count > 0)
                {
                    ctx.Formatter.WriteMessage("Start Here:");
                    foreach (var path in status.StartHere)
                        ctx.Formatter.WriteMessage($"  - {path}");
                    ctx.Formatter.WriteMessage("");
                }

                // Required artifacts
                if (status.RequiredArtifacts.Count > 0)
                {
                    ctx.Formatter.WriteMessage("Required Artifacts:");
                    foreach (var a in status.RequiredArtifacts)
                    {
                        var icon = a.Present ? "OK" : "MISSING";
                        ctx.Formatter.WriteMessage($"  [{icon}] {a.Path} ({a.Role})");
                    }
                    ctx.Formatter.WriteMessage("");
                }

                // Maintenance status
                if (status.MaintenanceArtifacts.Count > 0)
                {
                    ctx.Formatter.WriteMessage("Maintained Artifacts:");
                    foreach (var m in status.MaintenanceArtifacts)
                    {
                        var icon = m.Stale ? "STALE" : "OK   ";
                        ctx.Formatter.WriteMessage($"  [{icon}] {m.Id}: {m.Path}");
                    }
                    ctx.Formatter.WriteMessage("");
                }

                // Completeness
                ctx.Formatter.WriteMessage($"Completeness: {status.PresentCount}/{status.RequiredCount} required artifacts present");
                if (status.StaleCount > 0)
                    ctx.Formatter.WriteMessage($"Stale artifacts: {status.StaleCount}");
            }

            return ExitCodes.Success;
        });

        return command;
    }

    internal static RepositoryStatus ComputeStatus(
        RepositoryPolicy? policy,
        string? profile,
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
            RepositoryType = policy?.Repository?.Type,
            Profile = profile,
            FileCount = files.Count,
            RequiredArtifacts = requiredArtifacts,
            MaintenanceArtifacts = maintenanceArtifacts,
            StartHere = policy?.Governance?.StartHere ?? [],
            PresentCount = requiredArtifacts.Count(a => a.Present),
            RequiredCount = requiredArtifacts.Count,
            StaleCount = maintenanceArtifacts.Count(m => m.Stale)
        };
    }

    internal sealed class RepositoryStatus
    {
        public string? RepositoryName { get; init; }
        public string? RepositoryType { get; init; }
        public string? Profile { get; init; }
        public int FileCount { get; init; }
        public List<ArtifactStatus> RequiredArtifacts { get; init; } = [];
        public List<MaintenanceStatus> MaintenanceArtifacts { get; init; } = [];
        public List<string> StartHere { get; init; } = [];
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
