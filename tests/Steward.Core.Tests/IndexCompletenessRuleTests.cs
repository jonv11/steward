using FluentAssertions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class IndexCompletenessRuleTests
{
    [Fact]
    public async Task Evaluate_MissingLink_ReportsWarning()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/docs/index.md", "# Index\n\n- [Alpha](items/alpha.md)\n")
            .AddFile("/repo/docs/items/alpha.md", "# Alpha")
            .AddFile("/repo/docs/items/beta.md", "# Beta");

        var policy = new RepositoryPolicy
        {
            Artifacts =
            [
                new ArtifactDefinition { Path = "docs/index.md", IndexOf = "docs/items" }
            ]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles =
            [
                new DiscoveredFile("docs/index.md", 100, false),
                new DiscoveredFile("docs/items/alpha.md", 50, false),
                new DiscoveredFile("docs/items/beta.md", 50, false)
            ],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new IndexCompletenessRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().ContainSingle();
        diagnostics[0].RuleId.Should().Be("STWD-011");
        diagnostics[0].Path.Should().Be("docs/items/beta.md");
    }

    [Fact]
    public async Task Evaluate_AllLinked_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/docs/index.md", "# Index\n\n- [Alpha](items/alpha.md)\n- [Beta](items/beta.md)\n")
            .AddFile("/repo/docs/items/alpha.md", "# Alpha")
            .AddFile("/repo/docs/items/beta.md", "# Beta");

        var policy = new RepositoryPolicy
        {
            Artifacts =
            [
                new ArtifactDefinition { Path = "docs/index.md", IndexOf = "docs/items" }
            ]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles =
            [
                new DiscoveredFile("docs/index.md", 100, false),
                new DiscoveredFile("docs/items/alpha.md", 50, false),
                new DiscoveredFile("docs/items/beta.md", 50, false)
            ],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new IndexCompletenessRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_NoIndexOfArtifacts_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/readme.md", "# Hello");

        var policy = new RepositoryPolicy
        {
            Artifacts =
            [
                new ArtifactDefinition { Path = "readme.md", Role = "readme" }
            ]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("readme.md", 100, false)],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new IndexCompletenessRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_IndexFileItself_NotFlagged()
    {
        // If the index lives inside the indexed directory, it should not flag itself
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/docs/index.md", "# Index\n\n- [Alpha](alpha.md)\n")
            .AddFile("/repo/docs/alpha.md", "# Alpha");

        var policy = new RepositoryPolicy
        {
            Artifacts =
            [
                new ArtifactDefinition { Path = "docs/index.md", IndexOf = "docs" }
            ]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles =
            [
                new DiscoveredFile("docs/index.md", 100, false),
                new DiscoveredFile("docs/alpha.md", 50, false)
            ],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new IndexCompletenessRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_MultipleUnlinked_ReportsAll()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/index.md", "# Index\n\nNothing linked here.\n")
            .AddFile("/repo/items/a.md", "# A")
            .AddFile("/repo/items/b.md", "# B")
            .AddFile("/repo/items/c.md", "# C");

        var policy = new RepositoryPolicy
        {
            Artifacts =
            [
                new ArtifactDefinition { Path = "index.md", IndexOf = "items" }
            ]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles =
            [
                new DiscoveredFile("index.md", 100, false),
                new DiscoveredFile("items/a.md", 50, false),
                new DiscoveredFile("items/b.md", 50, false),
                new DiscoveredFile("items/c.md", 50, false)
            ],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new IndexCompletenessRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(3);
        diagnostics.Should().OnlyContain(d => d.RuleId == "STWD-011");
    }
}
