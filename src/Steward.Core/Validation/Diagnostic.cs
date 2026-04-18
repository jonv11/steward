namespace Steward.Core.Validation;

public sealed record Diagnostic(
    string RuleId,
    DiagnosticSeverity Severity,
    string Category,
    string? Path,
    int? Line,
    string Message,
    string? Remediation,
    string? Source,
    IReadOnlyDictionary<string, object>? Details = null);

public enum DiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public sealed record ValidationResult
{
    public required ValidationSummary Summary { get; init; }
    public required List<Diagnostic> Diagnostics { get; init; }
}

public sealed record ValidationSummary
{
    public required string Scope { get; init; }
    public required int FilesChecked { get; init; }
    public required int Errors { get; init; }
    public required int Warnings { get; init; }
    public required int Infos { get; init; }
    public required bool Pass { get; init; }
}
