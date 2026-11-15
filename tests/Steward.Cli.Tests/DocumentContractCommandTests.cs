using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Steward.Cli.Tests.Helpers;
using Xunit;

namespace Steward.Cli.Tests;

[Collection("Console")]
public sealed class DocumentContractCommandTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "steward-contract-" + Guid.NewGuid().ToString("N"));
    private readonly string _previous = Directory.GetCurrentDirectory();

    public DocumentContractCommandTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, ".git"));
        // Initialize the assertion library before redirecting Console for JSON capture.
        Directory.Exists(_root).Should().BeTrue();
        Write(".steward/config.yaml", "profile: minimal\n");
        Write(".steward/policy.yaml", """
            artifacts:
              - path: README.md
                index_pattern: docs/*.md
            document_contracts:
              budgets:
                - pattern: docs/*.md
                  max_words: 20
                  max_table_cell_words: 3
                - pattern: README.md
                  max_index_entries: 2
              identities:
                - pattern: docs/*.md
                  field: subject
              relations:
                - pattern: docs/*.md
                  field: evidence
                  target_pattern: reports/*.md
              source_collections:
                - id: original
                  parts_pattern: parts/*.md
                  parts: [parts/01.md, parts/02.md]
                  sha256: DIGEST
            """.Replace("DIGEST", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("first\nsecond\n")))));
        Write("README.md", "# Home\n\n[Reference](docs/a.md)\n");
        Write("docs/a.md", "---\nsubject: frames\nevidence: reports/a.md\n---\n# Frames\nShort reference.\n");
        Write("reports/a.md", "# Measured evidence\n");
        Write("parts/01.md", "---\ndescription: original source\n---\nfirst\n");
        Write("parts/02.md", "second\n");
        Directory.SetCurrentDirectory(_root);
    }

    [Fact]
    public void GoodCorpusPassesAndReportsNoContractErrors()
    {
        Check(0, null);
        var (exit, output, error) = CliTestHelper.InvokeCapture("config", "validate");
        exit.Should().Be(0, output + error);
    }

    [Theory]
    [InlineData("words", "STWD-022")]
    [InlineData("cell", "STWD-022")]
    [InlineData("index", "STWD-022")]
    [InlineData("identity", "STWD-023")]
    [InlineData("relation", "STWD-023")]
    [InlineData("source", "STWD-024")]
    [InlineData("unlisted", "STWD-024")]
    [InlineData("coverage", "STWD-011")]
    public void PlantedViolationFailsRealCheck(string fault, string rule)
    {
        switch (fault)
        {
            case "words": Write("docs/a.md", "---\nsubject: frames\n---\n" + string.Join(' ', Enumerable.Repeat("word", 21))); break;
            case "cell": Write("docs/a.md", "---\nsubject: frames\n---\n| A | B |\n|---|---|\n| ok | one two three four |\n"); break;
            case "index": Write("README.md", "[a](docs/a.md) [b](reports/a.md) [c](parts/01.md)"); break;
            case "identity": Write("docs/b.md", "---\nsubject: frames\n---\nDuplicate."); break;
            case "relation": Write("docs/a.md", "---\nsubject: frames\nevidence: reports/missing.md\n---\nBroken relation."); break;
            case "source": Write("parts/02.md", "changed\n"); break;
            case "unlisted": Write("parts/03.md", "extra\n"); break;
            case "coverage": Write("docs/b.md", "---\nsubject: other\n---\nUnindexed."); break;
        }
        // STWD-011 is warning by default; verify severity override participates in the real gate.
        if (fault == "coverage")
            File.AppendAllText(Path.Combine(_root, ".steward/policy.yaml"), "\nvalidation:\n  severity_overrides:\n    STWD-011: error\n");
        Check(1, rule);
    }

    [Fact]
    public void ScopedCheckStillFindsDuplicateIdentityAndChangedSourceOutsideScope()
    {
        Write("docs/b.md", "---\nsubject: frames\n---\nDuplicate.");
        Write("parts/02.md", "changed\n");
        var (exit, output, error) = CliTestHelper.InvokeCapture("check", "--paths", "README.md", "--output", "json");
        exit.Should().Be(1, output + error);
        using var json = JsonDocument.Parse(output);
        var rules = json.RootElement.GetProperty("data").GetProperty("diagnostics").EnumerateArray()
            .Select(d => d.GetProperty("ruleId").GetString()).ToArray();
        rules.Should().Contain("STWD-023").And.Contain("STWD-024");
    }

    [Fact]
    public void InvalidBudgetIsConfigurationError()
    {
        Write(".steward/policy.yaml", "document_contracts:\n  budgets: [{pattern: '*.md', max_words: -1}]\n");
        var (exit, _, _) = CliTestHelper.InvokeCapture("check", "--output", "json");
        exit.Should().Be(2);
    }

    private void Check(int expected, string? rule)
    {
        var (exit, output, error) = CliTestHelper.InvokeCapture("check", "--output", "json");
        exit.Should().Be(expected, output + error);
        using var json = JsonDocument.Parse(output);
        var diagnostics = json.RootElement.GetProperty("data").GetProperty("diagnostics").EnumerateArray().ToArray();
        if (rule != null)
            diagnostics.Should().Contain(d => d.GetProperty("ruleId").GetString() == rule &&
                d.GetProperty("severity").GetString() == "error");
        else diagnostics.Should().NotContain(d => d.GetProperty("ruleId").GetString()!.StartsWith("STWD-02"));
    }

    private void Write(string path, string content)
    {
        var full = Path.Combine(_root, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_previous);
        Directory.Delete(_root, true);
    }
}
