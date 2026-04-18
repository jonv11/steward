---
type: audit
status: Active
last_updated: 2026-04-18
---

# AI-Agent Contract Review - 2026-04-18

## Executive Summary

### Verdict

**Final verdict: partially agent-usable**

Steward already has several strong machine-facing surfaces. `check`, `status`, `orient`, `search`, `refs`, and parts of `md` are genuinely useful to an autonomous agent because they return deterministic JSON, stable rule IDs, and repo-relative paths.

However, the CLI contract is not yet strong enough to call credibly agent-ready. The main blockers are not "JSON exists" problems. They are contract-coherence problems:

1. `--output json` does not guarantee a JSON response on failure.
2. `--json-envelope standard` is not applied consistently across commands.
3. command-success semantics are ambiguous because `success` is used for domain failure as well as transport failure.
4. command handoff still depends on path-and-prose glue instead of canonical machine addresses.
5. one mutation surface, `refactor move --apply --output json`, reports success without performing the move.

An autonomous agent can use Steward today, but only with command-specific prompt glue, stderr scraping, repo-specific assumptions, and extra postcondition checks around mutations.

## Commands And Flows Reviewed

### Current repo flows

- `orient --output json`
- `orient --signals --output json --json-envelope standard`
- `status --coverage --output json --json-envelope standard`
- `check --output json --json-envelope standard`
- `search "json envelope" --mode all --max 5 --output json --json-envelope standard`
- `refs README.md --output json --json-envelope standard`
- `explain path docs/planning/milestone-plan.md --output json`

### Disposable flow A: frontmatter remediation

- Repo: temporary governed repo with `.steward/` config and `docs/guide.md`
- `check --output json --json-envelope standard`
- `explain path docs/guide.md --output json --json-envelope standard`
- `md edit fm-set docs/guide.md --key status --value Draft --output json --json-envelope standard --apply`
- `check --output json --json-envelope standard`

Observed outcome:
- `check` found the missing `status` field as structured STWD-003 diagnostics.
- `explain path` exposed effective frontmatter requirements.
- `md edit fm-set` applied cleanly.
- Re-check still failed because `README.md` also lacked frontmatter, which was not a direct machine-handoff target from the first fix.

### Disposable flow B: no-config repo and move/remediation loop

- Repo: temporary repo with `.git/` but no `.steward/`
- `check --output json --json-envelope standard`
- `status --output json --json-envelope standard`
- `refs docs/guide.md --output json --json-envelope standard`
- `refactor move docs/guide.md docs/guide-renamed.md --preview --output json --json-envelope standard`
- `refactor move docs/guide.md docs/guide-renamed.md --apply --output json --json-envelope standard`

Observed outcome:
- `check` passed in an unconfigured repo by falling back to the implicit `minimal` profile.
- `status` failed on the same repo and emitted only plain stderr text.
- `refs` identified only inbound/outbound file paths, not concrete link instances.
- `refactor move --apply --output json` returned a plan object and exit code `0`, but did not move the file or rewrite links.

### Disposable flow C: invalid configuration recovery

- Repo: temporary repo with malformed `.steward/policy.yaml`
- `config validate --output json --json-envelope standard`

Observed outcome:
- This is one of the better failure contracts in the product: it returned structured JSON with `valid: false`, `success: false`, `exitCode: 2`, and an `errors` array.

### Failure probes

- `md query docs/guide.md "heading[" --output json --json-envelope standard`
- `md query docs/guide.md "heading[Missing]" --output json --json-envelope standard`
- `search "(" --regex --output json --json-envelope standard`
- `explain BOGUS --output json --json-envelope standard`
- `explain path docs/missing.md --output json --json-envelope standard`

Observed outcome:
- selector syntax errors and invalid regex errors were plain stderr strings, not JSON
- empty selector matches were valid structured JSON
- unknown rule IDs were plain stderr strings, not JSON
- `explain path` on a missing file returned success with no `exists` indicator

## Contract Strengths

