using FluentAssertions;
using Steward.Core.Discovery;
using Steward.Core.Maintenance;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests.Maintenance;

public class ManifestMaintainerTests
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
    public void Evaluate_GeneratesManifestJson()
    {
        var fs = new InMemoryFileSystem();
        var files = new[]
        {
            new DiscoveredFile("README.md", 100, false),
            new DiscoveredFile("src/Program.cs", 200, false)
        };

        var config = new MaintenanceArtifactConfig
        {
            Id = "manifest",
            Path = ".steward/generated/manifest.json",
            Type = "manifest"
        };

        var maintainer = new ManifestMaintainer();
        var action = maintainer.Evaluate(config, CreateContext(fs, files));

        action.HasChanges.Should().BeTrue();
        action.ArtifactId.Should().Be("manifest");
        action.ExpectedContent.Should().Contain("\"generatedBy\": \"steward maintain\"");
        action.ExpectedContent.Should().Contain("\"fileCount\": 2");
        action.ExpectedContent.Should().Contain("\"path\": \"README.md\"");
        action.ExpectedContent.Should().Contain("\"path\": \"src/Program.cs\"");
    }

    [Fact]
    public void Evaluate_IncludesFileExtensions()
    {
        var fs = new InMemoryFileSystem();
        var files = new[]
        {
            new DiscoveredFile("README.md", 100, false),
            new DiscoveredFile("src/main.py", 200, false)
        };

        var config = new MaintenanceArtifactConfig
        {
            Id = "manifest",
            Path = ".steward/generated/manifest.json",
            Type = "manifest"
        };

        var maintainer = new ManifestMaintainer();
        var action = maintainer.Evaluate(config, CreateContext(fs, files));

        action.ExpectedContent.Should().Contain("\".md\"");
        action.ExpectedContent.Should().Contain("\".py\"");
    }

    [Fact]
    public void Evaluate_Idempotent_WhenUpToDate()
    {
        var files = new[]
        {
            new DiscoveredFile("README.md", 100, false)
        };

        var config = new MaintenanceArtifactConfig
        {
            Id = "manifest",
            Path = ".steward/generated/manifest.json",
            Type = "manifest"
        };

        var maintainer = new ManifestMaintainer();

        // First pass: generates content
        var fs1 = new InMemoryFileSystem();
        var action1 = maintainer.Evaluate(config, CreateContext(fs1, files));
        action1.HasChanges.Should().BeTrue();

        // Second pass: file matches expected
        var fs2 = new InMemoryFileSystem()
            .AddFile("/repo/.steward/generated/manifest.json", action1.ExpectedContent!);
        var action2 = maintainer.Evaluate(config, CreateContext(fs2, files));
        action2.HasChanges.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_ExcludesDirectories_FromFileCount()
    {
        var fs = new InMemoryFileSystem();
        var files = new[]
        {
            new DiscoveredFile("README.md", 100, false),
            new DiscoveredFile("src", 0, true),
            new DiscoveredFile("src/app.js", 300, false)
        };

        var config = new MaintenanceArtifactConfig
        {
            Id = "manifest",
            Path = ".steward/generated/manifest.json",
            Type = "manifest"
        };

        var maintainer = new ManifestMaintainer();
        var action = maintainer.Evaluate(config, CreateContext(fs, files));

        // Only files, not directories
        action.ExpectedContent.Should().Contain("\"fileCount\": 2");
    }

    [Fact]
    public void Evaluate_IncludesHeadings_ForMarkdownFiles()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("/repo/README.md", "# Title\n\n## Section One\n\nContent here.\n\n## Section Two\n\nMore content.");

        var files = new[]
        {
            new DiscoveredFile("README.md", 100, false)
        };

        var config = new MaintenanceArtifactConfig
        {
            Id = "manifest",
            Path = ".steward/generated/manifest.json",
            Type = "manifest"
        };

        var docCache = new Core.Markdown.DocumentCache(fs, "/repo");
        var ctx = new MaintenanceContext
        {
            RepositoryRoot = "/repo",
            FileSystem = fs,
            Files = files.ToList(),
            DocumentCache = docCache
        };

        var maintainer = new ManifestMaintainer();
        var action = maintainer.Evaluate(config, ctx);

        action.ExpectedContent.Should().Contain("\"headings\"");
        action.ExpectedContent.Should().Contain("\"text\": \"Title\"");
        action.ExpectedContent.Should().Contain("\"text\": \"Section One\"");
        action.ExpectedContent.Should().Contain("\"text\": \"Section Two\"");
    }

    [Fact]
    public void Evaluate_SortsFiles_CaseInsensitive()
    {
        var fs = new InMemoryFileSystem();
        var files = new[]
        {
            new DiscoveredFile("zeta.md", 100, false),
            new DiscoveredFile("Alpha.md", 100, false),
            new DiscoveredFile("beta.md", 100, false)
        };

        var config = new MaintenanceArtifactConfig
        {
            Id = "manifest",
            Path = ".steward/generated/manifest.json",
            Type = "manifest"
        };

        var maintainer = new ManifestMaintainer();
        var action = maintainer.Evaluate(config, CreateContext(fs, files));

        var alphaIndex = action.ExpectedContent!.IndexOf("Alpha.md", StringComparison.Ordinal);
        var betaIndex = action.ExpectedContent.IndexOf("beta.md", StringComparison.Ordinal);
        var zetaIndex = action.ExpectedContent.IndexOf("zeta.md", StringComparison.Ordinal);

        alphaIndex.Should().BeLessThan(betaIndex);
        betaIndex.Should().BeLessThan(zetaIndex);
    }
}
