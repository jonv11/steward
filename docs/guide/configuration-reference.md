---
type: guide
status: Active
last_updated: 2026-08-24
---

# Configuration Reference

Steward uses a `.steward/` directory at the repository root with up to three YAML configuration files. All three are optional — Steward works without any configuration, but its validation power requires at least `policy.yaml`.

Run `steward init --profile <name>` to scaffold starter files.

## Files overview

| File | Purpose | Required |
|------|---------|----------|
| `config.yaml` | Runtime settings: output format, discovery exclusions, coverage exclusions | No |
| `policy.yaml` | Repository contract: artifacts, families, governance rules, maintenance, validation | No, but needed for meaningful validation |
| `path-policy.yaml` | Path and naming rules: required/forbidden paths, naming conventions | No |

## config.yaml

Controls runtime behavior. CLI flags always override these settings.

```yaml
profile: software           # Built-in profile (see Profiles below)

output:
  format: text              # text | json
  verbosity: normal         # quiet | normal | verbose | debug
  no_color: false           # Disable colored output

discovery:
  exclude:                  # Glob patterns to exclude from ALL steward operations
    - "node_modules/"       # These files become completely invisible to steward
    - "dist/"
    - ".vs/"
    - ".tools/"

coverage:
  exclude:                  # Glob patterns to exclude from coverage calculations only
    - "tests/fixtures/**"   # Files are still discovered and validated,
                            # but excluded from the coverage percentage
```

### Fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `profile` | string | _(none)_ | Built-in profile name: `software`, `docs`, `minimal`, `mixed`, `knowledge` |
| `output.format` | string | `text` | Default output format: `text` or `json` |
| `output.verbosity` | string | `normal` | `quiet`, `normal`, `verbose`, or `debug` |
| `output.no_color` | bool | `false` | Disable colored terminal output |
| `discovery.exclude` | list | `[]` | Glob patterns excluded from all discovery — files matching these are invisible to every steward command |
| `coverage.exclude` | list | `[]` | Glob patterns excluded from coverage calculation only — files are still discovered and validated |

### discovery.exclude vs coverage.exclude

These serve different purposes:

- **`discovery.exclude`** makes files completely invisible to Steward. Use it for build output, dependencies, tool installations, and anything that should never appear in any Steward command.
- **`coverage.exclude`** excludes files from the governance coverage percentage reported by `steward status --coverage`, but those files are still discovered, validated, and shown in other commands. Use it for test fixtures, generated files, or vendor content you govern but don't want counting against your coverage metric.

SARIF is not a valid repository-wide `output.format` default. Use `steward check --output sarif` explicitly when a CI system needs SARIF 2.1.0; other commands support text and JSON.

## policy.yaml

The core configuration file. Declares what the repository contains, what rules apply, and what artifacts are maintained.

### repository section

```yaml
repository:
  name: my-project
  description: A sample project
  type: software              # Informational label: software, documentation, mixed, knowledge, general, tool
  terminology:                # Custom term definitions (informational)
    adr: Architecture Decision Record
```

All fields are informational — they appear in `orient` and `status` output but do not affect validation behavior.

### artifacts section

Declares specific files or directories the repository is expected to contain.

```yaml
artifacts:
  - path: README.md
    role: readme
    description: Project overview
    required: true
    importance: required

  - path: CHANGELOG.md
    role: changelog
    importance: recommended

  - path: docs/adr/
    role: decision
    description: Architecture Decision Records
    index_of: docs/adr/

  - path: docs/status.md
    role: current-state
    freshness:
      max_age_days: 30
```

#### Artifact fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `path` | string | _(required)_ | Relative path to file or directory |
| `role` | string | _(none)_ | Artifact role (see Role values below) |
| `description` | string | _(none)_ | Human-readable description shown in orient/status |
| `required` | bool | `false` | Shorthand for `importance: required` — file must exist (STWD-001) |
| `importance` | string | _(resolved)_ | `required`, `recommended`, or `optional` |
| `index_of` | string | _(none)_ | Directory this artifact indexes (enables STWD-011) |
| `freshness.max_age_days` | int | _(none)_ | Days before the file is considered stale (enables STWD-012) |

#### Importance resolution

When `importance` is not set explicitly, it is resolved in this order:

1. If `required: true` → `required`
2. Role-linked default from built-in role defaults
3. Fallback → `optional`

#### Role values

Roles are open-ended strings — you can use any value. The following roles have semantic behavior in orient, status, and search:

