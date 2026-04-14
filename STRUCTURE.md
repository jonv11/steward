# Repository Structure

```
├── .editorconfig
├── .gitignore
├── .steward
├── Directory.Build.props
├── Directory.Packages.props
├── docs/
│   ├── decisions/
│   │   ├── adrs
│   │   ├── decision-index.md
│   │   └── rfcs
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
│   │   ├── Commands
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
    │   ├── ExitCodeTests.cs
    │   ├── ExplainCommandTests.cs
    │   ├── GlobalOptionsTests.cs
    │   ├── Helpers
    │   ├── JsonFormatterTests.cs
    │   ├── MaintainCommandTests.cs
    │   ├── MdEditCommandTests.cs
    │   ├── StatusCommandTests.cs
    │   ├── Steward.Cli.Tests.csproj
    │   ├── TextFormatterTests.cs
    │   └── VersionCommandTests.cs
    ├── Steward.Core.Tests/
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
    │   ├── SearchEngineTests.cs
    │   ├── SecretFilterTests.cs
    │   ├── SectionSizeRuleTests.cs
    │   ├── Steward.Core.Tests.csproj
    │   ├── StructuralEditorTests.cs
    │   └── ValidationEngineTests.cs
    └── Steward.TestFixtures/
        ├── InMemoryFileSystem.cs
        └── Steward.TestFixtures.csproj
```