- `check` is the strongest current machine contract. Its JSON payload is explicit and mostly well-shaped: `summary`, `completion`, and `diagnostics` are stable and easy to parse.
- Rule IDs, severities, and categories are consistent enough for automation. `STWD-003` and peers are usable machine identifiers.
- `status --coverage --output json` is a good machine summary surface. It separates required artifacts, recommended artifacts, state documents, maintenance artifacts, family summaries, and governance coverage.
- `orient --output json` provides a deterministic repo map with role/classification data and `startHere` guidance. This is genuinely useful as an agent session-start primitive.
- `config validate --output json` is the best current example of a structured failure response.
- JSON mode keeps machine output on stdout and human chatter on stderr via `JsonOutputFormatter`. That separation is correct in principle.

## Contract Weaknesses

- JSON is present but not universal. An agent cannot rely on `--output json` alone to mean "stdout will contain parseable JSON for both success and failure."
- Envelope support is incomplete. Observed directly: `explain path --output json --json-envelope standard` still returns a raw object. Source inspection shows the same problem for `refactor move`, `outline`, and `version`.
- Repository preconditions are inconsistent. `check` works without `.steward/`; `status` does not. An agent cannot infer one repo-state model for the command family.
- Several payloads are still "anonymous object" contracts rather than explicitly modeled public DTOs. That makes accidental drift more likely.
- Contract tests exist, but they mostly check legacy happy-path shape and string presence. They do not yet lock down standard-envelope behavior, structured failures, or mutation postconditions.

## JSON Schema / Design Issues

### 1. Envelope semantics are inconsistent

Observed:

```json
{
  "schemaVersion": "steward-json/v1",
  "command": "check",
  "success": false,
  "exitCode": 1,
  "data": {
    "summary": { "pass": false }
  }
}
```

This makes `success` mean "validation passed" for `check`, not "the command executed successfully." That conflicts with RFC-010's intended split between process success and validation pass. The same ambiguity appears in `config validate` and `config doctor`.

### 2. Envelope coverage is incomplete

Observed:

```json
{
  "path": "docs/guide.md",
  "classification": "guide",
  "requiredFrontmatterFields": ["title", "status"]
}
```

That was returned by `explain path ... --output json --json-envelope standard`. No `schemaVersion`, `command`, `toolVersion`, `success`, or `exitCode` appeared.

Confirmed in source:
- `src/Steward.Cli/Commands/ExplainCommand.cs` writes raw JSON for the `path` subcommand.
- `src/Steward.Cli/Commands/RefactorCommand.cs` writes raw JSON for `refactor move`.
- `src/Steward.Cli/Commands/OutlineCommand.cs` and `VersionCommand.cs` also bypass the envelope helper.

### 3. Single-mode and batch-mode schemas diverge

Observed:

- single-file `md query` returns `selector`, `matchCount`, and `matches[].range`
- batch `md query --pattern` returns `pattern`, `selector`, and `results[]`, but drops `matchCount` and `range`

This forces callers to branch on mode instead of consuming one normalized result shape such as `targets[]`.

### 4. Several fields mix machine state with human prose

- `check.completion.rules[].description` contains strings like `maintained artifact(s) stale -> run 'steward maintain --apply'`
- `config validate.errors[]` is a list of free-form strings that embed file identity and parser detail

These are human-helpful, but the machine-relevant parts are not split into typed fields such as `recommendedCommand`, `file`, `line`, `column`, or `errorKind`.

### 5. Location data is under-specified

The core diagnostic model carries:

- `path`
- optional `line`

It does not carry:

- `column`
- `endLine`
- `endColumn`
- selector/range identity
- typed resource address
- remediation target ID

That is enough for simple file-level checks, but not enough for durable machine handoff on heading-, block-, or link-level remediations.

### 6. Diff payloads are not structured enough for agents

Observed from `md edit fm-set ... --output json --apply`:

```json
{
  "hasChanges": true,
  "message": "Set frontmatter field 'status' = 'Draft'.",
  "diff": "--- a/file\r\n+++ b/file\r\n ...",
  "applied": true
}
```

