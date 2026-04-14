using FluentAssertions;
using Steward.Core.Markdown;
using Xunit;

namespace Steward.Core.Tests;

public class StructuralEditorTests
{
    private static StructuredDocument ParseDoc(string content)
        => MarkdownParser.Parse("test.md", content);

    [Fact]
    public void EnsureSection_SectionDoesNotExist_CreatesIt()
    {
        var content = "# Introduction\nSome text.\n";
        var doc = ParseDoc(content);

        var result = StructuralEditor.EnsureSection(doc, "Goals");

        result.HasChanges.Should().BeTrue();
        result.NewContent.Should().Contain("# Goals");
    }

    [Fact]
    public void EnsureSection_SectionAlreadyExists_NoOp()
    {
        var content = "# Introduction\nSome text.\n# Goals\nGoal content.\n";
        var doc = ParseDoc(content);

        var result = StructuralEditor.EnsureSection(doc, "Goals");

        result.HasChanges.Should().BeFalse();
        result.Message.Should().Contain("already exists");
    }

    [Fact]
    public void EnsureSection_Under_CreatesChildSection()
    {
        var content = "# Introduction\nSome text.\n";
        var doc = ParseDoc(content);

        var result = StructuralEditor.EnsureSection(doc, "Goals", under: "Introduction");

        result.HasChanges.Should().BeTrue();
        result.NewContent.Should().Contain("## Goals");
    }

    [Fact]
    public void EnsureSection_WithContent_IncludesBodyText()
    {
        var content = "# Introduction\nSome text.\n";
        var doc = ParseDoc(content);

        var result = StructuralEditor.EnsureSection(doc, "Goals", content: "Our goals are...");

        result.HasChanges.Should().BeTrue();
        result.NewContent.Should().Contain("Our goals are...");
    }

    [Fact]
    public void SetSection_ExistingSection_ReplacesContent()
    {
        var content = "# Introduction\nOld text.\n# Goals\nOld goals.\n";
        var doc = ParseDoc(content);

        var result = StructuralEditor.SetSection(doc, "Goals", "New goals here.");

        result.HasChanges.Should().BeTrue();
        result.NewContent.Should().Contain("New goals here.");
        result.NewContent.Should().NotContain("Old goals.");
    }

