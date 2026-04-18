using FluentAssertions;
using Steward.Core.Discovery;
using Steward.Core.Maintenance;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class DirectoryIndexMaintainerTests
{
    [Fact]
    public void Evaluate_GeneratesTable()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/docs/alpha.md", "---\ntitle: Alpha Doc\nstatus: Accepted\ndescription: First document\n---\n# Alpha")
            .AddFile("/repo/docs/beta.md", "---\ntitle: Beta Doc\nstatus: Draft\ndescription: Second document\n---\n# Beta");

        var config = new MaintenanceArtifactConfig
        {
            Id = "doc-index",
            Path = "docs/index.md",
            Type = "directory-index",
            Source = "docs/*.md"
        };

        var context = new MaintenanceContext
        {
            RepositoryRoot = "/repo",
            FileSystem = fs,
            Files =
            [
                new DiscoveredFile("docs/alpha.md", 100, false),
                new DiscoveredFile("docs/beta.md", 100, false)
            ]
        };

        var maintainer = new DirectoryIndexMaintainer();
        var action = maintainer.Evaluate(config, context);

        action.HasChanges.Should().BeTrue();
        action.ExpectedContent.Should().Contain("| Alpha Doc |");
        action.ExpectedContent.Should().Contain("| Beta Doc |");
        action.ExpectedContent.Should().Contain("First document");
        action.ExpectedContent.Should().Contain("| Title | Path | Status | Description |");
    }

    [Fact]
    public void Evaluate_SortsByFilename()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/docs/zebra.md", "---\ndescription: Zebra document\n---\n# Zebra")
            .AddFile("/repo/docs/alpha.md", "---\ndescription: Alpha document\n---\n# Alpha");

        var config = new MaintenanceArtifactConfig
        {
            Id = "doc-index",
            Path = "docs/index.md",
            Type = "directory-index",
            Source = "docs/*.md",
            Sort = "filename"
        };

        var context = new MaintenanceContext
        {
            RepositoryRoot = "/repo",
            FileSystem = fs,
            Files =
            [
                new DiscoveredFile("docs/zebra.md", 100, false),
                new DiscoveredFile("docs/alpha.md", 100, false)
            ]
        };

        var maintainer = new DirectoryIndexMaintainer();
        var action = maintainer.Evaluate(config, context);

        action.HasChanges.Should().BeTrue();
        var lines = action.ExpectedContent!.Split('\n');
        var alphaIdx = Array.FindIndex(lines, l => l.Contains("Alpha"));
        var zebraIdx = Array.FindIndex(lines, l => l.Contains("Zebra"));
        alphaIdx.Should().BeLessThan(zebraIdx);
    }

    [Fact]
    public void Evaluate_IsIdempotent()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/docs/alpha.md", "---\ndescription: Alpha overview\n---\n# Alpha");

        var config = new MaintenanceArtifactConfig
        {
            Id = "doc-index",
            Path = "docs/index.md",
            Type = "directory-index",
            Source = "docs/*.md"
        };

        var context = new MaintenanceContext
        {
            RepositoryRoot = "/repo",
            FileSystem = fs,
            Files = [new DiscoveredFile("docs/alpha.md", 100, false)]
        };

        var maintainer = new DirectoryIndexMaintainer();

        // First pass — generates content
        var action1 = maintainer.Evaluate(config, context);
        action1.HasChanges.Should().BeTrue();

        // Write the expected content
        fs.AddFile("/repo/docs/index.md", action1.ExpectedContent!);
        context = new MaintenanceContext
        {
            RepositoryRoot = "/repo",
            FileSystem = fs,
            Files =
            [
                new DiscoveredFile("docs/alpha.md", 100, false),
                new DiscoveredFile("docs/index.md", 100, false)
            ]
        };

        // Second pass — should detect no changes
        var action2 = maintainer.Evaluate(config, context);
        action2.HasChanges.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_UsesLinksRelativeToTargetFile()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/docs/decisions/adrs/ADR-001.md", "---\ndescription: Decision\n---\n# ADR 001");

        var config = new MaintenanceArtifactConfig
        {
            Id = "decision-index",
            Path = "docs/decisions/index.md",
            Type = "directory-index",
            Source = "docs/decisions/adrs/*.md"
        };

        var context = new MaintenanceContext
        {
            RepositoryRoot = "/repo",
            FileSystem = fs,
            Files = [new DiscoveredFile("docs/decisions/adrs/ADR-001.md", 100, false)]
        };

        var action = new DirectoryIndexMaintainer().Evaluate(config, context);

        action.ExpectedContent.Should().Contain("[ADR-001.md](adrs/ADR-001.md)");
        action.ExpectedContent.Should().NotContain("(docs/decisions/adrs/ADR-001.md)");
    }

    [Fact]
    public void GenerateTable_ProducesValidMarkdown()
    {
        var rows = new List<(string Title, string RelPath, string Status, string Description)>
        {
            ("My | Title", "docs/my-file.md", "Accepted", "Description with | pipe"),
            ("Simple", "docs/simple.md", "", "")
        };

        var table = DirectoryIndexMaintainer.GenerateTable(rows);

        table.Should().Contain("| My \\| Title |");
        table.Should().Contain("Description with \\| pipe");
        table.Should().Contain("[simple.md](docs/simple.md)");
    }

    [Fact]
    public void Evaluate_MissingDescription_BlocksGeneration()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/docs/alpha.md", "# Alpha");

        var config = new MaintenanceArtifactConfig
        {
            Id = "doc-index",
            Path = "docs/index.md",
            Type = "directory-index",
            Source = "docs/*.md"
        };

        var context = new MaintenanceContext
        {
            RepositoryRoot = "/repo",
            FileSystem = fs,
            Files = [new DiscoveredFile("docs/alpha.md", 100, false)]
        };

        var action = new DirectoryIndexMaintainer().Evaluate(config, context);

        action.IsBlocked.Should().BeTrue();
        action.Description.Should().Contain("missing a non-empty frontmatter.description");
    }
}
