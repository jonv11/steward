# 07 — Invalid front-matter YAML is reported as every required field missing

Status: needs-triage
Type: task
Category: bug
Blocked by: None — can start immediately
Map: `.scratch/adoption-trial-defects/map.md`

## What to build

A front matter line `description: Engineering audit: broken yaml` (an unquoted colon followed by a space inside a plain scalar) is invalid YAML. Steward 0.18.1 does not say so:

- `steward check` reports STWD-003 "Required frontmatter field 'description' is missing" — and the same for `kind`, `status` and `updated`, which are present on the following lines.
- `steward maintain` reports "Directory index generation is blocked: 1 source file(s) are missing a non-empty frontmatter.description field".

Both send the maintainer looking for a field that is plainly there. In `mpes` this hit 12 of 31 descriptions written by an agent, and surfaced only because index generation refused to run. Reproduce with any family or `frontmatter_requirements` and that line.

## Acceptance criteria

- [ ] A front-matter block that does not parse produces one diagnostic naming the file, the line and the YAML parser's message (STWD-003 or a dedicated rule), instead of per-field "missing" diagnostics.
- [ ] `maintain` says the block failed to parse rather than "missing description".
- [ ] Tests cover an unquoted colon-space value and an unterminated quote.
