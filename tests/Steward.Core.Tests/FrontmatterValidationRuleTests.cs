using FluentAssertions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class FrontmatterValidationRuleTests
{
    [Fact]
    public async Task Evaluate_MissingFrontmatter_ReportsError()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/readme.md", "# Title\nContent\n");

        var policy = new RepositoryPolicy
        {
            Validation = new ValidationConfig
            {
                RequiredFrontmatterFields = ["title"]
            }
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("readme.md", 20, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new RequiredFrontmatterFieldRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].RuleId.Should().Be("STWD-003");
        diagnostics[0].Message.Should().Contain("missing frontmatter");
    }

    [Fact]
    public async Task Evaluate_MissingRequiredField_ReportsError()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/readme.md", "---\nauthor: someone\n---\n# Title\n");

        var policy = new RepositoryPolicy
        {
            Validation = new ValidationConfig
            {
                RequiredFrontmatterFields = ["title"]
            }
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("readme.md", 30, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new RequiredFrontmatterFieldRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].Message.Should().Contain("title");
    }

    [Fact]
    public async Task Evaluate_AllFieldsPresent_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/readme.md", "---\ntitle: Test\n---\n# Title\n");

        var policy = new RepositoryPolicy
        {
            Validation = new ValidationConfig
            {
                RequiredFrontmatterFields = ["title"]
            }
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("readme.md", 30, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new RequiredFrontmatterFieldRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_NoFrontmatterPolicy_NoDiagnostics()
    {
        var context = new ValidationContext
        {
            Policy = new RepositoryPolicy(),
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("readme.md", 10, false)],
            FileSystem = new InMemoryFileSystem(),
            RepositoryRoot = "root"
        };

        var rule = new RequiredFrontmatterFieldRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_NonMarkdownFiles_Skipped()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/data.json", "{}");

        var policy = new RepositoryPolicy
        {
            Validation = new ValidationConfig
            {
                RequiredFrontmatterFields = ["title"]
            }
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("data.json", 5, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new RequiredFrontmatterFieldRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }
}
