using FluentAssertions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests;

public class OrphanedDocumentRuleTests
{
    [Fact]
    public async Task Evaluate_UnlinkedFile_ReportsInfo()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/root/docs/orphan.md", "# Orphaned");
        fs.AddFile("/root/README.md", "# Root\nHello.");

        var policy = new RepositoryPolicy
        {
            Artifacts = [new() { Path = "README.md", Role = "authoritative" }]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles =
            [
                new DiscoveredFile("README.md", 20, false),
                new DiscoveredFile("docs/orphan.md", 15, false)
            ],
            FileSystem = fs,
            RepositoryRoot = "/root"
        };

        var rule = new OrphanedDocumentRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().HaveCount(1);
        diagnostics[0].RuleId.Should().Be("STWD-013");
        diagnostics[0].Severity.Should().Be(DiagnosticSeverity.Info);
        diagnostics[0].Path.Should().Contain("orphan.md");
    }

    [Fact]
    public async Task Evaluate_LinkedFile_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/root/README.md", "# Root\n[Guide](docs/guide.md)");
        fs.AddFile("/root/docs/guide.md", "# Guide");

        var policy = new RepositoryPolicy
        {
            Artifacts = [new() { Path = "README.md", Role = "authoritative" }]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles =
            [
                new DiscoveredFile("README.md", 30, false),
                new DiscoveredFile("docs/guide.md", 10, false)
            ],
            FileSystem = fs,
            RepositoryRoot = "/root"
        };

        var rule = new OrphanedDocumentRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_StartHereFile_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/root/getting-started.md", "# Getting Started");

        var policy = new RepositoryPolicy
        {
            Governance = new GovernanceConfig { StartHere = ["getting-started.md"] }
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("getting-started.md", 20, false)],
            FileSystem = fs,
            RepositoryRoot = "/root"
        };

        var rule = new OrphanedDocumentRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_StandaloneTrue_Suppressed()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/root/scratch.md", "---\nstandalone: true\n---\n# Scratch");

        var policy = new RepositoryPolicy();

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("scratch.md", 40, false)],
            FileSystem = fs,
            RepositoryRoot = "/root"
        };

        var rule = new OrphanedDocumentRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_ArtifactPath_NoDiagnostics()
    {
        var fs = new InMemoryFileSystem();
        fs.AddFile("/root/docs/PRD.md", "# PRD");

        var policy = new RepositoryPolicy
        {
            Artifacts = [new() { Path = "docs/PRD.md", Role = "requirements" }]
        };

        var context = new ValidationContext
        {
            Policy = policy,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/PRD.md", 30, false)],
            FileSystem = fs,
            RepositoryRoot = "/root"
        };

        var rule = new OrphanedDocumentRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_ScopedRun_FileReferencedByOutOfScopeFile_NoFalsePositive()
    {
        // docs/index.md (out of scope) links to docs/guide.md (in scope).
        // Running with only docs/guide.md in TargetFiles must NOT flag it as orphaned,
        // because AllDiscoveredFiles provides the full inbound-link graph.
        var fs = new InMemoryFileSystem();
        fs.AddFile("/repo/docs/index.md", "# Index\n\nSee [guide](guide.md).");
        fs.AddFile("/repo/docs/guide.md", "# Guide");

        var context = new ValidationContext
        {
            Policy = null,
            PathPolicy = null,
            TargetFiles = [new DiscoveredFile("docs/guide.md", 100, false)],
            AllDiscoveredFiles =
            [
                new DiscoveredFile("docs/index.md", 200, false),
                new DiscoveredFile("docs/guide.md", 100, false)
            ],
            FileSystem = fs,
            RepositoryRoot = "/repo"
        };

        var rule = new OrphanedDocumentRule();
        var diagnostics = await rule.EvaluateAsync(context);

        diagnostics.Should().BeEmpty("docs/guide.md is referenced by docs/index.md which is in AllDiscoveredFiles");
    }
}
