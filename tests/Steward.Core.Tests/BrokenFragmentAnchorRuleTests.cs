using FluentAssertions;
using Steward.Core.Discovery;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class BrokenFragmentAnchorRuleTests
{
    [Fact]
    public async Task Evaluate_ValidFragmentAnchor_NoDiagnostics()
    {
        var targetContent = "# Introduction\n\nSome text.\n\n## Getting Started\n\nMore text.";
        var sourceContent = "# Overview\n\nSee [Getting Started](guide.md#getting-started).";

        var fs = new InMemoryFileSystem()
            .AddFile("/repo/readme.md", sourceContent)
            .AddFile("/repo/guide.md", targetContent);

        var context = new ValidationContext
        {
            Policy = null,
            PathPolicy = null,
            TargetFiles = [
                new DiscoveredFile("readme.md", 100, false),
                new DiscoveredFile("guide.md", 200, false)
            ],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new BrokenFragmentAnchorRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_BrokenFragmentAnchor_ReportsWarning()
    {
        var targetContent = "# Introduction\n\n## Usage\n\nSome content.";
        var sourceContent = "# Overview\n\nSee [Getting Started](guide.md#getting-started).";

        var fs = new InMemoryFileSystem()
            .AddFile("/repo/readme.md", sourceContent)
            .AddFile("/repo/guide.md", targetContent);

        var context = new ValidationContext
        {
            Policy = null,
            PathPolicy = null,
            TargetFiles = [
                new DiscoveredFile("readme.md", 100, false),
                new DiscoveredFile("guide.md", 200, false)
            ],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new BrokenFragmentAnchorRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().ContainSingle();
        diagnostics[0].RuleId.Should().Be("STWD-018");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostics[0].Message.Should().Contain("getting-started");
        diagnostics[0].Message.Should().Contain("guide.md");
    }

    [Fact]
    public async Task Evaluate_FragmentOnlyLink_ChecksCurrentFile()
    {
        // A fragment-only link (#heading) refers to the current file
        var content = "# Title\n\n## Usage\n\nSee [Details](#missing-section).";

        var fs = new InMemoryFileSystem().AddFile("/repo/readme.md", content);

        var context = new ValidationContext
        {
            Policy = null,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("readme.md", 100, false)],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new BrokenFragmentAnchorRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().ContainSingle();
        diagnostics[0].RuleId.Should().Be("STWD-018");
        diagnostics[0].Message.Should().Contain("missing-section");
    }

    [Fact]
    public async Task Evaluate_FragmentOnlyLink_ValidHeading_NoDiagnostics()
    {
        var content = "# Title\n\n## Usage\n\nSee [this section](#usage).";

        var fs = new InMemoryFileSystem().AddFile("/repo/readme.md", content);

        var context = new ValidationContext
        {
            Policy = null,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("readme.md", 100, false)],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new BrokenFragmentAnchorRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_ExternalLinkWithFragment_Skipped()
    {
        var content = "See [external](https://example.com/docs#heading).";
        var fs = new InMemoryFileSystem().AddFile("/repo/readme.md", content);

        var context = new ValidationContext
        {
            Policy = null,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("readme.md", 100, false)],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new BrokenFragmentAnchorRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_LinkToMissingFile_SkippedBySTWD018()
    {
        // STWD-008 handles missing files; STWD-018 only validates fragments in existing files
        var content = "See [broken](nonexistent.md#heading).";
        var fs = new InMemoryFileSystem().AddFile("/repo/readme.md", content);

        var context = new ValidationContext
        {
            Policy = null,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("readme.md", 100, false)],
            AllDiscoveredFiles = [new DiscoveredFile("readme.md", 100, false)],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new BrokenFragmentAnchorRule();
        var diagnostics = await rule.EvaluateAsync(context);

        // STWD-018 should NOT report when the target file doesn't exist
        // (STWD-008 handles that separately)
        diagnostics.Should().BeEmpty(
            because: "STWD-018 only validates fragment anchors in files that exist; missing files are STWD-008's concern");
    }

    [Fact]
    public async Task Evaluate_ScopedMode_UsesAllDiscoveredFiles_NoBrokenFragment()
    {
        // In scoped mode, only changed.md is in TargetFiles, but the link target is in AllDiscoveredFiles
        var targetContent = "# Guide\n\n## Installation\n\nSteps here.";
        var sourceContent = "See [install](guide.md#installation).";

        var fs = new InMemoryFileSystem()
            .AddFile("/repo/readme.md", sourceContent)
            .AddFile("/repo/guide.md", targetContent);

        var context = new ValidationContext
        {
            Policy = null,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("readme.md", 100, false)],
            AllDiscoveredFiles = [
                new DiscoveredFile("readme.md", 100, false),
                new DiscoveredFile("guide.md", 200, false)
            ],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new BrokenFragmentAnchorRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty(
            because: "STWD-018 uses AllDiscoveredFiles for file existence, preventing scoped false-positives");
    }

    [Fact]
    public async Task Evaluate_HeadingSlugNormalization_HandlesSpecialChars()
    {
        // "Getting Started!" should slug to "getting-started"
        var targetContent = "# Getting Started!\n\nContent here.";
        var sourceContent = "See [start](target.md#getting-started).";

        var fs = new InMemoryFileSystem()
            .AddFile("/repo/readme.md", sourceContent)
            .AddFile("/repo/target.md", targetContent);

        var context = new ValidationContext
        {
            Policy = null,
            PathPolicy = null,
            TargetFiles = [
                new DiscoveredFile("readme.md", 100, false),
                new DiscoveredFile("target.md", 50, false)
            ],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new BrokenFragmentAnchorRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty(
            because: "heading 'Getting Started!' should normalize to slug 'getting-started'");
    }

    [Fact]
    public async Task Evaluate_RelativePath_ResolvesCorrectly()
    {
        var targetContent = "# API Reference\n\n## Endpoints\n\nDetails.";
        var sourceContent = "See [endpoints](../api/reference.md#endpoints).";

        var fs = new InMemoryFileSystem()
            .AddFile("/repo/docs/guide.md", sourceContent)
            .AddFile("/repo/api/reference.md", targetContent);

        var context = new ValidationContext
        {
            Policy = null,
            PathPolicy = null,
            TargetFiles = [
                new DiscoveredFile("docs/guide.md", 100, false),
                new DiscoveredFile("api/reference.md", 100, false)
            ],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new BrokenFragmentAnchorRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void ExtractFragmentLinks_ExtractsFragmentOnly()
    {
        var content = "See [no-fragment](file.md) and [with-fragment](other.md#section).";

        var links = BrokenFragmentAnchorRule.ExtractFragmentLinks(content);

        // Only the link with a fragment should be returned
        links.Should().ContainSingle();
        links[0].FileTarget.Should().Be("other.md");
        links[0].Fragment.Should().Be("section");
    }

    [Fact]
    public void ExtractFragmentLinks_SkipsExternalLinks()
    {
        var content = "See [external](https://example.com/page#section).";

        var links = BrokenFragmentAnchorRule.ExtractFragmentLinks(content);

        links.Should().BeEmpty();
    }

    [Fact]
    public async Task RuleMetadata_IsCorrect()
    {
        var rule = new BrokenFragmentAnchorRule();
        rule.RuleId.Should().Be("STWD-018");
        rule.Category.Should().Be("broken-link");
        rule.DefaultSeverity.Should().Be(DiagnosticSeverity.Warning);
    }
}
