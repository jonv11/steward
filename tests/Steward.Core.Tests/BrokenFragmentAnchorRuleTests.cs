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

    [Fact]
    public async Task ComputeFixes_RenamedHeading_SuggestsBestMatchAnchor()
    {
        // guide.md heading was renamed from "Getting Started" to "Get Started"
        var targetContent = "# Guide\n\n## Get Started\n\nContent.";
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
        var fixes = await rule.ComputeFixesAsync(context);

        fixes.Should().ContainSingle();
        fixes[0].RuleId.Should().Be("STWD-018");
        fixes[0].FilePath.Should().Be("readme.md");
        fixes[0].NewContent.Should().Contain("#get-started");
        fixes[0].NewContent.Should().NotContain("#getting-started");
    }

    [Fact]
    public async Task ComputeFixes_CompletelyUnrelatedAnchor_ProducesNoFix()
    {
        // No heading in guide.md is close to the broken anchor
        var targetContent = "# Guide\n\n## Installation\n\nContent.";
        var sourceContent = "# Overview\n\nSee [something](guide.md#xyz-123-completely-unrelated).";

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
        var fixes = await rule.ComputeFixesAsync(context);

        fixes.Should().BeEmpty(because: "no close heading match exists within the edit distance threshold");
    }

    [Fact]
    public async Task ComputeFixes_MultipleLinksInSameFile_BatchedIntoSingleFix()
    {
        // Two broken links in the same source file → one fix with both replacements applied
        var targetContent = "# Guide\n\n## Install\n\n## Configure\n\nContent.";
        var sourceContent = "# Overview\n\nSee [install](guide.md#instll) and [configure](guide.md#configur).";

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
        var fixes = await rule.ComputeFixesAsync(context);

        // Both broken anchors should be fixed in a single Fix record
        fixes.Should().ContainSingle(because: "all fixes for the same source file are batched");
        fixes[0].FilePath.Should().Be("readme.md");
        fixes[0].NewContent.Should().Contain("#install");
        fixes[0].NewContent.Should().Contain("#configure");
    }

    [Fact]
    public async Task ComputeFixes_ValidAnchors_ProducesNoFixes()
    {
        var targetContent = "# Guide\n\n## Usage\n\nContent.";
        var sourceContent = "# Overview\n\nSee [usage](guide.md#usage).";

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
        var fixes = await rule.ComputeFixesAsync(context);

        fixes.Should().BeEmpty();
    }
}
