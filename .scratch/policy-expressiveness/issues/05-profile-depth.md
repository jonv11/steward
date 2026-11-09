# 05 — Profile depth and additional init profiles

Status: needs-triage
Type: research
Category: enhancement
Blocked by: None — can start immediately
Map: `.scratch/policy-expressiveness/map.md`

## The question

All 5 profiles (`software`, `docs`, `minimal` public; `mixed`, `knowledge` deferred per
ADR-014) only gate file presence and required/optional status today — no archetype-specific
behaviour beyond that, so most feel like "require a README" with extra steps. What richer,
more differentiated defaults should each profile carry before `mixed`/`knowledge` are
re-enabled or the public set grows?

## Acceptance criteria

- [ ] A recommendation lists the differentiated defaults per public profile.
- [ ] It states the evidence threshold for re-enabling `mixed` and `knowledge` under ADR-014.
- [ ] Follow-up task tickets exist for the accepted defaults.
