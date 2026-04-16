using System.Text;
using DotNet.Globbing;
using Steward.Core.Markdown;

namespace Steward.Core.Maintenance;

/// <summary>
/// Generates an index of files matching a glob, with headings and optional frontmatter.
/// </summary>
public sealed class IndexMaintainer : IArtifactMaintainer
{
    public string Type => "index";

    public MaintenanceAction Evaluate(MaintenanceArtifactConfig config, MaintenanceContext context)
    {
        var sourceGlob = Glob.Parse(config.Source ?? "**/*.md");
        var sort = config.Sort ?? "filename";

        var matchingFiles = context.Files
            .Where(f => !f.IsDirectory && sourceGlob.IsMatch(f.RelativePath))
            .ToList();

        // Sort
        matchingFiles = sort switch
        {
            "filename" => matchingFiles.OrderBy(f => Path.GetFileName(f.RelativePath), StringComparer.OrdinalIgnoreCase).ToList(),
            "path" => matchingFiles.OrderBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase).ToList(),
            _ => throw new ArgumentException($"Invalid sort value '{sort}'. Expected 'filename' or 'path'.")
        };

        // Generate index entries
        var entries = new List<string>();
        foreach (var file in matchingFiles)
        {
            var fullPath = Path.Combine(context.RepositoryRoot, file.RelativePath);
            var title = file.RelativePath;

            if (context.FileSystem.FileExists(fullPath) &&
                file.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                var doc = context.DocumentCache?.GetOrParse(file.RelativePath)
                    ?? MarkdownParser.Parse(fullPath, context.FileSystem.ReadAllText(fullPath));

                if (doc.Frontmatter?.Fields.TryGetValue("title", out var t) == true && t != null)
                    title = t.ToString()!;
                else if (doc.Sections.Count > 0)
                    title = doc.Sections[0].Heading;
            }

            entries.Add($"- [{title}]({file.RelativePath.Replace('\\', '/')})");
        }

        var expectedSection = string.Join('\n', entries);

        // If there's a managed section, update within the target file
        if (config.ManagedSection != null)
        {
            return UpdateManagedSection(config, context, expectedSection);
        }

        // Otherwise generate full index file
        var expected = GenerateFullIndex(config, entries);
        var targetPath = Path.Combine(context.RepositoryRoot, config.Path);
        var current = context.FileSystem.FileExists(targetPath)
            ? context.FileSystem.ReadAllText(targetPath)
            : null;

        var hasChanges = current != expected;
        return new MaintenanceAction
        {
            ArtifactId = config.Id,
            ArtifactPath = config.Path,
            Type = Type,
            Description = hasChanges
                ? $"Index needs updating ({matchingFiles.Count} entries)."
                : "Index is up to date.",
            HasChanges = hasChanges,
            ExpectedContent = expected,
            CurrentContent = current
        };
    }

    private MaintenanceAction UpdateManagedSection(
        MaintenanceArtifactConfig config, MaintenanceContext context, string expectedContent)
    {
        var targetPath = Path.Combine(context.RepositoryRoot, config.Path);
        if (!context.FileSystem.FileExists(targetPath))
        {
            return new MaintenanceAction
            {
                ArtifactId = config.Id,
                ArtifactPath = config.Path,
                Type = Type,
                Description = $"Target file '{config.Path}' does not exist.",
                HasChanges = false
            };
        }

        var content = context.FileSystem.ReadAllText(targetPath);
        var expected = ManagedRegionRewriter.Replace(content, config.ManagedSection!, expectedContent);

        if (expected == null)
        {
            return new MaintenanceAction
            {
                ArtifactId = config.Id,
                ArtifactPath = config.Path,
                Type = Type,
                Description = $"Managed section '{config.ManagedSection}' not found in '{config.Path}'.",
                HasChanges = false
            };
        }

        var hasChanges = content != expected;

        return new MaintenanceAction
        {
            ArtifactId = config.Id,
            ArtifactPath = config.Path,
            Type = Type,
            Description = hasChanges
                ? $"Managed section '{config.ManagedSection}' needs updating."
                : "Managed section is up to date.",
            HasChanges = hasChanges,
            ExpectedContent = expected,
            CurrentContent = content
        };
    }

    private static string GenerateFullIndex(MaintenanceArtifactConfig config, List<string> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Index");
        sb.AppendLine();
        foreach (var entry in entries)
        {
            sb.AppendLine(entry);
        }
        return sb.ToString();
    }
}
