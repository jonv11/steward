using System.Globalization;
using DotNet.Globbing;
using Steward.Core.Markdown;

namespace Steward.Core.Maintenance;

/// <summary>
/// Auto-maintains frontmatter fields (e.g., last_updated) across targeted files.
/// </summary>
public sealed class FrontmatterAutoMaintainer : IArtifactMaintainer
{
    public string Type => "frontmatter-auto";

    public MaintenanceAction Evaluate(MaintenanceArtifactConfig config, MaintenanceContext context)
    {
        if (config.Fields == null || config.Fields.Count == 0)
        {
            return new MaintenanceAction
            {
                ArtifactId = config.Id,
                ArtifactPath = config.Targets ?? "*",
                Type = Type,
                Description = "No fields configured for auto-maintenance.",
                HasChanges = false
            };
        }

        var targetGlob = Glob.Parse(config.Targets ?? "**/*.md");
        var matchingFiles = context.Files
            .Where(f => !f.IsDirectory && targetGlob.IsMatch(f.RelativePath))
            .ToList();

        var changedFiles = new List<string>();

        foreach (var file in matchingFiles)
        {
            var fullPath = Path.Combine(context.RepositoryRoot, file.RelativePath);
            if (!context.FileSystem.FileExists(fullPath)) continue;

            var content = context.FileSystem.ReadAllText(fullPath);
            var doc = MarkdownParser.Parse(fullPath, content);

            foreach (var (field, source) in config.Fields)
            {
                var expectedValue = ResolveFieldValue(source, fullPath, context);
                if (expectedValue == null) continue;

                var currentValue = doc.Frontmatter?.Fields.TryGetValue(field, out var v) == true
                    ? v?.ToString() : null;

                if (currentValue != expectedValue)
                {
                    changedFiles.Add(file.RelativePath);
                    break;
                }
            }
        }

        return new MaintenanceAction
        {
            ArtifactId = config.Id,
            ArtifactPath = config.Targets ?? "*",
            Type = Type,
            Description = changedFiles.Count > 0
                ? $"{changedFiles.Count} file(s) need frontmatter updates."
                : "All frontmatter fields are up to date.",
            HasChanges = changedFiles.Count > 0
        };
    }

    private static string? ResolveFieldValue(string source, string filePath, MaintenanceContext context)
    {
        return source switch
        {
            "file-mtime" => GetModificationTime(filePath, context),
            _ => source // Literal value
        };
    }

    private static string? GetModificationTime(string filePath, MaintenanceContext context)
    {
        try
        {
            var lastModified = context.FileSystem.GetLastWriteTimeUtc(filePath);
            return lastModified.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }
}
