using FluentAssertions;
using Steward.Core.Discovery;
using Steward.Core.Maintenance;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests.Maintenance;

public class IndexMaintainerTests
{
    private static MaintenanceContext CreateContext(InMemoryFileSystem fs, params DiscoveredFile[] files)
    {
        return new MaintenanceContext
        {
            RepositoryRoot = "/repo",
            FileSystem = fs,
            Files = files.ToList()
        };
    }

    [Fact]
    public void Evaluate_GeneratesFullIndex()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/docs/guide.md", "---\ntitle: User Guide\n---\n# Guide\nContent.")
            .AddFile("/repo/docs/faq.md", "# FAQ\nQuestions.");

        var files = new[]
        {
            new DiscoveredFile("docs/guide.md", 100, false),
            new DiscoveredFile("docs/faq.md", 50, false)
        };

        var config = new MaintenanceArtifactConfig
        {
            Id = "doc-index",
            Path = "INDEX.md",
            Type = "index",
            Source = "docs/**/*.md"
        };

        var maintainer = new IndexMaintainer();
        var action = maintainer.Evaluate(config, CreateContext(fs, files));

        action.HasChanges.Should().BeTrue();
        action.ExpectedContent.Should().Contain("# Index");
        action.ExpectedContent.Should().Contain("[FAQ](docs/faq.md)");
        action.ExpectedContent.Should().Contain("[User Guide](docs/guide.md)");
    }

    [Fact]
    public void Evaluate_ExtractsTitleFromFrontmatter()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/doc.md", "---\ntitle: My Document\n---\n# Heading\nBody.");

        var files = new[] { new DiscoveredFile("doc.md", 100, false) };

        var config = new MaintenanceArtifactConfig
        {
            Id = "idx",
            Path = "INDEX.md",
            Type = "index",
            Source = "**/*.md"
        };

        var maintainer = new IndexMaintainer();
        var action = maintainer.Evaluate(config, CreateContext(fs, files));

        action.ExpectedContent.Should().Contain("[My Document](doc.md)");
    }

    [Fact]
    public void Evaluate_ExtractsTitleFromFirstHeading()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/doc.md", "# First Heading\nBody text.");

        var files = new[] { new DiscoveredFile("doc.md", 100, false) };

        var config = new MaintenanceArtifactConfig
        {
            Id = "idx",
            Path = "INDEX.md",
            Type = "index",
            Source = "**/*.md"
        };

        var maintainer = new IndexMaintainer();
        var action = maintainer.Evaluate(config, CreateContext(fs, files));

        action.ExpectedContent.Should().Contain("[First Heading](doc.md)");
    }

    [Fact]
    public void Evaluate_SortsByFilename()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/z-file.md", "# Z File")
            .AddFile("/repo/a-file.md", "# A File");

        var files = new[]
        {
            new DiscoveredFile("z-file.md", 10, false),
            new DiscoveredFile("a-file.md", 10, false)
        };

        var config = new MaintenanceArtifactConfig
        {
            Id = "idx",
            Path = "INDEX.md",
            Type = "index",
            Source = "**/*.md",
            Sort = "filename"
        };

        var maintainer = new IndexMaintainer();
        var action = maintainer.Evaluate(config, CreateContext(fs, files));

        var content = action.ExpectedContent!;
        var aIdx = content.IndexOf("a-file.md");
        var zIdx = content.IndexOf("z-file.md");
        aIdx.Should().BeLessThan(zIdx);
    }

    [Fact]
    public void Evaluate_UpdatesManagedSection()
    {
        var existingContent = "# README\n\nSome intro.\n\n<!-- steward:begin id=\"file-index\" owner=\"steward\" -->\n- [Old](old.md)\n<!-- steward:end -->\n\nFooter text.";

        var fs = new InMemoryFileSystem()
            .AddFile("/repo/README.md", existingContent)
            .AddFile("/repo/docs/new.md", "# New Document");

        var files = new[] { new DiscoveredFile("docs/new.md", 50, false) };

        var config = new MaintenanceArtifactConfig
        {
            Id = "file-index",
            Path = "README.md",
            Type = "index",
            Source = "docs/**/*.md",
            ManagedSection = "file-index"
        };

        var maintainer = new IndexMaintainer();
        var action = maintainer.Evaluate(config, CreateContext(fs, files));

        action.HasChanges.Should().BeTrue();
        action.ExpectedContent.Should().Contain("[New Document](docs/new.md)");
        action.ExpectedContent.Should().Contain("Footer text.");
        action.ExpectedContent.Should().NotContain("[Old](old.md)");
    }

    [Fact]
    public void Evaluate_ManagedSection_MissingMarkers_ReportsNoChanges()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/README.md", "# README\nNo markers here.");

        var files = new[] { new DiscoveredFile("docs/a.md", 50, false) };

        var config = new MaintenanceArtifactConfig
        {
            Id = "idx",
            Path = "README.md",
            Type = "index",
            Source = "docs/**/*.md",
            ManagedSection = "idx"
        };

        var maintainer = new IndexMaintainer();
        var action = maintainer.Evaluate(config, CreateContext(fs, files));

        action.HasChanges.Should().BeFalse();
        action.Description.Should().Contain("not found");
    }

    [Fact]
    public void Evaluate_IsIdempotent()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/doc.md", "# Document");

        var files = new[] { new DiscoveredFile("doc.md", 50, false) };

        var config = new MaintenanceArtifactConfig
        {
            Id = "idx",
            Path = "INDEX.md",
            Type = "index",
            Source = "**/*.md"
        };

        var maintainer = new IndexMaintainer();

        // First run generates content
        var first = maintainer.Evaluate(config, CreateContext(fs, files));
        first.HasChanges.Should().BeTrue();

        // Write content and re-evaluate
        fs.AddFile("/repo/INDEX.md", first.ExpectedContent!);
        var second = maintainer.Evaluate(config, CreateContext(fs, files));
        second.HasChanges.Should().BeFalse();
    }
}
