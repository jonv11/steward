using FluentAssertions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Maintenance;
using Steward.TestFixtures;
using Xunit;

namespace Steward.Core.Tests.Maintenance;

public class MaintenanceEngineTests
{
    private static MaintenanceContext CreateContext(InMemoryFileSystem fs, params DiscoveredFile[] files)
    {
        return new MaintenanceContext
        {
            RepositoryRoot = "/repo",
            FileSystem = fs,
            Files = files.ToList()
        };
    }

    [Fact]
    public void Evaluate_NoConfig_ReturnsEmptyPlan()
    {
        var fs = new InMemoryFileSystem();
        var engine = new MaintenanceEngine();
        var plan = engine.Evaluate(null, CreateContext(fs));

        plan.Actions.Should().BeEmpty();
        plan.HasChanges.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_NoArtifacts_ReturnsEmptyPlan()
    {
        var policy = new RepositoryPolicy { Maintenance = new MaintenanceConfig() };
        var fs = new InMemoryFileSystem();
        var engine = new MaintenanceEngine();
        var plan = engine.Evaluate(policy, CreateContext(fs));

        plan.Actions.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_AutoFieldsWithoutExplicitArtifacts_SynthesizesFrontmatterMaintenance()
    {
        var policy = new RepositoryPolicy
        {
            Governance = new GovernanceConfig
            {
                Frontmatter = new FrontmatterConfig
                {
                    AutoFields = new Dictionary<string, bool> { ["last_updated"] = true }
                }
            }
        };

        var fs = new InMemoryFileSystem()
            .AddFile("/repo/doc.md", "---\nlast_updated: 2026-04-01\n---\n# Doc\n");
        var engine = new MaintenanceEngine();

        var plan = engine.Evaluate(
            policy,
            new MaintenanceContext
            {
                RepositoryRoot = "/repo",
                FileSystem = fs,
                Files = [new DiscoveredFile("doc.md", 100, false)],
                ChangedFiles = null
            });

        plan.Actions.Should().ContainSingle(action => action.Type == "frontmatter-auto");
    }

    [Fact]
    public void Evaluate_DispatchesToCorrectMaintainer()
    {
        var policy = new RepositoryPolicy
        {
            Maintenance = new MaintenanceConfig
            {
                Artifacts =
                [
                    new MaintenanceArtifactDef
                    {
                        Id = "struct",
                        Path = "STRUCTURE.md",
                        Type = "structure-document"
                    }
                ]
            }
        };

        var fs = new InMemoryFileSystem();
        var files = new[] { new DiscoveredFile("README.md", 100, false) };
        var engine = new MaintenanceEngine();
        var plan = engine.Evaluate(policy, CreateContext(fs, files));

        plan.Actions.Should().HaveCount(1);
        plan.Actions[0].ArtifactId.Should().Be("struct");
        plan.Actions[0].Type.Should().Be("structure-document");
    }

    [Fact]
    public void Evaluate_ScopeFiltersArtifacts()
    {
        var policy = new RepositoryPolicy
        {
            Maintenance = new MaintenanceConfig
            {
                Artifacts =
                [
                    new MaintenanceArtifactDef { Id = "a", Path = "A.md", Type = "structure-document" },
                    new MaintenanceArtifactDef { Id = "b", Path = "B.md", Type = "structure-document" }
                ]
            }
        };

        var fs = new InMemoryFileSystem();
        var files = new[] { new DiscoveredFile("README.md", 100, false) };
        var engine = new MaintenanceEngine();
        var plan = engine.Evaluate(policy, CreateContext(fs, files), scope: "b");

        plan.Actions.Should().HaveCount(1);
        plan.Actions[0].ArtifactId.Should().Be("b");
    }

    [Fact]
    public void Evaluate_SkipsUnknownType()
    {
        var policy = new RepositoryPolicy
        {
            Maintenance = new MaintenanceConfig
            {
                Artifacts =
                [
                    new MaintenanceArtifactDef { Id = "custom", Path = "X.md", Type = "unknown-type" }
                ]
            }
        };

        var fs = new InMemoryFileSystem();
        var engine = new MaintenanceEngine();
        var plan = engine.Evaluate(policy, CreateContext(fs));

        plan.Actions.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_MultipleArtifacts_AllEvaluated()
    {
        var policy = new RepositoryPolicy
        {
            Maintenance = new MaintenanceConfig
            {
                Artifacts =
                [
                    new MaintenanceArtifactDef { Id = "s1", Path = "S1.md", Type = "structure-document" },
                    new MaintenanceArtifactDef { Id = "s2", Path = "S2.md", Type = "structure-document" }
                ]
            }
        };

        var fs = new InMemoryFileSystem();
        var files = new[] { new DiscoveredFile("file.md", 50, false) };
        var engine = new MaintenanceEngine();
        var plan = engine.Evaluate(policy, CreateContext(fs, files));

        plan.Actions.Should().HaveCount(2);
    }

    [Fact]
    public void Evaluate_SkipsArtifactsWithNullIdOrType()
    {
        var policy = new RepositoryPolicy
        {
            Maintenance = new MaintenanceConfig
            {
                Artifacts =
                [
                    new MaintenanceArtifactDef { Id = null, Path = "X.md", Type = "structure-document" },
                    new MaintenanceArtifactDef { Id = "valid", Path = "V.md", Type = null },
                    new MaintenanceArtifactDef { Id = "ok", Path = "OK.md", Type = "structure-document" }
                ]
            }
        };

        var fs = new InMemoryFileSystem();
        var files = new[] { new DiscoveredFile("file.md", 50, false) };
        var engine = new MaintenanceEngine();
        var plan = engine.Evaluate(policy, CreateContext(fs, files));

        plan.Actions.Should().HaveCount(1);
        plan.Actions[0].ArtifactId.Should().Be("ok");
    }

    [Fact]
    public void Evaluate_CustomMaintainers()
    {
        var custom = new DummyMaintainer("custom-type");
        var engine = new MaintenanceEngine([custom]);

        var policy = new RepositoryPolicy
        {
            Maintenance = new MaintenanceConfig
            {
                Artifacts =
                [
                    new MaintenanceArtifactDef { Id = "c1", Path = "C.md", Type = "custom-type" }
                ]
            }
        };

        var fs = new InMemoryFileSystem();
        var plan = engine.Evaluate(policy, CreateContext(fs));

        plan.Actions.Should().HaveCount(1);
        plan.Actions[0].ArtifactId.Should().Be("c1");
        plan.Actions[0].Description.Should().Be("Custom evaluation");
    }

    private sealed class DummyMaintainer(string type) : IArtifactMaintainer
    {
        public string Type => type;

        public MaintenanceAction Evaluate(MaintenanceArtifactConfig config, MaintenanceContext context)
        {
            return new MaintenanceAction
            {
                ArtifactId = config.Id,
                ArtifactPath = config.Path,
                Type = Type,
                Description = "Custom evaluation",
                HasChanges = false
            };
        }
    }
}
