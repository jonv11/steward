using FluentAssertions;
using Steward.Core.Validation;
using Xunit;

namespace Steward.Core.Tests;

public class DiagnosticTests
{
    [Fact]
    public void Diagnostic_ConstructsCorrectly()
    {
        var d = new Diagnostic(
            RuleId: "STWD-001",
            Severity: DiagnosticSeverity.Error,
            Category: "path-policy",
            Path: "README.md",
            Line: 1,
            Message: "Missing README.md",
            Remediation: "Create the file",
            Source: "policy.yaml");

        d.RuleId.Should().Be("STWD-001");
        d.Severity.Should().Be(DiagnosticSeverity.Error);
        d.Category.Should().Be("path-policy");
        d.Path.Should().Be("README.md");
        d.Line.Should().Be(1);
        d.Message.Should().Be("Missing README.md");
        d.Remediation.Should().Be("Create the file");
        d.Source.Should().Be("policy.yaml");
    }

    [Fact]
    public void Diagnostic_NullOptionalFields()
    {
        var d = new Diagnostic(
            RuleId: "STWD-002",
            Severity: DiagnosticSeverity.Warning,
            Category: "path-policy",
            Path: null,
            Line: null,
            Message: "some warning",
            Remediation: null,
            Source: null);

        d.Path.Should().BeNull();
        d.Line.Should().BeNull();
        d.Remediation.Should().BeNull();
        d.Source.Should().BeNull();
    }

    [Fact]
    public void ValidationSummary_RequiredProperties()
    {
        var s = new ValidationSummary
        {
            Scope = "full",
            FilesChecked = 10,
            Errors = 2,
            Warnings = 1,
            Infos = 0,
            Pass = false
        };

        s.Scope.Should().Be("full");
        s.FilesChecked.Should().Be(10);
        s.Errors.Should().Be(2);
        s.Warnings.Should().Be(1);
        s.Infos.Should().Be(0);
        s.Pass.Should().BeFalse();
    }

    [Fact]
    public void ValidationResult_HoldsData()
    {
        var diags = new List<Diagnostic>
        {
            new("R1", DiagnosticSeverity.Error, "cat", "f.txt", null, "msg", null, null)
        };
        var summary = new ValidationSummary
        {
            Scope = "full",
            FilesChecked = 1,
            Errors = 1,
            Warnings = 0,
            Infos = 0,
            Pass = false
        };
        var result = new ValidationResult { Diagnostics = diags, Summary = summary };

        result.Diagnostics.Should().HaveCount(1);
        result.Summary.Errors.Should().Be(1);
        result.Summary.Pass.Should().BeFalse();
    }
}
