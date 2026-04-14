using FluentAssertions;
using Steward.Core.Configuration;
using Xunit;

namespace Steward.Core.Tests;

public class ProfileMergerTests
{
    [Theory]
    [InlineData("software")]
    [InlineData("docs")]
    [InlineData("mixed")]
    [InlineData("knowledge")]
    [InlineData("minimal")]
    public void GetProfilePolicy_ValidProfile_ReturnsPolicy(string profile)
    {
        var policy = ProfileDefaults.GetProfilePolicy(profile);
        policy.Should().NotBeNull();
    }

    [Fact]
    public void GetProfilePolicy_UnknownProfile_ReturnsNull()
    {
        var policy = ProfileDefaults.GetProfilePolicy("nonexistent");
        policy.Should().BeNull();
    }

    [Fact]
    public void SoftwareProfile_RequiresReadme()
    {
        var policy = ProfileDefaults.GetProfilePolicy("software")!;
        policy.Artifacts.Should().Contain(a => a.Path == "README.md" && a.Required);
    }

    [Fact]
    public void CreateDefaultConfig_SetsProfile()
    {
        var config = ProfileDefaults.CreateDefaultConfig("docs");
        config.Profile.Should().Be("docs");
        config.Discovery!.Exclude.Should().NotBeEmpty();
    }
}
