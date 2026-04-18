# AI-Agent Contract Review — Steward CLI

**Date:** 2026-04-18
**Scope:** Full command surface, JSON output contracts, diagnostics, selectors, remediation flows
**Method:** Direct command invocations against this repo, source inspection, end-to-end agent flow simulation
**Version under review:** 0.15.0

## Implementation Status

The following findings have been addressed in the unreleased 0.16.0 work:

| Finding | Status | Implementation |
|---------|--------|----------------|
| CC-01: Errors escape JSON | **Resolved** | `JsonEnvelopeWriter.WriteError()` on all command error paths |
| CC-02: Envelope not universal | **Resolved** | All JSON commands now respect `--json-envelope standard` |
| CC-03: success semantics ambiguous | **Resolved** | `success: true` for domain outcomes; exit code differentiates |
| CC-04: refactor move --apply broken in JSON | **Resolved** | Apply logic executes before output format branching |
| CC-05: explain path missing exists | **Resolved** | `exists` boolean field added |
| CC-06: Diagnostic remediation lacks precision | **Resolved** | `details` dict on Diagnostic; STWD-003/008/010/016 populated |
| CC-07: Batch vs single md query shapes differ | **Resolved** | Single-file normalized to `results[]` with `matchCount`/`range` |
| CC-08: Config validate errors unstructured | **Resolved** | Errors now `[{file, message}]` objects |
| CC-09: Contract test gaps | **Resolved** | 16 new tests in `JsonContractTests.cs` |
| CC-10: refactor move preview lacks safety data | **Resolved** | Added `sourceExists`, `collision`, `applied`, `affectedFileCount`, per-edit `rewrites` |

---

## Executive Summary

Steward's command surface is **partially agent-usable** — read-oriented commands (`check`, `status`, `orient`, `search`, `refs`, `explain`) produce well-shaped JSON that an autonomous agent can parse and reason over without bespoke glue. The check diagnostic model is genuinely strong: stable rule IDs, repo-relative paths, severity/category classification, and remediation hints make it a first-class machine surface.

However, the CLI is **not yet credibly agent-ready** because of five structural contract problems that would cause any competent autonomous agent to hesitate, retry, or guess:

1. **Error responses bypass JSON.** When `--output json` is active, failures (missing files, invalid selectors, unknown rule IDs) emit plain text to stderr and produce no JSON on stdout. An agent cannot rely on parsing stdout alone.
2. **Standard envelope is not universal.** `--json-envelope standard` is silently ignored by `version`, `outline`, `explain path`, and `refactor move`. An agent requesting a consistent envelope gets inconsistent shapes.
3. **Envelope `success` conflates process and domain outcomes.** `check` returns `success: false` for validation failures, making it impossible to distinguish "the command broke" from "the repo has violations" without inspecting nested fields.
4. **Mutation in JSON mode is broken for `refactor move`.** The `--apply` flag has no effect when `--output json` is active — the file is not moved, links are not rewritten, but exit code is 0.
5. **Handoff between commands requires ad-hoc translation.** There is no canonical machine address that flows from `search` → `md query`, from `check` diagnostics → `explain path`, or from `refs` → `refactor move`.

An autonomous agent can use Steward today for read-oriented governance workflows, but must add command-specific error handling, stderr fallback parsing, and postcondition verification for any mutation path.

## Commands and Flows Reviewed

### Commands tested with JSON output

| Command | JSON output | Standard envelope | Error as JSON |
|---------|:-----------:|:-----------------:|:-------------:|
| `version` | Yes | **No** — silently ignored | N/A |
| `orient --signals` | Yes | Yes | Not tested (always succeeds with config) |
| `outline` | Yes | **No** — silently ignored | Plain text only |
| `status --coverage` | Yes | Yes | Plain text for missing config |
| `check` | Yes | Yes | N/A (failures are domain results) |
| `explain` (list rules) | Yes | **No** — raw array | Plain text for unknown rule |
| `explain path` | Yes | **No** — silently ignored | Returns success on missing file |
| `search` | Yes | Yes | Plain text for invalid regex |
| `refs` | Yes | Yes | Not tested (succeeds on existing file) |
| `maintain` | Yes | Yes | Not tested |
| `md query` | Yes | Yes | Plain text for syntax error; JSON for empty match |
| `md outline` | Yes | Yes | Plain text for missing file |
| `md edit *` | Yes | Yes | Plain text for missing file/section |
| `md edit fm-validate` | Yes | Yes | Not tested (succeeds) |
| `config validate` | Yes | Yes | **Yes** — structured `{valid: false, errors}` |
| `config doctor` | Yes | Yes | Not tested |
| `refactor move` | Yes | **No** — silently ignored | Plain text for missing args |

