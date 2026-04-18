using FluentAssertions;
using Steward.Core.Discovery;
using Steward.Core.Maintenance;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests.Maintenance;

public class FrontmatterAutoMaintainerTests
{
    private static MaintenanceContext CreateContext(
        InMemoryFileSystem fs,
        IReadOnlySet<string>? changedFiles = null,
        params DiscoveredFile[] files)
    {
        return new MaintenanceContext
        {
            RepositoryRoot = "/repo",
            FileSystem = fs,
            Files = files.ToList(),
            ChangedFiles = changedFiles
        };
    }

    [Fact]
    public void Evaluate_DetectsOutdatedFrontmatter()
    {
        // The file has last_updated="2020-01-01" but file mtime will be today
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/doc.md", "---\nlast_updated: \"2020-01-01\"\n---\n# Doc\nContent.");

        var files = new[] { new DiscoveredFile("doc.md", 100, false) };

        var config = new MaintenanceArtifactConfig
        {
            Id = "fm-auto",
            Path = "",
            Type = "frontmatter-auto",
            Targets = "**/*.md",
            Fields = new Dictionary<string, string> { ["last_updated"] = "file-mtime" }
        };

        var maintainer = new FrontmatterAutoMaintainer();
        var action = maintainer.Evaluate(config, CreateContext(fs, null, files));

