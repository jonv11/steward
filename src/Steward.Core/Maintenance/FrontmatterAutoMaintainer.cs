using System.Globalization;
using DotNet.Globbing;
using Steward.Core.Markdown;

namespace Steward.Core.Maintenance;

/// <summary>
/// Auto-maintains frontmatter fields (e.g., last_updated) across targeted files.
/// </summary>
public sealed class FrontmatterAutoMaintainer : IArtifactMaintainer
{
    private const string LocalChangeDateSource = "today-if-local-change";

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

        if (RequiresLocalChangeDetection(config.Fields) && context.ChangedFiles == null)
        {
            return new MaintenanceAction
            {
                ArtifactId = config.Id,
                ArtifactPath = config.Targets ?? "*",
                Type = Type,
                Description = "Local-change-based frontmatter automation is blocked because git change detection is unavailable.",
                HasChanges = false,
                IsBlocked = true,
                BlockedReason = "Configure git with a readable HEAD so Steward can detect locally modified files."
            };
        }

        var targetGlob = Glob.Parse(config.Targets ?? "**/*.md");
        var matchingFiles = context.Files
            .Where(f => !f.IsDirectory && targetGlob.IsMatch(f.RelativePath))
            .ToList();

        var fileEdits = new List<MaintenanceFileEdit>();

        foreach (var file in matchingFiles)
        {
            var fullPath = Path.Combine(context.RepositoryRoot, file.RelativePath);
            if (!context.FileSystem.FileExists(fullPath)) continue;

            var content = context.FileSystem.ReadAllText(fullPath);
            var doc = context.DocumentCache?.GetOrParse(file.RelativePath)
                ?? MarkdownParser.Parse(fullPath, content);

            var pendingFields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (field, source) in config.Fields)
            {
                var expectedValue = ResolveFieldValue(source, field, file.RelativePath, fullPath, doc, context);
                if (expectedValue == null) continue;

                var currentValue = doc.Frontmatter?.Fields.TryGetValue(field, out var v) == true
                    ? v?.ToString() : null;

                if (currentValue != expectedValue)
                    pendingFields[field] = expectedValue;
            }

            if (pendingFields.Count == 0)
                continue;

            var edit = FrontmatterEditor.SetFields(doc, pendingFields);
            if (!edit.HasChanges)
                continue;

            fileEdits.Add(new MaintenanceFileEdit
            {
                FilePath = file.RelativePath,
                Description = BuildEditDescription(pendingFields),
                CurrentContent = edit.OriginalContent,
                ExpectedContent = edit.NewContent
            });
        }

        return new MaintenanceAction
        {
            ArtifactId = config.Id,
            ArtifactPath = config.Targets ?? "*",
            Type = Type,
            Description = fileEdits.Count > 0
                ? $"{fileEdits.Count} file(s) need frontmatter updates."
                : "All frontmatter fields are up to date.",
            HasChanges = fileEdits.Count > 0,
            FileEdits = fileEdits
        };
    }

    private static string? ResolveFieldValue(
        string source,
        string field,
        string relativePath,
        string filePath,
        StructuredDocument doc,
        MaintenanceContext context)
    {
        if (string.Equals(source, LocalChangeDateSource, StringComparison.OrdinalIgnoreCase))
        {
            if (doc.Frontmatter?.Fields.ContainsKey(field) != true)
                return null;

            if (context.ChangedFiles?.Contains(relativePath) != true)
                return null;

            return DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

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

    private static bool RequiresLocalChangeDetection(Dictionary<string, string> fields)
        => fields.Values.Any(value => string.Equals(value, LocalChangeDateSource, StringComparison.OrdinalIgnoreCase));

    private static string BuildEditDescription(IReadOnlyDictionary<string, string> pendingFields)
    {
        return pendingFields.Count == 1
            ? $"Update frontmatter field '{pendingFields.Keys.Single()}'."
            : $"Update frontmatter fields: {string.Join(", ", pendingFields.Keys.OrderBy(key => key, StringComparer.OrdinalIgnoreCase))}.";
    }
}