### End-to-end agent flow: discover → diagnose → remediate

**Flow A: Scoped check → explain → fix**

```
1. check --paths docs/planning/implementation-instructions.md --output json --json-envelope standard
   → Returns 6 STWD-008 diagnostics with path, line, message, remediation
   → Each diagnostic has ruleId, severity, category — stable machine identifiers
   → Agent can filter by ruleId to prioritize

2. explain STWD-008 --output json
   → Returns description, remediation text
   → But no standard envelope — agent must handle two JSON shapes

3. Agent reads the file at the reported path:line to find the broken link
   → Must parse the "Broken link to '...' — file not found" message string
   → No structured field for the target path of the broken link
   → No selector or address for handoff to md edit
```

**Verdict:** The discovery phase works well. The remediation handoff requires message-string parsing because the broken-link target is embedded in prose rather than a typed field.

**Flow B: Orient → status → check → maintain**

```
1. orient --signals --output json --json-envelope standard
   → Stable envelope, startHere array, classified entries
   → Agent knows which files to read first

2. status --coverage --output json --json-envelope standard
   → Required/recommended/state artifacts, family counts, coverage
   → Agent can detect missing artifacts, stale docs

3. check --output json --json-envelope standard
   → Diagnostics array — agent processes violations

4. maintain --output json --json-envelope standard
   → Preview shows hasChanges, per-artifact actions, fileEdits
   → If stale, agent can run maintain --apply
```

**Verdict:** This chain works reliably for the read path. The standard envelope is consistent across these four commands.

**Flow C: Search → md query → md edit**

```
1. search "freshness" --output json --max 5
   → Returns path, line, column, snippet, kind, headingContext
   → But no canonical selector for handoff to md query

2. md query <path> "heading[<headingContext>]" --output json
   → Agent must construct selector from headingContext string
   → Returns content, range.start/end (line numbers)

3. md edit set-section <path> --heading <heading> --content "..." --output json
   → Preview with diff, hasChanges, applied=false
   → Add --apply to commit
```

**Verdict:** Works with prompt glue. The `headingContext` → `heading[...]` selector construction is guesswork that will break on headings with special characters.

## Contract Strengths

### 1. Check diagnostics are strong machine contracts

```json
{
  "ruleId": "STWD-008",
  "severity": "warn",
  "category": "broken-link",
  "path": "docs/planning/implementation-instructions.md",
  "line": 18,
  "message": "Broken link to '../decisions/adrs/ADR-013...' — file not found.",
  "remediation": "Verify the link target exists or update the reference."
}
```

Stable rule IDs (`STWD-001` through `STWD-017`), consistent severity values, repo-relative paths, and line numbers. An agent can filter, sort, and triage without guessing.

### 2. Status provides comprehensive machine-readable inventory

The `status --coverage` JSON cleanly separates required artifacts, recommended artifacts, state documents, maintenance artifacts, family summaries, and coverage stats. Each artifact has `path`, `role`, `importance`, `present`, and optionally `stale`/`freshnessMaxAgeDays`. This is exactly what an agent needs for gap analysis.

### 3. Maintain preview is reviewable

```json
{
  "hasChanges": false,
  "applied": false,
  "actions": [
    {
      "artifactId": "structure",
      "artifactPath": "STRUCTURE.md",
      "type": "structure-document",
      "description": "Structure document is up to date.",
      "hasChanges": false,
      "blocked": false,
      "fileEdits": []
    }
  ]
}
```

Each action has a stable `artifactId`, type, change flag, and block status. The `fileEdits` array would list affected files when changes exist. This is good preview/apply separation.

### 4. Explain path exposes governance context

```json
{
  "path": "docs/planning/implementation-instructions.md",
  "classification": "workflow",
  "matchedPattern": "docs/planning/*.md",
  "matchedFamily": "planning",
  "requiredFrontmatterFields": ["type", "status"],
  "allowedValues": { "type": ["planning"], "status": ["Draft", "Active", ...] },
  "applicableRules": ["STWD-001", "STWD-002", ...]
}
```