| Role | Behavior | Default importance |
|------|----------|-------------------|
| `authoritative` | Primary source-of-truth docs | — |
| `governance` | Repo governance docs (CONTRIBUTING, CODE_OF_CONDUCT, etc.) | — |
| `documentation` | General documentation | — |
| `changelog` | Change logs | — |
| `workflow` | Process and workflow docs | — |
| `state-document` | Generic state tracking | — |
| `vision` | Vision documents | — |
| `roadmap` | Roadmap docs | — |
| `current-state` | Current state summaries | — |
| `milestones` | Milestone tracking | — |
| `decision-log` | Decision logs | — |
| `requirements` | Requirements docs | `required` |
| `generated` | Generated artifacts | `recommended` |
| `guide` | Guides and tutorials | `recommended` |
| `audit` | Assessment and review records | `optional` |
| `index` | Directory index files | — |

Roles marked as state-document roles (`state-document`, `vision`, `roadmap`, `current-state`, `milestones`, `decision-log`) receive special treatment in `orient` and `status` — they are shown under "State Documents" and checked for freshness.

### artifact_families section

Groups recurring document types (e.g., ADRs, RFCs, runbooks) with convention-based discovery and type-aware validation.

```yaml
artifact_families:
  - family: adr
    display_name: Architecture Decision Record
    match:
      path_pattern: "docs/adr/ADR-*.md"
    role: governance
    importance: recommended
    frontmatter_schema:
      required: [type, status]
      allowed_fields: [type, status, description, last_updated]
      allowed_values:
        type: [adr]
        status: [Draft, Proposed, Accepted, Superseded, Deprecated]
      deprecated_fields:
        date: last_updated
    required_sections: [Context, Decision, Consequences]
    naming_pattern: "^ADR-[0-9]{3}-[a-z0-9-]+\\.md$"
    title_pattern: "^ADR-[0-9]{3}: .+"
    section_pattern: "^[A-Z][A-Za-z ]+$"
    section_schema:
      heading_match: exact
      enforce_order: true
      allow_extra: true
      sections:
        - heading: Context
        - heading: Decision
        - heading: Consequences
        - heading: Alternatives
          required: false
    directory_expectations:
      min_count: 1
```

#### Family fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `family` | string | _(required)_ | Unique identifier (e.g., `adr`, `rfc`, `runbook`) |
| `display_name` | string | _(none)_ | Human-readable name shown in status output |
| `match.path_pattern` | string | _(required)_ | Glob pattern for files belonging to this family |
| `match.frontmatter` | map | _(none)_ | Field→value conditions (AND, case-insensitive) for frontmatter-based matching |
| `role` | string | _(none)_ | Role assigned to all matched files |
| `importance` | string | `optional` | `required`, `recommended`, or `optional` |
| `frontmatter_schema.required` | list | `[]` | Frontmatter field names that must be present (STWD-003) |
| `frontmatter_schema.allowed_values` | map | `{}` | Field→allowed values for validation (case-insensitive) |
| `frontmatter_schema.allowed_fields` | list | _(none)_ | Complete allowed field set for a closed schema. Unexpected fields produce STWD-003 warnings; global auto-fields are implicitly allowed |
| `frontmatter_schema.deprecated_fields` | map | `{}` | Deprecated field→replacement mapping, or `null` for removal. STWD-003 reports and can auto-fix migrations |
| `required_sections` | list | `[]` | Heading text that must appear in matched files (STWD-014) |
| `naming_pattern` | string | _(none)_ | Regex that matched filenames must satisfy (STWD-016) |
| `title_pattern` | string | _(none)_ | Case-sensitive regex that the H1 heading must satisfy (STWD-019) |
| `section_pattern` | string | _(none)_ | Case-sensitive regex that every H2 heading must satisfy (STWD-020) |
| `section_schema` | object | _(none)_ | H2 document schema for required/optional sections, ordering, and extra-section policy (STWD-021) |
| `directory_expectations.min_count` | int | _(none)_ | Minimum number of files matching this family (STWD-015) |
| `directory_expectations.description` | string | _(none)_ | Description shown in min-count violation messages |

#### Section schema fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `section_schema.sections` | list | `[]` | Ordered H2 schema entries |
| `section_schema.sections[].heading` | string | _(required)_ | Heading text matched against document H2 headings |
| `section_schema.sections[].required` | bool | `true` | Whether the section must exist |
| `section_schema.heading_match` | string | `contains` | `contains` for case-insensitive substring matching or `exact` for case-insensitive equality |
| `section_schema.enforce_order` | bool | `false` | Require present sections to follow schema order |
| `section_schema.allow_extra` | bool | `true` | Allow H2 headings not declared in the schema |

