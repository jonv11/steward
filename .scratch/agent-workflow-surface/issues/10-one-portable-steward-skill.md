# 10 — One portable Steward skill

Status: done
Type: task
Category: enhancement
Blocked by: None — can start immediately
Map: `.scratch/agent-workflow-surface/map.md`

## What to build

Four portable skills (`steward-cli` routing to `steward-cli-maintainer`, `-contributor` and
`-agent`) repeated the guides in `docs/guide/` and each other: install, exit codes and the
auto-fix list appeared up to four times, and the agent skill still showed the pre-fix `filePath`
diagnostic field. An agent had to load two skills to do one task. Fold them into one thin skill
that says when to use which command and what not to do, and routes to `--help`, `explain`,
`schema` and the guides for the rest.

## Acceptance criteria

- [x] `.agents/skills/` holds `steward-cli`, `setup-steward` and `steward-self-cli` only.
- [x] Every guardrail the persona skills carried is in the merged skill or a guide.
- [x] No live document links to a removed skill.
- [x] The CHANGELOG `[Unreleased]` section tells adopters to replace the copied persona skills.

## Answer

`steward-cli` is one 86-line skill: install, live contract commands, the loop, JSON/SARIF and
`md` editing, and the guardrails, with absolute links to the four guides so it still works when
copied into another repository. AGENTS.md, `setup-steward` and `steward-self-cli` now point at it.
Historical reviews keep their old names as point-in-time evidence.
