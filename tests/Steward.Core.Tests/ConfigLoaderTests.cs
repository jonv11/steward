using FluentAssertions;
using Steward.Core.Configuration;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class ConfigLoaderTests
{
    [Fact]
    public void LoadConfig_ValidYaml_ParsesCorrectly()
    {
        var fs = new InMemoryFileSystem()
            .AddDirectory("root/.steward")
            .AddFile("root/.steward/config.yaml", "profile: software\ndiscovery:\n  exclude:\n    - node_modules/\n    - .vs/\n");

        var loader = new ConfigLoader(fs);
        var config = loader.LoadConfig("root/.steward");

        config.Should().NotBeNull();
        config!.Profile.Should().Be("software");
        config.Discovery.Should().NotBeNull();
        config.Discovery!.Exclude.Should().Contain("node_modules/");
    }

    [Fact]
    public void LoadConfig_MissingFile_ReturnsNull()
    {
        var fs = new InMemoryFileSystem()
            .AddDirectory("root/.steward");

        var loader = new ConfigLoader(fs);
        var config = loader.LoadConfig("root/.steward");

        config.Should().BeNull();
    }

    [Fact]
    public void LoadPolicy_ValidYaml_ParsesArtifacts()
    {
        var yaml = """
            repository:
              name: test
              type: software
            artifacts:
              - path: README.md
                role: authoritative
                required: true
            """;

        var fs = new InMemoryFileSystem()
            .AddDirectory("root/.steward")
            .AddFile("root/.steward/policy.yaml", yaml);

        var loader = new ConfigLoader(fs);
        var policy = loader.LoadPolicy("root/.steward");

        policy.Should().NotBeNull();
        policy!.Repository!.Name.Should().Be("test");
        policy.Artifacts.Should().HaveCount(1);
        policy.Artifacts![0].Path.Should().Be("README.md");
        policy.Artifacts[0].Required.Should().BeTrue();
    }

    [Fact]
    public void LoadConfig_UnknownFields_Throws()
    {
        var fs = new InMemoryFileSystem()
            .AddDirectory("root/.steward")
            .AddFile("root/.steward/config.yaml", "profile: software\nunknown_field: value\n");

        var loader = new ConfigLoader(fs);
        var action = () => loader.LoadConfig("root/.steward");

        action.Should().Throw<StewardConfigException>();
    }

    [Fact]
    public void LoadConfig_InvalidProfile_Throws()
    {
        var fs = new InMemoryFileSystem()
            .AddDirectory("root/.steward")
            .AddFile("root/.steward/config.yaml", "profile: default\n");

        var loader = new ConfigLoader(fs);
        var action = () => loader.LoadConfig("root/.steward");

        action.Should().Throw<StewardConfigException>()
            .WithMessage("*Invalid profile*");
    }

    [Fact]
    public void FindConfigDirectory_WalkUp_FindsStewardDir()
    {
        var fs = new InMemoryFileSystem()
            .AddDirectory("root/.steward")
            .AddDirectory("root/.git")
            .AddDirectory("root/src/deep");

        var loader = new ConfigLoader(fs);
        var dir = loader.FindConfigDirectory("root/src/deep");

        dir.Should().NotBeNull();
        dir.Should().EndWith(".steward");
    }

    [Fact]
    public void FindConfigDirectory_NoDir_ReturnsNull()
    {
        var fs = new InMemoryFileSystem()
            .AddDirectory("root/.git")
            .AddDirectory("root/src");

        var loader = new ConfigLoader(fs);
        var dir = loader.FindConfigDirectory("root/src");

        dir.Should().BeNull();
    }

    [Fact]
    public void FindConfigDirectory_Override_UsesOverride()
    {
        var fs = new InMemoryFileSystem()
            .AddDirectory("custom-config");

        var loader = new ConfigLoader(fs);
        var dir = loader.FindConfigDirectory("root", overridePath: "custom-config");

        dir.Should().Be("custom-config");
    }
}
