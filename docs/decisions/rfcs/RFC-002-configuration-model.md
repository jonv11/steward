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
├── policy.yaml          # Repository contract: rules, expectations, governance
├── profiles/            # Optional: profile overlays
│   └── docs-heavy.yaml  # Example profile overlay
└── path-policy.yaml     # Path and filename policy rulesets
```

### Two-file separation

| File | Purpose | Scope |
|------|---------|-------|
| `config.yaml` | Tool behavior: default output format, verbosity, color, feature flags | Runtime preferences; does not define repository semantics |
| `policy.yaml` | Repository contract: artifact roles, required artifacts, terminology, frontmatter rules, managed regions, completion policy | Shared semantics; enforced in `check` |

This separation fulfills REQ-CONFIG-002 and REQ-CONFIG-003: policy defines the contract, config controls how the tool runs.

### path-policy.yaml

Path and filename policy is kept in its own file due to its structured ruleset format (REQ-PATHPOL-001 through REQ-PATHPOL-013). It is referenced from policy.yaml but authored independently to keep individual files focused.

### config.yaml schema (core fields)

```yaml
# .steward/config.yaml
profile: software          # Built-in profile to inherit defaults from
output:
  format: text             # Default output format: text | json
  color: auto              # auto | always | never
  verbosity: normal        # quiet | normal | verbose | debug
discovery:
  exclude:                 # Additional exclude patterns (beyond .gitignore)
    - "**/.DS_Store"
    - "**/Thumbs.db"
    - "**/node_modules/**"
    - "**/bin/**"
    - "**/obj/**"
```

### policy.yaml schema (core fields)

```yaml
# .steward/policy.yaml
repository:
  name: steward
  type: software           # software | docs | mixed | knowledge | structured
  terminology:             # Custom labels
    artifact: document     # Override default term if needed

artifacts:
  roles:                   # Named artifact roles
    readme:
      path: README.md
      required: true
      role: authoritative
    roadmap:
      path: docs/planning/milestone-plan.md
      required: false
      role: workflow
  start_here:              # Entry points for orientation
    - README.md
    - docs/planning-index.md

governance:
  frontmatter:
    required_fields: [status]
    auto_fields:
      last_updated: true
  managed_regions:
    marker: "steward:managed"      # HTML comment marker format
    enforce_ownership: true
  completion_policy:
    rules:
      - id: all-required-present
        description: All required artifacts must exist
      - id: no-stale-indexes
        description: No governed index is stale

validation:
  scopes:
    default: changed       # Default scope for `steward check`
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

A profile is selected in `config.yaml` via `profile: <name>`. Repository-local policy always overrides profile defaults.

### Layering and precedence (most specific wins)

```
1. Built-in defaults (lowest precedence)
2. Profile defaults
3. Repository policy (.steward/policy.yaml)
4. Repository path-policy (.steward/path-policy.yaml)
5. Command-line flags (highest precedence for runtime config only)
```

CLI flags can override runtime behavior (output format, verbosity) but cannot override policy in enforced mode. This fulfills REQ-CONFIG-003 and REQ-CONFIG-006.

### Exclude rules

Exclude patterns are merged from all layers:
1. .gitignore (always respected)
2. Profile default excludes
3. config.yaml `discovery.exclude`
4. policy.yaml artifact-specific excludes

### Config validation

`steward config validate` checks:
- YAML syntax
- Schema conformance
- Reference integrity (artifact paths exist)
- Profile name is valid
- No conflicting rules

### Convention-based fallback

When `.steward/` does not exist, the CLI operates in **unconfigured mode** using conservative defaults:
- Treats the repo as `minimal` profile
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
