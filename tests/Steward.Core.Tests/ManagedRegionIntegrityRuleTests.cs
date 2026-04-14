using FluentAssertions;
using Steward.Core.Discovery;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class ManagedRegionIntegrityRuleTests
{
    private readonly ManagedRegionIntegrityRule _rule = new();

    private ValidationContext CreateContext(InMemoryFileSystem fs, string root, params string[] files)
    {
        return new ValidationContext
        {
            Policy = null,
            PathPolicy = null,
            TargetFiles = files.Select(f => new DiscoveredFile(f, 100, false)).ToList(),
            FileSystem = fs,
            RepositoryRoot = root
        };
    }

    [Fact]
    public async Task ProperlyPairedMarkers_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();
        var content = "# Title\n<!-- steward:begin id=\"toc\" owner=\"steward\" -->\nTOC content\n<!-- steward:end -->\n";
        fs.AddFile("/repo/doc.md", content);

        var context = CreateContext(fs, "/repo", "doc.md");
        var diagnostics = await _rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task MissingEndMarker_ProducesDiagnostic()
    {
        var fs = new InMemoryFileSystem();
        var content = "# Title\n<!-- steward:begin id=\"toc\" owner=\"steward\" -->\nTOC content\n";
        fs.AddFile("/repo/doc.md", content);

        var context = CreateContext(fs, "/repo", "doc.md");
        var diagnostics = await _rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].RuleId.Should().Be("STWD-005");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Error);
        diagnostics[0].Message.Should().Contain("no matching end marker");
    }

    [Fact]
    public async Task MissingBeginMarker_ProducesDiagnostic()
    {
        var fs = new InMemoryFileSystem();
        var content = "# Title\nSome content\n<!-- steward:end -->\n";
        fs.AddFile("/repo/doc.md", content);

        var context = CreateContext(fs, "/repo", "doc.md");
        var diagnostics = await _rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].Message.Should().Contain("without a matching");
    }

    [Fact]
    public async Task MissingIdAttribute_ProducesDiagnostic()
    {
        var fs = new InMemoryFileSystem();
        var content = "# Title\n<!-- steward:begin owner=\"steward\" -->\nContent\n<!-- steward:end -->\n";
        fs.AddFile("/repo/doc.md", content);

        var context = CreateContext(fs, "/repo", "doc.md");
        var diagnostics = await _rule.EvaluateAsync(context);

        // Missing id means begin marker isn't pushed, so there's also an orphaned end marker
        diagnostics.Should().HaveCount(2);
        diagnostics.Should().Contain(d => d.Message.Contains("missing 'id'"));
        diagnostics.Should().Contain(d => d.Message.Contains("without a matching"));
    }

    [Fact]
    public async Task NestedRegions_ProperlyPaired_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();
        var content = "# Title\n<!-- steward:begin id=\"outer\" owner=\"steward\" -->\nOuter\n<!-- steward:begin id=\"inner\" owner=\"steward\" -->\nInner\n<!-- steward:end -->\n<!-- steward:end -->\n";
        fs.AddFile("/repo/doc.md", content);

        var context = CreateContext(fs, "/repo", "doc.md");
        var diagnostics = await _rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task NonMarkdownFiles_Skipped()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/repo/readme.txt", "<!-- steward:begin id=\"x\" -->\n");

        var context = CreateContext(fs, "/repo", "readme.txt");
        var diagnostics = await _rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task MultipleFiles_ChecksAll()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/repo/good.md", "# Title\n<!-- steward:begin id=\"a\" owner=\"steward\" -->\nContent\n<!-- steward:end -->\n");
        fs.AddFile("/repo/bad.md", "# Title\n<!-- steward:begin id=\"b\" owner=\"steward\" -->\nContent\n");

        var context = CreateContext(fs, "/repo", "good.md", "bad.md");
        var diagnostics = await _rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].Path.Should().Be("bad.md");
    }
}