Problems:
- the diff is a single string, not structured hunks
- the filenames are placeholder `a/file` and `b/file`, not the actual path
- there is no changed-range metadata

## Error / Recovery Issues

### 1. Failure output is not uniformly structured

Observed failure responses under `--output json`:

- `status` in a repo without `.steward/`: plain stderr only
- `md query` invalid selector: plain stderr only
- `search --regex` invalid pattern: plain stderr only
- `explain BOGUS`: plain stderr only

Observed structured failure response:

- `config validate` invalid YAML: JSON envelope with `valid: false`

This inconsistency means an agent must branch on command family and, on some paths, scrape stderr prose to recover.

### 2. Error kinds are not classified cleanly

The current machine-facing failures do not distinguish:

- syntax error
- semantic validation error
- missing file
- missing config
- unsupported operation
- internal failure

with an explicit `error.kind` or `error.code`. Exit codes help at a coarse level, but they are not enough for deterministic recovery strategies.

### 3. `explain path` hides path existence state

Observed:

```json
{
  "path": "docs/missing.md",
  "classification": "unknown",
  "artifact": null,
  "applicableRules": ["STWD-002", "STWD-003", "STWD-004", "STWD-008", "STWD-009", "STWD-013"]
}
```

The command succeeded with exit code `0`, but it did not say whether the file exists. An agent cannot tell whether this is:

- a planned target path
- a typo
- a deleted file
- a path outside governed discovery

without extra file-system probing.

### 4. Retry strategy often depends on prose

`config validate` gives a repo-usable next step. Most other failures only expose a message string. They do not expose structured retry hints such as:

- `retryable: false`
- `recommendedNextCommand`
- `requiredArgument`
- `expectedFormat`

## Address / Handoff Issues

- `search` returns `path`, `line`, `column`, `snippet`, `kind`, and `headingContext`, but no canonical `address` or canonical selector. To hand off to `md query`, an agent must invent `heading[...]` from the result text.
- `refs` returns only path arrays. It does not return link instances, locations, link text, or source selectors. That is too weak for precise remediation or safe bulk rewrite review.
- `check` diagnostics identify the violated file, but not a typed remediation target. For example, missing frontmatter is file-scoped; broken links and section-size failures are not.
- `explain path` is useful as a supplement, but it is not directly linked from diagnostics. The agent must choose to call it and translate the diagnostic `path` into a new command argument manually.
- RFC-009 correctly identifies the missing piece: there is still no canonical typed resource address that can move from `search` to `md query`, from `check` to `explain path`, or from `refs` to `refactor move`.

## Determinism / Safety Issues

### 1. Preview/apply semantics vary by command

- `md edit`: preview by default, `--apply` to commit
- `maintain`: preview by default, `--apply` to commit
- `check --fix`: preview when `--fix`, commit when `--fix --apply`
- `refactor move`: requires either `--preview` or `--apply`

These are individually understandable but not normalized enough for generic agent tooling.

### 2. `refactor move --apply --output json` is currently unsafe

Observed:

- preview JSON and apply JSON are identical
- exit code is `0`
- the file was not moved
- links were not rewritten

Confirmed in source:
- `src/Steward.Cli/Commands/RefactorCommand.cs` performs apply side effects only inside the text-output branch

This is a contract bug, not just an ergonomics gap.

### 3. Blast radius is under-described in JSON

Observed `refactor move --preview --output json`:

```json
{
  "oldPath": "docs/guide.md",
  "newPath": "docs/guide-renamed.md",
  "edits": [
    { "file": "README.md" }
  ]
}
```

Missing safety data:

- whether the source exists
- whether the destination already exists
- whether the source file itself will be moved
- how many links inside each file will change
- what those link rewrites are
- whether the operation is dry-run or applied

### 4. Some review-only affordances are text-only

Confirmed in source:

- `maintain --diff` only enriches text mode
- `check --fix` computes fixes but does not add a structured fix plan to JSON output