`config validate` rejects invalid title/section regexes, blank closed-schema fields, required fields omitted from `allowed_fields`, deprecated replacements omitted from `allowed_fields`, and unsupported `heading_match` values.

### governance section

Controls global governance behavior.

```yaml
governance:
  section_size_warning_threshold: 500    # Lines per section before STWD-004 fires

  start_here:                            # Ordered entry-point files for orient/status
    - README.md
    - docs/index.md

  frontmatter:
    required_fields: [status, owner]     # Fields all governed Markdown files must declare
    auto_fields:
      last_updated: true                 # Auto-update last_updated on changed files

  managed_regions:
    marker: steward                      # Default marker for managed section markers
    enforce_ownership: true

  completion_policy:
    rules:
      - id: STWD-008
        description: All internal links must resolve
```

#### Governance fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `section_size_warning_threshold` | int | `500` | Lines per section before STWD-004 warns |
| `start_here` | list | `[README.md]` | Ordered entry-point files shown in orient/status |
| `frontmatter.required_fields` | list | `[]` | Frontmatter fields required on all governed Markdown files (STWD-003) |
| `frontmatter.auto_fields` | map | `{}` | Field name → `true` to auto-update date fields on locally changed files |
| `managed_regions.marker` | string | `steward` | Default marker string for managed section delimiters |
| `managed_regions.enforce_ownership` | bool | — | Whether managed regions are enforced |
| `completion_policy.rules` | list | `[]` | Rule IDs and descriptions shown in the "Completion:" output summary. Reporting-only — does not affect pass/fail exit code. Use `validation.severity_overrides` to make a rule gate CI. |

#### Auto-fields behavior

When `governance.frontmatter.auto_fields.<field>: true` is set, Steward updates that frontmatter field to today's date (`yyyy-MM-dd`) on files that have local git changes. It only updates fields that already exist in the file's frontmatter — it does not add new fields.

This is implemented as an automatic maintenance task. Run `steward maintain --apply` to apply the updates.

### validation section

Controls which rules are active and their severity.

**Severity determines whether a rule gates CI.** `steward check` exits 1 only when at least one `error`-severity diagnostic is produced; `warning` and `info` are reported but exit 0. Only STWD-001, STWD-002, STWD-003, and STWD-005 default to `error`. Raise any other rule with `severity_overrides` to make it block a merge.

```yaml
validation:
  disabled_rules: [STWD-004]           # Suppress rules globally

  severity_overrides:
    STWD-008: error                     # Change a rule's severity: error | warning | info

  path_overrides:
    - pattern: "src/**/*.md"
      disabled_rules: [STWD-003]       # No frontmatter required in source-adjacent docs

  frontmatter_requirements:            # Scoped frontmatter rules (per-path)
    - pattern: "docs/decisions/**/*.md"
      required_fields: [status, date, deciders]
      allowed_values:
        status: [proposed, accepted, deprecated, superseded]
```

#### Validation fields

| Field | Type | Description |
|-------|------|-------------|
| `disabled_rules` | list | Rule IDs to suppress globally |
| `severity_overrides` | map | Rule ID → severity (`error`, `warning`, or `info`) |
| `path_overrides[].pattern` | string | Glob pattern |
| `path_overrides[].disabled_rules` | list | Rule IDs to suppress for matched paths |
| `frontmatter_requirements[].pattern` | string | Glob pattern |
| `frontmatter_requirements[].required_fields` | list | Frontmatter fields required for matched files (STWD-003) |
| `frontmatter_requirements[].allowed_values` | map | Field → allowed values for matched files |

#### Three exclusion mechanisms

Steward has three ways to exclude files or rules:

| Mechanism | Effect | Use when |
|-----------|--------|----------|
| `discovery.exclude` (config.yaml) | Files invisible to all commands | Build output, node_modules, tool installs |
| `validation.disabled_rules` | Rules disabled globally | You never want a rule to fire anywhere |
| `validation.path_overrides[].disabled_rules` | Rules disabled for specific paths | Frontmatter not needed in `src/`, freshness not needed in `drafts/` |

#### Per-file suppression

There is currently one per-file escape hatch, set in the file's own frontmatter rather than in `.steward/`:

```yaml
---
standalone: true
---
```

`standalone: true` suppresses **STWD-013** (discoverability) for that file only. Use it for archived evidence, historical records, and other documents that are intentionally not linked from any navigation surface.

