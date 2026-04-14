namespace Steward.Core.Search;

public sealed record SearchResult
{
    public required string Query { get; init; }
    public required SearchMode Mode { get; init; }
    public required IReadOnlyList<SearchMatch> Matches { get; init; }
    public int TotalMatches { get; init; }
    public bool Truncated { get; init; }
}

public sealed record SearchMatch
{
    public required string Path { get; init; }
    public required int Line { get; init; }
    public int Column { get; init; }
    public required string Snippet { get; init; }
    public required SearchMatchKind Kind { get; init; }
    public string? HeadingContext { get; init; }
}

public enum SearchMatchKind
{
    Content,
    Heading
}

public enum SearchMode
{
    All,
    Content,
    Headings
}
