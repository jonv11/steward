---
name: steward-cli-maintainer
description: Configure Steward governance for a repository — write or edit .steward/config.yaml, policy.yaml, and path-policy.yaml, declare required artifacts and artifact families (ADRs, RFCs, runbooks), gate CI on validation rules, and set up deterministic maintenance. Use when asked to set up Steward, adopt Steward on an existing repo, add or change a governance rule, define naming conventions, or make steward check fail CI on something it currently only warns about. Requires steward installed — see steward-cli.
---

# Steward CLI — Maintainer

Configure a repository's `.steward/` contract as the maintainer: what must exist, what must be true about it, and what gets generated automatically. Contributors and agents then validate against what you declare here — see steward-cli-contributor and steward-cli-agent for their side of this contract.

## Setup sequence

### 1. Initialize

```bash
cd /path/to/target-repo
steward init --profile <software|docs|minimal>
```

| Profile | Fits | Scaffolds |
|---|---|---|
| `software` | Code repos with standard docs | README, LICENSE, CHANGELOG, CONTRIBUTING declared; placeholders for all but LICENSE |
| `docs` | Documentation-focused repos | README placeholder + `docs/` |
| `minimal` | Lightweight start | Minimal config, no required artifacts |

`init` creates `config.yaml` and `policy.yaml`. It does **not** scaffold `path-policy.yaml` — add that by hand when you want naming or forbidden-path rules. Commit `.steward/` to version control.

### 2. Discover existing content (existing repos only)

```bash
steward config suggest
```

Heuristic starting point only, not authoritative — it scans for likely artifacts and roles but misses unusual paths. Review its output and add what it missed by hand; never treat its output as the finished policy.

### 3. Declare the contract

Edit `.steward/policy.yaml`. The sections, roughly in the order you'll touch them:

**`repository`** — informational only (name, type, terminology); does not affect validation.

**`artifacts`** — specific files/directories the repo must or should contain:

```yaml
artifacts:
  - path: README.md
    role: authoritative
    required: true
  - path: docs/architecture.md
    role: documentation
    importance: recommended
  - path: docs/status.md
    role: current-state
    freshness:
      max_age_days: 30       # enables STWD-012 staleness checking
```

`importance` resolves in order: explicit `required: true` → role's built-in default → `optional`. Roles are open-ended strings, but a fixed set (`authoritative`, `governance`, `requirements`, `generated`, `guide`, `audit`, state-document roles like `roadmap`/`current-state`, …) carry semantic behavior in `orient`/`status`/`search` — worth using the recognized ones over inventing new strings.

**`artifact_families`** — recurring document types (ADRs, RFCs, runbooks) with convention-based matching and type-aware validation:

```yaml
artifact_families:
  - family: adr
    display_name: Architecture Decision Record
    match:
      path_pattern: "docs/adr/ADR-*.md"
    frontmatter_schema:
      required: [type, status]
      allowed_values:
        status: [Draft, Proposed, Accepted, Superseded]
    required_sections: [Context, Decision, Consequences]
    naming_pattern: "^ADR-[0-9]{3}-[a-z0-9-]+\\.md$"
    directory_expectations:
      min_count: 1
```

Families can also enforce title/H2 patterns (`title_pattern`, `section_pattern`) and a full ordered document schema (`section_schema`) — reach for `steward config show --effective` after writing one to confirm it resolved as intended, and `steward config doctor` to catch a pattern that matches nothing.

**`governance`** — global behavior: `start_here` (ordered entry points for `orient`/`status`), `frontmatter.required_fields` (applies to *all* governed Markdown — see the adoption warning below), `section_size_warning_threshold`, `managed_regions`.

**`validation`** — which rules run and at what severity:

```yaml
validation:
  severity_overrides:
    STWD-008: error              # broken links now fail CI
  disabled_rules: [STWD-013]     # suppressed everywhere
  path_overrides:
    - pattern: "drafts/**"
      disabled_rules: [STWD-003, STWD-012]
  frontmatter_requirements:
    - pattern: "docs/decisions/**/*.md"
      required_fields: [status, date, deciders]
```

**`maintenance`** — artifacts Steward generates and keeps in sync (`structure-document`, `index`, `directory-index`, `managed-section`, `frontmatter-auto`, `manifest` types). Generated output must never be hand-edited; `steward check` flags stale generated content as STWD-007.

