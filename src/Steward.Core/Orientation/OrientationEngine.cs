using Steward.Core.Discovery;
using Steward.Core.Configuration;

namespace Steward.Core.Orientation;

public sealed class OrientationEngine
{
    private static readonly Dictionary<string, string> ExactClassifications = new(StringComparer.OrdinalIgnoreCase)
    {
        ["README.md"] = "authoritative",
        ["README"] = "authoritative",
        ["LICENSE"] = "authoritative",
        ["LICENSE.md"] = "authoritative",
        ["LICENSE.txt"] = "authoritative",
        ["CHANGELOG.md"] = "changelog",
        ["CHANGES.md"] = "changelog",
        ["CONTRIBUTING.md"] = "governance",
        ["CODE_OF_CONDUCT.md"] = "governance",
        ["SECURITY.md"] = "governance",
        [".gitignore"] = "configuration",
        [".editorconfig"] = "configuration",
        [".gitattributes"] = "configuration",
    };

    private static readonly Dictionary<string, string> DirectoryClassifications = new(StringComparer.OrdinalIgnoreCase)
    {
        ["docs"] = "documentation",
        ["doc"] = "documentation",
        ["documentation"] = "documentation",
        ["src"] = "source",
        ["lib"] = "source",
        ["source"] = "source",
        ["tests"] = "testing",
        ["test"] = "testing",
        ["spec"] = "testing",
        [".github"] = "workflow",
        [".azuredevops"] = "workflow",
        [".gitlab"] = "workflow",
        ["scripts"] = "tooling",
        ["tools"] = "tooling",
        ["build"] = "tooling",
        [".steward"] = "configuration",
        [".vscode"] = "configuration",
        [".idea"] = "configuration",
        ["examples"] = "documentation",
        ["samples"] = "documentation",
        ["assets"] = "resource",
        ["images"] = "resource",
        ["resources"] = "resource",
    };

    public OrientationResult Orient(
        string repositoryRoot,
        IReadOnlyList<DiscoveredFile> files,
        RepositoryPolicy? policy = null,
        string? profile = null,
        int maxDepth = 3,
        OrientationSignalInput? signals = null)
    {
        var startHere = policy?.Governance?.StartHere ?? [];
        var entries = BuildHierarchy(files, policy, startHere, maxDepth);
        return new OrientationResult
        {
            RepositoryRoot = repositoryRoot,
            RepositoryName = policy?.Repository?.Name,
            RepositoryType = policy?.Repository?.Type,
            Profile = profile,
            StartHere = [.. startHere],
            Signals = BuildSignals(signals),
            Entries = entries
        };
    }

    private static List<OrientationEntry> BuildHierarchy(
        IReadOnlyList<DiscoveredFile> files,
        RepositoryPolicy? policy,
        IReadOnlyList<string> startHere,
        int maxDepth)
    {
        var rootEntries = new List<OrientationEntry>();
        var startHereSet = new HashSet<string>(startHere, StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            var depth = file.RelativePath.Count(c => c == '/');
            if (depth >= maxDepth) continue;

            var classification = Classify(file, policy);

            rootEntries.Add(new OrientationEntry
            {
                Path = file.RelativePath,
                Classification = classification,
                IsDirectory = file.IsDirectory,
                Depth = depth,
                IsStartHere = startHereSet.Contains(file.RelativePath)
            });
        }

        return rootEntries;
    }

    public static string Classify(DiscoveredFile file, RepositoryPolicy? policy = null)
    {
        var configuredRole = ResolveConfiguredRole(file, policy);
        if (configuredRole != null)
        {
            // State-document roles get a "state:" prefix for distinct display
            if (WellKnownRoles.IsStateDocumentRole(configuredRole))
                return $"state:{configuredRole}";
            return configuredRole;
        }

        var fileName = Path.GetFileName(file.RelativePath);

        // Check exact file matches
        if (!file.IsDirectory && ExactClassifications.TryGetValue(fileName, out var exact))
            return exact;

        // Check top-level directory matches
        var topDir = file.RelativePath.Split('/')[0];
        if (DirectoryClassifications.TryGetValue(topDir, out var dirClass))
            return dirClass;

        // Check directory-specific matches
        if (file.IsDirectory && DirectoryClassifications.TryGetValue(fileName, out var dirSpecific))
            return dirSpecific;

        // Fallback heuristics by extension
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".md" or ".rst" or ".txt" or ".adoc" => "documentation",
            ".cs" or ".fs" or ".vb" or ".java" or ".py" or ".ts" or ".js" or ".go" or ".rs" => "source",
            ".csproj" or ".fsproj" or ".sln" or ".slnx" => "project",
            ".json" or ".yaml" or ".yml" or ".toml" or ".xml" or ".props" or ".targets" => "configuration",
            ".sh" or ".ps1" or ".bat" or ".cmd" => "tooling",
            ".png" or ".jpg" or ".gif" or ".svg" or ".ico" => "resource",
            _ => "other"
        };
    }

    private static string? ResolveConfiguredRole(DiscoveredFile file, RepositoryPolicy? policy)
    {
        if (policy?.Artifacts == null)
            return null;

        foreach (var artifact in policy.Artifacts)
        {
            if (string.IsNullOrWhiteSpace(artifact.Path) || string.IsNullOrWhiteSpace(artifact.Role))
                continue;

            var policyPath = artifact.Path!.TrimEnd('/');
            var isDirectory = artifact.Path.EndsWith('/');
            var isMatch = isDirectory
                ? file.IsDirectory && string.Equals(file.RelativePath, policyPath, StringComparison.OrdinalIgnoreCase)
                : string.Equals(file.RelativePath, policyPath, StringComparison.OrdinalIgnoreCase);

            if (isMatch)
                return artifact.Role;
        }

        return null;
    }

    private static List<OrientationSignal> BuildSignals(OrientationSignalInput? signals)
    {
        if (signals == null)
            return [];

        var result = new List<OrientationSignal>();

        result.AddRange(signals.MissingRequiredArtifacts
            .Select(static artifact => new OrientationSignal
            {
                Code = "missing-required-artifact",
                Severity = "error",
                Path = artifact.Path,
                Message = artifact.Message
            }));

        result.AddRange(signals.StaleArtifacts
            .Select(static artifact => new OrientationSignal
            {
                Code = "stale-artifact",
                Severity = "warn",
                Path = artifact.Path,
                Message = artifact.Message
            }));

        return result;
    }
}
