using FluentAssertions;
using Steward.Core.Configuration;
using Xunit;

namespace Steward.Core.Tests;

public class ArtifactFamilyClassifierTests
{
    private static ArtifactFamilyDefinition MakeFamily(
        string name,
        string? pathPattern = null,
        Dictionary<string, string>? frontmatter = null,
        string? displayName = null)
    {
        return new ArtifactFamilyDefinition
        {
            Family = name,
            DisplayName = displayName,
            Match = new ArtifactFamilyMatch
            {
                PathPattern = pathPattern,
                Frontmatter = frontmatter
            }
        };
    }

    [Fact]
    public void Classify_PathPatternOnly_MatchesCorrectPath()
    {
        var classifier = new ArtifactFamilyClassifier([
            MakeFamily("adr", pathPattern: "docs/adrs/ADR-*.md")
        ]);

        var result = classifier.Classify("docs/adrs/ADR-001-test.md", null);

        result.Should().NotBeNull();
        result!.Family.Should().Be("adr");
    }

    [Fact]
    public void Classify_PathPatternOnly_NoMatchOnDifferentPath()
    {
        var classifier = new ArtifactFamilyClassifier([
            MakeFamily("adr", pathPattern: "docs/adrs/ADR-*.md")
        ]);

        var result = classifier.Classify("docs/rfcs/RFC-001-test.md", null);

        result.Should().BeNull();
    }

    [Fact]
    public void Classify_FrontmatterOnly_MatchesWhenFieldMatches()
    {
        var classifier = new ArtifactFamilyClassifier([
            MakeFamily("adr", frontmatter: new() { ["type"] = "adr" })
        ]);

        var fields = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["type"] = "adr"
        };

        var result = classifier.Classify("any/path/doc.md", fields);

        result.Should().NotBeNull();
        result!.Family.Should().Be("adr");
    }

    [Fact]
    public void Classify_FrontmatterOnly_NoMatchWhenFieldDiffers()
    {
        var classifier = new ArtifactFamilyClassifier([
            MakeFamily("adr", frontmatter: new() { ["type"] = "adr" })
        ]);

        var fields = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["type"] = "rfc"
        };

        var result = classifier.Classify("any/path/doc.md", fields);

        result.Should().BeNull();
    }

    [Fact]
    public void Classify_AndSemantics_BothCriteriaMustMatch()
    {
        var classifier = new ArtifactFamilyClassifier([
            MakeFamily("adr",
                pathPattern: "docs/adrs/ADR-*.md",
                frontmatter: new() { ["type"] = "adr" })
        ]);

        // Path matches but frontmatter doesn't
        var fields = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["type"] = "rfc"
        };
        classifier.Classify("docs/adrs/ADR-001-test.md", fields).Should().BeNull();

        // Path doesn't match but frontmatter does
        var fields2 = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["type"] = "adr"
        };
        classifier.Classify("docs/rfcs/RFC-001-test.md", fields2).Should().BeNull();

        // Both match
        classifier.Classify("docs/adrs/ADR-001-test.md", fields2).Should().NotBeNull();
    }

    [Fact]
    public void Classify_FirstFamilyWinsOnMultipleMatches()
    {
        var classifier = new ArtifactFamilyClassifier([
            MakeFamily("first", pathPattern: "docs/**/*.md"),
            MakeFamily("second", pathPattern: "docs/**/*.md")
        ]);

        var result = classifier.Classify("docs/adrs/ADR-001.md", null);

        result!.Family.Should().Be("first");
    }

    [Fact]
    public void Classify_NullFamiliesList_ReturnsNull()
    {
        var classifier = new ArtifactFamilyClassifier(null);

        var result = classifier.Classify("docs/anything.md", null);

        result.Should().BeNull();
    }

    [Fact]
    public void Classify_EmptyFamiliesList_ReturnsNull()
    {
        var classifier = new ArtifactFamilyClassifier([]);

        var result = classifier.Classify("docs/anything.md", null);

        result.Should().BeNull();
    }

    [Fact]
    public void Classify_NullFrontmatterFields_MatchesPathPatternOnly()
    {
        var classifier = new ArtifactFamilyClassifier([
            MakeFamily("adr", pathPattern: "docs/adrs/ADR-*.md")
        ]);

        // path-pattern only family: null frontmatter fields is fine
        var result = classifier.Classify("docs/adrs/ADR-001-test.md", null);

        result.Should().NotBeNull();
        result!.Family.Should().Be("adr");
    }

    [Fact]
    public void Classify_FrontmatterMatchRequiredButFieldsMissing_NoMatch()
    {
        var classifier = new ArtifactFamilyClassifier([
            MakeFamily("adr",
                pathPattern: "docs/adrs/ADR-*.md",
                frontmatter: new() { ["type"] = "adr" })
        ]);

        // No frontmatter fields (null): AND semantics → no match because type criterion fails
        var result = classifier.Classify("docs/adrs/ADR-001-test.md", null);

        result.Should().BeNull();
    }

    [Fact]
    public void Classify_FrontmatterComparison_IsCaseInsensitive()
    {
        var classifier = new ArtifactFamilyClassifier([
            MakeFamily("adr", frontmatter: new() { ["type"] = "ADR" })
        ]);

        var fields = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["type"] = "adr"
        };

        classifier.Classify("docs/adrs/ADR-001.md", fields).Should().NotBeNull();
    }

    [Fact]
    public void AllFamilies_ReturnsDeclaredFamilies()
    {
        var classifier = new ArtifactFamilyClassifier([
            MakeFamily("adr", pathPattern: "docs/adrs/**"),
            MakeFamily("rfc", pathPattern: "docs/rfcs/**")
        ]);

        classifier.AllFamilies.Should().HaveCount(2);
        classifier.AllFamilies.Select(f => f.Family).Should().BeEquivalentTo(["adr", "rfc"]);
    }
}