An agent can determine exactly what frontmatter to set, what values are valid, and which rules will fire — without reading config files.

### 5. Standard envelope (where supported) is well-designed

```json
{
  "schemaVersion": "steward-json/v1",
  "command": "check",
  "toolVersion": "0.15.0",
  "success": true,
  "exitCode": 0,
  "data": { ... }
}
```

The envelope includes schema versioning, command identity, tool version, and wraps payload cleanly. The design is right — it just needs universal application.

### 6. Search results include heading context

```json
{
  "path": ".steward/policy.yaml",
  "line": 38,
  "column": 5,
  "snippet": "freshness:",
  "kind": "content",
  "headingContext": "..."
}
```

The `headingContext` field is a genuine machine convenience — it tells the agent where in the document structure a match lives.

### 7. FM-validate is a complete governance check

```json
{
  "valid": true,
  "issues": [],
  "requiredFields": ["type", "status"],
  "allowedValues": { "type": ["planning"], "status": ["Draft", "Active", ...] }
}
```

This tells an agent exactly what's required and what's allowed, with a clear boolean verdict. A model example of a machine-oriented contract.

### 8. Config validate is the best structured failure example

When config is invalid, it returns `{ valid: false, errors: [...] }` wrapped in a standard envelope with `success: false, exitCode: 2`. This is the pattern all failure responses should follow.

## Contract Weaknesses

### 1. Errors escape JSON entirely

When `--output json` is active:

| Error scenario | Actual output | Expected for agent |
|---------------|---------------|-------------------|
| `md query nonexistent.md "heading[X]"` | stderr: `File not found: nonexistent.md` | JSON error object on stdout |
| `md edit set-section README.md --heading "NonExistentSection"` | stderr: `Section 'NonExistentSection' not found.` | JSON error object on stdout |
| `explain BOGUS` | stderr: `Unknown rule ID: 'BOGUS'. Use 'steward explain' to list all rules.` | JSON error object on stdout |
| `refactor move` (no `--preview`/`--apply`) | stderr: `Specify --preview to see changes, or --apply to execute.` | JSON error object on stdout |

An agent parsing stdout for JSON gets nothing. It must also scrape stderr, detect non-JSON content, and branch on error patterns — classic prompt-glue territory.

### 2. Standard envelope is silently ignored by several commands

Tested with `--output json --json-envelope standard`:

| Command | Envelope applied? |
|---------|:-----------------:|
| `version` | No — raw `{version, runtimeVersion, ...}` |
| `outline` (directory) | No — raw `{rootPath, entries}` |
| `explain path` | No — raw `{path, classification, ...}` |
| `explain` (list rules) | No — raw array `[{ruleId, ...}]` |
| `refactor move --preview` | No — raw `{oldPath, newPath, edits}` |

These commands silently ignore the flag. No warning, no indication. An agent using `--json-envelope standard` universally will get mixed shapes.

### 3. `success` semantics are ambiguous

In the standard envelope:
- `check` with violations: `success: false, exitCode: 1` — but the command executed perfectly; the *repo* failed validation.
- `config validate` with errors: `success: false, exitCode: 2` — the config is bad, which is a usage-level failure.

Both use `success: false` but mean different things. An agent cannot distinguish "try again differently" from "the answer is: violations found" without command-specific logic.

### 4. `explain path` succeeds silently on missing files

```json
{
  "path": "nonexistent.md",
  "classification": "unknown",
  "pathPolicyCategory": "unclassified",
  "matchedPattern": null,
  "matchedFamily": null,
  "artifact": null,
  "applicableRules": ["STWD-002", "STWD-004", ...]
}
```

Exit code 0. No `exists` field. An agent cannot tell whether this is a planned new file, a typo, or a deleted file without probing the filesystem separately.

### 5. Batch vs single-file md query have different shapes

Single file:
```json
{
  "selector": "heading[Commands]",
  "matchCount": 1,
  "matches": [{ "kind": "section", "label": "Commands", "range": {...}, "content": "..." }]
}
```

Batch (`--pattern`):
```json
{
  "pattern": "docs/planning/*.md",
  "selector": "frontmatter.status",
  "results": [{ "file": "...", "selector": "...", "matches": [...] }]
}
```

Batch mode adds `results[]` wrapping and drops `matchCount` and `range` from individual matches. An agent must branch on mode.

### 6. Diagnostic remediation targets lack precision

