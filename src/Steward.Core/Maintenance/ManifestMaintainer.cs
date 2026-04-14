using System.Text.Json;
using System.Text.Json.Serialization;
using Steward.Core.Markdown;

namespace Steward.Core.Maintenance;

/// <summary>
/// Generates .steward/generated/manifest.json — a machine-readable repository manifest.
/// Contains file inventory, artifact roles, and heading index.
/// </summary>
public sealed class ManifestMaintainer : IArtifactMaintainer
{
    public string Type => "manifest";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public MaintenanceAction Evaluate(MaintenanceArtifactConfig config, MaintenanceContext context)
    {
        var manifest = BuildManifest(config, context);
        var expected = JsonSerializer.Serialize(manifest, JsonOptions) + Environment.NewLine;

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
                ? "Manifest needs updating."
                : "Manifest is up to date.",
            HasChanges = hasChanges,
            ExpectedContent = expected,
            CurrentContent = current
        };
    }

    private static ManifestDocument BuildManifest(MaintenanceArtifactConfig config, MaintenanceContext context)
    {
        var files = context.Files
            .Where(f => !f.IsDirectory)
            .OrderBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase)
            .Select(f => new ManifestFileEntry
            {
                Path = f.RelativePath,
                Extension = Path.GetExtension(f.RelativePath)
            })
            .ToList();

        // Extract headings from markdown files via document cache
        var headings = new List<ManifestHeadingEntry>();
        foreach (var file in context.Files.Where(f => f.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase) && !f.IsDirectory))
        {
            if (context.DocumentCache != null)
            {
                var doc = context.DocumentCache.GetOrParse(file.RelativePath);
                CollectHeadings(doc.Sections, file.RelativePath, headings);
            }
        }

        return new ManifestDocument
        {
            GeneratedBy = "steward maintain",
            FileCount = files.Count,
            Files = files,
            Headings = headings.Count > 0 ? headings : null
        };
    }

    private static void CollectHeadings(IReadOnlyList<Section> sections, string filePath, List<ManifestHeadingEntry> result)
    {
        foreach (var section in sections)
        {
            result.Add(new ManifestHeadingEntry
            {
                File = filePath,
                Level = section.Level,
                Text = section.Heading,
                Line = section.Range.Start
            });
            CollectHeadings(section.Children, filePath, result);
        }
    }

    private sealed class ManifestDocument
    {
        public string GeneratedBy { get; init; } = "";
        public int FileCount { get; init; }
        public List<ManifestFileEntry> Files { get; init; } = [];
        public List<ManifestHeadingEntry>? Headings { get; init; }
    }

    private sealed class ManifestFileEntry
    {
        public string Path { get; init; } = "";
        public string Extension { get; init; } = "";
    }

    private sealed class ManifestHeadingEntry
    {
        public string File { get; init; } = "";
        public int Level { get; init; }
        public string Text { get; init; } = "";
        public int Line { get; init; }
    }
}
