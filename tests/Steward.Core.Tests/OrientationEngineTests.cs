using FluentAssertions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Orientation;
using Xunit;

namespace Steward.Core.Tests;

public class OrientationEngineTests
{
    [Fact]
    public void Classify_Readme_IsAuthoritative()
    {
        var file = new DiscoveredFile("README.md", 100, false);
        OrientationEngine.Classify(file).Should().Be("authoritative");
    }

    [Fact]
    public void Classify_License_IsAuthoritative()
    {
        var file = new DiscoveredFile("LICENSE", 1000, false);
        OrientationEngine.Classify(file).Should().Be("authoritative");
    }

    [Fact]
    public void Classify_SrcDirectory_IsSource()
    {
        var file = new DiscoveredFile("src", 0, true);
        OrientationEngine.Classify(file).Should().Be("source");
    }

    [Fact]
    public void Classify_TestsDirectory_IsTesting()
    {
        var file = new DiscoveredFile("tests", 0, true);
        OrientationEngine.Classify(file).Should().Be("testing");
    }

    [Fact]
    public void Classify_DocsDirectory_IsDocumentation()
    {
        var file = new DiscoveredFile("docs", 0, true);
        OrientationEngine.Classify(file).Should().Be("documentation");
    }

    [Fact]
    public void Classify_GithubDirectory_IsWorkflow()
    {
        var file = new DiscoveredFile(".github", 0, true);
        OrientationEngine.Classify(file).Should().Be("workflow");
    }

    [Fact]
    public void Classify_CSharpFile_IsSource()
    {
        var file = new DiscoveredFile("app.cs", 500, false);
        OrientationEngine.Classify(file).Should().Be("source");
    }

    [Fact]
    public void Classify_MarkdownFile_IsDocumentation()
    {
        var file = new DiscoveredFile("notes.md", 200, false);
        OrientationEngine.Classify(file).Should().Be("documentation");
    }

    [Fact]
    public void Classify_FileInSrcDir_IsSource()
    {
        var file = new DiscoveredFile("src/deep/app.cs", 500, false);
        OrientationEngine.Classify(file).Should().Be("source");
    }

    [Fact]
    public void Classify_UsesPolicyRole_WhenConfigured()
    {
        var file = new DiscoveredFile("AGENT_GUIDE.txt", 10, false);
        var policy = new RepositoryPolicy
        {
            Artifacts =
            [
                new ArtifactDefinition
                {
                    Path = "AGENT_GUIDE.txt",
                    Role = "authoritative",
                    Required = false
                }
            ]
        };

        OrientationEngine.Classify(file, policy).Should().Be("authoritative");
    }

    [Fact]
    public void Orient_RespectsMaxDepth()
    {
        var files = new List<DiscoveredFile>
        {
            new("src", 0, true),
            new("src/app", 0, true),
            new("src/app/deep", 0, true),
            new("src/app/deep/deeper", 0, true),
        };

        var engine = new OrientationEngine();
        var result = engine.Orient("/repo", files, maxDepth: 2);

        result.Entries.Should().HaveCount(2);
        result.Entries.Should().Contain(e => e.Path == "src");
        result.Entries.Should().Contain(e => e.Path == "src/app");
    }

    [Theory]
    [InlineData("vision")]
    [InlineData("roadmap")]
    [InlineData("current-state")]
    [InlineData("milestones")]
    [InlineData("decision-log")]
    public void Classify_StateDocumentRole_GetsStatePrefix(string role)
    {
        var file = new DiscoveredFile("VISION.md", 100, false);
        var policy = new RepositoryPolicy
        {
            Artifacts = [new ArtifactDefinition { Path = "VISION.md", Role = role }]
        };

        var result = OrientationEngine.Classify(file, policy);

        result.Should().Be($"state:{role}");
    }

    [Theory]
    [InlineData("authoritative")]
    [InlineData("governance")]
    [InlineData("documentation")]
    public void Classify_NonStateRole_NoPrefix(string role)
    {
        var file = new DiscoveredFile("doc.md", 100, false);
        var policy = new RepositoryPolicy
        {
            Artifacts = [new ArtifactDefinition { Path = "doc.md", Role = role }]
        };

        var result = OrientationEngine.Classify(file, policy);

        result.Should().Be(role);
    }

    [Fact]
    public void Classify_ExtensionFallback_JsonIsConfiguration()
    {
        var file = new DiscoveredFile("settings.json", 50, false);
        OrientationEngine.Classify(file).Should().Be("configuration");
    }

    [Fact]
    public void Classify_ExtensionFallback_ShIsTool()
    {
        var file = new DiscoveredFile("build.sh", 50, false);
        OrientationEngine.Classify(file).Should().Be("tooling");
    }

    [Fact]
    public void Classify_UnknownExtension_IsOther()
    {
        var file = new DiscoveredFile("data.xyz", 50, false);
        OrientationEngine.Classify(file).Should().Be("other");
    }
}
