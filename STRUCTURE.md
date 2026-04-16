# Repository Structure

```text
├── .editorconfig
├── .github/
│   ├── dependabot.yml
│   └── workflows/
│       └── ci.yml
├── .gitignore
├── .steward
├── Directory.Build.props
├── Directory.Packages.props
├── docs/
│   ├── audits/
│   │   ├── artifact-hygiene-cleanup-review-2026-04-16.md
│   │   ├── assessment-coding-agent-usefulness.md
│   │   ├── cli-expectation-fidelity-reassessment-2026-04-16.md
│   │   ├── cli-expectation-fidelity-review-2026-04-16.md
│   │   ├── cli-full-assessment-2026-04-16.md
│   │   ├── code-quality-pass-2026-04-16.md
│   │   ├── maintainer-review.md
│   │   ├── maintainer-usecase-expectations.md
│   │   ├── maintainer-usecase-ideas.md
│   │   ├── profile-readiness-review-2026-04-16.md
│   │   ├── release-governance-conformance-review-2026-04-16.md
│   │   ├── release-readiness-assessment-2026-04-15.md
│   │   ├── repo-actionability-pass-2026-04-16.md
│   │   ├── repository-audit-2026-04-14.md
│   │   ├── review-requirements.md
│   │   ├── usability-review-2026-04-15.md
│   │   └── usecase-consolidation-proposal.md
│   ├── decisions/
│   │   ├── adrs
│   │   ├── decision-index.md
│   │   └── rfcs
│   ├── implementation-status.md
│   ├── planning/
│   │   ├── curation-notes.md
│   │   ├── delivery-strategy.md
│   │   ├── implementation-instructions.md
│   │   ├── milestone-plan.md
│   │   ├── pre-1-0-readiness-plan.md
│   │   ├── pre-release-blockers.md
│   │   ├── release-publication-checklist.md
│   │   └── rfc-007-governance-enhancements-backlog.md
│   ├── planning-index.md
│   └── requirements/
│       ├── assumptions-constraints.md
│       ├── PRD.md
│       └── requirements-traceability.md
├── README.md
├── repository-steward-master-requirements.md
├── src/
│   ├── Steward.Cli/
│   │   ├── CommandContext.cs
│   │   ├── Commands
│   │   ├── CommandSetup.cs
│   │   ├── Formatting
│   │   ├── GlobalOptionsSetup.cs
│   │   ├── Program.cs
│   │   └── Steward.Cli.csproj
│   └── Steward.Core/
│       ├── Abstractions
│       ├── Configuration
│       ├── Discovery
│       ├── ExitCodes.cs
│       ├── Formatting
│       ├── GlobalOptions.cs
│       ├── Maintenance
│       ├── Markdown
│       ├── Models
│       ├── Orientation
│       ├── Search
│       ├── Steward.Core.csproj
│       └── Validation
├── steward.sln
├── STRUCTURE.md
└── tests/
    ├── Steward.Cli.Tests/
    │   ├── ChangeImpactTests.cs
    │   ├── CheckCommandTests.cs
    │   ├── CheckFixTests.cs
    │   ├── CliSnapshotTests.CheckJson_IsStable.verified.txt
    │   ├── CliSnapshotTests.cs
    │   ├── CliSnapshotTests.RootHelp_IsStable.verified.txt
    │   ├── ConfigCommandTests.cs
    │   ├── ConfigSettingsTests.cs
    │   ├── ExitCodeTests.cs
    │   ├── ExplainCommandTests.cs
    │   ├── GlobalOptionsTests.cs
    │   ├── GovernanceCoverageTests.cs
    │   ├── Helpers
    │   ├── InitCommandTests.cs
    │   ├── JsonFormatterTests.cs
    │   ├── MaintainCommandTests.cs
    │   ├── MdEditCommandTests.cs
    │   ├── MdQueryCommandTests.cs
    │   ├── OrientCommandTests.cs
    │   ├── OutlineCommandTests.cs
    │   ├── ProfileReadinessTests.cs
    │   ├── RefsCommandTests.cs
    │   ├── SearchCommandTests.cs
    │   ├── StableSurfaceContractTests.cs
    │   ├── StagedCompletenessTests.cs
    │   ├── StatusCommandTests.cs
    │   ├── Steward.Cli.Tests.csproj
    │   ├── TextFormatterTests.cs
    │   └── VersionCommandTests.cs
    ├── Steward.Core.Tests/
    │   ├── BootstrapAnalyzerTests.cs
    │   ├── BrokenArtifactReferenceRuleTests.cs
    │   ├── BrokenInternalLinkRuleTests.cs
    │   ├── ConfigLoaderTests.cs
    │   ├── DiagnosticTests.cs
    │   ├── DirectoryIndexMaintainerTests.cs
    │   ├── ExitCodeConstantsTests.cs
    │   ├── FileDiscoveryServiceTests.cs
    │   ├── ForbiddenPathRuleTests.cs
    │   ├── FreshnessRuleTests.cs
    │   ├── FrontmatterEditorTests.cs
    │   ├── FrontmatterValidationRuleTests.cs
    │   ├── GitIgnoreFilterTests.cs
    │   ├── IndexCompletenessRuleTests.cs
    │   ├── Maintenance
    │   ├── MaintenanceDependencyTests.cs
    │   ├── ManagedRegionIntegrityRuleTests.cs
    │   ├── MarkdownParserTests.cs
    │   ├── MdPathSelectorTests.cs
    │   ├── MoveEngineTests.cs
    │   ├── NamingConventionRuleTests.cs
    │   ├── OrientationEngineTests.cs
    │   ├── OrphanedDocumentRuleTests.cs
    │   ├── OutlineEngineTests.cs
    │   ├── PathPolicyEngineTests.cs
    │   ├── ProfileMergerTests.cs
    │   ├── RequiredArtifactRuleTests.cs
    │   ├── RoleDefaultsTests.cs
    │   ├── RuleRegistryTests.cs
    │   ├── SearchEngineTests.cs
    │   ├── SecretFilterTests.cs
    │   ├── SectionSizeRuleTests.cs
    │   ├── Steward.Core.Tests.csproj
    │   ├── StructuralEditorTests.cs
    │   ├── ValidationEngineTests.cs
    │   └── WellKnownRolesTests.cs
    └── Steward.TestFixtures/
        ├── InMemoryFileSystem.cs
        ├── Repos
        ├── RepositoryFixture.cs
        └── Steward.TestFixtures.csproj
```
