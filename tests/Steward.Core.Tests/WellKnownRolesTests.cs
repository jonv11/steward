using FluentAssertions;
using Steward.Core.Configuration;
using Xunit;

namespace Steward.Core.Tests;

public class WellKnownRolesTests
{
    [Theory]
    [InlineData("vision")]
    [InlineData("roadmap")]
    [InlineData("current-state")]
    [InlineData("milestones")]
    [InlineData("decision-log")]
    public void IsStateDocumentRole_KnownStateRoles_ReturnsTrue(string role)
    {
        WellKnownRoles.IsStateDocumentRole(role).Should().BeTrue();
    }

    [Theory]
    [InlineData("VISION")]
    [InlineData("Roadmap")]
    [InlineData("Current-State")]
    public void IsStateDocumentRole_CaseInsensitive(string role)
    {
        WellKnownRoles.IsStateDocumentRole(role).Should().BeTrue();
    }

    [Theory]
    [InlineData("authoritative")]
    [InlineData("governance")]
    [InlineData("documentation")]
    [InlineData("changelog")]
    [InlineData("workflow")]
    [InlineData("custom-role")]
    public void IsStateDocumentRole_NonStateRoles_ReturnsFalse(string role)
    {
        WellKnownRoles.IsStateDocumentRole(role).Should().BeFalse();
    }

    [Fact]
    public void IsStateDocumentRole_Null_ReturnsFalse()
    {
        WellKnownRoles.IsStateDocumentRole(null).Should().BeFalse();
    }

    [Fact]
    public void StateDocumentRoles_ContainsFiveRoles()
    {
        WellKnownRoles.StateDocumentRoles.Should().HaveCount(5);
    }

    [Fact]
    public void AllRoles_ContainsTenRoles()
    {
        WellKnownRoles.AllRoles.Should().HaveCount(10);
    }

    [Fact]
    public void AllRoles_ContainsAllStateDocumentRoles()
    {
        foreach (var role in WellKnownRoles.StateDocumentRoles)
        {
            WellKnownRoles.AllRoles.Should().Contain(role);
        }
    }
}
