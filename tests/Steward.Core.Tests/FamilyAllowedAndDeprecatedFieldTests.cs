using FluentAssertions;
using Steward.Core.Abstractions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

/// <summary>
/// Tests for RFC-014: allowed_fields and deprecated_fields behavior in STWD-003.
/// </summary>
public class FamilyAllowedAndDeprecatedFieldTests
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    private static RepositoryPolicy MakePolicyWithAllowedFields(
        List<string> allowed,
        List<string>? required = null,
        Dictionary<string, string?>? deprecated = null,
        Dictionary<string, bool>? autoFields = null) => new()
    {
        Governance = autoFields != null
            ? new GovernanceConfig
            {
                Frontmatter = new FrontmatterConfig { AutoFields = autoFields }
            }
            : null,
        ArtifactFamilies =
        [
            new ArtifactFamilyDefinition
            {
                Family = "adr",
                Match = new ArtifactFamilyMatch { PathPattern = "docs/adrs/*.md" },
                FrontmatterSchema = new ArtifactFamilyFrontmatterSchema
                {
                    Required = required,
                    AllowedFields = allowed,
                    DeprecatedFields = deprecated
                }
            }
        ]
    };

    private static RepositoryPolicy MakePolicyWithDeprecatedFields(
        Dictionary<string, string?> deprecated,
        List<string>? required = null) => new()
    {
        ArtifactFamilies =
        [
            new ArtifactFamilyDefinition
            {
                Family = "adr",
                Match = new ArtifactFamilyMatch { PathPattern = "docs/adrs/*.md" },
                FrontmatterSchema = new ArtifactFamilyFrontmatterSchema
                {
                    Required = required,
                    DeprecatedFields = deprecated
                }
            }
        ]
    };

    private static ValidationContext MakeContext(RepositoryPolicy policy, InMemoryFileSystem fs, string filePath) =>
        new()
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile(filePath, 50, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

    // ── allowed_fields: EvaluateAsync ─────────────────────────────────────────

    [Fact]
    public async Task Evaluate_AllowedFields_UnexpectedField_EmitsWarning()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "---\ntype: adr\nstatus: Draft\nextra_field: surprise\n---\n# ADR-001\n");

        var policy = MakePolicyWithAllowedFields(["type", "status"]);
        var context = MakeContext(policy, fs, "docs/adrs/ADR-001.md");

        var rule = new RequiredFrontmatterFieldRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().ContainSingle(d =>
            d.RuleId == "STWD-003" &&
            d.Severity == DiagnosticSeverity.Warning &&
            d.Message.Contains("extra_field") &&
            d.Message.Contains("allowed_fields"));
    }

    [Fact]
    public async Task Evaluate_AllowedFields_AllKnownFields_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "---\ntype: adr\nstatus: Draft\n---\n# ADR-001\n");

        var policy = MakePolicyWithAllowedFields(["type", "status"]);
        var context = MakeContext(policy, fs, "docs/adrs/ADR-001.md");

        var rule = new RequiredFrontmatterFieldRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_AllowedFields_AutoFieldImplicitlyAllowed()
    {
        var fs = new InMemoryFileSystem();
        // last_updated is in auto_fields — should not be flagged even though it's absent from allowed_fields
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "---\ntype: adr\nlast_updated: 2026-01-01\n---\n# ADR-001\n");

        var policy = MakePolicyWithAllowedFields(
            allowed: ["type"],
            autoFields: new Dictionary<string, bool> { ["last_updated"] = true });
        var context = MakeContext(policy, fs, "docs/adrs/ADR-001.md");

        var rule = new RequiredFrontmatterFieldRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_AllowedFields_AbsentFromPolicy_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();
        // No allowed_fields declared — schema remains open-ended
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "---\ntype: adr\nany_field_at_all: value\n---\n# ADR-001\n");

        var policy = new RepositoryPolicy
        {
            ArtifactFamilies =
            [
                new ArtifactFamilyDefinition
                {
                    Family = "adr",
                    Match = new ArtifactFamilyMatch { PathPattern = "docs/adrs/*.md" },
                    FrontmatterSchema = new ArtifactFamilyFrontmatterSchema
                    {
                        Required = ["type"]
                    }
                    // No AllowedFields
                }
            ]
        };
        var context = MakeContext(policy, fs, "docs/adrs/ADR-001.md");

        var rule = new RequiredFrontmatterFieldRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_AllowedFields_UnexpectedFieldDetails_ContainsFieldName()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "---\ntype: adr\nsurprise: yes\n---\n# ADR-001\n");

        var policy = MakePolicyWithAllowedFields(["type"]);
        var context = MakeContext(policy, fs, "docs/adrs/ADR-001.md");

        var rule = new RequiredFrontmatterFieldRule();
        var diagnostics = await rule.EvaluateAsync(context);

        var d = diagnostics.Single(x => x.Details?.ContainsKey("unexpectedField") == true);
        d.Details!["unexpectedField"].Should().Be("surprise");
    }

    // ── deprecated_fields: EvaluateAsync ──────────────────────────────────────

    [Fact]
    public async Task Evaluate_DeprecatedField_OnlyDeprecatedPresent_EmitsWarning()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "---\ntype: adr\ndate: 2025-01-01\n---\n# ADR-001\n");

        var policy = MakePolicyWithDeprecatedFields(new() { ["date"] = "last_updated" });
        var context = MakeContext(policy, fs, "docs/adrs/ADR-001.md");

        var rule = new RequiredFrontmatterFieldRule();
        var diagnostics = await rule.EvaluateAsync(context);

        var d = diagnostics.Should().ContainSingle().Subject;
        d.RuleId.Should().Be("STWD-003");
        d.Severity.Should().Be(DiagnosticSeverity.Warning);
        d.Message.Should().Contain("date");
        d.Message.Should().Contain("last_updated");
        d.Details!["deprecatedField"].Should().Be("date");
    }

    [Fact]
    public async Task Evaluate_DeprecatedField_BothDeprecatedAndReplacementPresent_EmitsError()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "---\ntype: adr\ndate: 2025-01-01\nlast_updated: 2026-01-01\n---\n# ADR-001\n");

        var policy = MakePolicyWithDeprecatedFields(new() { ["date"] = "last_updated" });
        var context = MakeContext(policy, fs, "docs/adrs/ADR-001.md");

        var rule = new RequiredFrontmatterFieldRule();
        var diagnostics = await rule.EvaluateAsync(context);

        var d = diagnostics.Should().ContainSingle().Subject;
        d.RuleId.Should().Be("STWD-003");
        d.Severity.Should().Be(DiagnosticSeverity.Error);
        d.Message.Should().Contain("both are present");
        d.Details!["conflict"].Should().Be(true);
    }

    [Fact]
    public async Task Evaluate_DeprecatedField_NullReplacement_EmitsWarning()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "---\ntype: adr\ndocument_id: DOC-001\n---\n# ADR-001\n");

        // null replacement means removal-only
        var policy = MakePolicyWithDeprecatedFields(new() { ["document_id"] = null });
        var context = MakeContext(policy, fs, "docs/adrs/ADR-001.md");

        var rule = new RequiredFrontmatterFieldRule();
        var diagnostics = await rule.EvaluateAsync(context);

        var d = diagnostics.Should().ContainSingle().Subject;
        d.Severity.Should().Be(DiagnosticSeverity.Warning);
        d.Message.Should().Contain("deprecated");
        d.Message.Should().Contain("document_id");
        d.Remediation.Should().Contain("Remove");
    }

    [Fact]
    public async Task Evaluate_DeprecatedField_NotPresentInFile_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "---\ntype: adr\nlast_updated: 2026-01-01\n---\n# ADR-001\n");

        var policy = MakePolicyWithDeprecatedFields(new() { ["date"] = "last_updated" });
        var context = MakeContext(policy, fs, "docs/adrs/ADR-001.md");

        var rule = new RequiredFrontmatterFieldRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_DeprecatedField_NotFlaggedAgainByAllowedFields()
    {
        // When both allowed_fields and deprecated_fields are configured,
        // a deprecated field should NOT also produce an unexpected-field warning.
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "---\ntype: adr\ndate: 2025-01-01\n---\n# ADR-001\n");

        var policy = MakePolicyWithAllowedFields(
            allowed: ["type", "last_updated"],
            deprecated: new() { ["date"] = "last_updated" });
        var context = MakeContext(policy, fs, "docs/adrs/ADR-001.md");

        var rule = new RequiredFrontmatterFieldRule();
        var diagnostics = await rule.EvaluateAsync(context);

        // Should only get the deprecated-field Warning, not an additional unexpected-field Warning
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Details!.Should().ContainKey("deprecatedField");
        diagnostics[0].Details!.Should().NotContainKey("unexpectedField");
    }

    // ── deprecated_fields: ComputeFixesAsync ──────────────────────────────────

    [Fact]
    public async Task ComputeFixes_DeprecatedField_RenamesField()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "---\ntype: adr\ndate: 2025-06-01\n---\n# ADR-001\n");

        var policy = MakePolicyWithDeprecatedFields(new() { ["date"] = "last_updated" });
        var context = MakeContext(policy, fs, "docs/adrs/ADR-001.md");

        var rule = new RequiredFrontmatterFieldRule();
        var fixes = await rule.ComputeFixesAsync(context);

        fixes.Should().HaveCount(1);
        fixes[0].RuleId.Should().Be("STWD-003");
        fixes[0].FilePath.Should().Be("docs/adrs/ADR-001.md");
        fixes[0].NewContent.Should().Contain("last_updated:");
        fixes[0].NewContent.Should().NotContain("date:");
        // Value should be preserved
        fixes[0].NewContent.Should().Contain("2025-06-01");
    }

    [Fact]
    public async Task ComputeFixes_DeprecatedNullReplacement_RemovesField()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "---\ntype: adr\ndocument_id: DOC-001\n---\n# ADR-001\n");

        var policy = MakePolicyWithDeprecatedFields(new() { ["document_id"] = null });
        var context = MakeContext(policy, fs, "docs/adrs/ADR-001.md");

        var rule = new RequiredFrontmatterFieldRule();
        var fixes = await rule.ComputeFixesAsync(context);

        fixes.Should().HaveCount(1);
        fixes[0].NewContent.Should().NotContain("document_id");
        fixes[0].NewContent.Should().Contain("type:");
    }

    [Fact]
    public async Task ComputeFixes_ErrorConflict_NoFixEmitted()
    {
        // When both deprecated and replacement are present → Error → no fix
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "---\ntype: adr\ndate: 2025-01-01\nlast_updated: 2026-01-01\n---\n# ADR-001\n");

        var policy = MakePolicyWithDeprecatedFields(new() { ["date"] = "last_updated" });
        var context = MakeContext(policy, fs, "docs/adrs/ADR-001.md");

        var rule = new RequiredFrontmatterFieldRule();
        var fixes = await rule.ComputeFixesAsync(context);

        fixes.Should().BeEmpty();
    }

    [Fact]
    public async Task ComputeFixes_DeprecatedAndMissing_SingleFixPerFile()
    {
        // File has a deprecated field AND a missing required field.
        // Must produce exactly ONE Fix (not two) to avoid write conflicts.
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "---\ntype: adr\ndate: 2025-06-01\n---\n# ADR-001\n");

        var policy = MakePolicyWithDeprecatedFields(
            deprecated: new() { ["date"] = "last_updated" },
            required: ["type", "status"]);
        var context = MakeContext(policy, fs, "docs/adrs/ADR-001.md");

        var rule = new RequiredFrontmatterFieldRule();
        var fixes = await rule.ComputeFixesAsync(context);

        // Exactly one fix for the file
        fixes.Should().HaveCount(1);
        fixes[0].FilePath.Should().Be("docs/adrs/ADR-001.md");

        // Fix renames date→last_updated AND inserts missing status field
        fixes[0].NewContent.Should().Contain("last_updated:");
        fixes[0].NewContent.Should().NotContain("date:");
        fixes[0].NewContent.Should().Contain("status:");
    }

    [Fact]
    public async Task ComputeFixes_MissingField_BehaviorUnchanged()
    {
        // Regression: when no deprecated_fields, missing-field fix behavior is unchanged
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/adrs/ADR-001.md",
            "---\ntype: adr\n---\n# ADR-001\n");

        var policy = new RepositoryPolicy
        {
            ArtifactFamilies =
            [
                new ArtifactFamilyDefinition
                {
                    Family = "adr",
                    Match = new ArtifactFamilyMatch { PathPattern = "docs/adrs/*.md" },
                    FrontmatterSchema = new ArtifactFamilyFrontmatterSchema
                    {
                        Required = ["type", "status"]
                    }
                }
            ]
        };
        var context = MakeContext(policy, fs, "docs/adrs/ADR-001.md");

        var rule = new RequiredFrontmatterFieldRule();
        var fixes = await rule.ComputeFixesAsync(context);

        fixes.Should().HaveCount(1);
        fixes[0].NewContent.Should().Contain("status:");
        fixes[0].NewContent.Should().Contain("type:");
    }
}
