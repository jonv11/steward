namespace Steward.Core.Validation;

/// <summary>
/// Extends IValidationRule with the ability to compute deterministic fixes.
/// Rules can optionally implement this to support 'steward check --fix'.
/// </summary>
public interface IFixableRule : IValidationRule
{
    /// <summary>
    /// Computes the set of fixes that would resolve the diagnostics produced by this rule.
    /// Each fix is a file path → new content pair.
    /// </summary>
    Task<IReadOnlyList<Fix>> ComputeFixesAsync(ValidationContext context);
}

/// <summary>
/// A deterministic fix for a validation diagnostic.
/// </summary>
public sealed record Fix(
    string RuleId,
    string FilePath,
    string Description,
    string NewContent);
