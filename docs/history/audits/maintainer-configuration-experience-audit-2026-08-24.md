---
type: audit
status: Historical
last_updated: 2026-08-24
standalone: true
---

# Maintainer Configuration Experience Audit — 2026-08-24

## Scope

Assessment of whether the user-facing documentation set is sufficient for an external maintainer to configure Steward for their own repository, and what product capabilities that use case still lacks.

Method: read `README.md` and `docs/guide/**`, diff documented configuration fields against `RepositoryPolicy.cs`, `StewardConfig.cs`, and `PathPolicyDocument.cs`, then run the built CLI through a from-scratch adoption (`init` → edit policy → `check` → `explain path` → `config doctor`) on a throwaway repository.

## Summary Judgment

The documentation is **accurate on configuration surface and honest about limitations, but teaches fields rather than decisions**. Every property in the configuration model is documented in `configuration-reference.md`. The persona split, the three-exclusion-mechanisms table, and the frontmatter-precedence table are all above the bar for a project at this stage.

The defects found were concentrated in two places: claims about runtime behavior that the code contradicts, and shipped surface area that no user-facing page mentions.

## Documentation Defects Found

All eight were corrected in the same pass.

| # | Defect | Evidence |
|---|--------|----------|
| 1 | Exit-code semantics documented as "one or more rules violated". `ValidationEngine.cs` sets `Pass = errors == 0`, so `warning` and `info` exit 0. 17 of 21 rules default to non-error severity, meaning most of the rule surface was silently non-blocking in CI. | `README.md` exit-code table, `maintainer-guide.md` Step 7, `agent-integration.md` exit-code table |
| 2 | `agent-integration.md` used `md edit fm-set --field`; the CLI requires `--key`. | `MdEditCommand.cs` `CreateFmSetCommand` |
| 3 | Maintainer guide claimed the `software` profile scaffolds a LICENSE placeholder. `InitCommand.GeneratePlaceholder` deliberately returns `null` for LICENSE, so `init` exits 0 and the immediately following `check` fails with STWD-001. | `InitCommand.cs` `GeneratePlaceholder` |
| 4 | `steward md split plan` shipped but appeared in no user-facing page — only in RFC-011. | `MdSplitCommand.cs` |
| 5 | `md edit` operations were never enumerated; the README described the command only as "sections, frontmatter, blocks". | `MdEditCommand.cs` |
| 6 | `standalone: true` — the only per-file rule suppression in the product — was documented nowhere, surfacing only in `explain` output text. | `OrphanedDocumentRule.cs` |
| 7 | MdPath selector grammar was referenced by the README and required by `md query`, but specified in no page. | `MdPathSelector.cs` |
| 8 | `path-policy.yaml` parses a `kind` field on rules that nothing reads, and `config validate` does not flag it. | `PathPolicyDocument.cs` |

## Capability Gaps for the Maintainer Use Case

Ranked by adoption impact. Recorded in the [backlog](../../project/backlog.md).

1. **No baseline or phase-in.** Enabling a rule applies it to all existing content at once. Reproduced: adding `governance.frontmatter.required_fields: [status]` to a repository turned every Markdown file into an error in a single step. On a repository with real history this is a wall a maintainer cannot merge past, so the rule gets disabled instead of adopted. This is the single largest barrier to adopting Steward on an existing repository.
2. **No per-rule, per-file suppression with justification.** RFC-013 remains deferred. Present granularity is global disable or glob `path_overrides`; a one-file exception forces a glob that over-suppresses. `standalone: true` covers only STWD-013.
3. **No severity threshold flag.** Gating CI on a warning-severity rule requires rewriting its severity in `policy.yaml`. A `--fail-on <severity>` option on `check` would remove the footgun behind defect 1 without a policy edit.
4. **No shareable or inheritable policy.** `ConfigLoader` has no `extends` or import. An organization running Steward across many repositories must copy `.steward/` and hand-sync drift — a notable hole for a governance tool, since governance is inherently multi-repo.
5. **No policy provenance in `explain path`.** Output lists applicable rule IDs with no severity and no indication of which config file or stanza activated each one. This is the missing debugging story for the whole configuration surface.
6. **No policy impact preview.** `config doctor` catches dead configuration, not the effect of a proposed policy change. A maintainer cannot see what a policy edit would do before committing it.

## Conclusion

Defects 1 and 3 were the two that would cost an external maintainer trust in the first session: a CI gate that validates nothing, and an `init` that fails its own `check`. Both were mechanical to fix. The capability gaps are consistent with what the backlog already tracks; the finding that sharpens them is that the phase-in gap, not rule coverage, is what blocks adoption on repositories that already have content.
