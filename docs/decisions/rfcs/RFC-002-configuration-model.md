# RFC-002: Configuration Model

- **Status:** Accepted
- **Resolves:** Config format, file layout, policy vs. runtime separation, profiles, layering, precedence, excludes

---

## Context

The requirements mandate a configuration model that separates repository semantics (policy) from tool behavior (runtime config), supports profiles and overlays, uses pattern-based rules, and ensures more specific policy overrides broader defaults deterministically.

## Decision

### Configuration directory

All configuration lives in `.steward/` at the repository root.

```
.steward/
├── config.yaml          # Runtime config: tool behavior, output preferences
├── policy.yaml          # Repository contract: artifacts, governance, validation, maintenance
└── path-policy.yaml     # Path and filename policy rulesets
```

### Two-file separation

| File | Purpose | Scope |
|------|---------|-------|
| `config.yaml` | Tool behavior: default output format, verbosity, color preference, discovery excludes | Runtime preferences; does not define repository semantics |
| `policy.yaml` | Repository contract: artifact roles, required artifacts, terminology, frontmatter rules, managed regions, completion policy | Shared semantics; enforced in `check` |

This separation fulfills REQ-CONFIG-002 and REQ-CONFIG-003: policy defines the contract, config controls how the tool runs.

### path-policy.yaml

Path and filename policy is kept in its own file due to its structured ruleset format (REQ-PATHPOL-001 through REQ-PATHPOL-013). It is loaded alongside `policy.yaml` but authored independently to keep individual files focused.

### config.yaml schema (core fields)

```yaml
# .steward/config.yaml
profile: software          # Built-in profile to inherit defaults from
output:
  format: text             # Default output format: text | json
  verbosity: normal        # quiet | normal | verbose | debug
  no_color: false          # Disable ANSI color in text output
discovery:
  exclude:                 # Additional exclude patterns (beyond .gitignore)
    - "**/node_modules/**"
    - "**/bin/**"
    - "**/obj/**"
```

### policy.yaml schema (core fields)

```yaml
# .steward/policy.yaml
repository:
  name: steward
  type: software           # Informational repository classification
  terminology:             # Custom labels
    artifact: document     # Override default term if needed

artifacts:
  - path: README.md
    role: authoritative
    required: true
    description: Project overview
  - path: docs/planning/milestone-plan.md
    role: milestones
    importance: recommended
    description: Pre-1.0 milestone sequencing

governance:
  start_here:              # Entry points for orientation
    - README.md
    - docs/planning-index.md
  frontmatter:
    required_fields: [status]
    auto_fields:
      last_updated: true
  managed_regions:
    marker: "steward:managed"      # HTML comment marker format
    enforce_ownership: true
  completion_policy:
    rules:
      - id: STWD-001
        description: Required artifacts must exist
      - id: STWD-007
        description: Maintained artifacts must not be stale

validation:
  disabled_rules: [STWD-004]
  severity_overrides:
    STWD-008: error
  path_overrides:
    - pattern: "src/**/*.md"
      disabled_rules: [STWD-003]
  frontmatter_requirements:
    - pattern: "docs/decisions/**/*.md"
      required_fields: [status]
```

### Profile system

Profiles are named presets that provide useful default policy and config values. They are opt-in (REQ-CONFIG-007).

**Built-in profiles:**

| Profile | Description |
|---------|-------------|
| `software` | Source code repository with standard structure (README, LICENSE, src/, tests/, docs/) |
| `docs` | Documentation-heavy repository (README, docs/, indexes) |
| `mixed` | Code + docs repository |
| `knowledge` | Content, lore, research, or writing repository |
| `minimal` | Bare minimum—almost no default rules |

A profile is selected in `config.yaml` via `profile: <name>`. Repository-local policy always overrides profile defaults. In the current pre-1.0 baseline, that merge is shallow: repository-local scalar/object values win, while repository-local list sections such as `artifacts:` replace the corresponding profile list as a whole.

### Layering and precedence (most specific wins)

```
1. Built-in defaults (lowest precedence)
2. Profile defaults
3. Repository-local config and policy in `.steward/`
4. Command-line flags (highest precedence for runtime config only)
```

`path-policy.yaml` is evaluated alongside `policy.yaml` for path and naming rules rather than as a generic override layer. CLI flags can override runtime behavior (output format, verbosity) but cannot override policy in enforced mode. This fulfills REQ-CONFIG-003 and REQ-CONFIG-006.

### Exclude rules

Exclude patterns are merged from all layers:
1. .gitignore (always respected)
2. Profile default excludes
3. config.yaml `discovery.exclude`

### Config validation

`steward config validate` checks:
- YAML syntax
- Semantic conformance (profile names, rule ids, maintainer types, glob/regex syntax, `depends_on` links)
- Profile name is valid
- No obviously invalid rule references or maintainer declarations

### Convention-based fallback

When `.steward/` does not exist, the CLI operates in **unconfigured mode** using conservative defaults:
- Treats the repo as `minimal`
- Respects .gitignore
- Provides orientation and search with heuristic artifact detection
- Validation is limited to universal rules only

## Alternatives considered

1. **Single config file:** Rejected—conflates runtime preferences with repository semantics. Violates REQ-CONFIG-002.
2. **TOML instead of YAML:** YAML chosen for broader ecosystem support, frontmatter consistency, and agent familiarity. See ADR-003.
3. **JSON config:** Rejected—no comments, poor human authoring experience.
4. **XDG-style user-level config:** Deferred—repository-scoped config is sufficient for v1.0.0. User-level defaults may be added later.

## Consequences

- Clear separation between what the repo expects (policy) and how the tool behaves (config).
- Profiles reduce initial setup friction.
- Layering is deterministic and documented.
- Path policy has its own file to keep it manageable.
- Unconfigured repos still get basic functionality.