        action.HasChanges.Should().BeTrue();
        action.Description.Should().Contain("1 file(s) need frontmatter updates");
        action.FileEdits.Should().HaveCount(1);
        action.FileEdits[0].ExpectedContent.Should().Contain("last_updated:");
    }

    [Fact]
    public void Evaluate_UpToDate_ReportsNoChanges()
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/doc.md", $"---\nlast_updated: \"{today}\"\n---\n# Doc");

        var files = new[] { new DiscoveredFile("doc.md", 100, false) };

        var config = new MaintenanceArtifactConfig
        {
            Id = "fm-auto",
            Path = "",
            Type = "frontmatter-auto",
            Targets = "**/*.md",
            Fields = new Dictionary<string, string> { ["last_updated"] = "file-mtime" }
        };

        var maintainer = new FrontmatterAutoMaintainer();
        var action = maintainer.Evaluate(config, CreateContext(fs, null, files));

        action.HasChanges.Should().BeFalse();
        action.Description.Should().Contain("up to date");
    }

    [Fact]
    public void Evaluate_NoFields_ReportsNoChanges()
    {
        var fs = new InMemoryFileSystem();

        var config = new MaintenanceArtifactConfig
        {
            Id = "fm-auto",
            Path = "",
            Type = "frontmatter-auto",
            Targets = "**/*.md"
        };

        var maintainer = new FrontmatterAutoMaintainer();
        var action = maintainer.Evaluate(config, CreateContext(fs));

        action.HasChanges.Should().BeFalse();
        action.Description.Should().Contain("No fields configured");
    }

    [Fact]
    public void Evaluate_LiteralFieldValue_DetectsMismatch()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/doc.md", "---\nstatus: draft\n---\n# Doc");

        var files = new[] { new DiscoveredFile("doc.md", 100, false) };

        var config = new MaintenanceArtifactConfig
        {
            Id = "fm-auto",
            Path = "",
            Type = "frontmatter-auto",
            Targets = "**/*.md",
            Fields = new Dictionary<string, string> { ["status"] = "published" }
        };

        var maintainer = new FrontmatterAutoMaintainer();
        var action = maintainer.Evaluate(config, CreateContext(fs, null, files));

        action.HasChanges.Should().BeTrue();
        action.FileEdits[0].ExpectedContent.Should().Contain("status: published");
    }

    [Fact]
    public void Evaluate_LiteralFieldValue_MatchingIsUpToDate()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/doc.md", "---\nstatus: published\n---\n# Doc");

        var files = new[] { new DiscoveredFile("doc.md", 100, false) };

        var config = new MaintenanceArtifactConfig
        {
            Id = "fm-auto",
            Path = "",
            Type = "frontmatter-auto",
            Targets = "**/*.md",
            Fields = new Dictionary<string, string> { ["status"] = "published" }
        };

        var maintainer = new FrontmatterAutoMaintainer();
        var action = maintainer.Evaluate(config, CreateContext(fs, null, files));

        action.HasChanges.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_MultipleFiles_CountsAllStale()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/a.md", "---\nstatus: old\n---\n# A")
            .AddFile("/repo/b.md", "---\nstatus: old\n---\n# B")
            .AddFile("/repo/c.md", "---\nstatus: current\n---\n# C");

        var files = new[]
        {
            new DiscoveredFile("a.md", 10, false),
            new DiscoveredFile("b.md", 10, false),
            new DiscoveredFile("c.md", 10, false)
        };

        var config = new MaintenanceArtifactConfig
        {
            Id = "fm-auto",
            Path = "",
            Type = "frontmatter-auto",
            Targets = "**/*.md",
            Fields = new Dictionary<string, string> { ["status"] = "current" }
        };

        var maintainer = new FrontmatterAutoMaintainer();
        var action = maintainer.Evaluate(config, CreateContext(fs, null, files));

        action.HasChanges.Should().BeTrue();
        action.Description.Should().Contain("2 file(s)");
        action.FileEdits.Should().HaveCount(2);
    }

    [Fact]
    public void Evaluate_TodayIfLocalChange_UpdatesConfiguredFieldWhenFileChanged()
    {
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/doc.md", "---\nlast_updated: 2020-01-01\n---\n# Doc\nChanged.");

        var config = new MaintenanceArtifactConfig
        {
            Id = "fm-auto",
            Path = "",
            Type = "frontmatter-auto",
            Targets = "**/*.md",
            Fields = new Dictionary<string, string> { ["last_updated"] = "today-if-local-change" }
        };

        var action = new FrontmatterAutoMaintainer().Evaluate(
            config,
            CreateContext(
                fs,
                new HashSet<string>(["doc.md"], StringComparer.OrdinalIgnoreCase),
                new DiscoveredFile("doc.md", 100, false)));

        action.HasChanges.Should().BeTrue();
        action.FileEdits.Should().HaveCount(1);
        action.FileEdits[0].ExpectedContent.Should().Contain($"last_updated: {today}");
    }

    [Fact]
    public void Evaluate_TodayIfLocalChange_SkipsFilesWithoutConfiguredField()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/doc.md", "---\ntitle: Doc\n---\n# Doc\nChanged.");

        var config = new MaintenanceArtifactConfig
        {
            Id = "fm-auto",
            Path = "",
            Type = "frontmatter-auto",
            Targets = "**/*.md",
            Fields = new Dictionary<string, string> { ["last_updated"] = "today-if-local-change" }
        };

        var action = new FrontmatterAutoMaintainer().Evaluate(
            config,
            CreateContext(
                fs,
                new HashSet<string>(["doc.md"], StringComparer.OrdinalIgnoreCase),
                new DiscoveredFile("doc.md", 100, false)));

        action.HasChanges.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_TodayIfLocalChange_WithoutGitChangeDetection_IsBlocked()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/doc.md", "---\nlast_updated: 2020-01-01\n---\n# Doc\nChanged.");

        var config = new MaintenanceArtifactConfig
        {
            Id = "fm-auto",
            Path = "",
            Type = "frontmatter-auto",
            Targets = "**/*.md",
            Fields = new Dictionary<string, string> { ["last_updated"] = "today-if-local-change" }
        };

        var action = new FrontmatterAutoMaintainer().Evaluate(
            config,
            CreateContext(fs, null, new DiscoveredFile("doc.md", 100, false)));

        action.IsBlocked.Should().BeTrue();
        action.BlockedReason.Should().Contain("git");
    }
}
