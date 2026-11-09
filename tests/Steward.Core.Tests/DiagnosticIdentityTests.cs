using FluentAssertions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class DiagnosticIdentityTests
{
    private static Diagnostic Make(string message, int? line = null, string? selector = null, string? path = "docs/a.md") =>
        new("STWD-008", DiagnosticSeverity.Error, "links", path, line, message, null, null, Selector: selector);

    [Fact]
    public void Of_IgnoresLineNumber()
    {
        DiagnosticIdentity.Of(Make("Broken link to 'x.md'", line: 3))
            .Should().Be(DiagnosticIdentity.Of(Make("Broken link to 'x.md'", line: 40)));
    }

    [Fact]
    public void Of_WithoutSelector_FallsBackToMessage()
    {
        DiagnosticIdentity.Of(Make("Broken link to 'x.md'")).Selector.Should().Be("Broken link to 'x.md'");
        DiagnosticIdentity.Of(Make("Broken link to 'x.md'"))
            .Should().NotBe(DiagnosticIdentity.Of(Make("Broken link to 'y.md'")));
    }

    [Fact]
    public void Of_ExplicitSelector_WinsOverMessage()
    {
        DiagnosticIdentity.Of(Make("Section 'A' (line 3)", selector: "section:A"))
            .Should().Be(DiagnosticIdentity.Of(Make("Section 'A' (line 9)", selector: "section:A")));
    }

    [Fact]
    public void Of_NormalizesPathSeparatorsAndNullPath()
    {
        DiagnosticIdentity.Of(Make("m", path: "docs\\a.md")).Path.Should().Be("docs/a.md");
        DiagnosticIdentity.Of(Make("m", path: null)).Path.Should().BeEmpty();
    }

    [Fact]
    public void CompareTo_OrdersByPathThenRuleThenSelector()
    {
        var ids = new[]
        {
            new DiagnosticIdentity("STWD-002", "b.md", "x"),
            new DiagnosticIdentity("STWD-001", "b.md", "y"),
            new DiagnosticIdentity("STWD-001", "b.md", "a"),
            new DiagnosticIdentity("STWD-009", "a.md", "z"),
        };

        ids.Order().Should().Equal(
            new DiagnosticIdentity("STWD-009", "a.md", "z"),
            new DiagnosticIdentity("STWD-001", "b.md", "a"),
            new DiagnosticIdentity("STWD-001", "b.md", "y"),
            new DiagnosticIdentity("STWD-002", "b.md", "x"));
    }

    // ── Rules with volatile messages supply line-independent selectors ─────────

    private static ValidationContext Context(InMemoryFileSystem fs, RepositoryPolicy? policy, string file) => new()
    {
        Policy = policy,
        PathPolicy = null,
        TargetFiles = [new DiscoveredFile(file, 100, false)],
        FileSystem = fs,
        RepositoryRoot = "root"
    };

    private static async Task<DiagnosticIdentity> SingleIdentity(IValidationRule rule, ValidationContext context)
    {
        var diagnostics = await rule.EvaluateAsync(context);
        diagnostics.Should().ContainSingle();
        return DiagnosticIdentity.Of(diagnostics[0]);
    }

    [Fact]
    public async Task UniqueHeadingText_IdentitySurvivesMovingTheDuplicate()
    {
        var before = new InMemoryFileSystem().AddFile("root/d.md", "# Status\n\n## Status\n");
        var after = new InMemoryFileSystem().AddFile("root/d.md", "# Intro\n\ntext\n\n## Status\n\nmore\n\n## Status\n");

        var a = await SingleIdentity(new UniqueHeadingTextRule(), Context(before, null, "d.md"));
        var b = await SingleIdentity(new UniqueHeadingTextRule(), Context(after, null, "d.md"));

        a.Should().Be(b);
        a.Selector.Should().NotContain("line");
    }

    [Fact]
    public async Task SectionSize_IdentitySurvivesGrowthAndMove()
    {
        static string Doc(int pad, int size) =>
            string.Join('\n', Enumerable.Repeat("", pad).Prepend("# Top")
                .Concat(["## Big"]).Concat(Enumerable.Range(0, size).Select(i => $"line {i}")));
        var policy = new RepositoryPolicy { Governance = new GovernanceConfig { SectionSizeWarningThreshold = 10 } };

        var a = await SingleIdentity(new SectionSizeRule(),
            Context(new InMemoryFileSystem().AddFile("root/d.md", Doc(0, 20)), policy, "d.md"));
        var b = await SingleIdentity(new SectionSizeRule(),
            Context(new InMemoryFileSystem().AddFile("root/d.md", Doc(3, 30)), policy, "d.md"));

        a.Should().Be(b);
    }

    [Fact]
    public async Task SectionSchema_UndeclaredSection_IdentityIgnoresLineButNotHeading()
    {
        var policy = new RepositoryPolicy
        {
            ArtifactFamilies =
            [
                new ArtifactFamilyDefinition
                {
                    Family = "adr",
                    Match = new ArtifactFamilyMatch { PathPattern = "docs/*.md" },
                    SectionSchema = new SectionSchemaConfig
                    {
                        Sections = [new SectionSchemaEntry { Heading = "Context", Required = false }],
                        AllowExtra = false
                    }
                }
            ]
        };

        async Task<DiagnosticIdentity> Run(string content) => await SingleIdentity(new FamilySectionSchemaRule(),
            Context(new InMemoryFileSystem().AddFile("root/docs/a.md", content), policy, "docs/a.md"));

        var a = await Run("# T\n\n## Context\n\n## Extra\n");
        var b = await Run("# T\n\nintro\n\n## Context\n\nbody\n\n## Extra\n");
        var c = await Run("# T\n\n## Context\n\n## Other\n");

        a.Should().Be(b);
        a.Should().NotBe(c);
    }

    [Fact]
    public async Task FamilyMinCount_IdentityIgnoresMatchedCount()
    {
        static RepositoryPolicy Policy() => new()
        {
            ArtifactFamilies =
            [
                new ArtifactFamilyDefinition
                {
                    Family = "adr",
                    Match = new ArtifactFamilyMatch { PathPattern = "docs/*.md" },
                    DirectoryExpectations = new DirectoryExpectations { MinCount = 5 }
                }
            ]
        };
        static ValidationContext Ctx(params string[] files)
        {
            var fs = new InMemoryFileSystem();
            foreach (var f in files) fs.AddFile("root/" + f, "# T\n");
            return new ValidationContext
            {
                Policy = Policy(),
                PathPolicy = null,
                TargetFiles = files.Select(f => new DiscoveredFile(f, 4, false)).ToList(),
                AllDiscoveredFiles = files.Select(f => new DiscoveredFile(f, 4, false)).ToList(),
                FileSystem = fs,
                RepositoryRoot = "root"
            };
        }

        var a = await SingleIdentity(new FamilyMinCountRule(), Ctx("docs/a.md"));
        var b = await SingleIdentity(new FamilyMinCountRule(), Ctx("docs/a.md", "docs/b.md"));

        a.Should().Be(b);
    }
}
