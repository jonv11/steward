# Repository Structure

```text
├── .agents/
│   └── skills/
│       └── steward-cli
├── .editorconfig
├── .github/
│   ├── dependabot.yml
│   ├── release-labels.json
│   └── workflows/
│       ├── ci.yml
│       ├── pr-release-intent.yml
│       ├── release-labels.yml
│       └── release.yml
├── .gitignore
├── .markdownlint-cli2.jsonc
├── .steward
├── AGENTS.md
├── CHANGELOG.md
├── CONTRIBUTING.md
├── Directory.Build.props
├── Directory.Packages.props
├── docs/
│   ├── decisions/
│   │   ├── adrs
│   │   ├── README.md
│   │   └── rfcs
│   ├── guide/
│   │   ├── agent-integration.md
│   │   ├── configuration-reference.md
│   │   ├── contributor-guide.md
│   │   ├── maintainer-guide.md
│   │   └── README.md
│   ├── history/
│   │   ├── audits
│   │   ├── plans
│   │   ├── README.md
│   │   ├── reviews
│   │   └── stubs
│   ├── project/
│   │   ├── backlog.md
│   │   ├── README.md
│   │   ├── release-process.md
│   │   ├── release-publication-checklist.md
│   │   ├── roadmap.md
│   │   ├── status.md
│   │   └── workflow-guide.md
│   ├── README.md
│   └── requirements/
│       ├── assumptions-constraints.md
│       ├── master-requirements-source.md
│       ├── PRD.md
│       └── requirements-traceability.md
├── LICENSE
├── package-lock.json
├── package.json
├── README.md
├── scripts/
│   └── release/
│       ├── Build-ReleaseAssets.ps1
│       ├── Export-ReleaseNotes.ps1
│       └── Sync-ReleaseLabels.ps1
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
│       ├── PathHelper.cs
│       ├── Search
│       ├── Steward.Core.csproj
│       └── Validation
├── steward.sln
├── STRUCTURE.md
└── tests/
    ├── Steward.Cli.Tests/
    │   ├── ArtifactFamiliesCommandTests.cs
    │   ├── ChangeImpactTests.cs
    │   ├── CheckCommandTests.cs
    │   ├── CheckFixTests.cs
    │   ├── CheckSinceTests.cs
    │   ├── CheckSubdirectoryTests.cs
    │   ├── CliIdentityTests.cs
    │   ├── CliSnapshotTests.CheckJson_IsStable.verified.txt
    │   ├── CliSnapshotTests.cs
    │   ├── CliSnapshotTests.RootHelp_IsStable.verified.txt
    │   ├── ConfigCommandTests.cs
    │   ├── ConfigSettingsTests.cs
    │   ├── ExitCodeTests.cs
    │   ├── ExplainCommandTests.cs
    │   ├── ExplainRemediationConsistencyTests.cs
    │   ├── GlobalOptionsTests.cs
    │   ├── GovernanceCoverageTests.cs
    │   ├── Helpers
    │   ├── InitCommandTests.cs
    │   ├── JsonContractTests.cs
    │   ├── JsonFormatterTests.cs
    │   ├── MaintainCommandTests.cs
    │   ├── MdEditCommandTests.cs
    │   ├── MdQueryCommandTests.cs
    │   ├── OrientCommandTests.cs
    │   ├── OutlineCommandTests.cs
    │   ├── ProfileReadinessTests.cs
    │   ├── ProgramErrorHandlingTests.cs
    │   ├── RefsCommandTests.cs
    │   ├── SarifOutputTests.cs
    │   ├── SearchCommandTests.cs
    │   ├── StableSurfaceContractTests.cs
    │   ├── StagedCompletenessTests.cs
    │   ├── StatusCommandTests.cs
    │   ├── Steward.Cli.Tests.csproj
    │   ├── TextFormatterTests.cs
    │   └── VersionCommandTests.cs
    ├── Steward.Core.Tests/
    │   ├── ArtifactFamilyClassifierTests.cs
    │   ├── ArtifactFamilyValidationTests.cs
    │   ├── BootstrapAnalyzerTests.cs
    │   ├── BrokenArtifactReferenceRuleTests.cs
    │   ├── BrokenFragmentAnchorRuleTests.cs
    │   ├── BrokenInternalLinkRuleTests.cs
    │   ├── ConfigIntegrityTests.cs
    │   ├── ConfigLoaderFamilyValidationTests.cs
    │   ├── ConfigLoaderTests.cs
    │   ├── DiagnosticTests.cs
    │   ├── DirectoryIndexMaintainerTests.cs
    │   ├── ExitCodeConstantsTests.cs
    │   ├── FamilyAllowedAndDeprecatedFieldTests.cs
    │   ├── FamilyMinCountRuleTests.cs
    │   ├── FamilyNamingPatternRuleTests.cs
    │   ├── FamilySectionPatternRuleTests.cs
    │   ├── FamilySectionSchemaRuleTests.cs
    │   ├── FamilyTitlePatternRuleTests.cs
    │   ├── FileDiscoveryServiceTests.cs
    │   ├── ForbiddenPathRuleTests.cs
    │   ├── FreshnessRuleFixTests.cs
    │   ├── FreshnessRuleTests.cs
    │   ├── FrontmatterEditorTests.cs
    │   ├── FrontmatterValidationRuleTests.cs
    │   ├── GitIgnoreFilterTests.cs
    │   ├── IndexCompletenessRuleTests.cs
    │   ├── Maintenance
    │   ├── MaintenanceDependencyTests.cs
    │   ├── ManagedRegionIntegrityRuleTests.cs
    │   ├── ManagedScopeViolationRuleTests.cs
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
    │   ├── RequiredSectionsRuleTests.cs
    │   ├── RoleDefaultsTests.cs
    │   ├── RuleRegistryTests.cs
    │   ├── SearchEngineTests.cs
    │   ├── SecretFilterTests.cs
    │   ├── SectionSizeRuleTests.cs
    │   ├── SinceScopeResolverTests.cs
    │   ├── Steward.Core.Tests.csproj
    │   ├── StructuralEditorTests.cs
    │   ├── UniqueHeadingTextRuleTests.cs
    │   ├── ValidationEngineTests.cs
    │   └── WellKnownRolesTests.cs
    └── Steward.TestFixtures/
        ├── FaultInjectingFileSystem.cs
        ├── InMemoryFileSystem.cs
        ├── Repos
        ├── RepositoryFixture.cs
        └── Steward.TestFixtures.csproj
```
