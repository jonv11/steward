---
name: steward-cli-agent
description: Run an automated Steward validation loop as an AI agent — orient, inspect rules before editing, validate with JSON/SARIF output, parse structured diagnostics, auto-fix or remediate, re-validate. Use when driving steward check/orient/status programmatically, wiring steward into PR/CI automation, doing structural Markdown edits (query/edit/split) instead of regex text manipulation, or explaining to another agent how to consume Steward's machine-readable output. Requires steward installed and a repo already governed by a maintainer — see steward-cli and steward-cli-maintainer.
---

# Steward CLI — Agent

Drive Steward as part of an automated edit-validate-remediate loop, using its structured diagnostics and JSON/SARIF output instead of parsing free text.

## Why this differs from the contributor workflow

steward-cli-contributor covers a human's pre-commit check. This skill covers programmatic use: JSON parsing over text-scanning, `--since` for PR-diff-scoped CI gating, SARIF for code-scanning integrations, and Markdown structural editing (`md query`/`md edit`/`md split`) in place of fragile find-and-replace on `.md` files.

## The validation loop

```
1. Orient   → understand repo structure and governance
2. Inspect  → check what rules apply to files about to change
3. Change   → make edits
4. Validate → steward check
5. Diagnose → parse the diagnostics array
6. Remediate → fix, or auto-fix where supported
7. Re-validate → confirm clean
```

### 1. Orient

```bash
steward orient --output json
steward status --coverage --output json
```

Returns a classified map: start-here entry points, artifact roles, governance state.

### 2. Inspect before editing

```bash
steward explain path docs/my-doc.md --output json
```

Shows what rules apply, required frontmatter, and enforced naming — before you write anything, not after.

### 3–4. Change, then validate

```bash
steward check --scope changed --output json
steward check --since origin/main --output json     # PR-diff-scoped, merge-base-aware
```

### 5. Diagnose

JSON output carries structured diagnostics, not just formatted text:

```json
{
  "diagnostics": [
    {
      "ruleId": "STWD-003",
      "severity": "error",
      "filePath": "docs/my-doc.md",
      "message": "Required frontmatter field 'status' is missing.",
      "remediation": "Add 'status' to the YAML frontmatter block at the top of the file."
    }
  ]
}
```

```bash
steward explain STWD-003 --output json
```

**Exit code 0 does not mean zero diagnostics** — only that none are `error` severity. If the loop must also act on warnings, parse the `diagnostics` array; don't branch on exit code alone.

| Code | Meaning | Loop action |
|---|---|---|
| 0 | Clean (may still carry warning/info) | Proceed |
| 1 | ≥1 error diagnostic | Parse, fix, re-check |
| 2 | Usage error | Fix the invocation |
| 3 | Internal error | Surface to the operator — don't retry blindly |

### 6. Remediate

Auto-fixable: STWD-003 (missing frontmatter), STWD-007 (stale maintained artifacts), STWD-012 (freshness dates), STWD-018 (unambiguous fragment links).

```bash
steward check --fix --apply
```

The other 17 rules need a manual edit — use `remediation` from the diagnostic, or `steward explain <rule-id> --output json` for detail.

### 7. Re-validate

Loop back to step 4 until clean, or until an internal error (exit 3) breaks the loop.

## SARIF for CI code-scanning

`steward check` (only this command) emits SARIF 2.1.0, consumable by GitHub Advanced Security for inline PR annotations:

```bash
steward check --since origin/main --output sarif > results.sarif
```

Every other command supports text and JSON only, not SARIF.

## Structural Markdown editing — don't hand-roll regex on `.md` files

```bash
steward md query README.md "heading[Features]" --output json
steward md outline <file>                                    # get exact heading text first
steward md edit ensure-section README.md --heading "FAQ" --under "Commands" --apply
steward md edit extract-section README.md --selector "heading[Features]" --to docs/features.md --apply
steward md edit fm-set docs/my-doc.md --key status --value Active --apply
steward md split plan docs/big-doc.md --max-lines 400 --output json   # non-mutating: proposes a split
```

`md edit` operations: `ensure-section`, `set-section`, `insert-section`, `append-block`, `prepend-block`, `extract-section`, `fm-set`, `fm-merge`, `fm-validate`. All preview by default; pass `--apply` to write.

### MdPath selectors

| Selector | Selects |
|---|---|
| `frontmatter` | The entire frontmatter block |
| `frontmatter.<field>` | One frontmatter field |
| `heading[Name]` | The section with that exact heading text |
| `heading[Parent/Child]` | A nested heading path |
| `heading[#N]` | The Nth heading in document order (1-based) |
| `heading[Name].lists` / `.tables` / `.codeblocks` | List/table/code blocks inside that section |
| `managed[<id>]` / `managed[*]` | One or all managed regions |
| `#anchor-slug` | A heading resolved through Markdown anchor normalization |

Heading matching is **exact, never fuzzy**. Run `steward md outline <file>` first when the exact heading text is unknown, or use the `#anchor-slug` form.

## Setting a repo up for agent use (if you're also the maintainer)

1. `steward init --profile software`
2. Set `output: { format: json }` in `.steward/config.yaml` so agents get JSON by default without passing `--output` every time
3. Add an `AGENTS.md` instructing agents to run `steward check` before finishing and to consult `steward explain <rule-id>` for remediation
4. Put `steward check` in CI so agents and CI see identical feedback

(Full governance setup — declaring artifacts, rules, families, severity — is steward-cli-maintainer's job, not this skill's.)

## Limitations to plan around

- **`search --role` matches only explicit `artifacts[]` role declarations**, not family-classified files. To enumerate every file in a family, use glob patterns or `steward orient --full --output json`.
- **4 of 21 rules auto-fix.** Most violations need a generated edit from the agent, not a flag.
- **Text-mode output omits structured next-step hints** that JSON carries — always use `--output json` for a programmatic loop.
- **No network calls, ever.** Steward cannot fetch remote content or call a hosting-platform API; any PR/issue interaction in the loop has to come from elsewhere (`gh`, `glab`, a platform SDK).
- **`--json-envelope` consistency is still landing** across the full command surface on some pre-1.0 builds. If a mutation command's JSON looks unexpected, check stderr and the exit code, not just the JSON body.