That weakens safe autonomous use because the highest-value review data is not consistently available in machine form.

## Documentation And Test Coverage Issues

- README and implementation/planning docs describe JSON automation support, but they do not publish one machine-facing contract document or JSON schema set.
- `docs/implementation-status.md` and `docs/planning/implementation-instructions.md` describe standard-envelope support broadly. Observed behavior does not yet match that claim for every JSON-producing command.
- Agent-usage docs are mostly happy-path. They do not document structured failure behavior, retry patterns, selector/address handoff, or mutation postcondition checks.
- Stable-surface tests do not cover the standard envelope in depth. Snapshot coverage currently locks root help and legacy `check --output json`, not the new cross-command machine contract.
- There are no CLI tests covering `refactor move`, including the apply/no-op regression observed in this audit.

## Examples Of Strong Contracts

### Strong example 1: `check`

- Good fields: `ruleId`, `severity`, `category`, `path`, `line`, `message`, `remediation`, `source`
- Good machine outcome field: `data.summary.pass`
- Good stable identifiers: `STWD-001` through `STWD-016`

### Strong example 2: `status --coverage`

- Good split between artifact inventory and governance coverage
- Good repo-relative paths
- Good summary counts for fast agent decisions

### Strong example 3: `config validate`

- Returns structured invalid-config data instead of forcing stderr scraping
- Gives a clear machine boolean: `valid`
- Preserves a distinct usage/config exit code

## Examples Of Weak Or Ambiguous Contracts

### Weak example 1: `explain path`

- ignores the standard envelope
- succeeds on missing paths without exposing `exists`
- gives path-level governance info but no provenance or address token

### Weak example 2: `status` in an unconfigured repo

- emits plain stderr only under `--output json`
- cannot be recovered programmatically without prose parsing
- contradicts `check` on the same repo, which succeeds

### Weak example 3: `refactor move --apply --output json`

- reports success but does not mutate
- gives no `applied` field
- gives no postcondition evidence

### Weak example 4: `md query` errors

- empty match is structured JSON
- syntax error is plain stderr

That means the caller must treat "no result" and "invalid selector" as two entirely different transport styles.

## Top Priority Changes Needed For Reliable Agent Use

- Make `--output json` guarantee JSON on stdout for both success and expected failure paths.
- Make `--json-envelope standard` truly universal across all JSON-producing commands.
- Redefine envelope `success` as process success only; keep domain result inside command-specific fields such as `pass`, `valid`, or `applied`.
- Add a structured error object with explicit kinds and recovery hints.
- Introduce canonical typed resource addresses and carry them across `search`, `refs`, `check`, `md query`, and `explain path`.
- Fix `refactor move --apply --output json` so JSON mode mutates correctly and reports postconditions.
- Normalize preview/apply schemas so agents can reason generically about dry-run vs commit behavior.
- Expand contract tests to include envelope mode, failure mode, and mutation postcondition mode.

## Nice-To-Have Improvements

- Publish machine-facing JSON schema documentation for the main commands.
- Add `exists`, `pathKind`, and provenance fields to `explain path`.
- Add structured link-instance objects to `refs`.
- Add canonical selector fields to `search` results when the match is in Markdown structure.
- Add structured diff hunks instead of diff strings for `md edit`, `maintain`, and `refactor move`.
- Add explicit `fixable`, `fixCommand`, or `recommendedNextCommand` metadata to diagnostics.

## Contract Changes To Implement

