using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class DocumentContractTests
{
    private static ValidationContext Context(DocumentContractsConfig contracts,
        params (string Path, string Content)[] documents)
    {
        var fs = new InMemoryFileSystem();
        foreach (var (path, content) in documents) fs.AddFile("root/" + path, content);
        return new ValidationContext
        {
            Policy = new RepositoryPolicy { DocumentContracts = contracts }, PathPolicy = null,
            RepositoryRoot = "root", FileSystem = fs,
            TargetFiles = documents.Select(d => new DiscoveredFile(d.Path, d.Content.Length, false)).ToArray()
        };
    }

    [Theory]
    [InlineData("one two three", 0)]
    [InlineData("one\ntwo\nthree", 0)]
    [InlineData("one two three four", 1)]
    [InlineData("```\none two three four\n```", 1)]
    [InlineData("<!-- one two three four -->", 1)]
    public async Task Budget_CountsSourceWordsRegardlessOfWrappingOrContainer(string body, int errors)
    {
        var contracts = new DocumentContractsConfig
        {
            Budgets = [new() { Pattern = "*.md", MaxWords = 3 }]
        };
        var context = Context(contracts, ("doc.md", "---\ndescription: excluded metadata words\n---\n" + body));
        var result = await new DocumentBudgetRule().EvaluateAsync(context);
        result.Should().HaveCount(errors);
        result.Should().OnlyContain(d => d.RuleId == "STWD-022");
    }

    [Fact]
    public async Task Budget_LaterMatchingFieldsOverrideAndUnmatchedFilesAreUnaffected()
    {
        var contracts = new DocumentContractsConfig
        {
            Budgets = [new() { Pattern = "docs/*.md", MaxWords = 5, MaxSectionWords = 3 },
                new() { Pattern = "docs/small.md", MaxWords = 2 }]
        };
        var result = await new DocumentBudgetRule().EvaluateAsync(Context(contracts,
            ("docs/small.md", "one two three"), ("outside.md", "one two three four five six")));
        result.Should().ContainSingle().Which.Path.Should().Be("docs/small.md");
    }

    [Fact]
    public async Task Budget_SectionsCountOwnContentRatherThanDescendants()
    {
        var contracts = new DocumentContractsConfig
        {
            Budgets = [new() { Pattern = "*.md", MaxSectionWords = 4 }]
        };
        var result = await new DocumentBudgetRule().EvaluateAsync(Context(contracts,
            ("doc.md", "# Parent\nintro\n## Child\none two three\n")));
        result.Should().ContainSingle().Which.Message.Should().Contain("section:Child").And.Contain("5 words");
    }

    [Fact]
    public async Task Budget_ParsesCellsWithEscapedPipesAndIgnoresFencedPseudoTables()
    {
        var contracts = new DocumentContractsConfig
        {
            Budgets = [new() { Pattern = "*.md", MaxTableCellWords = 3 }]
        };
        var result = await new DocumentBudgetRule().EvaluateAsync(Context(contracts, ("doc.md", """
            | Name | Details |
            | --- | --- |
            | Good | a b c |
            | Bad | one two \| three four |

            ```text
            | Not a table | one two three four five six |
            ```
            """)));
        result.Should().ContainSingle().Which.Message.Should().Contain("table-cell");
    }

    [Fact]
    public async Task Budget_IndexCountsLinksButNotImages()
    {
        var contracts = new DocumentContractsConfig
        {
            Budgets = [new() { Pattern = "*.md", MaxIndexEntries = 1 }]
        };
        var rule = new DocumentBudgetRule();
        (await rule.EvaluateAsync(Context(contracts, ("doc.md", "[one](one.md) ![picture](image.png)"))))
            .Should().BeEmpty();
        (await rule.EvaluateAsync(Context(contracts, ("doc.md", "[one](one.md) [two](two.md)"))))
            .Should().ContainSingle().Which.Message.Should().Contain("2 links");
    }

    [Fact]
    public async Task Identity_UsesFullCorpusDuringScopedValidation()
    {
        var contracts = new DocumentContractsConfig
        {
            Identities = [new() { Pattern = "*.md", Field = "subject" }]
        };
        var full = Context(contracts, ("a.md", "---\nsubject: Frames\n---\na"),
            ("b.md", "---\nsubject: frames\n---\nb"));
        var scoped = new ValidationContext
        {
            Policy = full.Policy, PathPolicy = null, FileSystem = full.FileSystem, RepositoryRoot = "root",
            TargetFiles = [full.TargetFiles[0]], AllDiscoveredFiles = full.TargetFiles
        };
        var result = await new DocumentIdentityRule().EvaluateAsync(scoped);
        result.Should().ContainSingle().Which.Message.Should().Contain("also belongs to 'a.md'");
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("subject: []", true)]
    [InlineData("subject: 123", false)]
    [InlineData("subject: frames", false)]
    public async Task Identity_RequiresNonemptyString(string metadata, bool bad)
    {
        var contracts = new DocumentContractsConfig
        {
            Identities = [new() { Pattern = "*.md", Field = "subject" }]
        };
        var result = await new DocumentIdentityRule().EvaluateAsync(Context(contracts,
            ("a.md", "---\n" + metadata + "\n---\nbody")));
        result.Count.Should().Be(bad ? 1 : 0);
    }

    [Theory]
    [InlineData("evidence: reports/a.md", false)]
    [InlineData("evidence: [reports/a.md]", false)]
    [InlineData("evidence: reports/missing.md", true)]
    [InlineData("evidence: ../reports/a.md", true)]
    [InlineData("evidence: /reports/a.md", true)]
    [InlineData("evidence: a.md", true)]
    [InlineData("evidence: 123", true)]
    public async Task Relations_ValidateScalarAndListTargets(string metadata, bool bad)
    {
        var contracts = new DocumentContractsConfig
        {
            Relations = [new() { Pattern = "a.md", Field = "evidence", TargetPattern = "reports/*.md" }]
        };
        var result = await new DocumentIdentityRule().EvaluateAsync(Context(contracts,
            ("a.md", "---\n" + metadata + "\n---\nbody"), ("reports/a.md", "# Evidence")));
        result.Count.Should().Be(bad ? 1 : 0);
    }

    [Theory]
    [InlineData("original", "same", false, false)]
    [InlineData("changed", "same", false, true)]
    [InlineData("original", "same", true, true)]
    [InlineData("original", "extra", false, true)]
    public async Task Sources_DetectChangedBodiesOrderAndUnlistedParts(string first, string second, bool reverse, bool bad)
    {
        var contracts = new DocumentContractsConfig
        {
            SourceCollections = [new()
            {
                Id = "source", PartsPattern = "parts/*.md", Parts = reverse ? ["parts/b.md", "parts/a.md"] :
                    ["parts/a.md", "parts/b.md"],
                Sha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("original\r\n日本語")))
            }]
        };
        var docs = new List<(string, string)>
        {
            ("parts/a.md", "---\nupdated: today\n---\n" + first + "\r\n"),
            ("parts/b.md", "---\r\nupdated: yesterday\r\n---\r\n日本語")
        };
        if (second == "extra") docs.Add(("parts/c.md", "unlisted"));
        var result = await new SourceCollectionRule().EvaluateAsync(Context(contracts, docs.ToArray()));
        result.Count.Should().Be(bad ? 1 : 0);
    }

    [Fact]
    public async Task Sources_MissingPartFailsEvenWithOtherFilesInScope()
    {
        var contracts = new DocumentContractsConfig
        {
            SourceCollections = [new() { Id = "missing", PartsPattern = "parts/*.md", Parts = ["parts/a.md"], Sha256 = new string('0', 64) }]
        };
        var result = await new SourceCollectionRule().EvaluateAsync(Context(contracts, ("other.md", "text")));
        result.Should().ContainSingle().Which.Message.Should().Contain("parts/a.md");
    }

    [Theory]
    [InlineData("budgets: [{pattern: '*.md', max_words: 0}]")]
    [InlineData("budgets: [{pattern: '*.md'}]")]
    [InlineData("budgets: [{pattern: '[invalid', max_words: 5}]")]
    [InlineData("identities: [{pattern: '*.md', field: ''}]")]
    [InlineData("relations: [{pattern: '*.md', field: evidence, target_pattern: ''}]")]
    [InlineData("source_collections: [{id: x, parts_pattern: '*.md', parts: ['../x.md'], sha256: bad}]")]
    public void BadConfigurationIsRejected(string yaml)
    {
        var fs = new InMemoryFileSystem().AddFile("root/.steward/policy.yaml", "document_contracts:\n  " + yaml);
        var action = () => new ConfigLoader(fs).LoadPolicy("root/.steward");
        action.Should().Throw<StewardConfigException>();
    }
}
