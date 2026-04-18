using FluentAssertions;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;
using Xunit;

namespace Steward.Core.Tests;

public class RuleRegistryTests
{
    [Fact]
    public void CreateAllRules_ReturnsEighteenRules()
    {
        var rules = RuleRegistry.CreateAllRules();
        rules.Should().HaveCount(18);
    }

    [Fact]
    public void CreateAllRules_ContainsAllExpectedTypes()
    {
        var rules = RuleRegistry.CreateAllRules();

        rules.Should().ContainSingle(r => r is RequiredArtifactRule);
        rules.Should().ContainSingle(r => r is ForbiddenPathRule);
        rules.Should().ContainSingle(r => r is RequiredFrontmatterFieldRule);
        rules.Should().ContainSingle(r => r is SectionSizeRule);
        rules.Should().ContainSingle(r => r is ManagedRegionIntegrityRule);
        rules.Should().ContainSingle(r => r is ManagedScopeViolationRule);
        rules.Should().ContainSingle(r => r is StaleArtifactRule);
        rules.Should().ContainSingle(r => r is BrokenInternalLinkRule);
        rules.Should().ContainSingle(r => r is BrokenArtifactReferenceRule);
        rules.Should().ContainSingle(r => r is NamingConventionRule);
        rules.Should().ContainSingle(r => r is IndexCompletenessRule);
        rules.Should().ContainSingle(r => r is FreshnessRule);
        rules.Should().ContainSingle(r => r is OrphanedDocumentRule);
        rules.Should().ContainSingle(r => r is RequiredSectionsRule);
        rules.Should().ContainSingle(r => r is FamilyMinCountRule);
        rules.Should().ContainSingle(r => r is FamilyNamingPatternRule);
        rules.Should().ContainSingle(r => r is UniqueHeadingTextRule);
        rules.Should().ContainSingle(r => r is BrokenFragmentAnchorRule);
    }

    [Fact]
    public void CreateAllRules_AllHaveUniqueIds()
    {
        var rules = RuleRegistry.CreateAllRules();
        var ids = rules.Select(r => r.RuleId).ToList();
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void CreateAllRules_ReturnsNewInstanceEachTime()
    {
        var rules1 = RuleRegistry.CreateAllRules();
        var rules2 = RuleRegistry.CreateAllRules();

        // Different array instances
        rules1.Should().NotBeSameAs(rules2);
    }
}