A broken-link diagnostic says:
```
"message": "Broken link to '../decisions/adrs/ADR-013...' — file not found."
```

But does not include:
- The target path as a structured field
- The column of the link in the source file
- The link text
- A selector or address for the link element

The agent must regex-parse the message to extract the broken target path.

## JSON Schema / Design Issues

### 1. Anonymous object contracts are fragile

Several JSON outputs are serialized from inline anonymous objects (visible in source at `MaintainCommand.cs`, `RefactorCommand.cs`, `CheckCommand.cs`). These are not backed by explicit DTO classes, making accidental field-name changes or omissions more likely across releases.

### 2. Diff payloads are opaque strings

The `md edit` preview returns:
```json
{
  "diff": "--- a/file\r\n+++ b/file\r\n ..."
}
```

The diff is a unified-diff string with placeholder filenames (`a/file`, `b/file`), not the actual path. An agent must parse the diff text to understand what changed. No structured hunk data (added lines, removed lines, ranges) is available.

### 3. Refs output is too coarse for remediation

```json
{
  "path": "README.md",
  "outbound": ["CHANGELOG.md", "CONTRIBUTING.md", ...],
  "inbound": [".agents/skills/steward-cli/SKILL.md", "AGENTS.md", ...]
}
```

Only file paths — no link instances, source line numbers, link text, or anchor targets. An agent preparing for a move cannot preview which specific links in which files will be rewritten.

### 4. Completion rules embed human prose

```json
{
  "ruleId": "STWD-007",
  "description": "maintained artifact(s) stale -> run 'steward maintain --apply'",
  "count": 0
}
```

The `description` field embeds a recommended command in prose. There is no structured `recommendedCommand` field. Same pattern in remediation text across diagnostics.

### 5. Config validate errors are unstructured strings

```json
{ "valid": false, "errors": ["config.yaml: Missing required field 'profile'"] }
```

Each error is a single string combining file name, location, and message. An agent must parse the string to determine which file is affected.

## Error / Recovery Issues

### 1. No structured error contract exists

There is no standard error shape like:
```json
{
  "error": { "kind": "file-not-found", "code": "E001", "message": "...", "details": {...} }
}
```

Failures are either plain-text stderr (most commands) or ad-hoc JSON with `valid: false` (config validate only). An agent has no way to programmatically classify error types across commands.

### 2. Error classification is impossible from output alone

An agent cannot distinguish these failure types from the current output:
- Syntax error (invalid MdPath selector)
- Semantic error (valid selector, no match)
- File not found
- Config missing
- Unsupported operation
- Internal failure

All produce exit code 2 with plain-text stderr, except "no match" which returns success with empty results.

### 3. No retry hints in failure responses

When a command fails, the response never includes:
- Whether the error is retryable
- What argument was malformed
- What format was expected
- What command to try instead

The human-oriented text sometimes includes guidance (e.g., "Use 'steward explain' to list all rules"), but this is not available as a machine field.

## Address / Handoff Issues

### 1. No canonical resource address

There is no shared identifier format (like `steward://path#heading` or `{path, selector}`) that moves between commands. Each command uses its own addressing:

| Command | Address format |
|---------|---------------|
| `search` | `{path, line, column}` |
| `md query` | `heading[...]` or `#slug` selector |
| `md edit` | `--heading "..."` argument |
| `check` diagnostics | `{path, line}` |
| `refs` | bare file paths |
| `explain path` | bare file path |

An agent must translate between these ad-hoc formats.

### 2. Search results don't produce md query selectors

`search` returns `headingContext` as a display string, but not a valid MdPath selector. To chain `search` → `md query`, the agent must construct `heading[<headingContext>]` and hope the heading text doesn't contain brackets or special characters.

### 3. Check diagnostics don't link to explain path

A diagnostic with `ruleId: "STWD-008"` and `path: "docs/planning/..."` gives enough for an agent to *guess* the follow-up command, but the diagnostic doesn't include a `nextCommand` or `explainUrl` field. The handoff is prompt-glue.

## Determinism / Safety Issues

### 1. Preview/apply semantics vary by command

| Command | Default | Preview flag | Apply flag |
|---------|---------|-------------|------------|
| `md edit *` | Preview | (default) | `--apply` |
| `maintain` | Preview | (default) | `--apply` |
| `check --fix` | List fixes | `--fix` | `--fix --apply` |
| `refactor move` | Error (must choose) | `--preview` | `--apply` |

