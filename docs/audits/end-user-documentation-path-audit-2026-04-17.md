---
type: audit
status: Active
last_updated: 2026-04-17
---

# End-User Documentation Path Audit — 2026-04-17

## Summary Judgment

The end-user documentation path before this pass was **functional but persona-blind, incomplete on implementation details, and stale in several important claims**. A new user could figure out how to use Steward from the README, but they had to self-identify their role without guidance, navigate a single undifferentiated workflow, and encounter multiple discrepancies between documented and actual capabilities.

After this pass, the documentation path is **explicitly persona-structured, code-verified, and implementation-aligned**.

## Persona Assessment

### Maintainer Path — Before

- No definition of the maintainer role anywhere in user-facing docs.
- The "Adapting to your repository" section in README provided a terse adoption workflow but assumed the reader already understood the init → configure → validate → maintain loop.
- `artifact_families` (v0.13.0) — the largest configuration surface added in the last two milestones — was completely absent from the README configuration docs.
- `coverage.exclude` was undocumented.
- Validation rules STWD-014 through STWD-016 (implemented and tested) were missing from the rules table, and the implementation-status and readiness plan both claimed these features were deferred.
- No reference table mapping enforcement areas to configuration locations and rule IDs.
- No guidance on severity tuning, suppression, or path overrides.

### Maintainer Path — After

- Explicit "Getting Started — Maintainer" section with the full init → suggest → configure → validate → doctor → check → maintain workflow.
- "Maintainer reference: what can be enforced today" table mapping all 16 enforcement areas to configuration knobs and rule IDs.
- `artifact_families` section with a complete worked example in the policy.yaml reference.
- `coverage.exclude` documented in config.yaml reference.
- "Common Workflows" section includes maintainer-specific workflows for adding artifact families and tuning severity/suppression.
- All 16 validation rules documented in the rules table with default severities.

### Contributor Path — Before

- No definition of the contributor role anywhere in user-facing docs.
- The "Quick Start" section showed init/orient/check/maintain but did not explain the contributor validation loop.
- No guidance on scoped checks (`--scope changed`, `--scope staged`).
- No explanation of exit codes.
- No guidance on interpreting failures or using `steward explain`.
- No mention of `--fix` / `--fix --apply` for auto-remediation.
- `--dry-run` was still listed as a visible option despite being hidden/deprecated since v0.12.0.

### Contributor Path — After

- Explicit "Getting Started — Contributor" section with the orient → edit → check → explain → fix → maintain → re-check workflow.
- Exit codes table with clear meanings.
- Scoped validation documented with `--scope changed` and `--scope staged` examples.
- `steward explain` and `steward explain path` documented in the contributor flow.
- `--fix` and `--fix --apply` documented in the contributor flow.
- `--dry-run` reference removed.

## Key Issues Found and Fixed

### Code-vs-Docs Discrepancies (Fixed)

| Issue | Location | Resolution |
|-------|----------|------------|
| README listed only 13 validation rules; code has 16 (STWD-014, 015, 016) | README.md Validation Rules table | Added STWD-014 through STWD-016 with correct severity and description |
| `--dry-run` shown as visible option; it's hidden/deprecated | README.md Commands table | Removed; documented `--fix` / `--fix --apply` instead |
| `check` command missing `--paths` and `--quiet` options | README.md Commands table | Added |
| `orient` command missing `--compact` and `--depth` options | README.md Commands table | Added |
| `artifact_families` completely absent from policy.yaml docs | README.md Configuration section | Added with full example |
| `coverage.exclude` absent from config.yaml docs | README.md Configuration section | Added |
| `importance` field absent from artifact declarations | README.md Configuration section | Added to examples |
| `freshness.max_age_days` not shown in artifact examples | README.md Configuration section | Added to examples |
| `--pattern` option for `md query` undocumented | README.md Commands table | Added |
| `--to` and `--from` options for `refs` undocumented | README.md Commands table | Added |
| `--preview` and `--apply` for `refactor move` undocumented | README.md Commands table | Added |
| Profile "Readiness" column unclear to end users | README.md Profiles table | Rewritten with plain-language status descriptions |
| Implementation-status claimed STWD-014/015/016 were deferred | docs/implementation-status.md | Updated to show them as delivered |
| Pre-1-0-readiness-plan claimed these features were unimplemented | docs/planning/pre-1-0-readiness-plan.md | Marked as completed |
| Test count stale (598 vs actual 627) | docs/implementation-status.md | Updated to 627 (436 core + 191 CLI) |
| Validation rule count said 13 | docs/implementation-status.md | Updated to 16 |

