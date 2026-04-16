# RFC-003: Validation and Diagnostics

- **Status:** Accepted
- **Resolves:** Check behavior, diagnostic schema, severity model, exit codes, remediation, scoping, dry-run

---

## Context

Validation is the core contract-enforcement surface. The requirements demand deterministic, scoped validation with machine-readable and human-readable output, stable exit codes, remediation guidance, and dry-run support.

## Decision

### Validation scopes

| Scope | Trigger | Behavior |
|-------|---------|----------|
| `full` | `--scope full` or default invocation | Evaluates all repository files against policy |
| `changed` | `--scope changed` | Evaluates files changed relative to `HEAD`; falls back to full scope if git metadata is unavailable |
| `staged` | `--scope staged` | Evaluates files in the git staging area |
| `paths` | `--paths file1 file2 dir/` | Evaluates exactly the specified files/directories |

The change set is determined via git integration when available. If git is not available for `changed` or `staged`, full scope is used as a conservative fallback.

### Diagnostic model

Each diagnostic is a structured record:

```json
{
  "ruleId": "STWD-001",
  "severity": "error",
  "category": "path-policy",
  "path": "README.md",
  "line": null,
  "message": "Required artifact is missing: README.md",
  "remediation": "Create a README.md file at the repository root.",
  "source": "path-policy.yaml#rulesets[0].required[0]"
}
```

| Field | Required | Description |
|-------|----------|-------------|
| `ruleId` | Yes | Stable rule identifier |
| `severity` | Yes | `error`, `warn`, `info` |
| `category` | Yes | Functional category (e.g., `path-policy`, `frontmatter`, `managed-scope`, `stale-artifact`) |
| `path` | Yes | File or directory path relative to repo root |
| `line` | No | Line number when applicable |
| `message` | Yes | Human-readable description |
| `remediation` | No | Suggested fix or next action |
| `source` | No | Policy source reference for explainability |

### Severity levels

| Severity | Meaning | Affects exit code |
|----------|---------|-------------------|
| `error` | Policy violation; must be fixed | Yes (exit 1) |
| `warning` | Potential issue; recommended fix | No |
| `info` | Informational observation | No |

### Validation categories

| Category | What it checks |
|----------|---------------|
| `path-policy` | Path and filename rules (required, forbidden, naming) |
| `frontmatter` | Frontmatter field presence, type, value |
| `managed-scope` | Managed region integrity and ownership |
| `stale-artifact` | Generated or maintained artifacts that are out of date |
| `broken-reference` | Internal links/references that do not resolve |
| `structure` | Heading structure, section requirements in governed docs |
| `completion-policy` | Workflow completeness rules |

### Output formats

**Human-readable (text):**
```
ERROR  path-policy  README.md
  Required artifact is missing: README.md
  → Create a README.md file at the repository root.

WARN   frontmatter  docs/PRD.md:1
  Missing required frontmatter field: status
  → Add 'status' field to the YAML frontmatter block.

✓ 42 files checked, 1 error, 1 warning
```

**Machine-readable (JSON):**
```json
{
  "summary": {
    "scope": "full",
    "filesChecked": 42,
    "errors": 1,
    "warnings": 1,
    "infos": 0,
    "pass": false
  },
  "diagnostics": [
    { "ruleId": "...", "severity": "error", ... },
    { "ruleId": "...", "severity": "warn", ... }
  ]
}
```

### Dry-run and fix

- `steward check --dry-run`: Reports what `--fix` would change without modifying files.
- `steward check --fix`: Applies deterministic auto-fixes (e.g., missing frontmatter fields, stale indexes).
- `--fix` only applies fixes for rules that have deterministic remediations. Non-deterministic issues are reported with guidance.
- `--fix --dry-run` shows planned fixes without applying.

### Completion policy

Completion-policy rules are evaluated as part of `steward check` and reported as `completion-policy` category diagnostics. These rules answer "is the work done?" per the repository's definition of done.

Examples:
- STWD-001 (`required artifact(s) missing`)
- STWD-007 (`maintained artifact(s) stale`)
- STWD-008 (`broken internal link(s)`)
- STWD-009 (`broken artifact reference(s) in policy`)

### Secret filtering

Before emitting diagnostics, the output pipeline strips:
- Content matching common secret patterns (API keys, tokens, passwords)
- File content snippets from paths matching configured sensitive-path patterns
- Environment variable values

This is a best-effort safety net, not a substitute for proper secret management.

## Alternatives considered

1. **SARIF format for diagnostics:** Considered but rejected for v1.0.0—SARIF is complex and primarily designed for static analysis tools. Our JSON schema is simpler and sufficient. SARIF export could be added later.
2. **Treating warnings as errors by default:** Rejected—warnings should be advisory. A `--strict` flag may be added later to promote warnings to errors.
3. **Validation only on explicit request:** Rejected—`steward check` with a sensible default scope provides immediate value.

## Consequences

- One consistent diagnostic model across all validation rules.
- Machine-readable and human-readable output from the same data.
- Exit codes are stable and unambiguous.
- Remediation guidance reduces the gap between detecting and fixing issues.
- Dry-run provides safety before mutation.