1. **Standardize the JSON envelope on every JSON-capable command.** Route `explain path`, `refactor move`, `outline`, `version`, and every other JSON surface through `JsonEnvelopeWriter`. Acceptance: `--output json --json-envelope standard` always emits `{ schemaVersion, command, toolVersion, success, exitCode, data }` on stdout.
2. **Separate process success from domain result.** Make envelope `success` mean "the command executed without transport/runtime failure." Keep domain booleans in payload fields such as `summary.pass`, `valid`, `hasChanges`, or `applied`. Acceptance: `check` with policy violations returns `success: true`, `exitCode: 1`, `data.summary.pass: false`.
3. **Add a structured JSON error contract.** Introduce `error.kind`, `error.code`, `message`, and `details` fields for usage/config/selector/regex/file-not-found/internal failures. Acceptance: `status` without `.steward/`, `md query` selector syntax errors, `search --regex` parse failures, and unknown rule IDs all return JSON error envelopes under `--output json`.
4. **Normalize repository preconditions across command families.** Decide explicitly whether commands require `.steward/` or can run with an implicit minimal policy, then enforce that consistently and document it. Acceptance: `check`, `status`, `orient`, `search`, and `refs` no longer disagree silently about repo initialization state.
5. **Introduce canonical resource addresses and use them as the handoff token.** Implement RFC-009 follow-on work so results can carry reusable addresses like `steward://...`. Acceptance: `search` emits `address`; `md query` accepts `--address`; `refs` and `check` diagnostics can point to the same address model.
6. **Upgrade location precision for diagnostics and references.** Extend machine location data to include `column`, `endLine`, `endColumn`, selector/range identity, and typed target metadata where relevant. Acceptance: broken-link and structural diagnostics can identify one precise remediation target without prose parsing.
7. **Fix `refactor move` JSON behavior and make mutation results reviewable.** Apply side effects in JSON mode, add `preview`/`applied` flags, and return structured move effects. Acceptance: `refactor move --apply --output json` physically moves the file, rewrites references, and reports moved file path, changed files, per-file rewrite counts, and destination collision state.
8. **Normalize `md query` into one schema across single-file and batch modes.** Replace the current split contract with a common `targets[]` or equivalent. Acceptance: single-file and batch query results share the same match object shape and both expose ranges and counts.
9. **Make config-validation findings machine-typed instead of string-packed.** Replace `errors: [string]` with structured objects carrying `file`, `line`, `column`, `phase`, and `message`. Acceptance: malformed YAML and semantic config failures can be triaged without string parsing.
10. **Expose review data in machine form for previewable commands.** Add structured diff/change objects to `md edit`, `maintain`, and `check --fix` JSON payloads. Acceptance: an agent can estimate blast radius without parsing unified-diff strings or switching to text mode.
11. **Add explicit existence/provenance fields to `explain path`.** Include `exists`, `discovered`, matched policy sources, and whether the path is explicit artifact vs inferred family match. Acceptance: calling `explain path` on a missing file or planned target is machine-distinguishable.
12. **Broaden contract tests to lock the actual agent surface.** Add snapshot and integration tests for standard envelope mode, JSON failure mode, and mutation postconditions, including `refactor move`. Acceptance: the current regressions observed in this audit are caught automatically.

## Final Verdict

Steward is **partially agent-usable**, not human-first only. The structured read surfaces are already useful, and `check` in particular is a real machine-facing asset.

It is **not yet credibly agent-ready** because a strong LLM still has to hesitate in too many places:

- "Will this failure still be JSON?"
- "Does `success: false` mean the command failed or the repo failed validation?"
- "What exact location token do I pass to the next command?"
- "Did `--apply` actually mutate, or do I need to verify side effects separately?"

Those are product-contract defects, not prompt-engineering problems.

## Appendix: Principles For Future CLI / JSON Contract Evolution

1. JSON mode should guarantee parseable JSON on stdout for every expected outcome, not just happy paths.
2. One envelope should mean one envelope. Options that imply a standard contract must not be silently ignored.
3. Process outcome and domain outcome must always be separate fields.
4. Human prose should never be the only carrier of machine-relevant facts.
5. One canonical address model should move across discovery, diagnostics, inspection, and remediation.
6. Preview and apply should share the same payload schema, with state flags rather than unrelated shapes.
7. Every mutation result should include enough postcondition evidence to avoid blind trust.
8. Every new or changed machine-facing surface should ship with contract tests for success, failure, and at least one end-to-end handoff.
