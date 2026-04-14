namespace Steward.Core.Models;

public sealed class VersionInfo
{
    public required string Version { get; init; }
    public required string RuntimeVersion { get; init; }
    public required string OsPlatform { get; init; }
    public required string Architecture { get; init; }
}
