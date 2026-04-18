using FluentAssertions;
using Steward.Core.Discovery;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class ManagedScopeViolationRuleTests
{
    [Fact]
    public async Task Evaluate_EmptyManagedRegion_ReportsWarning()
    {
        var content = "# Doc\n<!-- steward:begin id=\"toc\" owner=\"steward\" -->\n<!-- steward:end -->\nSome text.";
        var fs = new InMemoryFileSystem().AddFile("/repo/readme.md", content);

        var context = new ValidationContext
        {
            Policy = null,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("readme.md", 100, false)],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new ManagedScopeViolationRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().ContainSingle();
        diagnostics[0].RuleId.Should().Be("STWD-006");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostics[0].Message.Should().Contain("empty");
        diagnostics[0].Message.Should().Contain("toc");
        diagnostics[0].Message.Should().Contain("readme.md");
    }

    [Fact]
    public async Task Evaluate_PopulatedManagedRegion_NoDiagnostics()
    {
        var content = "# Doc\n<!-- steward:begin id=\"toc\" owner=\"steward\" -->\n- Item 1\n- Item 2\n<!-- steward:end -->\nSome text.";
        var fs = new InMemoryFileSystem().AddFile("/repo/readme.md", content);

        var context = new ValidationContext
        {
            Policy = null,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("readme.md", 100, false)],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new ManagedScopeViolationRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_NoManagedRegions_NoDiagnostics()
    {
        var content = "# Doc\nJust a regular document with no managed regions.";
        var fs = new InMemoryFileSystem().AddFile("/repo/readme.md", content);

        var context = new ValidationContext
        {
            Policy = null,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("readme.md", 100, false)],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new ManagedScopeViolationRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_MultipleEmptyRegions_ReportsDiagnosticPerRegion()
    {
        var content = "# Doc\n" +
                      "<!-- steward:begin id=\"a\" owner=\"steward\" -->\n<!-- steward:end -->\n" +
                      "Middle.\n" +
                      "<!-- steward:begin id=\"b\" owner=\"other\" -->\n<!-- steward:end -->\n";
        var fs = new InMemoryFileSystem().AddFile("/repo/readme.md", content);

        var context = new ValidationContext
        {
            Policy = null,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("readme.md", 100, false)],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new ManagedScopeViolationRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(2);
        diagnostics.Should().AllSatisfy(d => d.RuleId.Should().Be("STWD-006"));
        diagnostics.Select(d => d.Message).Should().Contain(m => m.Contains("\"a\"") || m.Contains("a"));
        diagnostics.Select(d => d.Message).Should().Contain(m => m.Contains("\"b\"") || m.Contains("b"));
    }

    [Fact]
    public async Task Evaluate_NonMarkdownFile_Skipped()
    {
        var fs = new InMemoryFileSystem().AddFile("/repo/notes.txt", "some text");

        var context = new ValidationContext
        {
            Policy = null,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("notes.txt", 50, false)],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new ManagedScopeViolationRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_EmptyRegionContainsFile_RemediationMentainsMaintain()
    {
        var content = "# Doc\n<!-- steward:begin id=\"index\" owner=\"steward\" -->\n<!-- steward:end -->";
        var fs = new InMemoryFileSystem().AddFile("/repo/readme.md", content);

        var context = new ValidationContext
        {
            Policy = null,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("readme.md", 100, false)],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new ManagedScopeViolationRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Remediation.Should().Contain("steward maintain");
    }

    [Fact]
    public void RuleId_IsSTWD006()
    {
        var rule = new ManagedScopeViolationRule();
        rule.RuleId.Should().Be("STWD-006");
    }

    [Fact]
    public void Description_DoesNotClaimOwnershipValidation()
    {
        var rule = new ManagedScopeViolationRule();
        // The description should reflect structural anomaly detection, not ownership enforcement
        rule.Description.Should().NotContain("only be modified by the declared owner",
            because: "STWD-006 no longer claims to validate content ownership");
        rule.Description.Should().Contain("empty");
    }
}
