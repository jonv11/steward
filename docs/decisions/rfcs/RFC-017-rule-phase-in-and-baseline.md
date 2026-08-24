---
type: rfc
status: Accepted
description: Defines a generated baseline mechanism that lets a rule be enabled against a repository's existing content without an immediate wall of errors, while still catching new violations
resolves: >-
  Enabling a rule today applies it to all pre-existing content at once, which the 2026-08-24 maintainer
  configuration experience audit found both trial repositories hit as a hard adoption blocker
last_updated: 2026-08-25
---

# RFC-017: Rule Phase-In and Baseline

---

## 1. Context

Steward validates the full target scope on every `steward check` run. Rule severity is controlled per-rule (`severity_overrides`), per-path (`path_overrides`), or globally (`disabled_rules`), but none of these mechanisms distinguish *when* a violation was introduced. Turning a rule on evaluates it against every file in scope, past and present, in the same pass.

`steward check --since <ref>` already narrows the *file scope* to what changed relative to a merge-base, but that is a scope flag for a single invocation, not a persistent statement that "this content was accepted as of date X." It does not help a repository that wants to enable a rule permanently while grandfathering what already exists.

The 2026-08-24 maintainer configuration experience audit ran Steward against two external repositories (`jvcode`, `mdrule`) with real pre-existing content. Both hit the same wall: enabling a rule meaningful to their governance goals produced an immediate flood of errors across every existing file, with no way to say "enforce this from here forward." The [backlog](../../project/backlog.md) recorded this as the highest-impact adoption gap, and it is now the committed [current milestone](../../project/roadmap.md).

### What existing mechanisms do not cover

- `disabled_rules` / `severity_overrides` are global — they mute a rule everywhere, not just for pre-existing content.
- `path_overrides` is glob-scoped — it can exempt a directory, but not "the files that existed before adoption" as a set, and it has no expiry or shrink signal (RFC-013 proposes lifecycle metadata for this, but that RFC is deferred and addresses manual, rationale-bearing suppressions, not bulk-generated grandfathering).
- `check --since <ref>` scopes which files are *checked* in one run; it does not change what a full, unscoped `check` reports, and CI systems that periodically run a full check would still see the pre-existing violations.

---

## 2. Problem Statement

A repository cannot adopt a new rule against content it already has without one of:

1. Fixing every existing violation before enabling the rule (often infeasible at adoption time — the whole point of adopting a governance tool is usually that the content is not yet clean), or
2. Accepting a red `check` indefinitely until someone gets around to it (defeats the purpose of `check` as a CI gate), or
3. Disabling the rule entirely (loses the rule for new content too).

None of these let a repository say "stop the bleeding now, clean up the backlog later" — the standard adoption path for lint-style tooling.

---

## 3. Proposed Capability

### 3.1 A generated baseline file

