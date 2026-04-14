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
            _ => matchingFiles.OrderBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase).ToList()
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
                var content = context.FileSystem.ReadAllText(fullPath);
                var doc = MarkdownParser.Parse(fullPath, content);

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

    private static MaintenanceAction UpdateManagedSection(
        MaintenanceArtifactConfig config, MaintenanceContext context, string expectedContent)
    {
        var targetPath = Path.Combine(context.RepositoryRoot, config.Path);
        if (!context.FileSystem.FileExists(targetPath))
        {
            return new MaintenanceAction
            {
                ArtifactId = config.Id,
                ArtifactPath = config.Path,
                Type = "index",
                Description = $"Target file '{config.Path}' does not exist.",
                HasChanges = false
            };
        }

        var content = context.FileSystem.ReadAllText(targetPath);
        var lines = content.Split('\n').ToList();
        var beginMarker = $"<!-- steward:begin id=\"{config.ManagedSection}\" owner=\"steward\" -->";
        var endMarker = "<!-- steward:end -->";

        var beginIdx = lines.FindIndex(l => l.TrimEnd('\r').Contains(beginMarker, StringComparison.OrdinalIgnoreCase));
        var endIdx = beginIdx >= 0
            ? lines.FindIndex(beginIdx + 1, l => l.TrimEnd('\r').Contains(endMarker, StringComparison.OrdinalIgnoreCase))
            : -1;

        if (beginIdx < 0 || endIdx < 0)
        {
            return new MaintenanceAction
            {
                ArtifactId = config.Id,
                ArtifactPath = config.Path,
                Type = "index",
                Description = $"Managed section '{config.ManagedSection}' not found in '{config.Path}'.",
                HasChanges = false
            };
        }

        // Replace content between markers
        var newLines = new List<string>(lines.Take(beginIdx + 1));
        newLines.AddRange(expectedContent.Split('\n'));
        newLines.AddRange(lines.Skip(endIdx));

        var expected = string.Join('\n', newLines);
        var hasChanges = content != expected;

        return new MaintenanceAction
        {
            ArtifactId = config.Id,
            ArtifactPath = config.Path,
            Type = "index",
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
