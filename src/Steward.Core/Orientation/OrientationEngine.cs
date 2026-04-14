using Steward.Core.Discovery;

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

    public OrientationResult Orient(string repositoryRoot, IReadOnlyList<DiscoveredFile> files, int maxDepth = 3)
    {
        var entries = BuildHierarchy(files, maxDepth);
        return new OrientationResult
        {
            RepositoryRoot = repositoryRoot,
            Entries = entries
        };
    }

    private List<OrientationEntry> BuildHierarchy(IReadOnlyList<DiscoveredFile> files, int maxDepth)
    {
        var rootEntries = new List<OrientationEntry>();

        foreach (var file in files)
        {
            var depth = file.RelativePath.Count(c => c == '/');
            if (depth >= maxDepth) continue;

            var classification = Classify(file);

            rootEntries.Add(new OrientationEntry
            {
                Path = file.RelativePath,
                Classification = classification,
                IsDirectory = file.IsDirectory,
                Depth = depth
            });
        }

        return rootEntries;
    }

    public static string Classify(DiscoveredFile file)
    {
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
}
