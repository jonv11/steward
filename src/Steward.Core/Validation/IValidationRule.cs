using Steward.Core.Abstractions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;

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
    public required IFileSystem FileSystem { get; init; }
    public required string RepositoryRoot { get; init; }
    public CancellationToken CancellationToken { get; init; }
}
