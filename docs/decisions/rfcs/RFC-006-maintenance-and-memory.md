---
type: rfc
status: Accepted
resolves: >-
  Maintenance flows, memory artifacts, auto-maintained documents, anti-drift, artifact roles
---

# RFC-006: Maintenance and Memory Artifacts

---

## Context

The requirements envision the CLI evolving from validator to maintainer: auto-updating indexes, registries, structure documents, and other governed artifacts deterministically. This RFC defines what maintenance means, what artifacts are maintained, and how maintenance flows work.

## Decision

### Maintenance scope

Maintenance covers deterministic, policy-driven regeneration or update of governed artifacts from actual repository state. Maintenance is:
- **Deterministic:** Same input → same output, always.
- **Idempotent:** Running twice changes nothing.
- **Minimal-diff:** Only meaningful changes appear in diffs.
- **Preview-first:** `steward maintain` defaults to preview; `--apply` commits changes.
- **Scoped:** Can target specific artifacts or run repository-wide.

### Maintainable artifact types

| Type | Example | How maintained |
|------|---------|---------------|
| **Repository structure document** | docs/STRUCTURE.md | Regenerated from file tree + policy |
| **Index / registry** | docs/index.md, docs/decision-index.md | Regenerated from governed file inventory |
| **Managed section** | A `<!-- steward:managed:begin -->` block inside a human-authored doc | Content between markers updated, rest preserved |
| **Frontmatter fields** | `last_updated`, `generated_by` fields | Fields updated based on policy rules |
| **Catalog / glossary** | docs/glossary.md | Updated from declared sources |

### Artifact roles in policy

Policy declares which artifacts are maintained and how:

```yaml
# .steward/policy.yaml (maintenance section)
maintenance:
  artifacts:
    - id: structure-doc
      path: docs/STRUCTURE.md
      type: structure-document
      source: file-tree
      options:
        depth: 3
        exclude: ["**/test-fixtures/**"]

    - id: decision-index
      path: docs/decisions/decision-index.md
      type: index
      source: docs/decisions/**/*.md
      managed_section: "steward:decision-list"
      sort: filename

    - id: frontmatter-freshness
      type: frontmatter-auto
      targets: "docs/**/*.md"
      fields:
        last_updated: file-mtime
```

### Maintenance command

```bash
# Preview all maintenance actions
steward maintain

# Preview a specific artifact
steward maintain --artifact structure-doc

# Apply all maintenance
steward maintain --apply

# Apply specific artifact maintenance
steward maintain --artifact decision-index --apply

# Machine-readable output
steward maintain --output json
```

### Preview output

Preview shows a per-artifact plan:

```
MAINTAIN  structure-doc  docs/STRUCTURE.md
  Would update 3 lines (tree entries added for new files)

MAINTAIN  decision-index  docs/decisions/decision-index.md
  Section "steward:decision-list" would update: 2 new entries, 0 removed

No changes applied. Run with --apply to commit changes.
```

With `--output json`, the plan is a structured array of planned changes.

### Anti-drift detection

`steward check` includes stale-artifact detection for maintained artifacts:
- Compares current artifact content against what maintenance would produce.
- Reports `stale-artifact` diagnostics when they differ.
- This is a read-only check; it does not modify files.

### Project-memory and state documents

Policy can declare artifacts with explicit memory/state roles:

```yaml
artifacts:
  - path: docs/VISION.md
    role: vision
    required: false
  - path: docs/planning/milestone-plan.md
    role: milestones
    required: false
  - path: docs/implementation-status.md
    role: current-state
    required: false
```

These roles affect:
- How the artifact appears in `steward orient` (highlighted under memory/state)
- Whether staleness checks apply
- Whether governance rules (frontmatter, structure) are enforced

### Machine-readable memory artifacts

The CLI can generate machine-readable inventory artifacts:

```yaml
# .steward/generated/manifest.json — auto-generated, do not hand-edit
{
  "generated_at": "2026-04-14T12:00:00Z",
  "generator": "steward v0.8.0",
  "files": [ ... ],
  "artifacts": [ ... ],
  "headings_index": [ ... ]
}
```

These are declared in policy under `maintenance.artifacts` with `type: manifest` or `type: search-index`. They:
- Are deterministic and refreshable (REQ-MRM-002).
- Support downstream automation (REQ-MRM-003).
- Are not required for core CLI functionality (REQ-MRM-004).

### Content preservation

Maintenance operations only modify:
- Content inside managed regions (between markers).
- Whole files declared as generated (`role: generated` in policy).
- Specific frontmatter fields declared for auto-maintenance.

Content outside these scopes is never modified by maintenance. This fulfills REQ-MAINT-011.

## Alternatives considered

1. **Maintenance as part of `check --fix`:** Rejected—maintenance is a broader concept than fixing validation failures. Some maintained artifacts have no corresponding validation rule.
2. **No managed-section model (whole-file only):** Rejected—many documents are human-authored with a maintained section embedded. Whole-file replacement would lose user content.
3. **Automatic maintenance on every `check`:** Rejected—maintenance is a mutation operation and must be explicit (safety-first).

## Consequences

- Maintenance is explicit, preview-first, and policy-driven.
- Anti-drift is detected by `check`; fixed by `maintain`.
- Mixed-content documents are handled via managed regions.
- Machine-readable artifacts support automation without being required.
- Project-memory documents are first-class but governance is opt-in.
