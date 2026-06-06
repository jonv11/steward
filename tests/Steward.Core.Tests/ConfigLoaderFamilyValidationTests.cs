using FluentAssertions;
using Steward.Core.Abstractions;
using Steward.Core.Configuration;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class ConfigLoaderFamilyValidationTests
{
    private static (ConfigLoader loader, InMemoryFileSystem fs) SetupWithPolicy(string policyYaml)
    {
        var fs = new InMemoryFileSystem()
            .AddDirectory("root/.steward")
            .AddFile("root/.steward/policy.yaml", policyYaml);
        var loader = new ConfigLoader(fs);
        return (loader, fs);
    }

    [Fact]
    public void LoadPolicy_ValidFamilyDefinition_ParsesCorrectly()
    {
        var yaml = """
            artifact_families:
              - family: adr
                display_name: Architecture Decision Record
                match:
                  path_pattern: "docs/adrs/ADR-*.md"
                  frontmatter:
                    type: adr
                role: governance
                importance: recommended
                frontmatter_schema:
                  required: [type, status]
                  allowed_values:
                    status: [Draft, Accepted]
            """;
        var (loader, _) = SetupWithPolicy(yaml);

        var policy = loader.LoadPolicy("root/.steward");

        policy.Should().NotBeNull();
        policy!.ArtifactFamilies.Should().HaveCount(1);
        var family = policy.ArtifactFamilies![0];
        family.Family.Should().Be("adr");
        family.DisplayName.Should().Be("Architecture Decision Record");
        family.Match!.PathPattern.Should().Be("docs/adrs/ADR-*.md");
        family.Match.Frontmatter.Should().ContainKey("type");
        family.FrontmatterSchema!.Required.Should().Contain("status");
        family.FrontmatterSchema.AllowedValues!["status"].Should().Contain("Draft");
    }

    [Fact]
    public void LoadPolicy_FamilyMissingMatchSection_ThrowsConfigException()
    {
        var yaml = """
            artifact_families:
              - family: adr
                role: governance
            """;
        var (loader, _) = SetupWithPolicy(yaml);

        var act = () => loader.LoadPolicy("root/.steward");

        act.Should().Throw<StewardConfigException>()
            .WithMessage("*match*");
    }

    [Fact]
    public void LoadPolicy_FamilyMatchWithNoPathOrFrontmatter_ThrowsConfigException()
    {
        var yaml = """
            artifact_families:
              - family: adr
                match: {}
            """;
        var (loader, _) = SetupWithPolicy(yaml);

        var act = () => loader.LoadPolicy("root/.steward");

        act.Should().Throw<StewardConfigException>()
            .WithMessage("*at least one*");
    }

    [Fact]
    public void LoadPolicy_FamilyWithInvalidGlob_ThrowsConfigException()
    {
        var yaml = """
            artifact_families:
              - family: adr
                match:
                  path_pattern: "[invalid"
            """;
        var (loader, _) = SetupWithPolicy(yaml);

        var act = () => loader.LoadPolicy("root/.steward");

        act.Should().Throw<StewardConfigException>()
            .WithMessage("*path_pattern*");
    }

    [Fact]
    public void LoadPolicy_DuplicateFamilyNames_ThrowsConfigException()
    {
        var yaml = """
            artifact_families:
              - family: adr
                match:
                  path_pattern: "docs/adrs/**"
              - family: adr
                match:
                  path_pattern: "docs/adrs2/**"
            """;
        var (loader, _) = SetupWithPolicy(yaml);

        var act = () => loader.LoadPolicy("root/.steward");

        act.Should().Throw<StewardConfigException>()
            .WithMessage("*Duplicate*adr*");
    }

    [Fact]
    public void LoadPolicy_BlankFamilyName_ThrowsConfigException()
    {
        var yaml = """
            artifact_families:
              - match:
                  path_pattern: "docs/adrs/**"
            """;
        var (loader, _) = SetupWithPolicy(yaml);

        var act = () => loader.LoadPolicy("root/.steward");

        act.Should().Throw<StewardConfigException>()
            .WithMessage("*family*name*");
    }

    [Fact]
    public void LoadPolicy_InvalidImportance_ThrowsConfigException()
    {
        var yaml = """
            artifact_families:
              - family: adr
                match:
                  path_pattern: "docs/adrs/**"
                importance: critical
            """;
        var (loader, _) = SetupWithPolicy(yaml);

        var act = () => loader.LoadPolicy("root/.steward");

        act.Should().Throw<StewardConfigException>()
            .WithMessage("*importance*critical*");
    }

    [Fact]
    public void LoadPolicy_BlankRequiredField_ThrowsConfigException()
    {
        var yaml = """
            artifact_families:
              - family: adr
                match:
                  path_pattern: "docs/adrs/**"
                frontmatter_schema:
                  required: [""]
            """;
        var (loader, _) = SetupWithPolicy(yaml);

        var act = () => loader.LoadPolicy("root/.steward");

        act.Should().Throw<StewardConfigException>()
            .WithMessage("*required*blank*");
    }

    [Fact]
    public void LoadPolicy_FamilyWithFrontmatterOnly_IsValid()
    {
        var yaml = """
            artifact_families:
              - family: chapter
                match:
                  frontmatter:
                    doc_type: chapter
                frontmatter_schema:
                  required: [doc_type, title]
            """;
        var (loader, _) = SetupWithPolicy(yaml);

        var policy = loader.LoadPolicy("root/.steward");

        policy!.ArtifactFamilies.Should().HaveCount(1);
        policy.ArtifactFamilies![0].Match!.Frontmatter.Should().ContainKey("doc_type");
    }

    [Fact]
    public void LoadPolicy_FixtureWithFamilies_LoadsWithoutErrors()
    {
        var fixturePath = RepositoryFixture.GetFixturePath("artifact-families");
        var stewardDir = Path.Combine(fixturePath, ".steward");

        var loader = new ConfigLoader(new PhysicalFileSystem());
        var policy = loader.LoadPolicy(stewardDir);

        policy.Should().NotBeNull();
        policy!.ArtifactFamilies.Should().HaveCount(2);
        policy.ArtifactFamilies!.Select(f => f.Family).Should().BeEquivalentTo(["adr", "rfc"]);
    }

    // ── RFC-014: allowed_fields ────────────────────────────────────────────────

    [Fact]
    public void LoadPolicy_AllowedFields_BlankEntry_ThrowsConfigException()
    {
        var yaml = """
            artifact_families:
              - family: adr
                match:
                  path_pattern: "docs/adrs/*.md"
                frontmatter_schema:
                  required: [type]
                  allowed_fields: [type, ""]
            """;
        var (loader, _) = SetupWithPolicy(yaml);

        var act = () => loader.LoadPolicy("root/.steward");
        act.Should().Throw<StewardConfigException>()
            .WithMessage("*allowed_fields*blank*");
    }

    [Fact]
    public void LoadPolicy_AllowedFields_RequiredFieldMissingFromAllowedFields_ThrowsConfigException()
    {
        var yaml = """
            artifact_families:
              - family: adr
                match:
                  path_pattern: "docs/adrs/*.md"
                frontmatter_schema:
                  required: [type, status]
                  allowed_fields: [type]
            """;
        var (loader, _) = SetupWithPolicy(yaml);

        var act = () => loader.LoadPolicy("root/.steward");
        act.Should().Throw<StewardConfigException>()
            .WithMessage("*status*allowed_fields*");
    }

    [Fact]
    public void LoadPolicy_AllowedFields_AllRequiredFieldsPresent_DoesNotThrow()
    {
        var yaml = """
            artifact_families:
              - family: adr
                match:
                  path_pattern: "docs/adrs/*.md"
                frontmatter_schema:
                  required: [type, status]
                  allowed_fields: [type, status, description]
            """;
        var (loader, _) = SetupWithPolicy(yaml);

        var act = () => loader.LoadPolicy("root/.steward");
        act.Should().NotThrow();
    }

    // ── RFC-014: deprecated_fields ─────────────────────────────────────────────

    [Fact]
    public void LoadPolicy_DeprecatedFields_BlankKey_ThrowsConfigException()
    {
        var yaml = """
            artifact_families:
              - family: adr
                match:
                  path_pattern: "docs/adrs/*.md"
                frontmatter_schema:
                  deprecated_fields:
                    "": last_updated
            """;
        var (loader, _) = SetupWithPolicy(yaml);

        var act = () => loader.LoadPolicy("root/.steward");
        act.Should().Throw<StewardConfigException>()
            .WithMessage("*deprecated_fields*blank*");
    }

    [Fact]
    public void LoadPolicy_AllowedFields_DeprecatedReplacementMissingFromAllowedFields_ThrowsConfigException()
    {
        var yaml = """
            artifact_families:
              - family: adr
                match:
                  path_pattern: "docs/adrs/*.md"
                frontmatter_schema:
                  required: [type]
                  allowed_fields: [type]
                  deprecated_fields:
                    date: last_updated
            """;
        var (loader, _) = SetupWithPolicy(yaml);

        // last_updated is the replacement but is not in allowed_fields
        var act = () => loader.LoadPolicy("root/.steward");
        act.Should().Throw<StewardConfigException>()
            .WithMessage("*last_updated*allowed_fields*");
    }

    [Fact]
    public void LoadPolicy_AllowedFields_NullReplacementExemptFromAllowedFieldsCheck_DoesNotThrow()
    {
        var yaml = """
            artifact_families:
              - family: adr
                match:
                  path_pattern: "docs/adrs/*.md"
                frontmatter_schema:
                  required: [type]
                  allowed_fields: [type]
                  deprecated_fields:
                    old_field: ~
            """;
        var (loader, _) = SetupWithPolicy(yaml);

        // null replacement (removal-only) is exempt from the allowed_fields check
        var act = () => loader.LoadPolicy("root/.steward");
        act.Should().NotThrow();
    }

    // ── RFC-014: title_pattern ─────────────────────────────────────────────────

    [Fact]
    public void LoadPolicy_TitlePattern_InvalidRegex_ThrowsConfigException()
    {
        var yaml = """
            artifact_families:
              - family: adr
                match:
                  path_pattern: "docs/adrs/*.md"
                title_pattern: "[invalid(regex"
            """;
        var (loader, _) = SetupWithPolicy(yaml);

        var act = () => loader.LoadPolicy("root/.steward");
        act.Should().Throw<StewardConfigException>()
            .WithMessage("*title_pattern*invalid*");
    }

    [Fact]
    public void LoadPolicy_TitlePattern_ValidRegex_DoesNotThrow()
    {
        var yaml = """
            artifact_families:
              - family: adr
                match:
                  path_pattern: "docs/adrs/*.md"
                title_pattern: "^ADR-[0-9]{3}: .+"
            """;
        var (loader, _) = SetupWithPolicy(yaml);

        var act = () => loader.LoadPolicy("root/.steward");
        act.Should().NotThrow();
    }

    [Fact]
    public void LoadPolicy_TitlePattern_ParsedIntoModel()
    {
        var yaml = """
            artifact_families:
              - family: adr
                match:
                  path_pattern: "docs/adrs/*.md"
                title_pattern: "^ADR-[0-9]{3}: .+"
            """;
        var (loader, _) = SetupWithPolicy(yaml);

        var policy = loader.LoadPolicy("root/.steward");

        policy!.ArtifactFamilies![0].TitlePattern.Should().Be("^ADR-[0-9]{3}: .+");
    }
}