    [Fact]
    public void SetSection_NonExistentSection_ReturnsError()
    {
        var content = "# Introduction\nSome text.\n";
        var doc = ParseDoc(content);

        var result = StructuralEditor.SetSection(doc, "NonExistent", "Content");

        result.IsError.Should().BeTrue();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public void InsertSection_AtTopLevel_InsertsAtEnd()
    {
        var content = "# Introduction\nSome text.\n";
        var doc = ParseDoc(content);

        var result = StructuralEditor.InsertSection(doc, "Appendix");

        result.HasChanges.Should().BeTrue();
        result.NewContent.Should().Contain("# Appendix");
        // The new section should appear after existing content
        var introIdx = result.NewContent.IndexOf("# Introduction", StringComparison.Ordinal);
        var appendixIdx = result.NewContent.IndexOf("# Appendix", StringComparison.Ordinal);
        appendixIdx.Should().BeGreaterThan(introIdx);
    }

    [Fact]
    public void InsertSection_UnderParent_InsertsAsChild()
    {
        var content = "# Introduction\nSome text.\n";
        var doc = ParseDoc(content);

        var result = StructuralEditor.InsertSection(doc, "Background", under: "Introduction");

        result.HasChanges.Should().BeTrue();
        result.NewContent.Should().Contain("## Background");
    }

    [Fact]
    public void InsertSection_UnderNonExistentParent_ReturnsError()
    {
        var content = "# Introduction\nSome text.\n";
        var doc = ParseDoc(content);

        var result = StructuralEditor.InsertSection(doc, "Sub", under: "Missing");

        result.IsError.Should().BeTrue();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public void AppendBlock_AddsContentAtEndOfSection()
    {
        var content = "# Introduction\nExisting text.\n# Goals\nGoal text.\n";
        var doc = ParseDoc(content);

        var result = StructuralEditor.AppendBlock(doc, "Introduction", "Appended paragraph.");

        result.HasChanges.Should().BeTrue();
        result.NewContent.Should().Contain("Appended paragraph.");
    }

    [Fact]
    public void AppendBlock_SectionNotFound_ReturnsError()
    {
        var content = "# Introduction\nSome text.\n";
        var doc = ParseDoc(content);

        var result = StructuralEditor.AppendBlock(doc, "Missing", "text");

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public void PrependBlock_AddsContentAfterHeading()
    {
        var content = "# Introduction\nExisting text.\n";
        var doc = ParseDoc(content);

        var result = StructuralEditor.PrependBlock(doc, "Introduction", "Prepended paragraph.");

        result.HasChanges.Should().BeTrue();
        result.NewContent.Should().Contain("Prepended paragraph.");
        // Prepended content should appear before existing content
        var prependIdx = result.NewContent.IndexOf("Prepended paragraph.", StringComparison.Ordinal);
        var existingIdx = result.NewContent.IndexOf("Existing text.", StringComparison.Ordinal);
        prependIdx.Should().BeLessThan(existingIdx);
    }

    [Fact]
    public void PrependBlock_SectionNotFound_ReturnsError()
    {
        var content = "# Introduction\nSome text.\n";
        var doc = ParseDoc(content);

        var result = StructuralEditor.PrependBlock(doc, "Missing", "text");

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public void EditResult_GetUnifiedDiff_ShowsChanges()
    {
        var content = "# Title\nOld text.\n";
        var doc = ParseDoc(content);

        var result = StructuralEditor.SetSection(doc, "Title", "New text.");

        result.HasChanges.Should().BeTrue();
        var diff = result.GetUnifiedDiff();
        diff.Should().Contain("---");
        diff.Should().Contain("+++");
    }

    [Fact]
    public void EditResult_NoOp_EmptyDiff()
    {
        var result = EditResult.NoOp("content", "no change");

        result.GetUnifiedDiff().Should().BeEmpty();
    }

    [Fact]
    public void OwnershipEnforcement_WrongOwner_BlocksEdit()
    {
        var content = "# Title\n<!-- steward:begin id=\"toc\" owner=\"bot\" -->\n## Generated\nGenerated content.\n<!-- steward:end -->\n";
        var doc = ParseDoc(content);

        var result = StructuralEditor.SetSection(doc, "Generated", "Replacement");

        result.IsError.Should().BeTrue();
        result.Message.Should().Contain("owner");
    }

    [Fact]
    public void OwnershipEnforcement_CorrectOwner_AllowsEdit()
    {
        var content = "# Title\n<!-- steward:begin id=\"toc\" owner=\"steward\" -->\n## Generated\nGenerated content.\n<!-- steward:end -->\n";
        var doc = ParseDoc(content);

        var result = StructuralEditor.SetSection(doc, "Generated", "Updated content.");

        result.IsError.Should().BeFalse();
        result.HasChanges.Should().BeTrue();
    }

    [Fact]
    public void OwnershipEnforcement_NoRegion_AllowsEdit()
    {
        var content = "# Title\n## Open Section\nFree content.\n";
        var doc = ParseDoc(content);

        var result = StructuralEditor.SetSection(doc, "Open Section", "New content.");

        result.IsError.Should().BeFalse();
        result.HasChanges.Should().BeTrue();
    }

    [Fact]
    public void EnsureSection_PreservesUnrelatedContent()
    {
        var content = "# Introduction\nIntro text.\n# Goals\nGoal text.\n";
        var doc = ParseDoc(content);

        var result = StructuralEditor.EnsureSection(doc, "Appendix");

        result.NewContent.Should().Contain("Intro text.");
        result.NewContent.Should().Contain("Goal text.");
    }

    [Fact]
    public void InsertSection_After_InsertsAfterTargetSection()
    {
        var content = "# Introduction\nIntro text.\n# Goals\nGoal text.\n# Appendix\nAppendix text.\n";
        var doc = ParseDoc(content);

        var result = StructuralEditor.InsertSection(doc, "New Section", after: "Goals");

        result.HasChanges.Should().BeTrue();
        var introIdx = result.NewContent.IndexOf("# Introduction", StringComparison.Ordinal);
        var goalsIdx = result.NewContent.IndexOf("# Goals", StringComparison.Ordinal);
        var newIdx = result.NewContent.IndexOf("# New Section", StringComparison.Ordinal);
        var appendixIdx = result.NewContent.IndexOf("# Appendix", StringComparison.Ordinal);
        newIdx.Should().BeGreaterThan(goalsIdx);
        newIdx.Should().BeLessThan(appendixIdx);
    }

    [Fact]
    public void InsertSection_Before_InsertsBeforeTargetSection()
    {
        var content = "# Introduction\nIntro text.\n# Goals\nGoal text.\n";
        var doc = ParseDoc(content);

        var result = StructuralEditor.InsertSection(doc, "Preamble", before: "Goals");

        result.HasChanges.Should().BeTrue();
        var preambleIdx = result.NewContent.IndexOf("# Preamble", StringComparison.Ordinal);
        var goalsIdx = result.NewContent.IndexOf("# Goals", StringComparison.Ordinal);
        preambleIdx.Should().BeLessThan(goalsIdx);
    }

    [Fact]
    public void InsertSection_After_NonExistent_ReturnsError()
    {
        var content = "# Introduction\nSome text.\n";
        var doc = ParseDoc(content);

        var result = StructuralEditor.InsertSection(doc, "New", after: "Missing");

        result.IsError.Should().BeTrue();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public void InsertSection_Before_NonExistent_ReturnsError()
    {
        var content = "# Introduction\nSome text.\n";
        var doc = ParseDoc(content);

        var result = StructuralEditor.InsertSection(doc, "New", before: "Missing");

        result.IsError.Should().BeTrue();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public void InsertSection_Level_OverridesDefault()
    {
        var content = "# Introduction\nSome text.\n";
        var doc = ParseDoc(content);

        var result = StructuralEditor.InsertSection(doc, "Deep Section", level: 3);

        result.HasChanges.Should().BeTrue();
        result.NewContent.Should().Contain("### Deep Section");
    }

    [Fact]
    public void InsertSection_After_InheritsTargetLevel()
    {
        var content = "# Introduction\nIntro text.\n## Details\nDetail text.\n";
        var doc = ParseDoc(content);

        var result = StructuralEditor.InsertSection(doc, "More Details", after: "Details");

        result.HasChanges.Should().BeTrue();
        result.NewContent.Should().Contain("## More Details");
    }

    [Fact]
    public void InsertSection_After_WithLevelOverride()
    {
        var content = "# Introduction\nIntro text.\n## Details\nDetail text.\n";
        var doc = ParseDoc(content);

        var result = StructuralEditor.InsertSection(doc, "Sidebar", after: "Details", level: 3);

        result.HasChanges.Should().BeTrue();
        result.NewContent.Should().Contain("### Sidebar");
    }
}
