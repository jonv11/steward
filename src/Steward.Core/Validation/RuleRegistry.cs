using Steward.Core.Validation.Rules;

namespace Steward.Core.Validation;

/// <summary>
/// Single source of truth for all validation rules.
/// Referenced by CheckCommand and ExplainCommand to avoid duplicate arrays.
/// </summary>
public static class RuleRegistry
{
    public static IValidationRule[] CreateAllRules() =>
    [
        new RequiredArtifactRule(),
        new ForbiddenPathRule(),
        new RequiredFrontmatterFieldRule(),
        new SectionSizeRule(),
        new ManagedRegionIntegrityRule(),
        new ManagedScopeViolationRule(),
        new StaleArtifactRule(),
        new BrokenInternalLinkRule(),
        new BrokenArtifactReferenceRule(),
        new NamingConventionRule(),
        new IndexCompletenessRule(),
        new FreshnessRule()
    ];
}
