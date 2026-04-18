namespace Steward.Cli.Formatting;

/// <summary>
/// Standard structured error response for JSON mode.
/// Emitted on stdout when --output json is active and a command fails.
/// </summary>
internal sealed class JsonErrorResponse
{
    public required string Kind { get; init; }
    public string? Code { get; init; }
    public required string Message { get; init; }
    public Dictionary<string, object>? Details { get; init; }
    public bool Retryable { get; init; }
    public string? SuggestedNextStep { get; init; }
}
