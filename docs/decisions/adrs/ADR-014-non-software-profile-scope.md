---
type: adr
status: Accepted
category: Product / Configuration
date: 2026-04-16
---

# ADR-014: Non-Software Profile Scope for First Public Release

---

## Context

Steward ships five built-in profiles via `init --profile`: `software`, `docs`, `mixed`, `knowledge`, and `minimal`. Pre-release blocker B5 requires an explicit release decision on which non-software profiles to keep publicly offered.

The [Profile Readiness Review — 2026-04-16](../../audits/profile-readiness-review-2026-04-16.md) evaluated each profile against a command-level release checklist (`init`, `config validate`, `config show --effective`, `status`, `orient`, `check`, `config doctor`) using fixture-backed CLI coverage. The key observations were:

| Profile | Effective contract | Archetype distinctiveness |
|---------|--------------------|---------------------------|
| `software` | README + LICENSE required, CHANGELOG + CONTRIBUTING recommended | Strong — clear opinionated set |
| `docs` | README + `docs/` directory required | Strong — meaningfully documentation-specific |
| `mixed` | README required, `docs/` optional | Weak — collapses to "README only" in practice |
| `knowledge` | README required | Weak — indistinguishable from a generic repo |
| `minimal` | README optional (but `authoritative` role still triggers status reporting) | Moderate — intentionally lightweight, but semantics need clarity |

## Decision

### Keep: `software`, `docs`, `minimal`

- **`software`** is the primary profile. No change.
- **`docs`** is the strongest non-software candidate. Its contract (README + `docs/` required, lower section threshold) is meaningfully archetype-specific.
- **`minimal`** is kept with documented semantics: it is a README-first baseline for repositories that want governance awareness without opinionated structure. README is not required (`Required = false`) but is still reported in `status` due to its `authoritative` role. This is the intended behavior.

### Defer: `mixed`, `knowledge`

- **`mixed`** is deferred from the public `init --profile` offering. Its effective contract (README required, `docs/` optional) does not produce meaningfully distinct governance behavior compared to `software` minus the license requirement. The profile definition remains in `ProfileDefaults.cs` and can be restored when its contract is enriched in a future milestone.
- **`knowledge`** is deferred for the same reason. Its effective contract (README required) is indistinguishable from a generic repository with no archetype-specific governance value.

Deferral means:

1. `init --profile mixed` and `init --profile knowledge` are removed from the advertised profile list.
2. The profile definitions remain in code for existing users who may have already scaffolded with them — `config validate` and `check` continue to work.
3. These profiles are candidates for re-introduction when richer archetype-specific defaults (e.g., knowledge-specific structure rules, mixed-repo boundary detection) are designed.

### Rationale

The decision rule from the Profile Readiness Review: *Keep a profile enabled only if the command checklist has representative evidence and the resulting policy feels meaningfully archetype-specific.* `mixed` and `knowledge` fail the distinctiveness criterion despite having fixture-backed execution evidence.

## Consequences

- The `init --profile` help text and advertised set narrows to `software`, `docs`, `minimal`.
- `ProfileDefaults.cs` retains all five profiles for backward compatibility; only the advertised init set changes.
- B5 can be marked as resolved once this ADR is accepted and the init-time filtering is implemented.
- Future work to enrich `mixed` and `knowledge` should reference this ADR and demonstrate improved archetype distinctiveness before re-enabling.

## Alternatives Considered

1. **Keep all five profiles.** Rejected — shipping profiles that collapse to "README only" overpromises archetype specialization.
2. **Remove `mixed` and `knowledge` entirely from code.** Rejected — breaks backward compatibility for any existing users.
3. **Defer `minimal` as well.** Rejected — `minimal` has a distinct purpose (lightest governance touch) even if its contract is small.
