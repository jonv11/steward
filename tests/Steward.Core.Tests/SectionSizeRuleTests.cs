using FluentAssertions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class SectionSizeRuleTests
{
    [Fact]
    public async Task Evaluate_LargeSection_ReportsInfo()
    {
        var fs = new InMemoryFileSystem();
        // Create a file with a section exceeding 10 lines (threshold set to 10)
        var lines = new List<string> { "# Big Section" };
        for (var i = 0; i < 20; i++) lines.Add($"Line {i}");
        fs.AddFile("root/doc.md", string.Join('\n', lines));

        var policy = new RepositoryPolicy
        {
            Governance = new GovernanceConfig
            {
                SectionSizeWarningThreshold = 10
            }
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("doc.md", 500, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new SectionSizeRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Info);
        diagnostics[0].RuleId.Should().Be("STWD-004");
        diagnostics[0].Message.Should().Contain("Big Section");
    }

    [Fact]
    public async Task Evaluate_SmallSections_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/doc.md", "# Small\nLine 1\nLine 2\n");

        var policy = new RepositoryPolicy
        {
            Governance = new GovernanceConfig
            {
                SectionSizeWarningThreshold = 500
            }
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("doc.md", 30, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new SectionSizeRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_DefaultThreshold_UsedWhenNotConfigured()
    {
        var fs = new InMemoryFileSystem();
        // 10 lines won't exceed the default 500
        var content = "# Title\n" + string.Concat(Enumerable.Repeat("Line\n", 10));
        fs.AddFile("root/doc.md", content);

        var context = new ValidationContext
        {
            Policy = new RepositoryPolicy(),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("doc.md", 100, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new SectionSizeRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_NestedLargeSection_Reported()
    {
        var fs = new InMemoryFileSystem();
        var lines = new List<string> { "# Parent", "Intro" };
        lines.Add("## Child");
        for (var i = 0; i < 15; i++) lines.Add($"Detail {i}");
        fs.AddFile("root/doc.md", string.Join('\n', lines));

        var policy = new RepositoryPolicy
        {
            Governance = new GovernanceConfig
            {
                SectionSizeWarningThreshold = 10
            }
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("doc.md", 500, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new SectionSizeRule();
        var diagnostics = await rule.EvaluateAsync(context);

        // Both parent (18 lines total) and child (16 lines) exceed threshold
        diagnostics.Should().HaveCountGreaterThanOrEqualTo(1);
        diagnostics.Should().Contain(d => d.Message.Contains("Child"));
    }
}
