using FluentAssertions;
using Steward.Cli.Commands;
using Steward.Core.Validation;
using Xunit;

namespace Steward.Cli.Tests;

/// <summary>
/// Asserts that ExplainCommand.GetRemediation returns non-trivial text for every rule
/// in the registry, detecting drift between ExplainCommand and rule implementations.
/// </summary>
public class ExplainRemediationConsistencyTests
{
    [Fact]
    public void GetRemediation_AllRules_ReturnNonGenericText()
    {
        var rules = RuleRegistry.CreateAllRules();

        foreach (var rule in rules)
        {
            var remediation = ExplainCommand.GetRemediation(rule.RuleId);
            remediation.Should().NotBe("No specific remediation guidance available.",
                because: $"rule {rule.RuleId} should have specific remediation guidance in ExplainCommand.GetRemediation");
        }
    }

    [Fact]
    public void GetRemediation_AllRules_ReturnNonNullNonEmpty()
    {
        var rules = RuleRegistry.CreateAllRules();

        foreach (var rule in rules)
        {
            var remediation = ExplainCommand.GetRemediation(rule.RuleId);
            remediation.Should().NotBeNullOrWhiteSpace(
                because: $"rule {rule.RuleId} must have non-empty remediation in ExplainCommand.GetRemediation");
        }
    }

    [Fact]
    public void GetRemediation_UnknownRule_ReturnsFallback()
    {
        var remediation = ExplainCommand.GetRemediation("STWD-999");
        remediation.Should().Be("No specific remediation guidance available.");
    }

    [Fact]
    public void GetRemediation_STWD006_ReflectsNarrowedContract()
    {
        var remediation = ExplainCommand.GetRemediation("STWD-006");
        remediation.Should().Contain("maintain",
            because: "STWD-006 remediation should mention 'steward maintain --apply'");
    }

    [Fact]
    public void GetRemediation_STWD012_MentionsLastUpdated()
    {
        var remediation = ExplainCommand.GetRemediation("STWD-012");
        remediation.Should().Contain("last_updated",
            because: "STWD-012 remediation should specifically mention the last_updated field");
    }

    [Fact]
    public void GetRemediation_STWD018_Exists()
    {
        var remediation = ExplainCommand.GetRemediation("STWD-018");
        remediation.Should().NotBe("No specific remediation guidance available.",
            because: "STWD-018 is a new rule and must have specific remediation text");
    }

    [Fact]
    public void AllRulesInRegistry_HaveRemediationInExplainCommand()
    {
        var rules = RuleRegistry.CreateAllRules();

        // Every rule in the registry should have an entry in GetRemediation that is
        // distinct from the fallback.
        var undocumentedRules = rules
            .Where(r => ExplainCommand.GetRemediation(r.RuleId) == "No specific remediation guidance available.")
            .Select(r => r.RuleId)
            .ToList();

        undocumentedRules.Should().BeEmpty(
            because: $"all rules should have specific remediation in ExplainCommand. Missing: [{string.Join(", ", undocumentedRules)}]");
    }
}
