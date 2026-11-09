# 02 — Family matching and validation on non-YAML metadata

Status: needs-triage
Type: task
Category: enhancement
Blocked by: None — can start immediately
Map: `.scratch/policy-expressiveness/map.md`

## What to build

`match.frontmatter` is the only content-based discriminator. Repositories that record
`Type:`/`Status:` as body lines rather than YAML cannot split one glob into two families.
Both trial repositories hit this and dropped otherwise-justified section schemas because of
it, and this repository's [`.scratch` tracker](../../../docs/agents/issue-tracker.md) cannot
validate its ticket header fields or require `## Acceptance criteria` of `task` tickets only.

## Acceptance criteria

- [ ] A family can match on a `Key: value` body line directly under the H1, alongside `match.frontmatter`.
- [ ] The allowed values of such a line can be constrained, so a ticket's `Status:`, `Type:` and `Category:` vocabularies are enforceable.
- [ ] This repository's `ticket` family uses the feature for its header fields and `Type:`-specific section schemas.
- [ ] The configuration reference documents the feature, and tests cover it.
