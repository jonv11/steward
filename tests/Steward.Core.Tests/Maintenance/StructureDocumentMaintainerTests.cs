using FluentAssertions;
using Steward.Core.Discovery;
using Steward.Core.Maintenance;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests.Maintenance;

public class StructureDocumentMaintainerTests
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
    public void Evaluate_GeneratesTreeDocument()
    {
        var fs = new InMemoryFileSystem();
        var files = new[]
        {
            new DiscoveredFile("README.md", 100, false),
            new DiscoveredFile("src/Program.cs", 200, false),
            new DiscoveredFile("src/Utils.cs", 150, false),
            new DiscoveredFile("docs/guide.md", 50, false)
        };

        var config = new MaintenanceArtifactConfig
        {
            Id = "structure",
            Path = "STRUCTURE.md",
            Type = "structure-document"
        };

        var maintainer = new StructureDocumentMaintainer();
        var action = maintainer.Evaluate(config, CreateContext(fs, files));

        action.HasChanges.Should().BeTrue();
        action.ArtifactId.Should().Be("structure");
        action.ExpectedContent.Should().Contain("# Repository Structure");
        action.ExpectedContent.Should().Contain("README.md");
        action.ExpectedContent.Should().Contain("src/");
        action.ExpectedContent.Should().Contain("Program.cs");
        action.ExpectedContent.Should().Contain("docs/");
    }

    [Fact]
    public void Evaluate_UpToDate_ReportsNoChanges()
    {
        var files = new[]
        {
            new DiscoveredFile("README.md", 100, false)
        };

        var config = new MaintenanceArtifactConfig
        {
            Id = "structure",
            Path = "STRUCTURE.md",
            Type = "structure-document"
        };

        // First pass to get expected content
        var fs = new InMemoryFileSystem();
        var maintainer = new StructureDocumentMaintainer();
        var first = maintainer.Evaluate(config, CreateContext(fs, files));

        // Now write it and evaluate again
        fs.AddFile("/repo/STRUCTURE.md", first.ExpectedContent!);
        var second = maintainer.Evaluate(config, CreateContext(fs, files));

        second.HasChanges.Should().BeFalse();
        second.Description.Should().Contain("up to date");
    }

    [Fact]
    public void Evaluate_RespectsDepthOption()
    {
        var fs = new InMemoryFileSystem();
        var files = new[]
        {
            new DiscoveredFile("a/b/c/deep.txt", 10, false)
        };

        var config = new MaintenanceArtifactConfig
        {
            Id = "structure",
            Path = "STRUCTURE.md",
            Type = "structure-document",
            Options = new MaintenanceOptions { Depth = 2 }
        };

        var maintainer = new StructureDocumentMaintainer();
        var action = maintainer.Evaluate(config, CreateContext(fs, files));

        // With depth=2, should show "a" and "b" but not "c" or "deep.txt"
        action.ExpectedContent.Should().Contain("a/");
        action.ExpectedContent.Should().Contain("b");
        action.ExpectedContent.Should().NotContain("deep.txt");
    }

    [Fact]
    public void Evaluate_RespectsExcludeOption()
    {
        var fs = new InMemoryFileSystem();
        var files = new[]
        {
            new DiscoveredFile("README.md", 100, false),
            new DiscoveredFile("node_modules/pkg/index.js", 50, false)
        };

        var config = new MaintenanceArtifactConfig
        {
            Id = "structure",
            Path = "STRUCTURE.md",
            Type = "structure-document",
            Options = new MaintenanceOptions { Exclude = ["node_modules/**"] }
        };

        var maintainer = new StructureDocumentMaintainer();
        var action = maintainer.Evaluate(config, CreateContext(fs, files));

        action.ExpectedContent.Should().Contain("README.md");
        action.ExpectedContent.Should().NotContain("node_modules");
    }

    [Fact]
    public void Evaluate_IsIdempotent()
    {
        var fs = new InMemoryFileSystem();
        var files = new[]
        {
            new DiscoveredFile("README.md", 100, false),
            new DiscoveredFile("src/main.cs", 200, false)
        };

        var config = new MaintenanceArtifactConfig
        {
            Id = "structure",
            Path = "STRUCTURE.md",
            Type = "structure-document"
        };

        var maintainer = new StructureDocumentMaintainer();
        var first = maintainer.Evaluate(config, CreateContext(fs, files));
        fs.AddFile("/repo/STRUCTURE.md", first.ExpectedContent!);

        var second = maintainer.Evaluate(config, CreateContext(fs, files));
        second.HasChanges.Should().BeFalse();

        // Running a third time also produces no changes
        var third = maintainer.Evaluate(config, CreateContext(fs, files));
        third.HasChanges.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_DeterministicSort()
    {
        var fs = new InMemoryFileSystem();
        var files = new[]
        {
            new DiscoveredFile("zebra.txt", 10, false),
            new DiscoveredFile("alpha.txt", 10, false),
            new DiscoveredFile("middle.txt", 10, false)
        };

        var config = new MaintenanceArtifactConfig
        {
            Id = "structure",
            Path = "STRUCTURE.md",
            Type = "structure-document"
        };

        var maintainer = new StructureDocumentMaintainer();
        var action = maintainer.Evaluate(config, CreateContext(fs, files));

        var content = action.ExpectedContent!;
        var alphaIdx = content.IndexOf("alpha.txt");
        var middleIdx = content.IndexOf("middle.txt");
        var zebraIdx = content.IndexOf("zebra.txt");

        alphaIdx.Should().BeLessThan(middleIdx);
        middleIdx.Should().BeLessThan(zebraIdx);
    }
}
