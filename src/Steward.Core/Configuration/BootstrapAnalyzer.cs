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
            files.Select(f => f.RelativePath.Replace('\\', '/')),
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
            .Where(f => f.RelativePath.Replace('\\', '/').Equals("docs/README.md", StringComparison.OrdinalIgnoreCase) ||
                        f.RelativePath.Replace('\\', '/').Equals("docs/index.md", StringComparison.OrdinalIgnoreCase))
            .Select(f => f.RelativePath.Replace('\\', '/'))
            .FirstOrDefault();

        if (docsIndex != null)
            suggestion.StartHere.Add(docsIndex);

        // 3. Docs directory artifacts
        var docsFiles = files
            .Where(f => f.RelativePath.Replace('\\', '/').StartsWith("docs/", StringComparison.OrdinalIgnoreCase)
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
            var rel = f.RelativePath.Replace('\\', '/');
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

        // 5. Exclude patterns
        foreach (var exclude in CommonExcludes)
        {
            var trimmed = exclude.TrimEnd('/');
            if (files.Any(f => f.RelativePath.Replace('\\', '/').StartsWith(trimmed + "/", StringComparison.OrdinalIgnoreCase)))
            {
                suggestion.ExcludePatterns.Add(exclude);
            }
        }

        return suggestion;
    }
}
