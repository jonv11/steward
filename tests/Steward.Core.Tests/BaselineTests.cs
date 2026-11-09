using FluentAssertions;
using Steward.Core.Baseline;
using Steward.Core.Configuration;
using Steward.Core.Validation;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class BaselineTests
{
    private static Diagnostic Diag(string rule, string path, string message, int? line = null, string? selector = null) =>
        new(rule, DiagnosticSeverity.Error, "c", path, line, message, null, null, Selector: selector);

    private static readonly DateOnly Day = new(2026, 11, 9);

    [Fact]
    public void Serialize_SortsEntriesAndIsByteStable()
    {
        var diagnostics = new[]
        {
            Diag("STWD-014", "docs/b.md", "missing Context"),
            Diag("STWD-008", "docs/a.md", "broken x", line: 4),
            Diag("STWD-001", "docs/b.md", "missing file"),
        };

        var first = BaselineDocument.FromDiagnostics(diagnostics, Day, "0.19.0").Serialize();
        var second = BaselineDocument.FromDiagnostics(diagnostics.Reverse(), Day, "0.19.0").Serialize();

        first.Should().Be(second);
        first.IndexOf("docs/a.md", StringComparison.Ordinal).Should().BeLessThan(first.IndexOf("docs/b.md", StringComparison.Ordinal));
        first.IndexOf("STWD-001", StringComparison.Ordinal).Should().BeLessThan(first.IndexOf("STWD-014", StringComparison.Ordinal));
        first.Should().EndWith("}\n").And.Contain("\"generated_at\": \"2026-11-09\"").And.NotContain("\r");
    }

    [Fact]
    public void Parse_RoundTripsSerialize()
    {
        var doc = BaselineDocument.FromDiagnostics([Diag("STWD-017", "a.md", "dup 'X' — it's", selector: "heading:x")], Day, null);

        var parsed = BaselineDocument.Parse(doc.Serialize(), "b.json");

        parsed.Serialize().Should().Be(doc.Serialize());
        parsed.Entries.Should().ContainSingle().Which.Should().Be(new BaselineEntry("STWD-017", "a.md", "heading:x"));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{\"generated_at\": \"2026-11-09\"}")]
    [InlineData("{\"generated_at\": \"2026-11-09\", \"entries\": [{\"rule\": \"\", \"path\": \"a\", \"selector\": \"s\"}]}")]
    [InlineData("{\"generated_at\": \"2026-11-09\", \"entries\": [], \"surprise\": 1}")]
    public void Parse_Malformed_ThrowsConfigExceptionNamingFile(string json)
    {
        var act = () => BaselineDocument.Parse(json, ".steward/baseline.json");

        act.Should().Throw<StewardConfigException>().WithMessage("*.steward/baseline.json*");
    }

    [Fact]
    public void Match_PartitionsNewBaselinedAndDrift()
    {
        var baseline = BaselineDocument.FromDiagnostics(
            [Diag("STWD-008", "a.md", "broken x"), Diag("STWD-014", "b.md", "missing Context")], Day, null);

        var current = new[]
        {
            Diag("STWD-008", "a.md", "broken x", line: 99), // same violation, moved
            Diag("STWD-008", "a.md", "broken y"),           // new
        };

        var match = BaselineMatcher.Match(current, baseline);

        match.Baselined.Should().ContainSingle().Which.Message.Should().Be("broken x");
        match.New.Should().ContainSingle().Which.Message.Should().Be("broken y");
        match.Drift.Should().ContainSingle().Which.Should().Be(new BaselineEntry("STWD-014", "b.md", "missing Context"));
    }

    [Fact]
    public void Match_UsesMultisetSemanticsForRepeatedIdentities()
    {
        var two = new[] { Diag("STWD-017", "a.md", "m", selector: "heading:x"), Diag("STWD-017", "a.md", "m", selector: "heading:x") };
        var baseline = BaselineDocument.FromDiagnostics(two, Day, null);

        var oneFixed = BaselineMatcher.Match(two.Take(1), baseline);
        oneFixed.Baselined.Should().HaveCount(1);
        oneFixed.Drift.Should().HaveCount(1);

        var oneAdded = BaselineMatcher.Match(two.Append(two[0]), baseline);
        oneAdded.Baselined.Should().HaveCount(2);
        oneAdded.New.Should().HaveCount(1);
        oneAdded.Drift.Should().BeEmpty();
    }

    [Fact]
    public void Settings_DefaultLivesInConfigDirectory_ExplicitPathIsRepoRelative()
    {
        var root = Path.Combine(Path.GetTempPath(), "repo");

        var byDefault = BaselineSettings.Resolve(null, root, Path.Combine(root, ".steward-draft"));
        byDefault.DisplayPath.Should().Be(".steward-draft/baseline.json");

        var explicitPolicy = new RepositoryPolicy { Baseline = new BaselineConfig { Path = "ci/baseline.json" } };
        BaselineSettings.Resolve(explicitPolicy, root, Path.Combine(root, ".steward")).DisplayPath.Should().Be("ci/baseline.json");
    }

    [Fact]
    public void Settings_ActiveWhenFileExistsUnlessDisabled()
    {
        var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "repo"));
        var fs = new InMemoryFileSystem().AddFile(Path.Combine(root, ".steward", "baseline.json"), "{}");

        BaselineSettings.Resolve(null, root, null).IsActive(fs).Should().BeTrue();
        BaselineSettings.Resolve(null, root, null).IsActive(new InMemoryFileSystem()).Should().BeFalse();
        BaselineSettings.Resolve(new RepositoryPolicy { Baseline = new BaselineConfig { Enabled = false } }, root, null)
            .IsActive(fs).Should().BeFalse();
    }

    [Fact]
    public void LoadPolicy_AcceptsBaselineBlockAndRejectsUnknownFields()
    {
        var fs = new InMemoryFileSystem()
            .AddFile("root/.steward/policy.yaml", "baseline:\n  enabled: true\n  path: ci/baseline.json\n")
            .AddFile("bad/.steward/policy.yaml", "baseline:\n  enabled: true\n  pth: x\n");
        var loader = new ConfigLoader(fs);

        var policy = loader.LoadPolicy("root/.steward");
        policy!.Baseline!.Enabled.Should().BeTrue();
        policy.Baseline.Path.Should().Be("ci/baseline.json");

        var act = () => loader.LoadPolicy("bad/.steward");
        act.Should().Throw<StewardConfigException>();
    }

    [Fact]
    public void ProfileMerge_KeepsRepositoryBaseline()
    {
        var repo = new RepositoryPolicy { Baseline = new BaselineConfig { Enabled = false } };
        ProfileMerger.Merge(repo, ProfileDefaults.GetProfilePolicy("software")!).Baseline!.Enabled.Should().BeFalse();
    }
}
