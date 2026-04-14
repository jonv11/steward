using FluentAssertions;
using Steward.Core.Discovery;
using Steward.Core.Maintenance;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests.Maintenance;

public class ManagedSectionMaintainerTests
{
    private static MaintenanceContext CreateContext(InMemoryFileSystem fs)
    {
        return new MaintenanceContext
        {
            RepositoryRoot = "/repo",
            FileSystem = fs,
            Files = []
        };
    }

    [Fact]
    public void Evaluate_UpdatesSectionContent()
    {
        var target = "# Doc\n\n<!-- steward:begin id=\"status\" owner=\"steward\" -->\nOld content\n<!-- steward:end -->\n\nMore text.";
        var source = "New dynamic content\nLine 2";

        var fs = new InMemoryFileSystem()
            .AddFile("/repo/doc.md", target)
            .AddFile("/repo/source.txt", source);

        var config = new MaintenanceArtifactConfig
        {
            Id = "status",
            Path = "doc.md",
            Type = "managed-section",
            Source = "source.txt",
            ManagedSection = "status"
        };

        var maintainer = new ManagedSectionMaintainer();
        var action = maintainer.Evaluate(config, CreateContext(fs));

        action.HasChanges.Should().BeTrue();
        action.ExpectedContent.Should().Contain("New dynamic content");
        action.ExpectedContent.Should().Contain("Line 2");
        action.ExpectedContent.Should().Contain("More text.");
        action.ExpectedContent.Should().NotContain("Old content");
    }

    [Fact]
    public void Evaluate_UpToDate_ReportsNoChanges()
    {
        var source = "Current content";
        var target = "# Doc\n\n<!-- steward:begin id=\"sec\" owner=\"steward\" -->\nCurrent content\n<!-- steward:end -->";

        var fs = new InMemoryFileSystem()
            .AddFile("/repo/doc.md", target)
            .AddFile("/repo/source.txt", source);

        var config = new MaintenanceArtifactConfig
        {
            Id = "sec",
            Path = "doc.md",
            Type = "managed-section",
            Source = "source.txt"
        };

        var maintainer = new ManagedSectionMaintainer();
        var action = maintainer.Evaluate(config, CreateContext(fs));

        action.HasChanges.Should().BeFalse();
        action.Description.Should().Contain("up to date");
    }

    [Fact]
    public void Evaluate_TargetFileNotFound_ReportsNoChanges()
    {
        var fs = new InMemoryFileSystem();

        var config = new MaintenanceArtifactConfig
        {
            Id = "sec",
            Path = "nonexistent.md",
            Type = "managed-section",
            Source = "source.txt"
        };

        var maintainer = new ManagedSectionMaintainer();
        var action = maintainer.Evaluate(config, CreateContext(fs));

        action.HasChanges.Should().BeFalse();
        action.Description.Should().Contain("does not exist");
    }

    [Fact]
    public void Evaluate_MarkersNotFound_ReportsNoChanges()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/doc.md", "# Doc\nNo markers here.")
            .AddFile("/repo/source.txt", "Content");

        var config = new MaintenanceArtifactConfig
        {
            Id = "sec",
            Path = "doc.md",
            Type = "managed-section",
            Source = "source.txt"
        };

        var maintainer = new ManagedSectionMaintainer();
        var action = maintainer.Evaluate(config, CreateContext(fs));

        action.HasChanges.Should().BeFalse();
        action.Description.Should().Contain("markers not found");
    }

    [Fact]
    public void Evaluate_NoSource_ReportsNoChanges()
    {
        var target = "# Doc\n\n<!-- steward:begin id=\"sec\" owner=\"steward\" -->\nOld\n<!-- steward:end -->";

        var fs = new InMemoryFileSystem()
            .AddFile("/repo/doc.md", target);

        var config = new MaintenanceArtifactConfig
        {
            Id = "sec",
            Path = "doc.md",
            Type = "managed-section"
        };

        var maintainer = new ManagedSectionMaintainer();
        var action = maintainer.Evaluate(config, CreateContext(fs));

        action.HasChanges.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_PreservesContentOutsideRegion()
    {
        var target = "Header line\n<!-- steward:begin id=\"x\" owner=\"steward\" -->\nOld\n<!-- steward:end -->\nFooter line";
        var source = "New";

        var fs = new InMemoryFileSystem()
            .AddFile("/repo/doc.md", target)
            .AddFile("/repo/src.txt", source);

        var config = new MaintenanceArtifactConfig
        {
            Id = "x",
            Path = "doc.md",
            Type = "managed-section",
            Source = "src.txt",
            ManagedSection = "x"
        };

        var maintainer = new ManagedSectionMaintainer();
        var action = maintainer.Evaluate(config, CreateContext(fs));

        action.ExpectedContent.Should().Contain("Header line");
        action.ExpectedContent.Should().Contain("Footer line");
        action.ExpectedContent.Should().Contain("New");
        action.ExpectedContent.Should().NotContain("Old");
    }

    [Fact]
    public void Evaluate_IsIdempotent()
    {
        var source = "Generated content";
        var target = "# Doc\n\n<!-- steward:begin id=\"sec\" owner=\"steward\" -->\nStale\n<!-- steward:end -->";

        var fs = new InMemoryFileSystem()
            .AddFile("/repo/doc.md", target)
            .AddFile("/repo/source.txt", source);

        var config = new MaintenanceArtifactConfig
        {
            Id = "sec",
            Path = "doc.md",
            Type = "managed-section",
            Source = "source.txt"
        };

        var maintainer = new ManagedSectionMaintainer();

        // First pass: has changes
        var first = maintainer.Evaluate(config, CreateContext(fs));
        first.HasChanges.Should().BeTrue();

        // Apply
        fs.AddFile("/repo/doc.md", first.ExpectedContent!);

        // Second pass: no changes
        var second = maintainer.Evaluate(config, CreateContext(fs));
        second.HasChanges.Should().BeFalse();
    }
}
