using System.Text;
using DotNet.Globbing;

namespace Steward.Core.Maintenance;

/// <summary>
/// Generates a tree-view structure document from file discovery.
/// </summary>
public sealed class StructureDocumentMaintainer : IArtifactMaintainer
{
    public string Type => "structure-document";

    public MaintenanceAction Evaluate(MaintenanceArtifactConfig config, MaintenanceContext context)
    {
        var depth = config.Options?.Depth ?? 3;
        var excludeGlobs = (config.Options?.Exclude ?? [])
            .Select(e => Glob.Parse(e))
            .ToList();

        var paths = context.Files
            .Where(f => !excludeGlobs.Any(g => g.IsMatch(f.RelativePath)))
            .Select(f => f.RelativePath)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var tree = BuildTree(paths, depth);
        var expected = GenerateDocument(config, tree);

        var fullPath = Path.Combine(context.RepositoryRoot, config.Path);
        var current = context.FileSystem.FileExists(fullPath)
            ? context.FileSystem.ReadAllText(fullPath)
            : null;

        var hasChanges = current != expected;
        return new MaintenanceAction
        {
            ArtifactId = config.Id,
            ArtifactPath = config.Path,
            Type = Type,
            Description = hasChanges
                ? "Structure document needs updating."
                : "Structure document is up to date.",
            HasChanges = hasChanges,
            ExpectedContent = expected,
            CurrentContent = current
        };
    }

    private static string GenerateDocument(MaintenanceArtifactConfig config, List<TreeNode> nodes)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Repository Structure");
        sb.AppendLine();
        sb.AppendLine("```");
        WriteTree(sb, nodes, "");
        sb.AppendLine("```");
        return sb.ToString();
    }

    private static void WriteTree(StringBuilder sb, List<TreeNode> nodes, string prefix)
    {
        for (var i = 0; i < nodes.Count; i++)
        {
            var isLast = i == nodes.Count - 1;
            var connector = isLast ? "└── " : "├── ";
            var childPrefix = isLast ? "    " : "│   ";

            sb.AppendLine($"{prefix}{connector}{nodes[i].Name}{(nodes[i].Children.Count > 0 ? "/" : "")}");
            WriteTree(sb, nodes[i].Children, prefix + childPrefix);
        }
    }

    private static List<TreeNode> BuildTree(List<string> paths, int maxDepth)
    {
        var root = new TreeNode { Name = "" };

        foreach (var path in paths)
        {
            var parts = path.Replace('\\', '/').Split('/');
            var current = root;
            var limit = Math.Min(parts.Length, maxDepth);

            for (var i = 0; i < limit; i++)
            {
                var existing = current.Children.FirstOrDefault(
                    c => string.Equals(c.Name, parts[i], StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                {
                    existing = new TreeNode { Name = parts[i] };
                    current.Children.Add(existing);
                }
                current = existing;
            }
        }

        return root.Children;
    }

    private sealed class TreeNode
    {
        public string Name { get; set; } = "";
        public List<TreeNode> Children { get; } = [];
    }
}
