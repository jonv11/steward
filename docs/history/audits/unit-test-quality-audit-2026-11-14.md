---
type: audit
status: Historical
summary: Test quality assessment and focused improvements to redaction, scoped validation, profile defaults, and test setup
last_updated: 2026-11-14
standalone: true
---

# Test Quality Assessment — 2026-11-14

Assessed checkout `20b9258` and the accompanying test improvements. This is point-in-time evidence;
[project status](../../project/status.md) owns current facts.

## Scope and method

Inspected both xUnit projects, shared fixtures, CI selection, and mechanical signals across C# tests.
Read behavior tests around secret filtering, search, profile merging, family validation, validation
orchestration, Git scoping, and remediation guidance. Ran the full suite before and after changes.
This is a risk-based review, not a line-by-line audit of every assertion. No branch coverage or
repository-wide mutation score was collected.

The baseline was 892 passing tests: 606 core and 286 CLI, with zero reported skips. The core
assembly mixes pure unit tests with filesystem/component tests and real Git integration tests;
assembly counts are not exact tier counts. CLI tests exercise the real command pipeline in-process,
with temporary repositories and serialized console/current-directory access. There is a useful
component-testing middle; this is neither an E2E-heavy pyramid nor an integration-free hourglass.

## Findings and disposition

| Severity | Evidence | Disposition |
| --- | --- | --- |
| P1 | Six secret-redaction rows checked only that a marker appeared and output changed; API-key leakage could pass. | Replaced with exact redacted outputs, including surrounding content, quotes, case, multiple secrets, and minimum-length controls. Detection and preservation share readable examples. |
| P1 | Three Git-dependent tests silently returned successfully when Git was unavailable. Setup commands also ignored exit status. | Removed early returns; required integration setup now fails explicitly, drains both output streams, checks exit codes, and kills timed-out processes. |
| P1 | `Check_Since_OnRealRepo_FiltersToChangedFiles` asserted only a scope-label substring. Its fixture changed policy rather than the claimed invalid document. | Now establishes two real frontmatter violations, changes one document, and checks parsed diagnostics, exit status, and exactly one checked file. |
| P2 | Valid-profile tests asserted only non-null policies. | Assert repository type and required/optional README behavior for each profile. Assert useful discovery exclusions. |
| P3 | Three tests separately scanned the same rule registry for remediation completeness. Empty registries could pass the scans. | Consolidated into one non-empty registry check covering blank and fallback guidance; retained specific remediation cases. |
| P3 | Search truncation used 200 lines to prove a five-result limit and only checked an approximate total. | Six lines now prove the limit, exact total and retained line numbers; added an exact-limit case that must not claim truncation. Role filtering asserts one actual result. |

No production code or CI configuration changed. No framework mocks, unconditional xUnit skips,
fixed sleeps, or live HTTP calls were found in the scanned C# tests. Existing suites contain
substantial positive/negative parser and validator coverage, regression comments, real filesystem
fixtures, and maintenance idempotence checks. Boolean assertions were reviewed in context:
asserting a boolean contract is appropriate; assertion count alone is not a quality measure.

## Fault detection evidence

In a disposable worktree, seeded two production faults together: append the original matched
API key after its redaction marker, and return all discovered files from `SinceScopeResolver`.

| Selected tests | Original assertions | Improved assertions |
| --- | --- | --- |
| Entire `SecretFilterTests` class | 14 pass despite API-key leakage | 2 fail, 17 pass; both API-key examples detect leakage |
| CLI `Check_Since_OnRealRepo_FiltersToChangedFiles` | 1 passes despite disabled filtering | 1 fails: expected one checked file, received five |

The selected command was `dotnet test steward.sln --filter
'FullyQualifiedName~SecretFilterTests|FullyQualifiedName~CheckSinceTests.Check_Since_OnRealRepo_FiltersToChangedFiles'`.
The improved run exited 1; the original-assertion run exited 0 with the same faults.
Faults never entered the working checkout. This is fault-injection evidence, not a claim of
feature-development TDD or a comprehensive mutation campaign.

## Validation and CI

| Tier | Command | CI job / trigger | Observed evidence | Can fail? |
| --- | --- | --- | --- | --- |
| Core/unit and component | `dotnet test steward.sln -c Release --no-build` | `ci / build-test-pack`, push and PR, Linux/Windows/macOS | Local: 612 pass, zero skips, under one second of test execution | Seeded redaction fault produces failing assertions and exit 1. |
| CLI integration and CLI helper unit tests | Same | Same | Local: 284 pass, zero skips, about one second of test execution | Seeded scope fault produces a failing assertion and exit 1. |
| Build | `dotnet build steward.sln -c Release` | Same | Local: no errors or warnings | Compiler failures are not ignored by the workflow. |

Focused core tests passed (54 cases); focused CLI tests passed (8 cases).
Markdown lint (`npm run lint:md`) passed. Structure maintenance reported up to date;
`dotnet run --project src/Steward.Cli -c Release --no-build -- check` passed with zero diagnostics.
Final local total: 896 passing tests. The two removed CLI tests were duplicate unit-level
registry scans folded into one; four standalone core redaction/detection tests were folded
into stronger parameterized cases. Real-engine integration coverage was retained and strengthened.
No speedup claim is made from these short, noisy local timings. Fixture size, redundant scans,
Git availability probes, and duplicated process setup were reduced.

The latest default-branch hosted run inspected was [CI run 43208762865](https://github.com/jonv11/steward/actions/runs/43208762865),
2026-11-12, commit `88f36e5`. Linux and macOS passed 607 core + 313 CLI tests; Windows passed
607 core and 307 CLI tests and failed six CLI tests. Its failures concern CRLF-sensitive schema
and completion assertions in files absent from this checkout. This is a different revision,
so those results neither verify nor invalidate these changes. The gate executes and can go red;
no test-step `continue-on-error` or `|| true` was found. No hosted run of these changes was triggered.

## Remaining risks and follow-ups

- Hosted Windows schema/completion failures need attention on the newer default-branch revision;
  verify fixes on a Windows runner without weakening content assertions.
- Freshness/fix tests and the in-memory filesystem still use `DateTime.UtcNow`. Exact-date checks
  can cross UTC midnight; a future clock seam would allow deterministic date-boundary coverage.
- Search has unreadable-file exception handling without focused injected I/O-failure tests in
  `SearchEngineTests`; add behavioral coverage at that owned filesystem boundary.
- Coverage quality outside the inspected hotspots remains unmeasured. Use targeted branch coverage
  or fault probes when changing a risky path, rather than adding a broad score gate or recurring
  mutation lane without evidence of benefit.
