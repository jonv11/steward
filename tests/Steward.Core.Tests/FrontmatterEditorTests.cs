using FluentAssertions;
using Steward.Core.Markdown;
using Xunit;

namespace Steward.Core.Tests;

public class FrontmatterEditorTests
{
    private static StructuredDocument ParseDoc(string content)
        => MarkdownParser.Parse("test.md", content);

    [Fact]
    public void SetField_NoFrontmatter_CreatesFrontmatter()
    {
        var content = "# Title\nSome text.\n";
        var doc = ParseDoc(content);

        var result = FrontmatterEditor.SetField(doc, "status", "draft");

        result.HasChanges.Should().BeTrue();
        result.NewContent.Should().Contain("---");
        result.NewContent.Should().Contain("status: draft");
        result.NewContent.Should().Contain("# Title");
    }

    [Fact]
    public void SetField_ExistingFrontmatter_AddsField()
    {
        var content = "---\ntitle: Test\n---\n# Title\n";
        var doc = ParseDoc(content);

        var result = FrontmatterEditor.SetField(doc, "status", "draft");

        result.HasChanges.Should().BeTrue();
        result.NewContent.Should().Contain("status: draft");
        result.NewContent.Should().Contain("title: Test");
    }

    [Fact]
    public void SetField_OverwritesExistingField()
    {
        var content = "---\nstatus: draft\n---\n# Title\n";
        var doc = ParseDoc(content);

        var result = FrontmatterEditor.SetField(doc, "status", "published");

        result.HasChanges.Should().BeTrue();
        result.NewContent.Should().Contain("status: published");
        result.NewContent.Should().NotContain("status: draft");
    }

    [Fact]
    public void MergeFields_NoFrontmatter_CreatesFrontmatter()
    {
        var content = "# Title\nSome text.\n";
        var doc = ParseDoc(content);

        var result = FrontmatterEditor.MergeFields(doc, "status: draft\nauthor: Test");

        result.HasChanges.Should().BeTrue();
        result.NewContent.Should().Contain("---");
        result.NewContent.Should().Contain("status: draft");
        result.NewContent.Should().Contain("author: Test");
    }

    [Fact]
    public void MergeFields_ExistingFrontmatter_MergesFields()
    {
        var content = "---\ntitle: Test\n---\n# Title\n";
        var doc = ParseDoc(content);

        var result = FrontmatterEditor.MergeFields(doc, "status: draft\nauthor: Test");

        result.HasChanges.Should().BeTrue();
        result.NewContent.Should().Contain("title: Test");
        result.NewContent.Should().Contain("status: draft");
        result.NewContent.Should().Contain("author: Test");
    }

    [Fact]
    public void MergeFields_OverwritesExistingFields()
    {
        var content = "---\nstatus: draft\ntitle: Old\n---\n# Title\n";
        var doc = ParseDoc(content);

        var result = FrontmatterEditor.MergeFields(doc, "title: New");

        result.HasChanges.Should().BeTrue();
        result.NewContent.Should().Contain("title: New");
        result.NewContent.Should().Contain("status: draft");
    }

    [Fact]
    public void MergeFields_InvalidYaml_ReturnsError()
    {
        var content = "---\ntitle: Test\n---\n# Title\n";
        var doc = ParseDoc(content);

        var result = FrontmatterEditor.MergeFields(doc, "{{invalid yaml::");

        result.IsError.Should().BeTrue();
        result.Message.Should().Contain("Invalid YAML");
    }

    [Fact]
    public void SetField_PreservesBodyContent()
    {
        var content = "---\ntitle: Test\n---\n# Title\nBody text here.\n";
        var doc = ParseDoc(content);

        var result = FrontmatterEditor.SetField(doc, "status", "draft");

        result.NewContent.Should().Contain("Body text here.");
        result.NewContent.Should().Contain("# Title");
    }

    // ── RFC-014: ReplaceAllFields ──────────────────────────────────────────────

    [Fact]
    public void ReplaceAllFields_OverwritesFieldsWithNewDictionary()
    {
        var content = "---\ntype: adr\ndate: 2025-01-01\n---\n# Title\n";
        var doc = ParseDoc(content);

        var newFields = new Dictionary<string, object?>
        {
            ["type"] = "adr",
            ["last_updated"] = "2025-01-01"
            // "date" is intentionally absent — simulates deprecated field rename
        };
        var result = FrontmatterEditor.ReplaceAllFields(doc, newFields, "Renamed date→last_updated.");

        result.HasChanges.Should().BeTrue();
        result.NewContent.Should().Contain("last_updated:");
        result.NewContent.Should().NotContain("date:");
        result.NewContent.Should().Contain("# Title");
    }

    [Fact]
    public void ReplaceAllFields_NoFrontmatter_ReturnsError()
    {
        var content = "# Just a heading, no frontmatter.\n";
        var doc = ParseDoc(content);

        var result = FrontmatterEditor.ReplaceAllFields(
            doc,
            new Dictionary<string, object?> { ["type"] = "adr" },
            "Should fail.");

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public void ReplaceAllFields_PreservesDocumentBodyAfterFrontmatter()
    {
        var content = "---\ntype: adr\n---\n# ADR-001: Title\n\nContent here.\n";
        var doc = ParseDoc(content);

        var newFields = new Dictionary<string, object?> { ["type"] = "adr", ["status"] = "Draft" };
        var result = FrontmatterEditor.ReplaceAllFields(doc, newFields, "Added status.");

        result.NewContent.Should().Contain("# ADR-001: Title");
        result.NewContent.Should().Contain("Content here.");
    }
}
