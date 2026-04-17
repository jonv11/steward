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

    public static Suggestion Analyze(IReadOnlyList<DiscoveredFile> files, IFileSystem fileSystem, string repositoryRoot)
    {
        var suggestion = new Suggestion();
        var relPaths = new HashSet<string>(
            files.Select(f => PathHelper.NormalizeSeparators(f.RelativePath)),
            StringComparer.OrdinalIgnoreCase);

        // 1. Well-known files → artifact suggestions
        foreach (var (pattern, (role, importance)) in WellKnownFiles)
        {
            if (relPaths.Contains(pattern))
            {
                suggestion.Artifacts.Add(new ArtifactSuggestion
                {
                    Path = pattern,
                    Role = role,
                    Importance = importance,
                    Reason = "Well-known repository file"
                });
            }
        }

        // 2. Start-here heuristic: README.md, then files in docs/ root
        if (relPaths.Contains("README.md"))
            suggestion.StartHere.Add("README.md");

        var docsIndex = files
            .Where(f => PathHelper.NormalizeSeparators(f.RelativePath).Equals("docs/README.md", StringComparison.OrdinalIgnoreCase) ||
                        PathHelper.NormalizeSeparators(f.RelativePath).Equals("docs/index.md", StringComparison.OrdinalIgnoreCase))
            .Select(f => PathHelper.NormalizeSeparators(f.RelativePath))
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
            suggestion.Artifacts.Add(new ArtifactSuggestion
            {
                Path = docsIndex,
                Role = "authoritative",
                Importance = "recommended",
                Reason = "Documentation index file"
            });
        }

        // 4. Requirements/PRD detection
        foreach (var f in files)
        {
            var rel = PathHelper.NormalizeSeparators(f.RelativePath);
            var name = Path.GetFileName(rel).ToLowerInvariant();
            if (name is "prd.md" or "requirements.md" or "spec.md" or "specification.md")
            {
                if (!suggestion.Artifacts.Any(a => string.Equals(a.Path, rel, StringComparison.OrdinalIgnoreCase)))
                {
                    suggestion.Artifacts.Add(new ArtifactSuggestion
                    {
                        Path = rel,
                        Role = "requirements",
                        Importance = "required",
                        Reason = "Requirements/specification document"
                    });
                }
            }
        }

        // 5. Decisions directory (ADR/RFC patterns)
        DetectDecisionDirectory(files, suggestion);

        // 6. Planning documents
        DetectPlanningDocuments(files, suggestion);

        // 7. State documents (milestone plans, status trackers, etc.)
        DetectStateDocuments(files, suggestion);

        // 8. Index files in subdirectories
        DetectIndexFiles(files, suggestion);

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

    private static void DetectDecisionDirectory(IReadOnlyList<DiscoveredFile> files, Suggestion suggestion)
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
                if (!suggestion.Artifacts.Any(a => string.Equals(a.Path, rel, StringComparison.OrdinalIgnoreCase)))
                {
                    suggestion.Artifacts.Add(new ArtifactSuggestion
                    {
                        Path = rel,
                        Role = "index",
                        Importance = "recommended",
                        Reason = "Decision records index"
                    });
                }
            }
        }
    }

    private static void DetectPlanningDocuments(IReadOnlyList<DiscoveredFile> files, Suggestion suggestion)
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
            var name = Path.GetFileName(rel).ToLowerInvariant();

            if (planningPatterns.TryGetValue(name, out var match))
            {
                if (!suggestion.Artifacts.Any(a => string.Equals(a.Path, rel, StringComparison.OrdinalIgnoreCase)))
                {
                    suggestion.Artifacts.Add(new ArtifactSuggestion
                    {
                        Path = rel,
                        Role = match.Role,
                        Importance = "optional",
                        Reason = match.Reason
                    });
                }
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
                if (!suggestion.Artifacts.Any(a => string.Equals(a.Path, rel, StringComparison.OrdinalIgnoreCase)))
                {
                    suggestion.Artifacts.Add(new ArtifactSuggestion
                    {
                        Path = rel,
                        Role = "index",
                        Importance = "optional",
                        Reason = "Planning documents index"
                    });
                }
            }
        }
    }

    private static void DetectStateDocuments(IReadOnlyList<DiscoveredFile> files, Suggestion suggestion)
    {
        // Files with state-tracking patterns in their names
        foreach (var f in files)
        {
            var rel = PathHelper.NormalizeSeparators(f.RelativePath);
            if (!rel.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) continue;

            var name = Path.GetFileNameWithoutExtension(rel).ToLowerInvariant();

            // Pattern: *-status.md, *-tracker.md, *-progress.md
            if (name.EndsWith("-status") || name.EndsWith("-tracker") || name.EndsWith("-progress"))
            {
                if (!suggestion.Artifacts.Any(a => string.Equals(a.Path, rel, StringComparison.OrdinalIgnoreCase)))
                {
                    suggestion.Artifacts.Add(new ArtifactSuggestion
                    {
                        Path = rel,
                        Role = "state-document",
                        Importance = "optional",
                        Reason = "State tracking document"
                    });
                }
            }
        }
    }

    private static void DetectIndexFiles(IReadOnlyList<DiscoveredFile> files, Suggestion suggestion)
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
            if (suggestion.Artifacts.Any(a => string.Equals(a.Path, rel, StringComparison.OrdinalIgnoreCase)))
                continue;

            var dir = PathHelper.NormalizeSeparators(Path.GetDirectoryName(rel) ?? "");
            if (string.IsNullOrEmpty(dir)) continue;

            suggestion.Artifacts.Add(new ArtifactSuggestion
            {
                Path = rel,
                Role = "index",
                Importance = "optional",
                Reason = $"Index file for {dir}/"
            });
        }
    }
}
