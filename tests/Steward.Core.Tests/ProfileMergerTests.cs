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

    [Fact]
    public void Merge_NullRepoPolicy_ReturnsProfilePolicy()
    {
        var profile = ProfileDefaults.GetProfilePolicy("software")!;
        var result = ProfileMerger.Merge(null, profile);

        result.Should().BeSameAs(profile);
    }

    [Fact]
    public void Merge_RepoOverridesProfileName()
    {
        var profile = ProfileDefaults.GetProfilePolicy("software")!;
        var repo = new RepositoryPolicy
        {
            Repository = new RepositoryInfo { Name = "my-repo" }
        };

        var result = ProfileMerger.Merge(repo, profile);

        result.Repository!.Name.Should().Be("my-repo");
        result.Repository.Type.Should().Be("software"); // from profile
    }

    [Fact]
    public void Merge_RepoArtifactsOverrideProfile()
    {
        var profile = ProfileDefaults.GetProfilePolicy("software")!;
        var customArtifacts = new List<ArtifactDefinition>
        {
            new() { Path = "CUSTOM.md", Role = "custom", Required = true }
        };
        var repo = new RepositoryPolicy { Artifacts = customArtifacts };

        var result = ProfileMerger.Merge(repo, profile);

        result.Artifacts.Should().BeSameAs(customArtifacts);
    }

    [Fact]
    public void Merge_FallsBackToProfileArtifacts()
    {
        var profile = ProfileDefaults.GetProfilePolicy("software")!;
        var repo = new RepositoryPolicy(); // no artifacts

        var result = ProfileMerger.Merge(repo, profile);

        result.Artifacts.Should().BeSameAs(profile.Artifacts);
    }

    [Fact]
    public void Merge_GovernanceThresholdFromRepo()
    {
        var profile = ProfileDefaults.GetProfilePolicy("software")!;
        var repo = new RepositoryPolicy
        {
            Governance = new GovernanceConfig { SectionSizeWarningThreshold = 200 }
        };

        var result = ProfileMerger.Merge(repo, profile);

        result.Governance!.SectionSizeWarningThreshold.Should().Be(200);
        result.Governance.StartHere.Should().NotBeNullOrEmpty(); // from profile
    }
}
