using FluentAssertions;
using Steward.Core.Configuration;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

/// <summary>
/// Tests for the new config validate integrity checks added in the rule-system completeness pass.
/// </summary>
public class ConfigIntegrityTests
{
    private static (ConfigLoader loader, InMemoryFileSystem fs) SetupWithPolicy(string policyYaml)
    {
        var fs = new InMemoryFileSystem()
            .AddDirectory("root/.steward")
            .AddFile("root/.steward/policy.yaml", policyYaml);
        var loader = new ConfigLoader(fs);
        return (loader, fs);
    }

    // ============================================================
    // Duplicate artifact path detection (config validate)
    // ============================================================

    [Fact]
    public void LoadPolicy_DuplicateArtifactPaths_ThrowsConfigException()
    {
        var yaml = """
            artifacts:
              - path: README.md
                role: readme
                required: true
              - path: README.md
                role: alternate
                required: false
            """;
        var (loader, _) = SetupWithPolicy(yaml);

        var act = () => loader.LoadPolicy("root/.steward");

        act.Should().Throw<StewardConfigException>()
            .WithMessage("*Duplicate artifact path*README.md*");
    }

    [Fact]
    public void LoadPolicy_UniquePaths_ParsesCorrectly()
    {
        var yaml = """
            artifacts:
              - path: README.md
                role: readme
              - path: CHANGELOG.md
                role: changelog
            """;
        var (loader, _) = SetupWithPolicy(yaml);

        var act = () => loader.LoadPolicy("root/.steward");

        act.Should().NotThrow();
    }

    [Fact]
    public void LoadPolicy_DuplicatePathsCaseInsensitive_ThrowsConfigException()
    {
        var yaml = """
            artifacts:
              - path: readme.md
                role: readme
              - path: README.md
                role: duplicate
            """;
        var (loader, _) = SetupWithPolicy(yaml);

        var act = () => loader.LoadPolicy("root/.steward");

        act.Should().Throw<StewardConfigException>()
            .WithMessage("*Duplicate artifact path*");
    }

    // ============================================================
    // Invalid regex detection at config validate time (STWD-010/016)
    // These are already in ConfigLoader — testing that they still work.
    // ============================================================

    [Fact]
    public void LoadPolicy_InvalidNamingPatternRegex_ThrowsConfigException()
    {
        var yaml = """
            artifact_families:
              - family: test
                match:
                  path_pattern: "docs/**/*.md"
                naming_pattern: "[invalid(regex"
            """;
        var (loader, _) = SetupWithPolicy(yaml);

        var act = () => loader.LoadPolicy("root/.steward");

        act.Should().Throw<StewardConfigException>()
            .WithMessage("*naming_pattern*regex*invalid*");
    }
}
