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

        var coverageOption = new Option<bool>("--coverage")
        {
            Description = "Include governance coverage report"
        };
        command.Add(coverageOption);

        command.SetAction(parseResult =>
        {
            if (!CommandSetup.TryBuild(parseResult, out var ctx))
                return ExitCodes.UsageError;

            var showCoverage = parseResult.GetValue(coverageOption);

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

                if (showCoverage)
                {
                    var coverage = ComputeCoverage(ctx.Policy, ctx.Files!);
                    ctx.Formatter.WriteMessage("");
                    ctx.Formatter.WriteMessage($"Governance coverage: {coverage.GovernedCount}/{coverage.TotalMarkdownFiles} Markdown files ({coverage.Percentage:F0}%)");
                    if (coverage.Ungoverned.Count > 0)
                    {
                        ctx.Formatter.WriteMessage("Ungoverned files:");
                        foreach (var path in coverage.Ungoverned.Take(20))
                            ctx.Formatter.WriteMessage($"  - {path}");
                        if (coverage.Ungoverned.Count > 20)
                            ctx.Formatter.WriteMessage($"  ... and {coverage.Ungoverned.Count - 20} more");
                    }
                }
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

    internal static CoverageResult ComputeCoverage(RepositoryPolicy? policy, IReadOnlyList<DiscoveredFile> files)
    {
        var mdFiles = files
            .Where(f => f.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            .Select(f => f.RelativePath.Replace('\\', '/'))
            .ToList();

        if (mdFiles.Count == 0)
            return new CoverageResult { TotalMarkdownFiles = 0, GovernedCount = 0, Percentage = 100, Ungoverned = [] };

        var governed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Artifact paths
        if (policy?.Artifacts != null)
        {
            foreach (var a in policy.Artifacts)
            {
                if (!string.IsNullOrWhiteSpace(a.Path))
                    governed.Add(a.Path.Replace('\\', '/').TrimEnd('/'));
            }
        }

        // 2. Maintenance source directories — files under them are governed
        if (policy?.Maintenance?.Artifacts != null)
        {
            foreach (var ma in policy.Maintenance.Artifacts)
            {
                if (string.IsNullOrWhiteSpace(ma.Source))
                    continue;
                var source = ma.Source.Replace('\\', '/').TrimEnd('/');
                foreach (var f in mdFiles)
                {
                    if (f.StartsWith(source + "/", StringComparison.OrdinalIgnoreCase) ||
                        f.Equals(source, StringComparison.OrdinalIgnoreCase))
                        governed.Add(f);
                }

                // Also the artifact path itself
                if (!string.IsNullOrWhiteSpace(ma.Path))
                    governed.Add(ma.Path.Replace('\\', '/'));
            }
        }

        // 3. start_here
        if (policy?.Governance?.StartHere != null)
        {
            foreach (var s in policy.Governance.StartHere)
                governed.Add(s.Replace('\\', '/'));
        }

        var ungoverned = mdFiles.Where(f => !governed.Contains(f)).OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();

        return new CoverageResult
        {
            TotalMarkdownFiles = mdFiles.Count,
            GovernedCount = mdFiles.Count - ungoverned.Count,
            Percentage = mdFiles.Count > 0 ? (double)(mdFiles.Count - ungoverned.Count) / mdFiles.Count * 100 : 100,
            Ungoverned = ungoverned
        };
    }

    internal sealed class CoverageResult
    {
        public int TotalMarkdownFiles { get; init; }
        public int GovernedCount { get; init; }
        public double Percentage { get; init; }
        public List<string> Ungoverned { get; init; } = [];
    }
}
