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
    public void GetRemediation_AllRegisteredRules_HaveSpecificNonEmptyGuidance()
    {
        var rules = RuleRegistry.CreateAllRules();
        rules.Should().NotBeEmpty("the registry must contain rules to check");

        foreach (var rule in rules)
        {
            var remediation = ExplainCommand.GetRemediation(rule.RuleId);
            remediation.Should().NotBeNullOrWhiteSpace($"rule {rule.RuleId} needs remediation guidance");
            remediation.Should().NotBe("No specific remediation guidance available.",
                $"rule {rule.RuleId} needs specific remediation guidance");
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
}
