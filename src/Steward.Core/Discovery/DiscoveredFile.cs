namespace Steward.Core.Discovery;

public sealed record DiscoveredFile(
    string RelativePath,
    long Size,
    bool IsDirectory);
