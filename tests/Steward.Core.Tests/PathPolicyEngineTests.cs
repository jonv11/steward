using FluentAssertions;
using Steward.Core.Configuration;
using Xunit;

namespace Steward.Core.Tests;

public class PathPolicyEngineTests
{
    private static PathPolicyDocument CreateDoc(params PathRule[] rules)
    {
        return new PathPolicyDocument
        {
            Rulesets =
            [
                new PathRuleSet { Name = "test", Rules = rules.ToList() }
            ]
        };
    }

    [Fact]
    public void Evaluate_NoRules_ReturnsUnclassified()
    {
        var engine = new PathPolicyEngine(null);
        var result = engine.Evaluate("README.md");

        result.Category.Should().Be("unclassified");
    }

    [Theory]
    [InlineData("required")]
    [InlineData("recommended")]
    [InlineData("optional")]
    [InlineData("discouraged")]
    [InlineData("forbidden")]
    [InlineData("reserved")]
    [InlineData("deprecated")]
    [InlineData("ignored")]
    public void Evaluate_AllCategories_Recognized(string category)
    {
        var doc = CreateDoc(new PathRule { Pattern = "*.md", Category = category });
        var engine = new PathPolicyEngine(doc);
        var result = engine.Evaluate("README.md");

        result.Category.Should().Be(category);
    }

    [Fact]
    public void Evaluate_ExactOverGlob_Wins()
    {
        var doc = CreateDoc(
            new PathRule { Pattern = "*.md", Category = "optional" },
            new PathRule { Pattern = "README.md", Category = "required", Exact = true });

        var engine = new PathPolicyEngine(doc);
        var result = engine.Evaluate("README.md");

        result.Category.Should().Be("required");
    }

    [Fact]
    public void Evaluate_HigherPriority_Wins()
    {
        var doc = CreateDoc(
            new PathRule { Pattern = "*.log", Category = "optional", Priority = 1 },
            new PathRule { Pattern = "*.log", Category = "forbidden", Priority = 10 });

        var engine = new PathPolicyEngine(doc);
        var result = engine.Evaluate("error.log");

        result.Category.Should().Be("forbidden");
    }

    [Fact]
    public void Evaluate_MoreSpecificPattern_Wins()
    {
        var doc = CreateDoc(
            new PathRule { Pattern = "*.md", Category = "optional" },
            new PathRule { Pattern = "docs/*.md", Category = "recommended" });

        var engine = new PathPolicyEngine(doc);
        var result = engine.Evaluate("docs/guide.md");

        result.Category.Should().Be("recommended");
    }

    [Fact]
    public void IsIgnored_IgnoredCategory_ReturnsTrue()
    {
        var doc = CreateDoc(new PathRule { Pattern = "*.tmp", Category = "ignored" });
        var engine = new PathPolicyEngine(doc);

        engine.IsIgnored("data.tmp").Should().BeTrue();
        engine.IsIgnored("data.txt").Should().BeFalse();
    }

    [Fact]
    public void Evaluate_GlobDoublestar_MatchesDeep()
    {
        var doc = CreateDoc(new PathRule { Pattern = "src/**/*.cs", Category = "source" });
        var engine = new PathPolicyEngine(doc);
        var result = engine.Evaluate("src/deep/nested/file.cs");

        result.Category.Should().Be("source");
    }

    [Fact]
    public void Evaluate_RequiredPresence_Tracked()
    {
        var doc = CreateDoc(
            new PathRule { Pattern = "README.md", Category = "required", Exact = true });

        var engine = new PathPolicyEngine(doc);
        var result = engine.Evaluate("README.md");

        result.Category.Should().Be("required");
        result.MatchedPattern.Should().Be("README.md");
    }

    [Fact]
    public void Evaluate_StricterCategory_WinsOnTie()
    {
        var doc = CreateDoc(
            new PathRule { Pattern = "file.md", Category = "optional", Exact = true },
            new PathRule { Pattern = "file.md", Category = "required", Exact = true });

        var engine = new PathPolicyEngine(doc);
        var result = engine.Evaluate("file.md");

        result.Category.Should().Be("required");
    }
}