Four different patterns. An agent cannot apply a generic "preview then apply" strategy — it must know which command uses which pattern.

### 2. `refactor move --apply --output json` is a no-op

Confirmed in source: the apply logic (File.Move, File.WriteAllText for link rewrites) is inside the text-output branch only. In JSON mode, `--apply` is silently ignored. Exit code is 0. The plan JSON is returned as if it were a successful preview. An agent trusting this output would believe the move happened.

### 3. Blast radius is under-described

`refactor move --preview` returns:
```json
{
  "edits": [{ "file": "docs/audits/artifact-hygiene-cleanup-review-2026-04-16.md" }, { "file": "docs/planning-index.md" }]
}
```

Missing: source existence check, destination collision check, per-file link count, what the actual link rewrites are, whether the source file move itself is included. An agent cannot estimate impact.

### 4. Maintain --diff is text-only

The `--diff` flag enriches text output with unified diffs, but this data is not available in JSON mode. An agent in JSON mode gets less review information than a human in text mode.

## Documentation for Agent Usage

### What exists

- README.md documents all commands, options, exit codes, and config model comprehensively.
- AGENTS.md provides agent-specific workflow guidance.
- `.agents/skills/steward-cli/SKILL.md` is an excellent agent skill file with step-by-step workflows, guardrails, and caveats.
- The `--json-envelope standard` option is documented in help output and README.
- The `--output json` option is universal across all commands.

### What's missing

- **No JSON schema reference.** No document describes the expected JSON shape for each command's output. An agent must discover shapes by calling commands.
- **No structured-failure documentation.** No document explains what happens when a command fails in JSON mode, or how to detect and recover from different error types.
- **No handoff documentation.** No document describes how to chain commands — how to go from `check` output to `explain` to `md edit`.
- **No machine-contract changelog.** When JSON shapes change between versions, there's no way for an agent to know what changed.
- **SKILL.md documents known gaps** (JSON envelope inconsistency) but doesn't provide workarounds for agents encountering them.

## Examples of Strong Contracts

### Strong: `check --output json --json-envelope standard`

The gold standard in this CLI. Consistent envelope, stable diagnostic model, clear summary with boolean `pass`, completion rollup with per-rule counts, and a flat diagnostics array with stable identifiers. An agent can process this without any glue.

### Strong: `status --coverage --output json --json-envelope standard`

Clean inventory model with boolean presence/staleness flags, family summaries, and coverage percentage. An agent can make gap-analysis decisions from this output alone.

### Strong: `md edit fm-validate --output json`

Returns `valid`, `issues`, `requiredFields`, and `allowedValues` — everything an agent needs to determine what frontmatter to set and what values to use.

### Strong: `maintain --output json --json-envelope standard`

Per-artifact preview with `hasChanges`, `blocked`, `blockedReason`, and `fileEdits`. Good preview/apply separation with the `applied` flag.

## Examples of Weak or Ambiguous Contracts

### Weak: Error output for `md query` invalid selector

```
$ steward md query README.md "heading[" --output json
# stderr only: plain text error about malformed selector
# stdout: nothing
# exit code: 2
```

Agent gets no JSON. Must scrape stderr. Cannot distinguish selector syntax error from file-not-found.

### Weak: `explain path` on missing file

```json
{
  "path": "nonexistent.md",
  "classification": "unknown",
  "pathPolicyCategory": "unclassified",
  "artifact": null
}
```

Exit code 0, no `exists` field. Indistinguishable from a valid but ungoverned path.

### Weak: `refactor move --apply --output json`

Returns preview data with exit code 0 but performs no mutation. Agent would believe the operation succeeded.

### Weak: `version --output json --json-envelope standard`

```json
{
  "version": "0.15.0",
  "runtimeVersion": ".NET 10.0.6",
  "osPlatform": "Microsoft Windows 10.0.26200",
  "architecture": "X64"
}
```

Standard envelope silently ignored. Agent expecting `{schemaVersion, command, data}` gets a raw object.

## Top Priority Changes Needed for Reliable Agent Use

1. **Make `--output json` guarantee JSON on stdout for all outcomes.** Every command must emit parseable JSON on stdout for success, expected failure, and usage error paths. Plain-text-only stderr responses when JSON is requested are contract violations.

2. **Apply `--json-envelope standard` universally.** `version`, `outline`, `explain path`, `explain` (rule list), and `refactor move` must respect the envelope flag. No silent ignore.

