using Steward.Core.Abstractions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Markdown;

namespace Steward.Core.Validation;

public interface IValidationRule
{
    string RuleId { get; }
    string Category { get; }
    DiagnosticSeverity DefaultSeverity { get; }
    string Description { get; }
    Task<IReadOnlyList<Diagnostic>> EvaluateAsync(ValidationContext context);
}

public sealed class ValidationContext
{
    public required RepositoryPolicy? Policy { get; init; }
    public required PathPolicyDocument? PathPolicy { get; init; }
    public required IReadOnlyList<DiscoveredFile> TargetFiles { get; init; }
    /// <summary>
    /// The full set of discovered files in the repository, regardless of scope.
    /// Repo-wide obligation rules (e.g. STWD-001, STWD-009) use this for existence
    /// checks so that scoped validation does not produce false positives.
    /// When null, falls back to <see cref="TargetFiles"/>.
    /// </summary>
    public IReadOnlyList<DiscoveredFile>? AllDiscoveredFiles { get; init; }
    public required IFileSystem FileSystem { get; init; }
    public required string RepositoryRoot { get; init; }
    public CancellationToken CancellationToken { get; init; }
    public DocumentCache? DocumentCache { get; init; }
}