No other rule supports per-file suppression. For everything else, scope the exemption with a `validation.path_overrides` glob.

#### Frontmatter requirements interaction

Frontmatter requirements come from three sources, applied additively:

1. **Global:** `governance.frontmatter.required_fields` — applies to all governed Markdown
2. **Scoped:** `validation.frontmatter_requirements[].required_fields` — applies to files matching the glob pattern
3. **Family:** `artifact_families[].frontmatter_schema.required` — applies to files matched by the family

A file that matches multiple sources must satisfy all of them.

### maintenance section

Declares artifacts that Steward generates or maintains automatically.

```yaml
maintenance:
  artifacts:
    - id: structure
      path: STRUCTURE.md
      type: structure-document
      options:
        depth: 3
        exclude:
          - ".git/**"
          - "node_modules/**"

    - id: docs-index
      path: docs/index.md
      type: directory-index
      source: "docs/*.md"
      sort: filename

    - id: decision-adr-index
      path: docs/decisions/README.md
      type: index
      source: "docs/decisions/adrs/*.md"
      managed_section: "ADRs"
      sort: filename
```

#### Maintenance artifact fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Unique identifier for this maintenance artifact |
| `path` | string | Output file path |
| `type` | string | Maintenance type (see below) |
| `source` | string | Glob pattern for input files |
| `managed_section` | string | Heading name for the managed section (for `index` and `managed-section` types) |
| `sort` | string | Sort order: `filename` (default) or `path` |
| `targets` | string | Glob pattern for target files (for `frontmatter-auto`) |
| `fields` | map | Field→source mapping (for `frontmatter-auto`) |
| `options.depth` | int | Directory depth (default: 3, for `structure-document`) |
| `options.exclude` | list | Glob patterns to exclude from the generated output |
| `depends_on` | list | IDs of other maintenance artifacts that must run first |

#### Maintenance types

| Type | Description |
|------|-------------|
| `structure-document` | Generates a directory tree document |
| `index` | Generates a managed section within an existing file, listing files from `source` |
| `directory-index` | Generates a full index document listing files from `source` (requires `description` frontmatter in indexed files) |
| `managed-section` | Manages a specific section within an existing file |
| `frontmatter-auto` | Updates frontmatter fields automatically |
| `manifest` | Generates a manifest file |

## path-policy.yaml

Enforces naming conventions and file presence/absence patterns. Optional.

```yaml
rulesets:
  - name: core-files
    description: Essential repository files
    rules:
      - pattern: "README.md"
        category: required
        exact: true

      - pattern: ".env"
        category: forbidden
        description: Environment files must not be committed

  - name: adr-naming
    description: ADR filename conventions
    rules:
      - pattern: "docs/adr/**/*.md"
        category: recommended
        must_match: "^ADR-[0-9]{3}-[a-z0-9-]+\\.md$"
```

### Path-policy fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `rulesets[].name` | string | _(required)_ | Ruleset identifier |
| `rulesets[].description` | string | _(none)_ | Fallback description for rules without their own |
| `rulesets[].rules[].pattern` | string | _(required)_ | Glob pattern (or exact path if `exact: true`) |
| `rulesets[].rules[].category` | string | _(required)_ | See category values below |
| `rulesets[].rules[].priority` | int | `0` | Higher priority wins on conflict |
| `rulesets[].rules[].description` | string | _(none)_ | Reason for the rule |
| `rulesets[].rules[].exact` | bool | `false` | Treat pattern as exact path match instead of glob |
| `rulesets[].rules[].must_match` | string | _(none)_ | Regex that matching filenames must satisfy (STWD-010) |

> **Note:** `path-policy.yaml` also parses a `kind` field on rules, but nothing reads it. It is accepted for backward compatibility and has no effect.

### Category values

| Category | Behavior |
|----------|----------|
| `forbidden` | Files matching this pattern must not exist (STWD-002 error) |
| `required` | Files are expected to exist |
| `reserved` | Reserved for specific purpose |
| `deprecated` | Discouraged, may be removed |
| `discouraged` | Not recommended |
| `recommended` | Suggested but not enforced |
| `optional` | Allowed, no enforcement |
| `ignored` | Excluded from path-policy evaluation |

## Profiles

Profiles provide starting-point defaults. Run `steward init --profile <name>` to scaffold files based on a profile.

At runtime, profile defaults merge in shallowly: your `policy.yaml` scalar/object values override profile values, and your list sections (like `artifacts:`) replace the profile list entirely.

