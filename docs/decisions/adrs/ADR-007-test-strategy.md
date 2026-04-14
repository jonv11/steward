# ADR-007: Test Strategy

- **Status:** Accepted
- **Category:** Quality

---

## Context

The requirements demand strong test backing (REQ-TEST-001 through REQ-TEST-004). Tests must be meaningful, deterministic, behavior-focused, and robust enough to make AI-assisted modification safe.

## Decision

### Frameworks

| Framework | Purpose |
|-----------|---------|
| **xUnit** | Test runner. The .NET standard; well-supported, extensible. |
| **FluentAssertions** | Assertion library. Readable, expressive, good failure messages. |
| **Verify** (VerifyTests) | Snapshot testing. Ideal for validating deterministic output, diagnostics, and structural results. |
| **Microsoft.Extensions.DependencyInjection** | Test service composition where needed. |

### Test types

| Type | Project | What it tests |
|------|---------|--------------|
| **Unit tests** | Steward.Core.Tests | Individual classes and functions. Uses interfaces/mocks for file system, git. Fast. |
| **Integration tests** | Steward.Cli.Tests | Full CLI invocation via process execution. Checks stdout, stderr, exit code. Uses fixture repos. |
| **Snapshot tests** | Both | Captures output and compares to verified baselines. Ideal for diagnostics, orientation output, search results. |
| **Fixture-based tests** | Both | Uses pre-built repository fixtures under `tests/Steward.TestFixtures/Repos/` to test against realistic repo structures. |

### Test project structure

```
tests/
  Steward.Core.Tests/
    Configuration/ConfigLoaderTests.cs
    Discovery/FileDiscoveryTests.cs
    Markdown/StructuredDocumentTests.cs
    Markdown/MdPathSelectorTests.cs
    Markdown/StructuralEditTests.cs
    Validation/PathPolicyRuleTests.cs
    Validation/FrontmatterRuleTests.cs
    ...
  Steward.Cli.Tests/
    CheckCommandTests.cs
    OrientCommandTests.cs
    SearchCommandTests.cs
    GlobalOptionsTests.cs
    ExitCodeTests.cs
    ...
  Steward.TestFixtures/
    Repos/
      minimal-repo/           # Bare minimum valid repo
      software-repo/          # Typical software repo with policy
      docs-repo/              # Documentation-heavy repo
      unconfigured-repo/      # No .steward/ directory
      broken-policy-repo/     # Invalid policy for error-path testing
    Builders/
      FixtureBuilder.cs       # Programmatic test repo construction
    TestHelpers.cs
```

### Test conventions

- **Test naming:** `MethodName_Condition_ExpectedResult` (e.g., `Validate_MissingRequiredArtifact_ReturnsError`).
- **One assertion per test** as a default. Related assertions for the same behavior may be grouped.
- **No test interdependence.** Each test creates its own state or uses a shared immutable fixture.
- **Snapshot tests use Verify.** Verified files are committed alongside the test.
- **File system abstraction.** Core tests use `IFileSystem` with in-memory implementations. CLI tests use real file fixtures.

### Coverage expectations

- Core domain logic: high coverage (aim for >80% on validation rules, markdown engine, config loading).
- CLI commands: integration tests cover the happy path and key error paths.
- Output formatting: snapshot tests verify both text and JSON output stability.
- No coverage targets for trivial code (constructors, DTOs).

### CI integration

- All tests run on `dotnet test` with no external dependencies.
- Tests must be deterministic—no reliance on network, timestamps, or random ordering.
- Test fixtures are self-contained within the repository.

## Alternatives considered

1. **NUnit:** Viable but xUnit is the .NET community standard for new projects.
2. **Moq for mocking:** Avoided—prefer hand-written test doubles (fakes/stubs) over mocking libraries. Fakes are more readable and less brittle.
3. **Playwright/Puppeteer for CLI testing:** Overkill. Process execution with stdout/stderr capture is sufficient.
4. **Coverlet with coverage gates:** Coverage measurement yes (via Coverlet), hard gates no—coverage percentage is a guideline, not a contract.

## Consequences

- Tests are meaningful, deterministic, and behavior-focused.
- Snapshot tests provide regression safety for output stability.
- Fixture repos give realistic test scenarios.
- AI coding agents can modify code confidently because tests catch regressions.
- No external dependencies required for test execution.
