using System.Text;
using DotNet.Globbing;
using Steward.Core.Markdown;

namespace Steward.Core.Maintenance;

/// <summary>
/// Generates a Markdown table index of files matching a glob,
/// with Title, Path, and Description columns extracted from
/// frontmatter or document headings.
/// </summary>
public sealed class DirectoryIndexMaintainer : IArtifactMaintainer
{
    public string Type => "directory-index";

    public MaintenanceAction Evaluate(MaintenanceArtifactConfig config, MaintenanceContext context)
    {
        var sourceGlob = Glob.Parse(config.Source ?? "**/*.md");
        var sort = config.Sort ?? "filename";

        var matchingFiles = context.Files
            .Where(f => !f.IsDirectory && sourceGlob.IsMatch(f.RelativePath))
            .Where(f => f.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            .ToList();

        matchingFiles = sort switch
        {
            "filename" => matchingFiles.OrderBy(f => Path.GetFileName(f.RelativePath), StringComparer.OrdinalIgnoreCase).ToList(),
            "path" => matchingFiles.OrderBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase).ToList(),
            _ => matchingFiles.OrderBy(f => Path.GetFileName(f.RelativePath), StringComparer.OrdinalIgnoreCase).ToList()
        };

        var rows = new List<(string Title, string RelPath, string Description)>();
        foreach (var file in matchingFiles)
        {
            var fullPath = Path.Combine(context.RepositoryRoot, file.RelativePath);
            var title = Path.GetFileNameWithoutExtension(file.RelativePath);
            var description = "";

            if (context.FileSystem.FileExists(fullPath))
            {
                var doc = context.DocumentCache?.GetOrParse(file.RelativePath)
                    ?? MarkdownParser.Parse(fullPath, context.FileSystem.ReadAllText(fullPath));

                if (doc.Frontmatter?.Fields.TryGetValue("title", out var t) == true && t != null)
                    title = t.ToString()!;
                else if (doc.Sections.Count > 0)
                    title = doc.Sections[0].Heading;

                if (doc.Frontmatter?.Fields.TryGetValue("description", out var d) == true && d != null)
                    description = d.ToString()!;
            }

            rows.Add((title, file.RelativePath.Replace('\\', '/'), description));
        }

        var tableContent = GenerateTable(rows);

        if (config.ManagedSection != null)
        {
            return UpdateManagedSection(config, context, tableContent);
        }

        var expected = $"# Index\n\n{tableContent}";
        var targetPath = Path.Combine(context.RepositoryRoot, config.Path);
        var current = context.FileSystem.FileExists(targetPath)
            ? context.FileSystem.ReadAllText(targetPath)
            : null;

        return new MaintenanceAction
        {
            ArtifactId = config.Id,
            ArtifactPath = config.Path,
            Type = Type,
            Description = current != expected
                ? $"Directory index needs updating ({rows.Count} entries)."
                : "Directory index is up to date.",
            HasChanges = current != expected,
            ExpectedContent = expected,
            CurrentContent = current
        };
    }

    internal static string GenerateTable(List<(string Title, string RelPath, string Description)> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("| Title | Path | Description |");
        sb.AppendLine("| --- | --- | --- |");
        foreach (var (title, relPath, description) in rows)
        {
            var escapedTitle = title.Replace("|", "\\|");
            var escapedDesc = description.Replace("|", "\\|");
            sb.AppendLine($"| {escapedTitle} | [{Path.GetFileName(relPath)}]({relPath}) | {escapedDesc} |");
        }
        return sb.ToString();
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

        return new MaintenanceAction
        {
            ArtifactId = config.Id,
            ArtifactPath = config.Path,
            Type = Type,
            Description = content != expected
                ? $"Managed section '{config.ManagedSection}' needs updating."
                : "Managed section is up to date.",
            HasChanges = content != expected,
            ExpectedContent = expected,
            CurrentContent = content
        };
    }
}
