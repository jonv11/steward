using FluentAssertions;
using Steward.Core.Discovery;
using Steward.Core.Maintenance;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class MoveEngineTests
{
    [Fact]
    public void ComputeMove_UpdatesReferencesInOtherFiles()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/root/README.md", "# Root\n[Guide](docs/guide.md)");
        fs.AddFile("/root/docs/guide.md", "# Guide");

        var files = new List<DiscoveredFile>
        {
            new("README.md", 30, false),
            new("docs/guide.md", 10, false)
        };

        var plan = MoveEngine.ComputeMove("docs/guide.md", "docs/tutorial.md", files, fs, "/root");

        plan.Edits.Should().HaveCount(1);
        plan.Edits[0].FilePath.Should().Be("README.md");
        plan.Edits[0].NewContent.Should().Contain("](docs/tutorial.md)");
        plan.Edits[0].NewContent.Should().NotContain("](docs/guide.md)");
    }

    [Fact]
    public void ComputeMove_NoReferences_EmptyEdits()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/root/README.md", "# Root\nNo links here.");
        fs.AddFile("/root/orphan.md", "# Orphan");

        var files = new List<DiscoveredFile>
        {
            new("README.md", 20, false),
            new("orphan.md", 10, false)
        };

        var plan = MoveEngine.ComputeMove("orphan.md", "archive/orphan.md", files, fs, "/root");

        plan.Edits.Should().BeEmpty();
    }

    [Fact]
    public void ComputeRelativePath_SameDirectory()
    {
        var result = MoveEngine.ComputeRelativePath("docs/guide.md", "docs/tutorial.md");
        result.Should().Be("tutorial.md");
    }

    [Fact]
    public void ComputeRelativePath_CrossDirectory()
    {
        var result = MoveEngine.ComputeRelativePath("docs/planning/guide.md", "docs/tutorial.md");
        result.Should().Be("../tutorial.md");
    }

    [Fact]
    public void ComputeRelativePath_FromRoot()
    {
        var result = MoveEngine.ComputeRelativePath("README.md", "docs/guide.md");
        result.Should().Be("docs/guide.md");
    }
}
