namespace Steward.Core.Orientation;

public sealed class OrientationResult
{
    public required string RepositoryRoot { get; init; }
    public required List<OrientationEntry> Entries { get; init; }
}

public sealed class OrientationEntry
{
    public required string Path { get; init; }
    public required string Classification { get; init; }
    public required bool IsDirectory { get; init; }
    public required int Depth { get; init; }
    public List<OrientationEntry>? Children { get; init; }
}
