# 06 — A front-matter value cannot be required to name something that exists

Status: needs-triage
Type: task
Category: enhancement
Blocked by: None — can start immediately
Map: `.scratch/policy-expressiveness/map.md`

## What to build

Agent-ready repositories cross-reference by identifier: an engineering issue's front matter says `effort: helper-resume` (a tracker Effort at `.scratch/helper-resume/effort.md`), and code and documents cite decisions as `ADR-22` (a `## ADR-22 — …` heading in one decision log). When the target is renamed or never existed, nothing fails. `mpes` (2026-11-14) wrote a 30-line shell script run as a separate gate to check both, which a policy should be able to say.

Two shapes cover what was needed:

- a frontmatter field whose value must resolve to a path template, e.g. `effort` → `.scratch/{value}/effort.md`;
- an identifier pattern in Markdown text whose number must match a heading in a named file, e.g. `ADR-([0-9]+)` → an H2 of `docs/poc/DECISIONS.md` matching `^ADR-{1} — .+` (source code citations would stay out of scope, since steward does not read code).

## Acceptance criteria

- [ ] A family or `frontmatter_requirements` entry can declare that a field's value must resolve to an existing path built from a template, with a diagnostic naming the field, the value and the expected path.
- [ ] A policy can declare an identifier pattern whose matches in Markdown must resolve to a heading in a named file.
- [ ] `config validate` rejects a template or pattern it cannot evaluate.
