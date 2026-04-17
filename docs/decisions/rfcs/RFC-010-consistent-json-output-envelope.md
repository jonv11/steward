---
type: rfc
status: Draft
resolves: >-
  Machine-readable output consistency across CLI commands without destabilizing current consumers
last_updated: 2026-04-17
---

# RFC-010: Consistent JSON Output Envelope

---

## Context

Steward already has meaningful JSON output across its highest-value command surfaces, and that is one of the product's strongest current assets for automation and AI-assisted use.

The problem is not lack of JSON. The problem is that each command still speaks its own top-level dialect.

Current examples from the shipped CLI:

- `status --output json` returns a repository-status object directly
- `search --output json` returns query and match data directly
- `md query --output json` returns selector and match data directly
- `refs --output json` returns path plus inbound and outbound arrays directly
- `check --output json` returns a deeper response with `summary`, `completion`, and `diagnostics`

That is workable for one-off command use, but weak for generic consumers that want one product-level contract.

## Problem Statement

Today there is no shared JSON envelope for:

- command identity
- schema versioning
- exit/success metadata
- future cross-command fields such as typed resource addresses

As a result:

1. generic agent code must special-case each command from the top level down
2. future output improvements risk increasing divergence rather than reducing it
3. documentation cannot point to one coherent machine-facing JSON story

## Goals

1. Introduce one consistent JSON envelope across commands.
2. Preserve current useful payloads rather than redesigning every command body at once.
3. Avoid destabilizing current consumers during the `0.15.x` line.
4. Create one clean place for future machine-facing metadata such as resource addresses.

## Non-Goals

1. Publishing full JSON Schema documents in `v0.15.0`.
2. Switching every consumer immediately to a new default format.
3. Turning CLI output into a streaming or event protocol.

## Decision

Steward should introduce a standard JSON envelope as an additive mode in `v0.15.0`, while preserving the current top-level payloads as the legacy compatibility mode for the rest of the `0.15.x` line.

### New Global Option

Add a global option:

`--json-envelope <legacy|standard>`

Rules:

- default is `legacy` in `v0.15.0`
- valid only when `--output json` is selected
- `standard` enables the new envelope

This keeps the migration explicit and reviewable while still letting new tooling adopt the standard form immediately.

### Standard Envelope Shape

The standard envelope is:

```json
{
  "schemaVersion": "steward-json/v1",
  "command": "status",
  "toolVersion": "0.14.0",
  "success": true,
  "exitCode": 0,
  "data": {}
}
```

Field meanings:

- `schemaVersion`: version of the envelope contract, not the command payload semantics
- `command`: the invoked command family, for example `status` or `md query`
- `toolVersion`: current Steward version
- `success`: whether the command completed successfully from the process perspective
- `exitCode`: numeric CLI exit code
- `data`: the existing command-specific payload

No timestamp field is included. Volatile timestamps add noise, complicate deterministic testing, and are unnecessary for the primary automation use cases.

### `check` Semantics

`check` needs an explicit distinction between transport success and validation pass:

- top-level `success` reflects the command exit result
- validation pass remains inside `data.summary.pass`

This lets consumers distinguish "the command ran and found policy failures" from "the command itself failed to execute correctly."

## v0.15.0 Scope

The following JSON-producing surfaces should support the standard envelope in `v0.15.0`:

- `check`
- `status`
- `orient`
- `search`
- `md query`
- `md outline`
- `refs`
- `maintain`
- `config validate`
- `config doctor`
- `config suggest`
- `config show --effective`
- `explain`

This looks broad, but the work is intentionally shallow for most commands because the command-specific payload stays where it is and moves under `data`.

## Migration Strategy

### `v0.15.0`

- ship `--json-envelope standard`
- keep `legacy` as the default
- add contract tests for both legacy and standard modes on the highest-value commands

### Later Pre-1.0

- decide whether `standard` should become the default before `v1.0.0`
- if so, keep `legacy` available for at least one more pre-1.0 milestone with explicit deprecation messaging in docs

## Implementation Notes

Add a shared envelope writer/helper in the CLI layer so commands stop constructing JSON top levels ad hoc.

Suggested helper contract:

```csharp
WriteJsonEnvelope(commandName, success, exitCode, data);
```

That helper should:

- read the current tool version once
- write the standard envelope when requested
- otherwise write the existing payload exactly as legacy mode expects

## Consequences

### Positive

- one machine-facing product contract becomes documentable
- future metadata additions have one consistent home
- address work and split/extract planning can enrich payloads cleanly
- existing consumers do not need to move immediately

### Negative

- every JSON-producing command needs at least some touch
- tests must cover both legacy and standard modes during the transition

## Explicitly Deferred

- full JSON Schema publication
- NDJSON or streaming output
- a cross-command RPC protocol