3. **Separate process success from domain result in the envelope.** Define `success` as "the command executed without internal/usage error." Validation failures (`check` with violations) should return `success: true, exitCode: 1, data.summary.pass: false`. Config errors should return `success: false, exitCode: 2`.

4. **Fix `refactor move --apply` in JSON mode.** The apply code path is unreachable in JSON output mode. This is a data-loss-risk bug — an agent would proceed as if the move happened.

5. **Add a structured error contract.** Define a standard error shape: `{ "error": { "kind": "...", "message": "...", "details": {...} } }` for file-not-found, selector-syntax-error, invalid-regex, unknown-rule, missing-config, and internal-error. Emit it on stdout when `--output json` is active.

6. **Add `exists` field to `explain path`.** A single boolean that tells the agent whether the file is on disk, eliminating the need for separate filesystem probing.

7. **Add structured remediation target fields to diagnostics.** For STWD-008 (broken link), include `targetPath` as a separate field. For STWD-003 (missing frontmatter), include `missingFields`. Don't force agents to regex-parse `message` strings.

## Nice-to-Have Improvements

- **Publish JSON schema definitions** for each command's output, or at minimum a machine-facing contract reference document.
- **Add `recommendedCommand` or `nextStep` field** to diagnostics and error responses so agents can chain commands without guessing.
- **Normalize preview/apply patterns** — either default-to-preview everywhere, or adopt a consistent flag convention.
- **Add structured diff objects** to `md edit` and `maintain` JSON outputs instead of/alongside unified-diff strings.
- **Enrich `refs` output** with link instances (source line, link text, anchor) instead of bare file paths.
- **Add `matchCount` and `range` to batch `md query`** results so single and batch modes share one shape.
- **Add `fixable` and `fixCommand` fields** to check diagnostics for rules that support auto-fix.
- **Add canonical MdPath selector** to `search` results when the match is under a Markdown heading.

## Contract Changes to Implement

### CC-01: Universal JSON error responses

**Problem:** 7+ commands emit plain-text stderr errors when `--output json` is active.
**Change:** Wrap all error paths through a shared `WriteJsonError(kind, message, details)` method that emits a structured error object on stdout. Preserve stderr for human-readable context, but stdout must always be valid JSON.
**Acceptance:** `md query nonexistent.md "heading[X]" --output json` returns `{"error": {"kind": "file-not-found", ...}}` on stdout.

### CC-02: Universal standard envelope

**Problem:** `version`, `outline`, `explain path`, `explain` (list), and `refactor move` ignore `--json-envelope standard`.
**Change:** Route all JSON-producing code paths through `JsonEnvelopeWriter.Write()` when `--json-envelope standard` is active.
**Acceptance:** Every command with `--output json --json-envelope standard` emits `{schemaVersion, command, toolVersion, success, exitCode, data}`.

### CC-03: Process vs domain success separation

**Problem:** `success: false` means both "command errored" and "validation found issues."
**Change:** Define envelope `success` as process-level success. `check` with violations: `success: true, exitCode: 1`. `config validate` with bad YAML: `success: false, exitCode: 2`.
**Acceptance:** `success: false` only appears for exit codes 2 (usage) and 3 (internal).

### CC-04: Fix refactor move apply in JSON mode

**Problem:** Apply logic is inside the text-output branch only; JSON mode returns preview data and exit code 0 without mutating.
**Change:** Move the apply logic (File.Move, link-rewrite WriteAllText) outside the output-format branch so it executes regardless of `--output json`.
**Acceptance:** `refactor move --apply --output json` physically moves the file and returns `{applied: true, movedFile: ..., rewrittenFiles: [...]}`.

### CC-05: Add `exists` to explain path

**Problem:** `explain path nonexistent.md` returns exit 0 with `classification: "unknown"` — indistinguishable from a valid ungoverned path.
**Change:** Add `"exists": true|false` to the explain path JSON response.
**Acceptance:** `explain path nonexistent.md --output json` includes `"exists": false`.

### CC-06: Structured remediation target in diagnostics

**Problem:** Broken-link target is embedded in the `message` string. Missing-frontmatter fields are not listed.
**Change:** Add typed fields to diagnostic objects: `targetPath` for STWD-008, `missingFields` for STWD-003, `expectedPattern` for STWD-010/STWD-016.
**Acceptance:** `check --output json` diagnostics for STWD-008 include `"targetPath": "relative/path/to/missing-target.md"` as a separate field.