| Profile | Repository type | Default artifacts | Section threshold | start_here |
|---------|----------------|-------------------|-------------------|------------|
| `software` | `software` | README.md (required), LICENSE (required), CHANGELOG.md, CONTRIBUTING.md | 500 lines | `[README.md]` |
| `docs` | `documentation` | README.md (required), docs/ (required) | 300 lines | `[README.md]` |
| `minimal` | `general` | README.md (`importance: optional`) | 500 lines | _(none)_ |
| `mixed` | `mixed` | README.md (required), docs/ | 500 lines | `[README.md]` |
| `knowledge` | `knowledge` | README.md (required) | 1000 lines | `[README.md]` |

> **Note:** `mixed` and `knowledge` profiles are defined internally but not yet offered via `steward init`. Only `software`, `docs`, and `minimal` are available for scaffolding.
> **Importance resolution:** The `minimal` profile sets `importance: optional` explicitly on README.md. Without this explicit override, the `authoritative` role default would make the artifact required. If your repo `policy.yaml` declares the same artifact without an explicit `importance:` field, role defaults apply. See [importance precedence](#artifact-fields) for the full resolution chain.

## Configuration precedence

Settings are resolved in this order (highest to lowest):

1. Explicit CLI flag (e.g., `--output json`)
2. `config.yaml` setting (e.g., `output.format: json`)
3. Profile defaults (when `profile:` is set in config.yaml)
4. Built-in defaults

## Configuration commands

| Command | Purpose |
|---------|---------|
| `steward config validate` | Check YAML syntax and semantic references (rule IDs, maintainer types, glob patterns) |
| `steward config doctor` | Detect valid-but-ineffective config: dead start_here entries, unmatched patterns, unreachable families |
| `steward config show --effective` | Print the resolved runtime defaults plus merged effective policy |
| `steward config suggest` | Analyze the repository and suggest artifact declarations for policy.yaml |

Run `config validate` after every policy change. Run `config doctor` periodically to catch silent problems like `start_here` entries pointing to missing files or family patterns matching nothing.

## Validation rules quick reference

| Rule | Default | Category | Description | Auto-fix |
|------|---------|----------|-------------|----------|
| STWD-001 | error | path-policy | Required artifacts must exist | No |
| STWD-002 | error | path-policy | Forbidden path patterns must not match | No |
| STWD-003 | error | frontmatter | Required frontmatter fields must be present | Yes |
| STWD-004 | info | governance | Sections should not exceed the configured size threshold | No |
| STWD-005 | error | structure | Managed region markers must be well-formed | No |
| STWD-006 | warning | managed-region | Managed regions should not be empty once declared | No |
| STWD-007 | warning | stale-artifact | Maintained artifacts must match expected state | Yes |
| STWD-008 | warning | broken-link | Internal Markdown links should resolve | No |
| STWD-009 | warning | broken-reference | Policy-declared artifact paths should resolve to existing files | No |
| STWD-010 | warning | path-policy | Files in governed directories must match declared naming conventions | No |
| STWD-011 | warning | index-completeness | All Markdown files in indexed directories should be linked from the index | No |
| STWD-012 | warning | freshness | State documents with freshness declarations should be updated within window | Yes |
| STWD-013 | info | discoverability | Markdown files should be reachable from at least one navigation surface | No |
| STWD-014 | warning | structure | Files in an artifact family must contain all required section headings | No |
| STWD-015 | warning | family-completeness | Artifact families with min_count must meet the declared minimum | No |
| STWD-016 | warning | naming | Files matched by an artifact family must satisfy the family's naming_pattern | No |
| STWD-017 | warning | structure | Heading text must be unique within a Markdown file after anchor-style normalization | No |
| STWD-018 | warning | broken-fragment-anchor | Markdown fragment links should reference headings that actually exist in the target file | Yes |
| STWD-019 | warning | family-title-pattern | Artifact-family H1 titles should match the declared title pattern | No |
| STWD-020 | warning | family-section-pattern | Artifact-family H2 headings should match the declared section pattern | No |
| STWD-021 | warning | family-section-schema | Artifact-family H2 sections should satisfy the declared document schema | No |

Four rules support auto-fix via `steward check --fix --apply`:

- **STWD-003** — adds missing frontmatter fields with placeholder values
- **STWD-007** — regenerates stale maintained artifacts
- **STWD-012** — updates the `last_updated` frontmatter date
- **STWD-018** — repairs fragment links when a single unambiguous heading match exists
