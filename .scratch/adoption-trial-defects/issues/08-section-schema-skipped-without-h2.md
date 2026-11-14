# 08 — Required sections are not reported for a file with no H2 headings

Status: needs-triage
Type: task
Category: bug
Blocked by: None — can start immediately
Map: `.scratch/adoption-trial-defects/map.md`

## What to build

A family with `section_schema` requiring `Sources` (`heading_match: exact`) reports STWD-021 "Required section 'Sources' is missing" for a matching file whose H2s are `## Findings`, but reports **nothing** for a matching file with only an H1 and a paragraph. The emptier document passes the stricter rule. Found on 2026-11-14 in `mpes` while testing its `research` family with deliberately bad files (steward 0.18.1). `required_sections` (STWD-014) should be checked for the same gap.

## Acceptance criteria

- [ ] A matched file with no H2 headings gets STWD-021 for each required section.
- [ ] The same holds for STWD-014 `required_sections`.
- [ ] Tests cover a file with no H2s, one with unrelated H2s and one that conforms.
