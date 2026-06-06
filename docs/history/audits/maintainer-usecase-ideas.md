---
type: audit
status: Historical
standalone: true
---
For this use case, I would treat it as a first-class Steward target, not an edge case. The PRD explicitly says Steward should support knowledge, lore, story, and creative repositories, and the product is supposed to validate, orient, search, and maintain governed artifacts rather than only lint them. 

The main maintainer improvement items I would want to enforce are these.

## 1. A dedicated story/worldbuilding profile

`knowledge` is a decent starting point, but this repo shape is specific enough that I would want either a built-in `story` / `worldbuilding` profile or at least an official documented overlay on top of `knowledge`. Steward already supports profile-based defaults with repository-local override, and policy is meant to be the contract.

That profile should encode:

* canon/story/adaptation separation
* typed artifact families
* stable IDs
* required indexes
* continuity-oriented checks

## 2. Hard boundary enforcement between canon, story, and adaptation

This is the single highest-value rule family for this repo.

I would enforce that:

* `docs/canon/**` contains universe truth artifacts only
* `docs/story/**` contains narrative execution artifacts only
* `docs/adaptation/**` contains transformation artifacts only
* adaptation files cannot become the source of canon truth
* chapter prose cannot silently become the only source for a canonical fact

This fits Steward’s policy-driven artifact-role model and its repo-contract approach.

## 3. Typed artifact taxonomy must be explicit

I would require every governed artifact to belong to a declared type family:

* character
* location
* faction
* item
* event
* mystery
* plot-thread
* arc
* chapter
* timeline-entry
* adaptation-plan
* visual-reference

Steward already wants artifact roles, explicit classification, and structured governance. 

## 4. Stable ID enforcement everywhere

I would make stable IDs mandatory for all canon and story-control artifacts.

Examples:

* `CHR-0001`
* `LOC-0003`
* `FAC-0002`
* `EVT-0010`
* `PLT-0004`
* `ARC-0001`
* `CH-0007`

And I would enforce:

* ID uniqueness repo-wide
* prefix matches folder/type
* file name starts with the ID
* frontmatter ID matches file name ID

This is exactly the kind of deterministic path/content contract Steward is supposed to validate. 

## 5. Strong filename contract

I would enforce `<id>-<slug>.md` for governed Markdown artifacts.

Also:

* slug required
* lowercase kebab-case slug
* no casual filenames like `main hero final.md`
* no renamed title drift without slug normalization warning

This belongs in `path-policy.yaml`, which already exists specifically for ruleset-based path and filename enforcement.

## 6. Frontmatter schemas per artifact type

This repo absolutely needs artifact-type-aware frontmatter contracts.

At minimum, I would enforce:

* `id`
* `title`
* `type`
* `status`
* `summary`
* `tags`

Then type-specific required fields, for example:

* character: `appears_in`, `related`, `continuity_level`
* chapter: `arc`, `pov`, `setting`, `characters_present`
* plot-thread: `current_state`, `stakes`, `resolution_conditions`
* event: `era`, `timeline_position`

Steward already defines frontmatter validation and type-aware expectations as a core requirement. 

## 7. Controlled vocabularies, not free-text drift

I would not allow important governance fields to be arbitrary strings.

Examples that should come from controlled vocabularies:

* `type`
* `status`
* `canon_status`
* `draft_status`
* `continuity_level`
* `adaptation_priority`
* `risk_level`

Without this, creative repos degrade quickly into synonym chaos. Steward’s policy model is designed for repository-specific terminology and controlled governance.

## 8. Index coverage enforcement

For this repo type, indexes are not optional polish. They are core discoverability infrastructure.

I would enforce that:

* every canon artifact appears in its typed index
* every plot thread appears in `plot-thread-index.md`
* every timeline event appears in a timeline index
* every chapter appears under its arc and in global chapter inventory
* adaptation artifacts appear in adaptation indexes

And I would want these indexes to be maintained deterministically by `steward maintain`, not by hand. Steward’s maintenance model already covers indexes, registries, catalogs, glossaries, and stale-artifact detection.

## 9. Orphan detection

I would add repo-specific orphan rules such as:

* canon artifact with zero inbound references
* plot thread never referenced by any arc/chapter
* chapter with no canon links
* location introduced but never used
* mystery marked open but no dependent artifact exists
* adaptation plan with no source story mapping

This is one of the most useful rule families for long-running narrative repos.

## 10. Cross-reference integrity must be first-class

I would treat frontmatter references and internal links as equally important.

Enforce that:

