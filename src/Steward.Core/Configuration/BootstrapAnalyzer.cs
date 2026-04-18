using DotNet.Globbing;
using Steward.Core;
using Steward.Core.Abstractions;
using Steward.Core.Discovery;

namespace Steward.Core.Configuration;

/// <summary>
/// Analyzes a repository and suggests initial governance configuration.
/// Suggestions are preview-only (never auto-applied).
/// </summary>
public static class BootstrapAnalyzer
{
    public sealed class Suggestion
    {
        public List<string> StartHere { get; init; } = [];
        public List<ArtifactSuggestion> Artifacts { get; init; } = [];
        public List<string> ExcludePatterns { get; init; } = [];
    }

    public sealed class ArtifactSuggestion
    {
        public required string Path { get; init; }
        public required string Role { get; init; }
        public required string Importance { get; init; }
        public string? Reason { get; init; }
        public required string Confidence { get; init; }
        public bool Conservative { get; init; }
    }

    private static readonly Dictionary<string, (string Role, string Importance)> WellKnownFiles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["README.md"]         = ("authoritative", "required"),
            ["CONTRIBUTING.md"]   = ("guide", "recommended"),
            ["CHANGELOG.md"]     = ("changelog", "optional"),
            ["LICENSE"]          = ("authoritative", "required"),
            ["LICENSE.md"]       = ("authoritative", "required"),
            ["CODE_OF_CONDUCT.md"] = ("guide", "optional"),
            ["SECURITY.md"]      = ("guide", "recommended"),
            ["ARCHITECTURE.md"]  = ("authoritative", "recommended"),
            ["STRUCTURE.md"]     = ("generated", "recommended"),
        };

    private static readonly string[] CommonExcludes =
    [
        "node_modules/", "bin/", "obj/", ".git/", "dist/", "build/",
        "vendor/", "__pycache__/", ".next/", "coverage/"
    ];

    private static readonly HashSet<string> LowTrustPathSegments =
    [
        "test",
        "tests",
        "fixture",
        "fixtures",
        "sample",
        "samples",
        "example",
        "examples",
        "testdata",
        "snapshot",
        "snapshots"
    ];

    public static Suggestion Analyze(
        IReadOnlyList<DiscoveredFile> files,
        IFileSystem fileSystem,
        string repositoryRoot,
        RepositoryPolicy? policy = null)
    {
        var suggestion = new Suggestion();
        var suppressedSuggestionGlobs = CompileSuppressedSuggestionGlobs(policy);
        var relPaths = new HashSet<string>(
            files.Select(f => PathHelper.NormalizeSeparators(f.RelativePath)),
            StringComparer.OrdinalIgnoreCase);

        // 1. Well-known files → artifact suggestions
        foreach (var (pattern, (role, importance)) in WellKnownFiles)
        {
            if (relPaths.Contains(pattern) && !ShouldSkipSuggestionPath(pattern, suppressedSuggestionGlobs))
            {
                AddArtifactSuggestion(
                    suggestion,
                    pattern,
                    role,
                    importance,
                    "Well-known repository file",
                    confidence: "high");
            }
        }

        // 2. Start-here heuristic: README.md, then files in docs/ root
        if (relPaths.Contains("README.md") && !ShouldSkipSuggestionPath("README.md", suppressedSuggestionGlobs))
            suggestion.StartHere.Add("README.md");

        var docsIndex = files
            .Where(f => PathHelper.NormalizeSeparators(f.RelativePath).Equals("docs/README.md", StringComparison.OrdinalIgnoreCase) ||
                        PathHelper.NormalizeSeparators(f.RelativePath).Equals("docs/index.md", StringComparison.OrdinalIgnoreCase))
            .Select(f => PathHelper.NormalizeSeparators(f.RelativePath))
            .Where(path => !ShouldSkipSuggestionPath(path, suppressedSuggestionGlobs))
            .FirstOrDefault();

        if (docsIndex != null)
            suggestion.StartHere.Add(docsIndex);

        // 3. Docs directory artifacts
        var docsFiles = files
            .Where(f => PathHelper.NormalizeSeparators(f.RelativePath).StartsWith("docs/", StringComparison.OrdinalIgnoreCase)
                        && f.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (docsFiles.Count > 0 && docsIndex != null)
        {
            AddArtifactSuggestion(
                suggestion,
                docsIndex,
                "authoritative",
                "recommended",
                "Documentation index file",
                confidence: "high");
        }

        // 4. Requirements/PRD detection
        foreach (var f in files)
        {
            var rel = PathHelper.NormalizeSeparators(f.RelativePath);
            if (ShouldSkipSuggestionPath(rel, suppressedSuggestionGlobs))
                continue;

            var name = Path.GetFileName(rel).ToLowerInvariant();
            if (name is "prd.md" or "requirements.md" or "spec.md" or "specification.md")
            {
                AddArtifactSuggestion(
                    suggestion,
                    rel,
                    "requirements",
                    "required",
                    "Requirements/specification document",
                    confidence: "high");
            }
        }

        // 5. Decisions directory (ADR/RFC patterns)
        DetectDecisionDirectory(files, suggestion, suppressedSuggestionGlobs);

        // 6. Planning documents
        DetectPlanningDocuments(files, suggestion, suppressedSuggestionGlobs);

        // 7. State documents (milestone plans, status trackers, etc.)
        DetectStateDocuments(files, suggestion, suppressedSuggestionGlobs);

        // 8. Index files in subdirectories
        DetectIndexFiles(files, suggestion, suppressedSuggestionGlobs);

        // 9. Exclude patterns
        foreach (var exclude in CommonExcludes)
        {
            var trimmed = exclude.TrimEnd('/');
            if (files.Any(f => PathHelper.NormalizeSeparators(f.RelativePath).StartsWith(trimmed + "/", StringComparison.OrdinalIgnoreCase)))
            {
                suggestion.ExcludePatterns.Add(exclude);
            }
        }

        return suggestion;
    }

    private static void DetectDecisionDirectory(
        IReadOnlyList<DiscoveredFile> files,
        Suggestion suggestion,
        IReadOnlyList<Glob> suppressedSuggestionGlobs)
    {
        // Detect ADR/RFC directories and their index files
        var decisionDirs = new[] { "docs/decisions", "docs/adrs", "docs/rfcs", "decisions", "adrs", "rfcs",
                                    "docs/decisions/adrs", "docs/decisions/rfcs" };

        foreach (var dir in decisionDirs)
        {
            var dirFiles = files.Where(f =>
                PathHelper.NormalizeSeparators(f.RelativePath).StartsWith(dir + "/", StringComparison.OrdinalIgnoreCase) &&
                f.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)).ToList();

            if (dirFiles.Count == 0) continue;

            // Look for an index file in the decision directory
            var indexFile = dirFiles.FirstOrDefault(f =>
            {
                var name = Path.GetFileName(f.RelativePath).ToLowerInvariant();
                return name is "index.md" or "readme.md" or "decision-index.md";
            });

            if (indexFile != null)
            {
                var rel = PathHelper.NormalizeSeparators(indexFile.RelativePath);
                if (ShouldSkipSuggestionPath(rel, suppressedSuggestionGlobs))
                    continue;

                AddArtifactSuggestion(
                    suggestion,
                    rel,
                    "index",
                    "recommended",
                    "Decision records index",
                    confidence: "medium",
                    conservative: true);
            }
        }
    }

    private static void DetectPlanningDocuments(
        IReadOnlyList<DiscoveredFile> files,
        Suggestion suggestion,
        IReadOnlyList<Glob> suppressedSuggestionGlobs)
    {
        var planningPatterns = new Dictionary<string, (string Role, string Reason)>(StringComparer.OrdinalIgnoreCase)
        {
            ["milestone-plan.md"] = ("state-document", "Milestone tracking document"),
            ["delivery-strategy.md"] = ("authoritative", "Delivery strategy document"),
            ["implementation-status.md"] = ("state-document", "Implementation status tracker"),
            ["pre-release-blockers.md"] = ("state-document", "Pre-release blockers tracker"),
            ["release-publication-checklist.md"] = ("guide", "Release checklist"),
            ["roadmap.md"] = ("state-document", "Project roadmap"),
        };

        foreach (var f in files)
        {
            var rel = PathHelper.NormalizeSeparators(f.RelativePath);
            if (ShouldSkipSuggestionPath(rel, suppressedSuggestionGlobs))
                continue;

            var name = Path.GetFileName(rel).ToLowerInvariant();

            if (planningPatterns.TryGetValue(name, out var match))
            {
                AddArtifactSuggestion(
                    suggestion,
                    rel,
                    match.Role,
                    "optional",
                    match.Reason,
                    confidence: "medium",
                    conservative: true);
            }
        }

        // Detect planning directory index
        var planningDirs = new[] { "docs/planning", "planning" };
        foreach (var dir in planningDirs)
        {
            var planningIndex = files.FirstOrDefault(f =>
            {
                var rel = PathHelper.NormalizeSeparators(f.RelativePath);
                return rel.StartsWith(dir + "/", StringComparison.OrdinalIgnoreCase) &&
                       Path.GetFileName(rel).ToLowerInvariant() is "index.md" or "readme.md" or "planning-index.md";
            });

            if (planningIndex != null)
            {
                var rel = PathHelper.NormalizeSeparators(planningIndex.RelativePath);
                if (ShouldSkipSuggestionPath(rel, suppressedSuggestionGlobs))
                    continue;

                AddArtifactSuggestion(
                    suggestion,
                    rel,
                    "index",
                    "optional",
                    "Planning documents index",
                    confidence: "medium",
                    conservative: true);
            }
        }
    }

    private static void DetectStateDocuments(
        IReadOnlyList<DiscoveredFile> files,
        Suggestion suggestion,
        IReadOnlyList<Glob> suppressedSuggestionGlobs)
    {
        // Files with state-tracking patterns in their names
        foreach (var f in files)
        {
            var rel = PathHelper.NormalizeSeparators(f.RelativePath);
            if (!rel.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) continue;
            if (ShouldSkipSuggestionPath(rel, suppressedSuggestionGlobs))
                continue;

            var name = Path.GetFileNameWithoutExtension(rel).ToLowerInvariant();

            // Pattern: *-status.md, *-tracker.md, *-progress.md
            if (name.EndsWith("-status") || name.EndsWith("-tracker") || name.EndsWith("-progress"))
            {
                AddArtifactSuggestion(
                    suggestion,
                    rel,
                    "state-document",
                    "optional",
                    "State tracking document",
                    confidence: "medium",
                    conservative: true);
            }
        }
    }

    private static void DetectIndexFiles(
        IReadOnlyList<DiscoveredFile> files,
        Suggestion suggestion,
        IReadOnlyList<Glob> suppressedSuggestionGlobs)
    {
        // Detect index.md files in subdirectories (not already suggested)
        var indexFiles = files
            .Where(f =>
            {
                var rel = PathHelper.NormalizeSeparators(f.RelativePath);
                var name = Path.GetFileName(rel).ToLowerInvariant();
                return name == "index.md" && rel.Contains('/');
            })
            .ToList();

        foreach (var f in indexFiles)
        {
            var rel = PathHelper.NormalizeSeparators(f.RelativePath);
            if (ShouldSkipSuggestionPath(rel, suppressedSuggestionGlobs))
                continue;

            if (suggestion.Artifacts.Any(a => string.Equals(a.Path, rel, StringComparison.OrdinalIgnoreCase)))
                continue;

            var dir = PathHelper.NormalizeSeparators(Path.GetDirectoryName(rel) ?? "");
            if (string.IsNullOrEmpty(dir)) continue;

            AddArtifactSuggestion(
                suggestion,
                rel,
                "index",
                "optional",
                $"Index file for {dir}/",
                confidence: "low",
                conservative: true);
        }
    }

    private static void AddArtifactSuggestion(
        Suggestion suggestion,
        string path,
        string role,
        string importance,
        string reason,
        string confidence,
        bool conservative = false)
    {
        if (suggestion.Artifacts.Any(a => string.Equals(a.Path, path, StringComparison.OrdinalIgnoreCase)))
            return;

        suggestion.Artifacts.Add(new ArtifactSuggestion
        {
            Path = path,
            Role = role,
            Importance = importance,
            Reason = reason,
            Confidence = confidence,
            Conservative = conservative
        });
    }

    private static IReadOnlyList<Glob> CompileSuppressedSuggestionGlobs(RepositoryPolicy? policy)
    {
        if (policy?.Validation?.PathOverrides == null || policy.Validation.PathOverrides.Count == 0)
            return [];

        var globs = new List<Glob>();
        foreach (var pathOverride in policy.Validation.PathOverrides)
        {
            if (string.IsNullOrWhiteSpace(pathOverride.Pattern))
                continue;

            try
            {
                globs.Add(Glob.Parse(pathOverride.Pattern));
            }
            catch
            {
                // config validate should catch invalid patterns; ignore defensively here.
            }
        }

        return globs;
    }

    private static bool ShouldSkipSuggestionPath(string relativePath, IReadOnlyList<Glob> suppressedSuggestionGlobs)
    {
        if (suppressedSuggestionGlobs.Any(glob => glob.IsMatch(relativePath)))
            return true;

        var segments = PathHelper.NormalizeSeparators(relativePath)
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        foreach (var segment in segments)
        {
            if (LowTrustPathSegments.Contains(segment.ToLowerInvariant()))
                return true;
        }

        return false;
    }
}
