namespace Steward.Core.Models;

public sealed class VersionInfo
{
    public required string Version { get; init; }
    public required string RuntimeVersion { get; init; }
    public required string OsPlatform { get; init; }
    public required string Architecture { get; init; }

    /// <summary>The commit a source build came from; null for a release build or when unknown.</summary>
    public string? Commit { get; init; }

    /// <summary>True for a build the release script produced.</summary>
    public bool ReleaseBuild { get; init; }
}