* all `related`, `appearsIn`, `dependsOn`, `resolvedBy` references resolve
* deprecated or retconned artifacts warn when referenced
* missing targets are errors for canon/story-control files
* broken Markdown links are validated too

Broken-reference validation is already part of Steward’s intended contract surface. 

## 11. Continuity-specific validation rules

This repo needs rules that go beyond generic document governance.

I would want custom checks for:

* impossible timeline ordering
* character appears in scene before introduction event
* dead/absent character referenced as present without explicit exception
* location state mismatch across arcs
* power-system contradiction markers
* plot thread closed before prerequisite event
* adaptation artifact asserting a canon fact not present in canon layer

These are repo-semantic rules, which fits the policy-first design much better than hardcoded global logic.

## 12. State-tracking artifacts should be explicit

I would formalize stateful continuity documents, for example:

* character state tracker
* relationship evolution tracker
* location state tracker
* unresolved questions tracker
* retcon log

Steward explicitly supports memory/state-oriented artifacts and keeping them discoverable and governable.

## 13. Managed regions for generated sections only

I would enforce mixed ownership very carefully.

Good candidates for managed sections:

* generated tables in indexes
* backlink sections
* “appears in” registries
* structure overviews
* glossary rollups

But I would keep prose, notes, and interpretation manual. Steward’s maintenance model already says managed regions are the safe place for mutation and that user-authored content outside those regions must be preserved.

## 14. Better orientation for new contributors and agents

For this repo type, `steward orient` should be especially strong.

I would require policy to define:

* start-here entry points
* authoritative roots
* memory/state docs
* current active arc
* continuity-critical indexes
* adaptation-critical entry docs

That aligns directly with the orient surface and its emphasis on highlighted entry points and important roots. 

## 15. Search scopes by narrative role

I would want search scopes like:

* canon
* story
* adaptation
* continuity
* timeline
* production

Steward already intends search scoping by area/role, which is ideal for this repository kind. 

## 16. Large-file and split guidance should be stronger here

Story repos easily accumulate giant files.

I would want warnings for:

* oversized chapter files
* oversized character bibles
* giant timeline files
* overgrown adaptation plans
* documents with too many headings at one level

Steward already plans outline and large-document introspection for exactly this sort of discoverability and maintainability problem. 

## 17. Asset hygiene and exclusion policy

Because this repo contains maps, concept art, references, and lettering, I would enforce clean discovery boundaries:

* generated exports ignored
* thumbnails/cache/temp art ignored
* huge raw binaries excluded from orient/search noise
* assets folder conventions enforced
* reference dumps kept out of main guidance surfaces

This matches Steward’s core `.gitignore` and exclusion behavior.

## 18. Completion policy must be repo-specific

For this repo, “done” should not just mean “files exist.”

I would define completion rules like:

* no stale typed indexes
* no broken canon references
* no duplicate IDs
* all governed files have valid frontmatter
* all open plot threads have an explicit status
* all chapters in an active arc have required canon links
* no unresolved retcon conflicts above threshold

Steward explicitly supports configurable completion policy rather than hardcoded definitions of done.

## 19. A dedicated realistic fixture repo should exist in Steward tests

From a product-maintainer perspective, this use case should not stay theoretical.

I would add a first-class test fixture repo for:

* lore/worldbuilding repo
* novel-to-comics adaptation repo
* mixed canon/story/adaptation structure
* continuity edge cases
* stale index and broken thread cases

The test strategy already expects realistic fixture repositories and deterministic integration coverage. 

## 20. First-class explainability for creative-repo failures

This repo type will generate many “why is this invalid?” moments.

I would want `steward explain` and diagnostics to be especially clear for rules like:

* “chapter is missing canon links”
* “plot thread exists but is not indexed”
* “adaptation note asserts undeclared canon fact”
* “event chronology conflicts with master timeline”

That is consistent with the product’s explainability and remediation goals. 

## Priority order I would use

If I were maintaining Steward for this use case, I would prioritize in this order:

1. canon/story/adaptation boundary enforcement
2. ID + filename + frontmatter contracts
3. index maintenance + stale-artifact checks
4. broken references + orphan detection
5. continuity/state tracking rules
6. story-specific profile + test fixture repo
7. richer search/orient scopes for narrative repositories

The short version is this:

Steward should not just “allow” this repository shape. It should actively make it hard to let canon drift, plot threads get lost, adaptation contaminate source truth, or narrative structure become undiscoverable. That is fully aligned with the current product direction around policy-as-contract, deterministic maintenance, governed Markdown, and explicit orientation/search/maintenance surfaces.
