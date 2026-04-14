namespace Steward.Core.Orientation;

public sealed class OutlineResult
{
    public required string RootPath { get; init; }
    public required List<OutlineEntry> Entries { get; init; }
}

public sealed class OutlineEntry
{
    public required string Path { get; init; }
    public required bool IsDirectory { get; init; }
    public required int Depth { get; init; }
    public long? Size { get; init; }
    public int? LineCount { get; init; }
}
