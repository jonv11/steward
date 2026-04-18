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
│   ├── audits/
│   │   ├── ai-agent-contract-review-2026-04-18.md
│   │   ├── artifact-hygiene-cleanup-review-2026-04-16.md
│   │   ├── assessment-coding-agent-usefulness.md
│   │   ├── audit-curation-decision-log-2026-04-18.md
│   │   ├── cli-expectation-fidelity-assessment-2026-04-17.md
│   │   ├── cli-expectation-fidelity-reassessment-2026-04-16.md
│   │   ├── cli-expectation-fidelity-review-2026-04-16.md
│   │   ├── cli-full-assessment-2026-04-16.md
│   │   ├── code-quality-pass-2026-04-16.md
│   │   ├── end-user-documentation-path-audit-2026-04-17.md
│   │   ├── fresh-eyes-onboarding-audit-2026-04-18.md
│   │   ├── fresh-eyes-reaudit-onboarding-2026-04-18.md
│   │   ├── historical-audit-synthesis.md
│   │   ├── maintainer-remarks-implementation-summary-2026-04-18.md
│   │   ├── maintainer-review.md
│   │   ├── maintainer-usecase-expectations.md
│   │   ├── maintainer-usecase-ideas.md
│   │   ├── pre-1-0-release-process-pass-2026-04-17.md
│   │   ├── profile-readiness-review-2026-04-16.md
│   │   ├── release-governance-conformance-review-2026-04-16.md
│   │   ├── release-readiness-assessment-2026-04-15.md
│   │   ├── repo-actionability-pass-2026-04-16.md
│   │   ├── repo-quality-hardening-pass-2026-04-18.md
│   │   ├── repository-audit-2026-04-14.md
│   │   ├── review-requirements.md
│   │   ├── rule-system-completeness-audit-2026-04-18.md
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
│   │   ├── release-process.md
│   │   ├── release-publication-checklist.md
│   │   ├── repo-quality-hardening-pass-plan.md
│   │   ├── rfc-007-governance-enhancements-backlog.md
│   │   └── v0-15-draft-preparation.md
│   ├── planning-index.md
│   ├── requirements/
│   │   ├── assumptions-constraints.md
│   │   ├── PRD.md
│   │   └── requirements-traceability.md
│   └── reviews/
│       ├── ai-agent-contract-review.md
│       ├── config-expressiveness-stress-test.md
│       ├── review-synthesis-action-plan.md
│       └── rule-system-completeness-audit.md
├── dgitstewarddocsreviews
├── package-lock.json
├── package.json
├── README.md
├── repository-steward-master-requirements.md
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
    │   ├── RefsCommandTests.cs
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
    │   ├── FamilyMinCountRuleTests.cs
    │   ├── FamilyNamingPatternRuleTests.cs
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
    │   ├── Steward.Core.Tests.csproj
    │   ├── StructuralEditorTests.cs
    │   ├── UniqueHeadingTextRuleTests.cs
    │   ├── ValidationEngineTests.cs
    │   └── WellKnownRolesTests.cs
    └── Steward.TestFixtures/
        ├── InMemoryFileSystem.cs
        ├── Repos
        ├── RepositoryFixture.cs
        └── Steward.TestFixtures.csproj
```
