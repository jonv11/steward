namespace Steward.Core.Maintenance;

/// <summary>
/// Updates content between managed region markers in a document.
/// </summary>
public sealed class ManagedSectionMaintainer : IArtifactMaintainer
{
    public string Type => "managed-section";

    public MaintenanceAction Evaluate(MaintenanceArtifactConfig config, MaintenanceContext context)
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
        var sectionId = config.ManagedSection ?? config.Id;

        // Compute expected content from source
        var expectedInner = ComputeExpectedContent(config, context);
        if (expectedInner == null)
        {
            return new MaintenanceAction
            {
                ArtifactId = config.Id,
                ArtifactPath = config.Path,
                Type = Type,
                Description = "No source content could be computed.",
                HasChanges = false
            };
        }

        var expected = ManagedRegionRewriter.Replace(content, sectionId, expectedInner);

        if (expected == null)
        {
            return new MaintenanceAction
            {
                ArtifactId = config.Id,
                ArtifactPath = config.Path,
                Type = Type,
                Description = $"Managed section '{sectionId}' markers not found.",
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
                ? $"Managed section '{sectionId}' needs updating."
                : $"Managed section '{sectionId}' is up to date.",
            HasChanges = hasChanges,
            ExpectedContent = expected,
            CurrentContent = content
        };
    }

    private static string? ComputeExpectedContent(MaintenanceArtifactConfig config, MaintenanceContext context)
    {
        if (config.Source == null) return null;

        var sourcePath = Path.Combine(context.RepositoryRoot, config.Source);
        if (context.FileSystem.FileExists(sourcePath))
        {
            return context.FileSystem.ReadAllText(sourcePath);
        }

        return null;
    }
}
