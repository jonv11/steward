using FluentAssertions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class BrokenInternalLinkRuleTests
{
    [Fact]
    public async Task Evaluate_BrokenLink_ReportsWarning()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/readme.md", "# Hello\n\nSee [guide](docs/guide.md) for more.");

        var context = new ValidationContext
        {
            Policy = null,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("readme.md", 100, false)],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new BrokenInternalLinkRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].RuleId.Should().Be("STWD-008");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostics[0].Message.Should().Contain("docs/guide.md");
    }

    [Fact]
    public async Task Evaluate_ValidLink_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/readme.md", "# Hello\n\nSee [guide](docs/guide.md) for more.")
            .AddFile("/repo/docs/guide.md", "# Guide");

        var context = new ValidationContext
        {
            Policy = null,
            PathPolicy = null,
            TargetFiles =
            [
                new DiscoveredFile("readme.md", 100, false),
                new DiscoveredFile("docs/guide.md", 50, false)
            ],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new BrokenInternalLinkRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_ExternalLink_Skipped()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/readme.md", "# Hello\n\nVisit [site](https://example.com) for more.");

        var context = new ValidationContext
        {
            Policy = null,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("readme.md", 100, false)],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new BrokenInternalLinkRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_LinkWithFragment_ChecksBaseFile()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/readme.md", "# Hello\n\nSee [section](other.md#section).");

        var context = new ValidationContext
        {
            Policy = null,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("readme.md", 100, false)],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new BrokenInternalLinkRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].Message.Should().Contain("other.md");
    }

    [Fact]
    public async Task Evaluate_RelativeLink_ResolvesCorrectly()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/docs/readme.md", "See [parent](../README.md).")
            .AddFile("/repo/README.md", "# Root");

        var context = new ValidationContext
        {
            Policy = null,
            PathPolicy = null,
            TargetFiles =
            [
                new DiscoveredFile("docs/readme.md", 50, false),
                new DiscoveredFile("README.md", 100, false)
            ],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new BrokenInternalLinkRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_NonMarkdownFiles_Skipped()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/script.sh", "echo hello");

        var context = new ValidationContext
        {
            Policy = null,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("script.sh", 20, false)],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new BrokenInternalLinkRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_MultipleLinks_ReportsEachBroken()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/doc.md", "See [a](missing-a.md) and [b](missing-b.md).");

        var context = new ValidationContext
        {
            Policy = null,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("doc.md", 100, false)],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new BrokenInternalLinkRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(2);
    }

    [Fact]
    public void ExtractInternalLinks_ParsesCorrectly()
    {
        var content = "# Title\n[a](file.md)\n[b](https://example.com)\n[c](other.md#section)";

        var links = BrokenInternalLinkRule.ExtractInternalLinks(content);

        links.Should().HaveCount(2);
        links[0].Target.Should().Be("file.md");
        links[0].Line.Should().Be(2);
        links[1].Target.Should().Be("other.md");
        links[1].Line.Should().Be(4);
    }

    [Fact]
    public void ResolveLinkTarget_NormalizesPath()
    {
        BrokenInternalLinkRule.ResolveLinkTarget("docs/readme.md", "../README.md")
            .Should().Be("README.md");

        BrokenInternalLinkRule.ResolveLinkTarget("readme.md", "docs/guide.md")
            .Should().Be("docs/guide.md");

        BrokenInternalLinkRule.ResolveLinkTarget("a/b/c.md", "../../root.md")
            .Should().Be("root.md");
    }

    [Fact]
    public async Task RuleMetadata_IsCorrect()
    {
        var rule = new BrokenInternalLinkRule();

        rule.RuleId.Should().Be("STWD-008");
        rule.Category.Should().Be("broken-link");
        rule.DefaultSeverity.Should().Be(DiagnosticSeverity.Warning);
    }

    [Fact]
    public async Task Evaluate_ScopedMode_UsesAllDiscoveredFiles_NoBrokenLinkFalsePositive()
    {
        // Scoped mode: only changed.md is in TargetFiles, but it links to unchanged.md
        // which is in AllDiscoveredFiles. Should NOT report a broken link.
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/changed.md", "# Changed\nSee [guide](unchanged.md).")
            .AddFile("/repo/unchanged.md", "# Unchanged");

        var context = new ValidationContext
        {
            Policy = null,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("changed.md", 100, false)],
            AllDiscoveredFiles = [
                new DiscoveredFile("changed.md", 100, false),
                new DiscoveredFile("unchanged.md", 50, false)
            ],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new BrokenInternalLinkRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty(
            because: "STWD-008 uses AllDiscoveredFiles for existence, so links to unchanged files should not be broken");
    }

    [Fact]
    public async Task Evaluate_ScopedMode_NullAllDiscoveredFiles_FallsBackToTargetFiles()
    {
        // When AllDiscoveredFiles is null, falls back to TargetFiles — existing behavior preserved
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/readme.md", "See [missing](truly-missing.md).");

        var context = new ValidationContext
        {
            Policy = null,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("readme.md", 100, false)],
            AllDiscoveredFiles = null,
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new BrokenInternalLinkRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().ContainSingle();
        diagnostics[0].RuleId.Should().Be("STWD-008");
    }
}
