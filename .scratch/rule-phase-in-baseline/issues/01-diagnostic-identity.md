# 01 — Every diagnostic has a stable, line-independent identity

Status: ready-for-agent
Type: task
Category: enhancement
Blocked by: None — can start immediately
Spec: `.scratch/rule-phase-in-baseline/spec.md`

## What to build

Give each `Diagnostic` an optional `Selector` and add one place in core that computes a
diagnostic's identity — rule ID, repository-relative path and selector — so baseline matching
never has to know about individual rules. When a rule supplies no selector the identity falls
back to the message; rules whose message embeds a line number, age or count supply an explicit
selector instead (heading text, artifact path, family name). This is the deepening the rest of
the effort sits on, and the main risk RFC-017 §9 names.

## Acceptance criteria

- [ ] `Diagnostic` carries an optional `Selector`; existing constructors and JSON output are unchanged apart from the new field.
- [ ] A single core type computes the identity; no other code builds identity strings.
- [ ] Rules whose message embeds a volatile value supply explicit selectors free of line numbers, ages and counts: STWD-004 (section size), STWD-007 (stale artifact), STWD-012 (freshness), STWD-015 (family minimum count), STWD-017 (unique heading), STWD-020 (section pattern) and STWD-021 (section schema).
- [ ] Tests prove the identity is unchanged when the violation moves to another line or the age or count changes, and differs when the violated heading, field or target differs.
