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
}
