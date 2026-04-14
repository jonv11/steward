# RFC-001: CLI Command Structure

- **Status:** Accepted
- **Resolves:** Product questions about command hierarchy, naming, global options, and CLI UX conventions

---

## Context

The requirements specify many capabilities (check, orient, search, outline, markdown ops, maintain, explain, status, config validation) but do not prescribe a specific command hierarchy. A clear, consistent, and memorable command structure is essential for both human usability and agent operability.

## Decision

### Top-level commands

| Command | Purpose | Milestone |
|---------|---------|-----------|
| `steward check` | Canonical workflow entry point: scoped validation, impact analysis, completion policy | v0.4.0 |
| `steward orient` | Session-start orientation: curated repository map | v0.2.0 |
| `steward outline` | Rich tree/outline with sizes, line counts, heading hierarchy | v0.2.0 |
| `steward search` | Repository-wide content and heading search | v0.5.0 |
| `steward md` | Markdown structural operations (subcommands: query, edit, outline) | v0.4.0 |
| `steward maintain` | Deterministic maintenance of governed artifacts | v0.8.0 |
| `steward status` | Lightweight current-state surface (no full validation) | v0.9.0 |
| `steward explain` | Explain a rule, artifact role, or failure | v0.9.0 |
| `steward config` | Config/policy operations (subcommand: validate, show) | v0.3.0 |
| `steward init` | Scaffold initial `.steward/` configuration | v0.3.0 |
| `steward version` | Print version and runtime info | v0.1.0 |

### Subcommand structure

```
steward
├── check [--scope full|changed|staged] [--paths <path>...] [--output json|text] [--fix] [--dry-run]
├── orient [--depth <n>] [--output json|text] [--signals]
├── outline [<path>] [--depth <n>] [--sizes] [--lines] [--headings] [--output json|text]
├── search <query> [--mode content|headings|all] [--scope <area>] [--max <n>] [--output json|text]
├── md
│   ├── query <file> <selector> [--output json|text]
│   ├── edit <file> <operation> [--preview] [--apply]
│   └── outline <file> [--output json|text]
├── maintain [--scope <artifact>...] [--preview] [--apply] [--output json|text]
├── status [--output json|text]
├── explain <rule-id|artifact>
├── config
│   ├── validate
│   └── show [--effective] [--output json|text]
├── init [--profile <name>]
└── version
```

### Global options

| Option | Short | Default | Description |
|--------|-------|---------|-------------|
| `--output` | `-o` | `text` | Output format: `text` or `json` |
| `--verbosity` | `-v` | `normal` | Verbosity: `quiet`, `normal`, `verbose`, `debug` |
| `--no-color` | | `false` | Disable colored output |
| `--config` | `-c` | auto-detect | Path to config directory |

### Naming conventions

- Commands use lowercase single words where possible (`check`, `orient`, `search`).
- Subcommands under `md` use verbs (`query`, `edit`, `outline`).
- Options use `--kebab-case`.
- Boolean flags have no value: `--dry-run`, `--no-color`, `--sizes`.
- Enum options use lowercase values: `--scope full`, `--output json`.

### Exit code scheme

| Code | Meaning |
|------|---------|
| 0 | Success / clean pass |
| 1 | Validation failure (policy violations found) |
| 2 | Usage or configuration error |
| 3 | Runtime / internal error |

### stdout / stderr contract

- **stdout:** Command output (diagnostics, results, maps, query output). Stable for piping and parsing.
- **stderr:** Progress messages, verbose/debug logging, warnings about tool behavior. Not parsed by automation.

## Alternatives considered

1. **Single `steward` command with flags only:** Rejected—the surface area is too broad for a flat flag model.
2. **Deeply nested subcommands (e.g., `steward repo orient`):** Rejected—adds unnecessary depth for no benefit.
3. **Abbreviated command names (e.g., `chk`, `mnt`):** Rejected—clarity over brevity for a stewardship tool.

## Consequences

- Clear, discoverable command hierarchy for both humans and agents.
- Each command has a single responsibility.
- Global options are consistent across all commands.
- Exit codes are stable and machine-parseable.
- Future commands can be added without restructuring.
