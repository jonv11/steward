namespace Steward.Core.Orientation;

public sealed class OrientationResult
{
    public required string RepositoryRoot { get; init; }
    public string? RepositoryName { get; init; }
    public string? RepositoryType { get; init; }
    public string? Profile { get; init; }
    public List<string> StartHere { get; init; } = [];
    public List<OrientationSignal> Signals { get; init; } = [];
    public required List<OrientationEntry> Entries { get; init; }
}

public sealed class OrientationEntry
{
    public required string Path { get; init; }
    public required string Classification { get; init; }
    public required bool IsDirectory { get; init; }
    public required int Depth { get; init; }
    public bool IsStartHere { get; init; }
    public List<OrientationEntry>? Children { get; init; }
}

public sealed class OrientationSignal
{
    public required string Code { get; init; }
    public required string Severity { get; init; }
    public required string Message { get; init; }
    public string? Path { get; init; }
}

public sealed class OrientationSignalInput
{
    public List<OrientationSignalInputItem> MissingRequiredArtifacts { get; init; } = [];
    public List<OrientationSignalInputItem> StaleArtifacts { get; init; } = [];
}

public sealed class OrientationSignalInputItem
{
    public required string Path { get; init; }
    public required string Message { get; init; }
}
