using FluentAssertions;
using Steward.Core.Configuration;
using Xunit;

namespace Steward.Core.Tests;

public class ProfileMergerTests
{
    [Theory]
    [InlineData("software", "software", true)]
    [InlineData("docs", "documentation", true)]
    [InlineData("mixed", "mixed", true)]
    [InlineData("knowledge", "knowledge", true)]
    [InlineData("minimal", "general", false)]
    public void GetProfilePolicy_ValidProfile_ReturnsRepositoryContract(
        string profile, string repositoryType, bool readmeRequired)
    {
        var policy = ProfileDefaults.GetProfilePolicy(profile);
        policy.Should().NotBeNull();
        policy!.Repository!.Type.Should().Be(repositoryType);
        policy.Artifacts.Should().ContainSingle(a => a.Path == "README.md")
            .Which.Required.Should().Be(readmeRequired);
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
        config.Discovery!.Exclude.Should().Contain(["node_modules/", ".vs/", "*.user", ".tools/"]);
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

    [Fact]
    public void Merge_GovernanceThreshold_ExplicitlySet500_IsRespected()
    {
        // Regression: previously the merger treated repo=500 as "not set" because it compared
        // to the magic value 500, overwriting an explicit user declaration with the profile value.
        var profile = ProfileDefaults.GetProfilePolicy("docs")!; // docs profile uses 300
        var repo = new RepositoryPolicy
        {
            Governance = new GovernanceConfig { SectionSizeWarningThreshold = 500 }
        };

        var result = ProfileMerger.Merge(repo, profile);

        // The explicit repo value (500) must win over the profile value (300).
        result.Governance!.SectionSizeWarningThreshold.Should().Be(500);
    }

    [Fact]
    public void Merge_GovernanceThreshold_NotSet_FallsBackToProfile()
    {
        var profile = ProfileDefaults.GetProfilePolicy("docs")!; // docs profile uses 300
        var repo = new RepositoryPolicy
        {
            Governance = new GovernanceConfig() // threshold left null
        };

        var result = ProfileMerger.Merge(repo, profile);

        result.Governance!.SectionSizeWarningThreshold.Should().Be(300);
    }

    [Fact]
    public void Merge_SearchRole_CanFindByPolicyRole()
    {
        // search --role uses artifact role to filter; verify profile artifacts carry their roles
        var profile = ProfileDefaults.GetProfilePolicy("software")!;
        var result = ProfileMerger.Merge(null, profile);

        result.Artifacts.Should().Contain(a => a.Role == "authoritative");
    }
}
