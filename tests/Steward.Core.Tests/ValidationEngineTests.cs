using FluentAssertions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Validation;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class ValidationEngineTests
{
    private sealed class AlwaysErrorRule : IValidationRule
    {
        public string RuleId => "TEST-001";
        public string Category => "test";
        public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Error;
        public string Description => "Always produces an error";

        public Task<IReadOnlyList<Diagnostic>> EvaluateAsync(ValidationContext context)
        {
            IReadOnlyList<Diagnostic> result =
            [
                new Diagnostic("TEST-001", DiagnosticSeverity.Error, "test", "file.txt", null, "fail", null, null)
            ];
            return Task.FromResult(result);
        }
    }

    private sealed class AlwaysWarningRule : IValidationRule
    {
        public string RuleId => "TEST-002";
        public string Category => "test";
        public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Warning;
        public string Description => "Always produces a warning";

        public Task<IReadOnlyList<Diagnostic>> EvaluateAsync(ValidationContext context)
        {
            IReadOnlyList<Diagnostic> result =
            [
                new Diagnostic("TEST-002", DiagnosticSeverity.Warning, "test", "other.txt", null, "warn", null, null)
            ];
            return Task.FromResult(result);
        }
    }

    private sealed class CleanRule : IValidationRule
    {
        public string RuleId => "TEST-003";
        public string Category => "test";
        public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Info;
        public string Description => "Always clean";

        public Task<IReadOnlyList<Diagnostic>> EvaluateAsync(ValidationContext context)
        {
            return Task.FromResult<IReadOnlyList<Diagnostic>>([]);
        }
    }

    [Fact]
    public async Task ValidateAsync_CollectsFromAllRules()
    {
        var engine = new ValidationEngine([new AlwaysErrorRule(), new AlwaysWarningRule()]);
        var context = MakeContext();

        var result = await engine.ValidateAsync(context);

        result.Diagnostics.Should().HaveCount(2);
        result.Summary.Errors.Should().Be(1);
        result.Summary.Warnings.Should().Be(1);
    }

    [Fact]
    public async Task ValidateAsync_NoRules_EmptyResult()
    {
        var engine = new ValidationEngine([]);
        var context = MakeContext();

        var result = await engine.ValidateAsync(context);

        result.Diagnostics.Should().BeEmpty();
        result.Summary.Errors.Should().Be(0);
        result.Summary.Warnings.Should().Be(0);
    }

    [Fact]
    public async Task ValidateAsync_CleanRules_Pass()
    {
        var engine = new ValidationEngine([new CleanRule()]);
        var context = MakeContext();

        var result = await engine.ValidateAsync(context);

        result.Diagnostics.Should().BeEmpty();
        result.Summary.Pass.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_SummaryCountsCorrect()
    {
        var engine = new ValidationEngine([new AlwaysErrorRule(), new AlwaysWarningRule(), new CleanRule()]);
        var context = MakeContext();

        var result = await engine.ValidateAsync(context);

        result.Summary.Errors.Should().Be(1);
        result.Summary.Warnings.Should().Be(1);
        result.Summary.Infos.Should().Be(0);
        result.Summary.Pass.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_FilesChecked_MatchesContext()
    {
        var engine = new ValidationEngine([new CleanRule()]);
        var context = MakeContext(
        [
            new DiscoveredFile("a.txt", 1, false),
            new DiscoveredFile("b.txt", 2, false)
        ]);

        var result = await engine.ValidateAsync(context);

        result.Summary.FilesChecked.Should().Be(2);
    }

    private static ValidationContext MakeContext(IReadOnlyList<DiscoveredFile>? files = null) => new()
    {
        Policy = new RepositoryPolicy(),
        PathPolicy = null,
        TargetFiles = files ?? [],
        FileSystem = new InMemoryFileSystem(),
        RepositoryRoot = "root"
    };
}
