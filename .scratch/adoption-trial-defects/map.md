# Map — Adoption-trial and setup defects

## Notes

Open defects found during the 2026-08-24 adoption trial on two external repositories
(`jvcode`, `mdrule`) and while building the `setup-steward` skill. The remaining defects
from that trial have all been fixed; see the [CHANGELOG](../../CHANGELOG.md) for what
changed, and the [audit](../../docs/history/audits/maintainer-configuration-experience-audit-2026-08-24.md)
for the evidence.

Tickets 06–11 come from adopting steward 0.18.1 in `mpes` (a Unity project with 37 Markdown
documents) on 2026-11-14: a repository-wide front-matter contract, artifact families keyed on a
`kind` field and a generated `directory-index`. Each ticket names its reproduction.

## Decisions so far

- Migrated from the project backlog on 2026-11-09; see the [backlog snapshot](../../docs/history/plans/backlog-2026-11-09.md).
- Relative `--config` paths resolve against the current directory everywhere, including `init` — tickets 02 and 03.

## Fog

- Whether `config doctor` should distinguish "declared but not yet populated" from "wrong" by category, or by an explicit opt-in on the declaration — ticket 01.
