# Repository Structure

```
├── .editorconfig
├── .gitignore
├── .steward
├── Directory.Build.props
├── Directory.Packages.props
├── docs/
│   ├── audits/
│   │   ├── assessment-coding-agent-usefulness.md
│   │   ├── maintainer-review.md
│   │   ├── repository-audit-2026-04-14.md
│   │   └── review-requirements.md
│   ├── decisions/
│   │   ├── adrs
│   │   ├── decision-index.md
│   │   └── rfcs
│   ├── implementation-status.md
│   ├── planning/
│   │   ├── curation-notes.md
│   │   ├── delivery-strategy.md
│   │   ├── implementation-instructions.md
│   │   └── milestone-plan.md
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
    │   ├── Helpers
    │   ├── InitCommandTests.cs
    │   ├── JsonFormatterTests.cs
    │   ├── MaintainCommandTests.cs
    │   ├── MdEditCommandTests.cs
    │   ├── OrientCommandTests.cs
    │   ├── OutlineCommandTests.cs
    │   ├── SearchCommandTests.cs
    │   ├── StatusCommandTests.cs
    │   ├── Steward.Cli.Tests.csproj
    │   ├── TextFormatterTests.cs
    │   └── VersionCommandTests.cs
    ├── Steward.Core.Tests/
    │   ├── BrokenArtifactReferenceRuleTests.cs
    │   ├── BrokenInternalLinkRuleTests.cs
    │   ├── ConfigLoaderTests.cs
    │   ├── DiagnosticTests.cs
    │   ├── ExitCodeConstantsTests.cs
    │   ├── FileDiscoveryServiceTests.cs
    │   ├── ForbiddenPathRuleTests.cs
    │   ├── FrontmatterEditorTests.cs
    │   ├── FrontmatterValidationRuleTests.cs
    │   ├── GitIgnoreFilterTests.cs
    │   ├── Maintenance
    │   ├── ManagedRegionIntegrityRuleTests.cs
    │   ├── MarkdownParserTests.cs
    │   ├── MdPathSelectorTests.cs
    │   ├── OrientationEngineTests.cs
    │   ├── OutlineEngineTests.cs
    │   ├── PathPolicyEngineTests.cs
    │   ├── ProfileMergerTests.cs
    │   ├── RequiredArtifactRuleTests.cs
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
        └── Steward.TestFixtures.csproj
```
