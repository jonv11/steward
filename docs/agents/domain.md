# Domain Docs

> Part of the [Agent skills](../../AGENTS.md#agent-skills) configuration for this repository.

How the engineering skills should consume this repo's domain documentation when exploring the codebase. This repo is **single-context**: one `CONTEXT.md` at the root, with all decision records under `docs/decisions/`.

## Before exploring, read these

- **`CONTEXT.md`** at the repo root, if it exists.
- **`docs/decisions/adrs/`**: read ADRs that touch the area you're about to work in.
- **`docs/decisions/rfcs/`**: read the RFC when an ADR references one, or when the area is still under proposal.

Note that these decision paths differ from the skill defaults (`docs/adr/`). Use the paths above.

If any of these files don't exist, **proceed silently**. Don't flag their absence; don't suggest creating them upfront. The `/domain-modeling` skill (reached via `/grill-with-docs` and `/improve-codebase-architecture`) creates them lazily when terms or decisions actually get resolved.

## File structure

```
/
├── CONTEXT.md              ← created lazily by /domain-modeling
├── docs/decisions/
│   ├── README.md           ← generated index; never hand-edit
│   ├── adrs/ADR-NNN-*.md   ← governed: see .steward/policy.yaml (adr family)
│   └── rfcs/RFC-NNN-*.md   ← governed: see .steward/policy.yaml (rfc family)
└── src/
```

## Writing a new decision record here

Decision records in this repo are governed artifacts, not free-form markdown. A new ADR must satisfy the `adr` family in `.steward/policy.yaml`:

- filename `ADR-NNN-kebab-case.md` under `docs/decisions/adrs/`, title `ADR-NNN: <Title>`
- frontmatter with `type: adr`, `status`, and `category`; `last_updated` is maintained by steward
- sections `Context`, `Decision`, `Consequences` (an `Alternatives` section is optional)

RFCs follow the parallel `rfc` family. After adding either, run `steward maintain --apply` to refresh the generated index in `docs/decisions/README.md`, then `steward check`.

## Use the glossary's vocabulary

When your output names a domain concept (in an issue title, a refactor proposal, a hypothesis, a test name), use the term as defined in `CONTEXT.md`. Don't drift to synonyms the glossary explicitly avoids.

If the concept you need isn't in the glossary yet, that's a signal: either you're inventing language the project doesn't use (reconsider) or there's a real gap (note it for `/domain-modeling`).

## Flag ADR conflicts

If your output contradicts an existing ADR, surface it explicitly rather than silently overriding:

> _Contradicts ADR-005 (validation engine design), but worth reopening because…_
