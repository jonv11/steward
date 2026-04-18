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

    [Fact]
    public async Task Evaluate_ScopedRequirements_ApplyOnlyToMatchingPaths()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/decisions/rfcs/RFC-001.md", "---\ntitle: RFC\n---\n# RFC\n");
        fs.AddFile("root/docs/readme.md", "---\ntitle: Readme\n---\n# Readme\n");

        var policy = new RepositoryPolicy
        {
            Validation = new ValidationConfig
            {
                FrontmatterRequirements =
                [
                    new FrontmatterRequirement
                    {
                        Pattern = "docs/decisions/**/*.md",
                        RequiredFields = ["status"]
                    }
                ]
            }
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles =
            [
                new DiscoveredFile("docs/decisions/rfcs/RFC-001.md", 30, false),
                new DiscoveredFile("docs/readme.md", 20, false)
            ],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new RequiredFrontmatterFieldRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].Path.Should().Be("docs/decisions/rfcs/RFC-001.md");
        diagnostics[0].Message.Should().Contain("status");
    }

    [Fact]
    public async Task Evaluate_ScopedRequirements_MergeWithGlobal()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/decisions/adr.md", "---\ntitle: ADR\n---\n# ADR\n");

        var policy = new RepositoryPolicy
        {
            Validation = new ValidationConfig
            {
                RequiredFrontmatterFields = ["title"],
                FrontmatterRequirements =
                [
                    new FrontmatterRequirement
                    {
                        Pattern = "docs/decisions/**/*.md",
                        RequiredFields = ["status"]
                    }
                ]
            }
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/decisions/adr.md", 30, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new RequiredFrontmatterFieldRule();
        var diagnostics = await rule.EvaluateAsync(context);

        // title is present, but status is missing
        diagnostics.Should().HaveCount(1);
        diagnostics[0].Message.Should().Contain("status");
    }

    [Fact]
    public async Task Evaluate_ScopedAllowedValues_ReportsInvalidValue()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/rfcs/RFC-001.md", "---\nstatus: Invalid\n---\n# RFC\n");

        var policy = new RepositoryPolicy
        {
            Validation = new ValidationConfig
            {
                FrontmatterRequirements =
                [
                    new FrontmatterRequirement
                    {
                        Pattern = "docs/rfcs/**/*.md",
                        RequiredFields = ["status"],
                        AllowedValues = new Dictionary<string, List<string>>
                        {
                            ["status"] = ["Draft", "Accepted", "Rejected"]
                        }
                    }
                ]
            }
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/rfcs/RFC-001.md", 30, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new RequiredFrontmatterFieldRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].Message.Should().Contain("not in the allowed set");
    }

    [Fact]
    public async Task Evaluate_ScopedAllowedValues_AcceptsValidValue()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/rfcs/RFC-001.md", "---\nstatus: Draft\n---\n# RFC\n");

        var policy = new RepositoryPolicy
        {
            Validation = new ValidationConfig
            {
                FrontmatterRequirements =
                [
                    new FrontmatterRequirement
                    {
                        Pattern = "docs/rfcs/**/*.md",
                        RequiredFields = ["status"],
                        AllowedValues = new Dictionary<string, List<string>>
                        {
                            ["status"] = ["Draft", "Accepted", "Rejected"]
                        }
                    }
                ]
            }
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/rfcs/RFC-001.md", 30, false)],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new RequiredFrontmatterFieldRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_DirectoryIndexSources_RequireDescriptionButExcludeTarget()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("root/docs/index.md", "# Index\n");
        fs.AddFile("root/docs/alpha.md", "---\nstatus: Draft\n---\n# Alpha\n");

        var policy = new RepositoryPolicy
        {
            Maintenance = new MaintenanceConfig
            {
                Artifacts =
                [
                    new MaintenanceArtifactDef
                    {
                        Id = "docs-index",
                        Path = "docs/index.md",
                        Type = "directory-index",
                        Source = "docs/*.md"
                    }
                ]
            }
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles =
            [
                new DiscoveredFile("docs/index.md", 20, false),
                new DiscoveredFile("docs/alpha.md", 20, false)
            ],
            FileSystem = fs,
            RepositoryRoot = "root"
        };

        var rule = new RequiredFrontmatterFieldRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().ContainSingle(diagnostic => diagnostic.Path == "docs/alpha.md");
        diagnostics[0].Message.Should().Contain("description");
        diagnostics[0].Remediation.Should().Contain("generated directory indexes");
    }
}