Add naming/forbidden-path rules separately in `.steward/path-policy.yaml`:

```yaml
rulesets:
  - name: core-files
    rules:
      - pattern: ".env"
        category: forbidden
      - pattern: "docs/adr/**/*.md"
        category: recommended
        must_match: "^ADR-[0-9]{3}-[a-z0-9-]+\\.md$"
```

### 4. Validate the configuration itself

```bash
steward config validate    # syntax + semantic errors (unknown rule IDs, bad types)
steward config doctor      # valid-but-ineffective config: dead start_here, unmatched patterns, unreachable families
```

Run both after every edit to `.steward/*`. `validate` catches what's structurally wrong; `doctor` catches what's silently doing nothing.

### 5. Run a check, iterate

```bash
steward check
steward explain <rule-id>            # look up any violation
steward explain path <file>          # see what applies to one file
```

### 6. Wire up maintenance, if configured

```bash
steward maintain          # preview
steward maintain --apply  # write
```

Each artifact reports `OK` (current), `MAINTAIN` (stale, `--apply` regenerates it), or `BLOCKED` (target missing, managed-region markers absent, or `source` doesn't exist — `--apply` writes nothing until the cause is fixed).

### 7. Add to CI

```yaml
# CI step
- run: steward check
```

## The gotcha every new maintainer hits: severity vs. exit code

`steward check` exits 1 **only** on `error`-severity diagnostics. Most rules default to `warning`, which prints but does not fail the build. If you want a rule to actually block a merge, you must raise it explicitly:

```yaml
validation:
  severity_overrides:
    STWD-008: error    # broken internal links now gate CI
```

`governance.completion_policy` is reporting-only and never affects the exit code, regardless of what it lists.

## Adopting Steward on a large existing repo

There is no baseline or phase-in mode — turning on a repo-wide rule (e.g. `governance.frontmatter.required_fields`) applies it to every existing non-conforming file immediately, all at once. To adopt incrementally:

- Scope a new requirement to one directory first, via `validation.frontmatter_requirements` (a glob) or an artifact family, instead of the repo-wide `governance.frontmatter` setting.
- Keep repo-wide rules at `warning` until the existing backlog is cleared, then raise to `error`.
- Use `steward check --since <ref>` in CI so the gate only judges what a branch actually touched, not the whole existing tree.

## Three ways to exclude — pick the right one

| Mechanism | Effect | Use for |
|---|---|---|
| `discovery.exclude` (config.yaml) | Files invisible to *every* Steward command | Build output, `node_modules/`, tool installs |
| `validation.disabled_rules` | A rule off everywhere | A rule you never want, repo-wide |
| `validation.path_overrides[].disabled_rules` | A rule off for a glob | Frontmatter not needed under `src/`, freshness not needed under `drafts/` |

There is exactly one per-file escape hatch, set in the file's own frontmatter, not in `.steward/`: `standalone: true` suppresses STWD-013 (discoverability) for that one file. Nothing else can be waived per-file — use a `path_overrides` glob instead.

## What Steward is not

Not a code linter (docs/governance only), not a CI system (you run it *in* CI), not a content generator (it maintains structure/indexes, not prose), no hosting-platform integration, no IDE plugin. Don't reach for it outside that scope.

## Common maintainer mistakes

- **Hand-editing generated output** (`STRUCTURE.md`, managed index sections) — Steward will flag it stale (STWD-007) and your edits get overwritten on the next `--apply`.
- **Treating `config suggest` as authoritative** — it's a heuristic starting point, not a finished policy.
- **Forgetting profile merge semantics** — profile defaults merge shallowly: your `policy.yaml` scalars/objects override the profile, but *list* sections like `artifacts:` **replace** the profile's list entirely rather than appending to it.
- **Assuming `search --role` finds every family-matched file** — it only matches explicit `artifacts[]` role declarations. To find every file in a family, use glob patterns or `steward orient --full`.
- **Not distinguishing `discovery.exclude` from `coverage.exclude`** — the former makes files invisible everywhere; the latter still discovers and validates files, just excludes them from the `status --coverage` percentage. Use `coverage.exclude` for governed-but-uncounted content like test fixtures.

Full field-by-field reference (every key, default, and interaction) is always available live via `steward config show --effective` against your own policy — treat that, not a memorized table, as ground truth for what's actually in effect.
