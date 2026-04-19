using System.CommandLine;
using DotNet.Globbing;
using Steward.Core;
using Steward.Core.Abstractions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Formatting;
using Steward.Core.Maintenance;
using Steward.Core.Markdown;
using Steward.Core.Validation.Rules;
using Steward.Cli.Formatting;

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
                if (ctx.OutputFormat == OutputFormat.Json)
                {
                    JsonEnvelopeWriter.WriteError(ctx.Formatter, "status", ExitCodes.UsageError,
                        "config-not-found", "No .steward configuration directory found. Run 'steward init' first.");
                    return ExitCodes.UsageError;
                }
                ctx.Formatter.WriteError("No .steward configuration directory found. Run 'steward init' first.");
                return ExitCodes.UsageError;
            }

            // Cheap status checks
            var status = ComputeStatus(ctx.Policy, ctx.Config?.Profile, ctx.FileSystem, ctx.RootPath, ctx.Files!);

            if (ctx.OutputFormat == OutputFormat.Json)
            {
                if (showCoverage)
                {
                    var coverage = ComputeCoverage(ctx.Policy, ctx.Files!, ctx.FileSystem, ctx.RootPath, ctx.Config?.Coverage?.Exclude);
                    status.Coverage = new CoverageResponse
                    {
                        GovernedCount = coverage.GovernedCount,
                        TotalMarkdownFiles = coverage.TotalMarkdownFiles,
                        Percentage = coverage.Percentage,
                        Ungoverned = coverage.Ungoverned
                    };
                }

                JsonEnvelopeWriter.Write(ctx.Formatter, "status", true, ExitCodes.Success, status);
            }
            else
            {
                ctx.Formatter.WriteMessage(
                    $"{OutputStyler.Style(ctx.Formatter, "Repository:", CliTextStyle.Heading)} {status.RepositoryName ?? "(unnamed)"}");
                if (!string.IsNullOrWhiteSpace(status.RepositoryType) || !string.IsNullOrWhiteSpace(status.Profile))
                {
                    var details = new List<string>();
                    if (!string.IsNullOrWhiteSpace(status.RepositoryType))
                        details.Add($"type={status.RepositoryType}");
                    if (!string.IsNullOrWhiteSpace(status.Profile))
                        details.Add($"profile={status.Profile}");
                    ctx.Formatter.WriteMessage(
                        $"{OutputStyler.Style(ctx.Formatter, "Context:", CliTextStyle.Heading)} {string.Join(", ", details)}");
                }
                ctx.Formatter.WriteMessage(
                    $"{OutputStyler.Style(ctx.Formatter, "Files:", CliTextStyle.Heading)} {status.FileCount}");
                ctx.Formatter.WriteMessage("");

                if (status.StartHere.Count > 0)
                {
                    ctx.Formatter.WriteMessage(OutputStyler.Style(ctx.Formatter, "Start Here:", CliTextStyle.Heading));
                    foreach (var path in status.StartHere)
                        ctx.Formatter.WriteMessage($"  - {path}");
                    ctx.Formatter.WriteMessage("");
                }

                // Required artifacts
                if (status.RequiredArtifacts.Count > 0)
                {
                    ctx.Formatter.WriteMessage(OutputStyler.Style(ctx.Formatter, "Required Artifacts:", CliTextStyle.Heading));
                    foreach (var a in status.RequiredArtifacts)
                    {
                        var icon = a.Present ? "OK" : "MISSING";
                        ctx.Formatter.WriteMessage(
                            $"  {FormatStatusIcon(ctx.Formatter, icon)} {a.Path} {OutputStyler.Style(ctx.Formatter, $"({a.Role})", CliTextStyle.Muted)}");
                    }
                    ctx.Formatter.WriteMessage("");
                }

                if (status.RecommendedArtifacts.Count > 0)
                {
                    ctx.Formatter.WriteMessage(OutputStyler.Style(ctx.Formatter, "Recommended Artifacts:", CliTextStyle.Heading));
                    foreach (var a in status.RecommendedArtifacts)
                    {
                        var icon = a.Present ? "OK" : "MISSING";
                        ctx.Formatter.WriteMessage(
                            $"  {FormatStatusIcon(ctx.Formatter, icon)} {a.Path} {OutputStyler.Style(ctx.Formatter, $"({a.Role})", CliTextStyle.Muted)}");
                    }
                    ctx.Formatter.WriteMessage("");
                }

                if (status.StateDocuments.Count > 0)
                {
                    ctx.Formatter.WriteMessage(OutputStyler.Style(ctx.Formatter, "State Documents:", CliTextStyle.Heading));
                    foreach (var stateDoc in status.StateDocuments)
                    {
                        var icon = stateDoc.Present
                            ? stateDoc.Stale ? "STALE" : "OK"
                            : "MISSING";
                        var freshness = stateDoc.FreshnessMaxAgeDays.HasValue
                            ? $", freshness={stateDoc.FreshnessMaxAgeDays.Value}d"
                            : string.Empty;
                        ctx.Formatter.WriteMessage(
                            $"  {FormatStatusIcon(ctx.Formatter, icon)} {stateDoc.Path} {OutputStyler.Style(ctx.Formatter, $"({stateDoc.Role}{freshness})", CliTextStyle.Muted)}");
                    }
                    ctx.Formatter.WriteMessage("");
                }

                // Maintenance status
                if (status.MaintenanceArtifacts.Count > 0)
                {
                    ctx.Formatter.WriteMessage(OutputStyler.Style(ctx.Formatter, "Maintained Artifacts:", CliTextStyle.Heading));
                    foreach (var m in status.MaintenanceArtifacts)
                    {
                        var icon = m.Stale ? "STALE" : "OK   ";
                        ctx.Formatter.WriteMessage($"  {FormatStatusIcon(ctx.Formatter, icon)} {m.Id}: {m.Path}");
                    }
                    ctx.Formatter.WriteMessage("");
                }

                // Artifact families
                if (status.ArtifactFamilies.Count > 0)
                {
                    ctx.Formatter.WriteMessage(OutputStyler.Style(ctx.Formatter, "Artifact Families:", CliTextStyle.Heading));
                    foreach (var f in status.ArtifactFamilies)
                    {
                        var label = f.DisplayName != null ? $"{f.Family} ({f.DisplayName})" : f.Family;
                        ctx.Formatter.WriteMessage($"  {label}: {f.MatchedCount} matched");
                    }
                    ctx.Formatter.WriteMessage("");
                }

                // Completeness
                ctx.Formatter.WriteMessage($"Completeness: {status.PresentCount}/{status.RequiredCount} required artifacts present");
                if (status.RecommendedCount > 0)
                    ctx.Formatter.WriteMessage($"Recommended artifacts: {status.RecommendedPresentCount}/{status.RecommendedCount} present");
                if (status.StateDocuments.Count > 0)
                    ctx.Formatter.WriteMessage($"State documents: {status.StateDocuments.Count(static doc => doc.Present)}/{status.StateDocuments.Count} present");
                if (status.StaleCount > 0)
                    ctx.Formatter.WriteMessage($"Stale artifacts: {status.StaleCount}");

                if (showCoverage)
                {
                    var coverage = ComputeCoverage(ctx.Policy, ctx.Files!, ctx.FileSystem, ctx.RootPath, ctx.Config?.Coverage?.Exclude);
                    ctx.Formatter.WriteMessage("");
                    ctx.Formatter.WriteMessage(
                        $"{OutputStyler.Style(ctx.Formatter, "Governance Coverage:", CliTextStyle.Heading)} {coverage.GovernedCount}/{coverage.TotalMarkdownFiles} Markdown files ({coverage.Percentage:F0}%)");
                    if (coverage.Ungoverned.Count > 0)
                    {
                        ctx.Formatter.WriteMessage(OutputStyler.Style(ctx.Formatter, "Ungoverned Files:", CliTextStyle.Heading));
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

    private static string FormatStatusIcon(IOutputFormatter formatter, string status)
    {
        return $"[{OutputStyler.StatusToken(formatter, status)}]";
    }

    internal static RepositoryStatus ComputeStatus(
        RepositoryPolicy? policy,
        string? profile,
        IFileSystem fileSystem,
        string rootPath,
        IReadOnlyList<DiscoveredFile> files)
    {
        var artifactStatuses = new List<ArtifactStatus>();
        var stateDocuments = new List<StateDocumentStatus>();
        if (policy?.Artifacts != null)
        {
            foreach (var artifact in policy.Artifacts.Where(static artifact => !string.IsNullOrWhiteSpace(artifact.Path)))
            {
                var status = BuildArtifactStatus(artifact, fileSystem, rootPath, files);
                artifactStatuses.Add(status);

                if (WellKnownRoles.IsStateDocumentRole(artifact.Role))
                {
                    stateDocuments.Add(new StateDocumentStatus
                    {
                        Path = status.Path,
                        Role = status.Role,
                        Importance = status.Importance,
                        Present = status.Present,
                        FreshnessMaxAgeDays = ResolveFreshnessDays(artifact),
                        Stale = status.Present && IsFreshnessStale(artifact, fileSystem, rootPath)
                    });
                }
            }
        }

        var requiredArtifacts = artifactStatuses
            .Where(static artifact => string.Equals(artifact.Importance, "required", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var recommendedArtifacts = artifactStatuses
            .Where(static artifact => string.Equals(artifact.Importance, "recommended", StringComparison.OrdinalIgnoreCase))
            .ToList();

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

        var familySummaries = ComputeFamilySummary(policy, files, fileSystem, rootPath);

        return new RepositoryStatus
        {
            RepositoryName = policy?.Repository?.Name,
            RepositoryType = policy?.Repository?.Type,
            Profile = profile,
            FileCount = files.Count,
            RequiredArtifacts = requiredArtifacts,
            RecommendedArtifacts = recommendedArtifacts,
            StateDocuments = stateDocuments,
            MaintenanceArtifacts = maintenanceArtifacts,
            StartHere = policy?.Governance?.StartHere ?? [],
            PresentCount = requiredArtifacts.Count(a => a.Present),
            RequiredCount = requiredArtifacts.Count,
            RecommendedPresentCount = recommendedArtifacts.Count(a => a.Present),
            RecommendedCount = recommendedArtifacts.Count,
            StaleCount = maintenanceArtifacts.Count(m => m.Stale),
            ArtifactFamilies = familySummaries
        };
    }

    internal static List<ArtifactFamilySummary> ComputeFamilySummary(
        RepositoryPolicy? policy,
        IReadOnlyList<DiscoveredFile> files,
        IFileSystem? fileSystem = null,
        string? repositoryRoot = null)
    {
        var families = policy?.ArtifactFamilies;
        if (families == null || families.Count == 0)
            return [];

        var classifier = new ArtifactFamilyClassifier(families);

        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var family in families)
        {
            if (!string.IsNullOrWhiteSpace(family.Family))
                counts[family.Family!] = 0;
        }

        foreach (var file in files.Where(f =>
            f.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)))
        {
            var matched = classifier.ClassifyFile(file.RelativePath, fileSystem, repositoryRoot);
            if (matched?.Family != null && counts.ContainsKey(matched.Family))
                counts[matched.Family]++;
        }

        return families
            .Where(f => !string.IsNullOrWhiteSpace(f.Family))
            .Select(f => new ArtifactFamilySummary
            {
                Family = f.Family!,
                DisplayName = f.DisplayName,
                MatchedCount = counts.TryGetValue(f.Family!, out var c) ? c : 0
            })
            .ToList();
    }

    internal sealed class RepositoryStatus
    {
        public string? RepositoryName { get; init; }
        public string? RepositoryType { get; init; }
        public string? Profile { get; init; }
        public int FileCount { get; init; }
        public List<ArtifactStatus> RequiredArtifacts { get; init; } = [];
        public List<ArtifactStatus> RecommendedArtifacts { get; init; } = [];
        public List<StateDocumentStatus> StateDocuments { get; init; } = [];
        public List<MaintenanceStatus> MaintenanceArtifacts { get; init; } = [];
        public List<string> StartHere { get; init; } = [];
        public int PresentCount { get; init; }
        public int RequiredCount { get; init; }
        public int RecommendedPresentCount { get; init; }
        public int RecommendedCount { get; init; }
        public int StaleCount { get; init; }
        public List<ArtifactFamilySummary> ArtifactFamilies { get; init; } = [];
        public CoverageResponse? Coverage { get; set; }
    }

    internal sealed class ArtifactFamilySummary
    {
        public required string Family { get; init; }
        public string? DisplayName { get; init; }
        public required int MatchedCount { get; init; }
    }

    internal sealed class ArtifactStatus
    {
        public required string Path { get; init; }
        public required string Role { get; init; }
        public required string Importance { get; init; }
        public required bool Present { get; init; }
    }

    internal sealed class StateDocumentStatus
    {
        public required string Path { get; init; }
        public required string Role { get; init; }
        public required string Importance { get; init; }
        public required bool Present { get; init; }
        public int? FreshnessMaxAgeDays { get; init; }
        public bool Stale { get; init; }
    }

    internal sealed class MaintenanceStatus
    {
        public required string Id { get; init; }
        public required string Path { get; init; }
        public required bool Stale { get; init; }
    }

    internal static CoverageResult ComputeCoverage(
        RepositoryPolicy? policy,
        IReadOnlyList<DiscoveredFile> files,
        IFileSystem? fileSystem = null,
        string? repositoryRoot = null,
        IReadOnlyList<string>? coverageExcludes = null)
    {
        var mdFiles = files
            .Where(f => f.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            .Select(f => PathHelper.NormalizeSeparators(f.RelativePath))
            .ToList();

        // Apply coverage exclude patterns
        if (coverageExcludes is { Count: > 0 })
        {
            var globs = coverageExcludes
                .Select(DotNet.Globbing.Glob.Parse)
                .ToList();
            mdFiles = mdFiles
                .Where(f => !globs.Any(g => g.IsMatch(f)))
                .ToList();
        }

        if (mdFiles.Count == 0)
            return new CoverageResult { TotalMarkdownFiles = 0, GovernedCount = 0, Percentage = 100, Ungoverned = [] };

        var mdFileSet = new HashSet<string>(mdFiles, StringComparer.OrdinalIgnoreCase);
        var governed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var frontier = new Queue<string>();

        void AddGovernedPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            var normalized = PathHelper.NormalizeAndTrim(path);
            if (!mdFileSet.Contains(normalized))
                return;

            if (governed.Add(normalized))
                frontier.Enqueue(normalized);
        }

        void AddGovernedDirectory(string? directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                return;

            var normalizedDir = PathHelper.NormalizeAndTrim(directoryPath);
            foreach (var path in mdFiles)
            {
                if (path.Equals(normalizedDir, StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith(normalizedDir + "/", StringComparison.OrdinalIgnoreCase))
                {
                    AddGovernedPath(path);
                }
            }
        }

        void AddGovernedSource(string? source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return;

            foreach (var path in mdFiles.Where(path => MaintenanceSourceMatcher.Matches(source, path)))
                AddGovernedPath(path);
        }

        // 1. Artifact paths
        if (policy?.Artifacts != null)
        {
            foreach (var a in policy.Artifacts)
            {
                if (!string.IsNullOrWhiteSpace(a.Path))
                {
                    if (a.Path!.EndsWith('/'))
                        AddGovernedDirectory(a.Path);
                    else
                        AddGovernedPath(a.Path);
                }

                if (!string.IsNullOrWhiteSpace(a.IndexOf))
                    AddGovernedDirectory(a.IndexOf);
            }
        }

        // 2. Maintenance scopes and maintained artifacts
        if (policy?.Maintenance?.Artifacts != null)
        {
            foreach (var ma in policy.Maintenance.Artifacts)
            {
                if (!string.IsNullOrWhiteSpace(ma.Path))
                    AddGovernedPath(ma.Path);

                AddGovernedSource(ma.Source);
            }
        }

        // 3. start_here
        if (policy?.Governance?.StartHere != null)
        {
            foreach (var s in policy.Governance.StartHere)
                AddGovernedPath(s);
        }

        // 4. Markdown files reachable from governed navigation surfaces
        if (fileSystem != null && !string.IsNullOrWhiteSpace(repositoryRoot))
        {
            while (frontier.Count > 0)
            {
                var currentPath = frontier.Dequeue();
                var fullPath = Path.Combine(repositoryRoot!, currentPath);
                if (!fileSystem.FileExists(fullPath))
                    continue;

                var content = fileSystem.ReadAllText(fullPath);
                var links = BrokenInternalLinkRule.ExtractInternalLinks(content);

                foreach (var (target, _) in links)
                {
                    var resolved = BrokenInternalLinkRule.ResolveLinkTarget(currentPath, target);
                    if (resolved != null)
                        AddGovernedPath(resolved);
                }
            }
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

    internal sealed class CoverageResponse
    {
        public int GovernedCount { get; init; }
        public int TotalMarkdownFiles { get; init; }
        public double Percentage { get; init; }
        public List<string> Ungoverned { get; init; } = [];
    }

    private static ArtifactStatus BuildArtifactStatus(
        ArtifactDefinition artifact,
        IFileSystem fileSystem,
        string rootPath,
        IReadOnlyList<DiscoveredFile> files)
    {
        return new ArtifactStatus
        {
            Path = artifact.Path ?? "",
            Role = artifact.Role ?? "",
            Importance = artifact.ResolveImportance(),
            Present = IsArtifactPresent(artifact, fileSystem, rootPath, files)
        };
    }

    private static bool IsArtifactPresent(
        ArtifactDefinition artifact,
        IFileSystem fileSystem,
        string rootPath,
        IReadOnlyList<DiscoveredFile> files)
    {
        if (string.IsNullOrWhiteSpace(artifact.Path))
            return false;

        var normalizedPath = PathHelper.NormalizeAndTrim(artifact.Path);
        if (artifact.Path.EndsWith('/'))
        {
            return files.Any(file =>
                file.RelativePath.StartsWith(normalizedPath + "/", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(file.RelativePath, normalizedPath, StringComparison.OrdinalIgnoreCase));
        }

        if (files.Any(file => string.Equals(file.RelativePath, normalizedPath, StringComparison.OrdinalIgnoreCase)))
            return true;

        return fileSystem.FileExists(Path.Combine(rootPath, normalizedPath));
    }

    private static int? ResolveFreshnessDays(ArtifactDefinition artifact)
    {
        if (artifact.Freshness?.MaxAgeDays > 0)
            return artifact.Freshness.MaxAgeDays;

        return RoleDefaults.GetDefaultFreshnessDays(artifact.Role);
    }

    private static bool IsFreshnessStale(ArtifactDefinition artifact, IFileSystem fileSystem, string rootPath)
    {
        var maxAgeDays = ResolveFreshnessDays(artifact);
        if (maxAgeDays is null or <= 0 || string.IsNullOrWhiteSpace(artifact.Path))
            return false;

        var fullPath = Path.Combine(rootPath, PathHelper.NormalizeSeparators(artifact.Path));
        if (!fileSystem.FileExists(fullPath))
            return false;

        var lastModified = FrontmatterEditor.TryGetLastUpdatedDate(fileSystem, fullPath) ?? fileSystem.GetLastWriteTimeUtc(fullPath);
        return (DateTime.UtcNow - lastModified).TotalDays > maxAgeDays.Value;
    }

}