### CC-07: Normalize batch/single md query shape

**Problem:** Single-file and `--pattern` batch modes return different JSON structures.
**Change:** Wrap single-file results in the same `results[]` array used by batch mode. Keep `matchCount` and `range` in both.
**Acceptance:** Single and batch modes both return `{selector, results: [{file, matchCount, matches: [{kind, label, range, content}]}]}`.

### CC-08: Structured config validate errors

**Problem:** `errors` is `string[]` combining filename, location, and message in one string.
**Change:** Replace with `errors: [{file, message, line?, column?}]`.
**Acceptance:** Invalid config returns `{valid: false, errors: [{file: "policy.yaml", message: "Unknown field 'foo'"}]}`.

### CC-09: Contract test expansion

**Problem:** Existing contract tests cover legacy JSON and happy paths. Standard envelope, error JSON, and mutation postconditions are untested.
**Change:** Add snapshot/integration tests for: (a) standard envelope on every JSON command, (b) JSON error responses for each failure type, (c) `refactor move --apply --output json` postconditions.
**Acceptance:** The regressions identified in this review (CC-01, CC-02, CC-04) are caught by automated tests.

### CC-10: Enrich refactor move preview with safety data

**Problem:** Preview shows only `{file}` per edit — no source/dest existence, collision check, link count, or actual rewrites.
**Change:** Add `sourceExists`, `destinationExists`, per-file `linkCount`, and `rewrites: [{line, oldLink, newLink}]` to the preview JSON.
**Acceptance:** Agent can compute blast radius from JSON alone without filesystem probing.

## Final Verdict

| Dimension | Rating | Notes |
|-----------|--------|-------|
| Parseability | **Good** | JSON shapes are valid, camelCase, indented. Fails on error paths. |
| Consistency | **Fair** | Envelope and error handling vary by command. |
| Explicitness | **Good** for reads, **Poor** for errors | Read commands are explicit. Failures are implicit/prose. |
| Recoverability | **Poor** | No structured error kinds, no retry hints, no failure JSON. |
| Determinism | **Good** for reads, **Broken** for `refactor move` | Read commands are deterministic. One mutation is a no-op in JSON mode. |
| Composability | **Fair** | Commands produce useful data but handoff requires ad-hoc translation. |
| Safety | **Good** (preview/apply pattern exists) | Preview-by-default is correct. `refactor move` JSON bug is unsafe. |

**Overall: Partially agent-usable.**

The read-oriented governance workflow (orient → status → check → explain) is genuinely usable by an autonomous agent today. The mutation workflow (md edit, maintain, refactor move) has a sound design but one critical bug and several missing machine affordances.

The gap between "partially usable" and "credibly agent-ready" is primarily CC-01 through CC-05 — five concrete changes that would eliminate the need for stderr scraping, shape branching, and postcondition verification.

## Appendix: Principles for Future CLI/JSON Contract Evolution

1. **JSON mode is a machine contract, not a formatting preference.** When `--output json` is active, every outcome — success, expected failure, usage error — must produce valid JSON on stdout. Plain text on stdout in JSON mode is a contract violation.

2. **Envelope flags must be total or absent.** If `--json-envelope standard` is accepted, it must be applied. Silently ignoring it is worse than rejecting it.

3. **Process success ≠ domain success.** The envelope `success` field must mean "the command ran to completion." Domain results (pass/fail, valid/invalid, changes/no-changes) belong in payload fields. Conflating them forces command-specific decoding.

4. **Machine-relevant data must exist in typed fields.** If an agent needs a file path, selector, rule ID, or target reference, it must appear as a named field — not embedded in a `message` or `description` string.

5. **One canonical address model.** Results from any command should carry enough identity (path + optional selector/range) that a follow-up command can consume it without ad-hoc construction.

6. **Preview and apply share one schema.** The only difference should be state flags (`preview: true`, `applied: false`). Different shapes for preview vs apply force branching.

7. **Mutations must report postconditions.** Every `--apply` response must include enough evidence (files changed, bytes written, operations performed) for the caller to verify success without re-probing.

8. **Contract tests gate contract changes.** Every JSON-producing command must have snapshot tests covering: (a) standard envelope shape, (b) at least one failure mode, (c) mutation postconditions where applicable. Breaking a snapshot is a signal to version the schema.