### Structural Issues (Fixed)

| Issue | Resolution |
|-------|------------|
| No persona identification or role definitions | Added "Who Is Steward For?" section with explicit maintainer and contributor definitions |
| No persona-based getting-started paths | Added "Getting Started — Maintainer" and "Getting Started — Contributor" sections |
| Quick Start didn't distinguish personas | Replaced with persona-specific getting-started sections |
| No maintainer enforcement reference | Added "Maintainer reference: what can be enforced today" table |
| No contributor exit-code documentation | Added exit codes table in contributor section |
| No troubleshooting section | Added troubleshooting section with common issues and resolutions |
| No "Current Status" section that separates working/planned | Added explicit Current Status section |
| "Using Steward In This Repo" positioned early, mixed with general guidance | Moved to near end of README, after all generic user docs |
| No mention of .NET 10 SDK prerequisite in Installation | Added prerequisite subsection |
| No explanation of how to run after building from source vs. global install | Added invocation examples for both paths |
| Default severities not shown in rules table | Added Default Severity column |
| Severity override and suppression not explained for contributors | Added note about customization below rules table |
| Maintenance types not enumerated | Added supported maintenance types list |
| No common workflows section | Added with persona-tagged examples |
| `search` command modes not fully documented | Added `--mode all|content|headings` and `--max` |
| `config doctor` description incomplete | Updated to include unreachable families |
| `--verbosity` values not listed in global options | Added all four values |
| Public feed install section misleading | Clarified as "Not yet available" |

## What Remains Deferred

| Item | Reason | Tracked In |
|------|--------|------------|
| Output examples showing what actual CLI output looks like | Would require maintaining example output that may drift from reality; better served by `steward explain` and actual CLI usage | Could be added in a future docs polish pass |
| Dedicated maintainer/contributor guide pages (separate files) | README is now comprehensive enough as a single canonical doc; splitting would add maintenance burden without proportional benefit at current product scale | Reconsider if README exceeds ~600 lines |
| Troubleshooting section for `md edit` operations | The structural editing subsystem is primarily agent-facing and lower priority for end-user documentation | Low-priority future enhancement |
| Documentation for `repository.terminology` config field | The field exists in the schema but its user-facing behavior is limited; documenting it now would overpromise | Document when terminology customization has visible user-facing effects |
| STWD-013 info diagnostic on `docs/audits/code-quality-review-2025-07-23.md` | Pre-existing unreferenced file; not related to this audit | Maintainer cleanup task |

## Files Changed

| File | Nature of Change |
|------|-----------------|
| [README.md](../../README.md) | Major rewrite: persona framing, getting-started sections, commands/rules/config updates, troubleshooting, common workflows, current status |
| [docs/implementation-status.md](../implementation-status.md) | Fixed validation rule count (13→16), test count (598→627), documented STWD-014/015/016 as delivered, removed stale "required-sections deferred" claim |
| [docs/planning/pre-1-0-readiness-plan.md](../planning/pre-1-0-readiness-plan.md) | Marked required-sections, naming-pattern, and min-count enforcement as completed |
| [STRUCTURE.md](../../STRUCTURE.md) | Regenerated via `steward maintain --apply` |
| This file | Created as audit record |
