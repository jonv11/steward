using FluentAssertions;
using Steward.Core.Discovery;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class UniqueHeadingTextRuleTests
{
    [Fact]
    public async Task EvaluateAsync_DuplicateHeadingText_ReportsWarning()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/docs.md", "# Status\nCurrent\n## Status\nMore current\n");

        var context = new ValidationContext
        {
            Policy = null,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs.md", 20, false)],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var diagnostics = await new UniqueHeadingTextRule().EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].RuleId.Should().Be("STWD-017");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostics[0].Message.Should().Contain("line 1");
    }

    [Fact]
    public async Task EvaluateAsync_DuplicateAnchorSlug_ReportsWarning()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/docs.md", "# Who Is Steward For?\nText\n## Who, Is Steward For?\nMore text\n");

        var context = new ValidationContext
        {
            Policy = null,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs.md", 20, false)],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var diagnostics = await new UniqueHeadingTextRule().EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].Message.Should().Contain("who-is-steward-for");
    }

    [Fact]
    public async Task EvaluateAsync_UniqueHeadings_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/docs.md", "# Overview\nText\n## Goals\nMore text\n");

        var context = new ValidationContext
        {
            Policy = null,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs.md", 20, false)],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var diagnostics = await new UniqueHeadingTextRule().EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }
}