A new command, `steward baseline generate`, runs `check` against the current repository state and writes every resulting diagnostic to `.steward/baseline.json` as an accepted-debt entry, keyed by rule ID and a content-stable identity for the violation (file path plus, where the rule has one, a position-independent selector such as a heading anchor or frontmatter field name — not a line number, so unrelated edits elsewhere in the file don't invalidate the entry).

```yaml
# .steward/policy.yaml
baseline:
  enabled: true
  path: .steward/baseline.json
```

```json
// .steward/baseline.json (generated, not hand-authored)
{
  "generated_at": "2026-08-25",
  "entries": [
    { "rule": "STWD-016", "path": "docs/legacy/old-notes.md", "selector": "file" },
    { "rule": "STWD-014", "path": "docs/legacy/old-notes.md", "selector": "section:Consequences" }
  ]
}
```

### 3.2 `check` behavior with a baseline present

- A diagnostic that matches a baseline entry (same rule, path, selector) is suppressed from the pass/fail result and reported separately as **baseline debt**, not as a failure.
- A diagnostic that does **not** match any baseline entry — new content, or an existing file edited such that its selector changed — is reported and scored at full configured severity, same as today.
- A baseline entry that no longer matches any current diagnostic (the violation was fixed) is reported as `baseline-drift`: an info-level note that the baseline is stale and should be regenerated. This is the shrink signal — it makes progress visible without requiring it.
- `steward check --fail-on-baseline-growth` (opt-in) fails if the baseline file itself was edited to add entries since the last commit, to discourage using the baseline as an escape hatch for new debt instead of a one-time grandfather.

### 3.3 Visibility

`steward baseline status` reports entry counts by rule and by age, so baseline debt is visible the same way `status --coverage` makes governance gaps visible today, rather than living only inside a JSON file no one revisits.

---

## 4. Rule and Config Changes Summary

| Component | Change |
|---|---|
| `steward baseline generate` *(new command)* | Snapshots current diagnostics into `.steward/baseline.json` |
| `steward baseline status` *(new command)* | Reports baseline entry counts and staleness |
| `check` | Suppresses baseline-matched diagnostics from pass/fail; reports `baseline-drift` for stale entries; new `--fail-on-baseline-growth` flag |
| `config validate` | Validates `baseline.path` points to a readable file when `baseline.enabled: true`; validates entry schema |
| `config doctor` | Flags baseline entries whose rule no longer exists (dead baseline entries), parallel to existing dead-config detection |

No existing rule's `DefaultSeverity` or evaluation logic changes. Baseline is a post-processing filter over the diagnostic list `check` already produces, not a new rule.

---

## 5. Backward Compatibility

Strictly opt-in:

- No `.steward/baseline.json` and no `baseline:` config block → behavior is identical to today.
- A repository that never runs `baseline generate` is unaffected by this RFC.
- Removing `.steward/baseline.json` reverts a repository to full enforcement immediately — no migration needed in either direction.

---

## 6. Relationship to Existing Mechanisms

| Mechanism | Scope | Lifecycle | Intent |
|---|---|---|---|
| `disabled_rules` / `severity_overrides` | Whole rule, repo-wide | Manual, indefinite | "This rule doesn't apply here" |
| `path_overrides` | Rule × glob | Manual, indefinite | "This rule doesn't apply to this area" |
| RFC-013 structured suppressions (deferred) | Rule × path, with metadata | Manual, reason-bearing, optionally expiring | "This specific exception is tracked and owned" |
| **Baseline (this RFC)** | Individual violation instance | Generated, expected to shrink | "This existing content is grandfathered; new content is not" |

Baseline is deliberately the coarsest and least deliberate of these — it is meant to be generated once at adoption time with no per-entry authorship, in exchange for being bulk-applicable to an entire pre-existing repository in one command. RFC-013's structured suppressions remain the right tool for a specific, rationale-bearing, long-lived exception; baseline is for "everything else that predates adoption."

---

## 7. Alternatives Considered

1. **Severity split — "warn on existing, error on new."** Downgrade violations on pre-existing content to Warning instead of suppressing them, using the same since-adoption-date logic. Rejected as the primary mechanism: it never reaches zero noise (a repository with thousands of legacy violations has thousands of permanent warnings), doesn't compose with `--fail-on <severity>` cleanly (backlog's other candidate item), and gives no shrink signal. It could still be added later as a display mode for baseline entries (report them as warnings instead of fully suppressing) — noted as a follow-on, not excluded by this design.

2. **Date-cutoff via git blame.** Only enforce a rule against files whose last-modified date is after the rule's enable date. Rejected: fragile (a one-line unrelated fix to an old file would suddenly pull the whole file into full enforcement), and git history isn't always available or trustworthy (shallow clones, squashed history).

3. **Extend `path_overrides` to accept a file list instead of a glob.** Considered as a lighter-weight version of baseline. Rejected as insufficient: it can grandfather a whole file but not "this rule passes on this file except for the three headings that already violate it," which is the common real case — a legacy file with mostly-clean content and a few known violations.

4. **Do nothing; rely on `--since` scope.** Rejected: `--since` is a per-invocation scope flag, not a persistent adoption boundary. A full `check` (as CI typically runs) still fails.

---

## 8. Out of Scope

| Item | Notes |
|---|---|
| Automatic baseline shrink suggestions | Steward reports drift; it does not propose fixes |
| Ticket-system integration for baseline entries | Left to RFC-013 if/when that work resumes |
| Expiry dates on baseline entries | Baseline is a snapshot, not a suppression with lifecycle metadata; RFC-013 already owns that concept if the two are ever merged |
| Per-entry rationale/ownership | Baseline entries are bulk-generated, not authored; RFC-013 covers rationale-bearing exceptions |

---

## 9. Open Design Risk

Selector stability is the main implementation risk: the identity used to match a baseline entry to a current diagnostic must survive unrelated edits to the file (or the baseline becomes noisy) but must also actually change when the violated content changes (or fixed violations silently stay suppressed forever). This RFC proposes rule-specific selectors (heading anchor, frontmatter field name, file-level for whole-file rules) rather than line numbers, but the exact selector for each of the 21 rules needs to be worked out during implementation, not decided here.
