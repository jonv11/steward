Below is the maintainer view I would want for this repo.

I am not treating this CLI as a simple linter. I am treating it as the operational entry point for the repo: the thing that makes the repository self-describing, enforceable, navigable, and safe to evolve over time.

For this kind of story / lore / universe repo, my goal is not only “clean files”. My goal is:

* stable canon
* discoverable artifacts
* explicit workflows
* continuity protection
* deterministic structure
* easy onboarding for humans and agents
* clear next actions
* safe adaptation from prose canon into BD / comics / manga planning

So the CLI must help me define, enforce, inspect, guide, and evolve the repo contract.

## 1. What I want this CLI to do for me

I want the CLI to do all of the following.

1. Make the intended repository structure explicit.
   Rationale: I do not want the repo shape to be implicit tribal knowledge. The CLI must know what belongs where, what each folder means, and what artifact types are allowed.

2. Enforce artifact conventions consistently.
   Rationale: consistency is what makes large story repos searchable and maintainable. Without enforcement, the repo degenerates into loose notes.

3. Treat the repo as a structured knowledge system, not just a folder tree.
   Rationale: this repo contains canon, worldbuilding, timeline, plot threads, draft narrative, and adaptation planning. Those are different artifact classes with different rules.

4. Protect canon integrity.
   Rationale: the most damaging failure mode in story repos is silent contradiction. The CLI should reduce canon drift, orphaned retcons, timeline conflicts, and unresolved dependency changes.

5. Keep everything discoverable.
   Rationale: if I or an agent cannot quickly locate character truth, location truth, plot-thread state, and adaptation implications, the repo is not serving its purpose.

6. Support explicit workflows, not just static validation.
   Rationale: I need guided creation, review, progression, and change management for artifacts.

7. Continuously show me what is incomplete, inconsistent, or next.
   Rationale: I want the CLI to act as a repo steward, not only as a failure reporter.

8. Make changes minimally and deterministically.
   Rationale: generated docs, indexes, and updates must be reviewable in git. No noisy rewrites.

9. Be configurable enough that the repo behavior is encoded in configuration rather than hardcoded product assumptions.
   Rationale: the CLI should adapt to this repo’s story-production model, not force a generic docs tool model.

10. Be equally useful to human maintainers and AI agents.
    Rationale: the repo should become easier to operate through the CLI as the shared interface.

## 2. Core product intent I would require

From the maintainer point of view, I would require the CLI to support these product intentions explicitly.

### 2.1 Repository contract enforcement

The CLI must let me define and validate:

* required top-level files
* required top-level folders
* allowed folder hierarchy
* artifact types per folder
* required indexes
* required templates
* required metadata fields
* allowed statuses
* allowed lifecycle transitions
* reference constraints between artifacts

Rationale: the repo must behave like a controlled system.

### 2.2 Story-universe artifact stewardship

The CLI must understand that this repo contains distinct classes such as:

* canon entities
* world rules
* timeline events
* plot threads
* arcs
* chapters
* scenes or beats
* continuity records
* adaptation artifacts
* production / planning artifacts

Rationale: each of these needs different validation and workflow logic.

### 2.3 Discoverability as a first-class goal

The CLI must help answer questions like:

* what is the canonical source for this character?
* where is this location defined?
* which chapters involve this plot thread?
* what canon changed recently?
* what is still unresolved?
* what artifacts are missing?
* what should be worked on next?

Rationale: discoverability is the point.

### 2.4 Guided progression

The CLI must not only say “invalid”. It must also help with:

* scaffold missing artifacts
* suggest the next valid artifact
* show blockers
* show recommended next steps
* explain why a rule exists
* point to the governing file, config, or template

Rationale: maintainers and agents need operational guidance, not only policing.

## 3. Exact explicit functionalities I would require

## 3.1 Repository initialization and bootstrap

1. `init`
   Must initialize the repo structure according to the configured model.
   It should create:

   * required folders
   * required base documents
   * templates
   * indexes
   * configuration stubs
   * optional example artifacts
     Rationale: the initial contract should be bootstrapped consistently.

2. `adopt`
   Must inspect an existing repo and produce an adoption plan:

   * detected current structure
   * gaps versus desired structure
   * naming issues
   * missing metadata
   * recommended migration steps
     Rationale: many repos already exist and need stewardship retrofitted.

3. `doctor`
   Must assess configuration completeness and repo health.
   Rationale: I want a single entry point to know whether the CLI is properly configured for this repo.

## 3.2 Structural validation

4. `validate-structure`
   Must validate:

   * required paths exist
   * forbidden paths do not exist in scoped locations
   * allowed artifact types match folder targets
   * required child artifacts exist under some parents
   * empty required registries are flagged
     Rationale: structure is the foundation.

5. Rule scoping by target path
   I need rules that apply to:

   * repo root
   * specific folders
   * filename patterns
   * artifact-type groups
   * nested scopes
     Rationale: different areas of the repo require different constraints.

6. Severity-aware validation
   Rules must support:

   * error
   * warning
   * info
   * disabled
     Rationale: not every rule should block.

## 3.3 Naming, IDs, and identity

7. `check-names`
   Must validate filename conventions.
   Rationale: deterministic naming keeps the repo navigable.

8. ID enforcement
   The CLI must enforce:

   * typed IDs
   * uniqueness
   * pattern validity
   * optional sequence policies
   * stable IDs independent of title changes
     Rationale: IDs are the anchor for references and indexing.

9. Slug policy validation
   Must validate slug formatting and optional slug-title consistency.
   Rationale: stable readable file names matter.

10. Rename support
    The CLI should support safe rename workflows:

* preserve ID
* update file name
* optionally update references and index entries
  Rationale: titles evolve; identity should not break.

## 3.4 Metadata and schema validation

11. `validate-metadata`
    Must validate frontmatter/schema by artifact type.
    Rationale: different artifact types need different required fields.

12. Per-type schema definitions
    I need configurable schemas for:

* character
* location
* faction
* item
* event
* mystery
* plot-thread
* arc
* chapter
* scene
* adaptation plan
* backlog item
* decision record
  Rationale: the CLI must know what “complete enough” means for each artifact type.

13. Field typing
    Must validate:

* string
* enum
* boolean
* list
* date
* ID reference
* path reference
* optional nested objects if supported
  Rationale: avoid inconsistent metadata shape.

14. Default value injection
    The CLI should support auto-populating default metadata values.
    Rationale: reduce repetitive manual work and improve consistency.

15. Required-versus-derived fields
    Some fields should be authored, others generated.
    The CLI must distinguish them.
    Rationale: avoid accidental manual edits to generated values.

## 3.5 Content structure validation inside Markdown

16. `validate-sections`
    Must validate required sections within artifacts by type.
    Example:

* characters require Overview, Role In Story, Relationships, Continuity Notes
* chapters require Summary, Key Beats, Continuity Constraints, Links To Canon
  Rationale: consistent internal structure greatly improves discoverability.

17. Section order rules
    Must optionally enforce canonical section order.
    Rationale: predictable document shape helps humans and agents navigate fast.

18. Duplicate/missing section detection
    Rationale: common authoring drift should be caught.

19. Section targeting/editing
    The CLI should be able to insert/update content in a section deterministically.
    Rationale: this is essential for automated maintenance with minimal diff noise.

## 3.6 Cross-reference and link integrity

20. `validate-links`
    Must validate:

* broken links
* broken ID references
* invalid backlink targets
* cross-type disallowed relationships
  Rationale: a structured repo is only useful if references are trustworthy.

21. Reference graph awareness
    The CLI must understand relationships such as:

* chapter references character
* character linked to faction
* event placed in timeline
* plot-thread referenced by chapters and arcs
* adaptation artifact sourced from story artifact
  Rationale: mere markdown link checking is not enough.

22. Orphan detection
    Must detect artifacts that are not referenced from any valid parent/index when they are expected to be discoverable.
    Rationale: hidden canon is effectively lost canon.

23. Backlink generation or verification
    Should optionally generate or validate backlinks sections.
    Rationale: bidirectional discoverability is useful.

## 3.7 Indexes, registries, and discoverability artifacts

24. `index`
    Must generate configured indexes deterministically.
    Rationale: indexes are essential discoverability surfaces.

25. Configurable indexes
    I need indexes for:

* canon
* characters
* locations
* factions
* items
* events
* mysteries
* plot threads
* arcs
* chapters
* adaptation artifacts
* timeline entries
* backlog / next items
  Rationale: the repo should always expose the important artifact surfaces.

26. Configurable index columns and sort order
    Example columns:

* ID
* title
* type
* status
* tags
* arc
* timeline slot
* plot-thread state
* canon risk
* owner
  Rationale: different indexes serve different purposes.

27. Index placement rules
    Must allow defining:

* which document receives generated sections
* which heading gets updated
* where generated tables or lists are inserted
  Rationale: generated docs must remain human-readable and stable.

28. Missing-from-index validation
    The CLI must flag artifacts that should appear in an index but do not.
    Rationale: discoverability should not rely on manual curation only.

## 3.8 Repository navigation and orientation

29. `outline`
    Must show structural outline of the repo with optional metadata.
    Rationale: maintainers and agents need rapid orientation.

30. `show`
    Must show artifact details in a compact, queryable way.
    Rationale: I want quick inspection without opening files manually.

31. `find`
    Must locate artifacts by:

* ID
* title
* tag
* type
* status
* related artifact
* timeline slot
  Rationale: basic repo search should be domain-aware.

32. `trace`
    Must trace relationships forward and backward.
    Example:

* from character to appearances
* from plot thread to related chapters
* from event to impacted artifacts
  Rationale: understanding dependency chains is critical.

33. `explain`
    Must explain:

* why a rule failed
* where the rule came from
* how to fix it
* which config governs it
  Rationale: a strict CLI must also be teachable.

## 3.9 Canon and continuity protection

34. `validate-canon`
    Must check for canon integrity issues.
    At minimum:

* invalid status transitions into canon
* direct edits to generated canon summaries if disallowed
* missing canonical source artifact
* contradictory field declarations across artifacts where rules exist
  Rationale: canon must be governed.

35. Timeline consistency checks
    Must support:

* event ordering validation
* impossible chronology warnings
* pre/post dependency checks
* age/date consistency if modeled
  Rationale: timeline drift is a major risk.

36. Plot-thread lifecycle validation
    Must validate:

* open/closed state
* missing dependencies
* missing payoff target
* plot threads marked resolved but still referenced as unresolved
  Rationale: plot thread tracking is central to story coherence.

37. Relationship consistency checks
    Examples:

* referenced character must exist
* reciprocal relationship rules where configured
* character state changes reflected where required
  Rationale: continuity lives in relationships.

38. Retcon/deprecation workflow support
    Must support:

* marking canon deprecated or retconned
* requiring a reason
* linking successor artifact
* optionally updating indexes
  Rationale: canon evolves; history must remain explicit.

39. Canon change impact analysis
    Must show what downstream artifacts may be affected by a canon change.
    Rationale: changes to core canon ripple into chapters, timelines, and adaptation docs.

## 3.10 Story production workflow support

40. `scaffold`
    Must create artifacts from templates for configured types.
    Rationale: guided creation improves quality and consistency.

41. Parent-child aware scaffolding
    Example:

* create chapter under arc
* create plot-thread under story area
* create issue outline under adaptation/comics
  Rationale: placement and structure should be automatic.

42. `status`
    Must summarize progress by artifact class and workflow stage.
    Example:

* chapters drafted vs reviewed
* plot threads open vs resolved
* canon artifacts missing mandatory fields
  Rationale: I need operational awareness.

43. `plan`
    Must show next actionable items according to configuration.
    This is important.
    It should be able to compute:

* missing required artifacts
* blocked items
* stale drafts
* review-needed items
* incomplete arcs
* unresolved canon dependencies
* highest-priority backlog items
  Rationale: I want the CLI to guide work sequencing.

44. `next`
    Must produce a concise “what should be done next” view.
    Rationale: maintainers and agents need a direct execution surface.

45. Workflow gate validation
    Example:

* a chapter cannot be final if referenced plot thread metadata is incomplete
* adaptation issue plan cannot be approved if source chapter status is draft
* canon artifact cannot be marked canon unless required sections are present
  Rationale: workflows need real gating.

46. Review queue support
    Must surface artifacts in review state and why.
    Rationale: unfinished review work should be visible.

47. Staleness detection
    Must detect artifacts that have not been updated despite upstream changes.
    Rationale: stale narrative and adaptation docs are dangerous.

## 3.11 Adaptation workflow support

48. Separation enforcement between source canon/story and adaptation artifacts
    The CLI should enforce that adaptation content lives in separate configured areas.
    Rationale: adaptation should not pollute canon.

49. Source linkage requirements for adaptation artifacts
    Example:

* page-beat file must reference source chapter(s) or scene(s)
* visual sheet must reference character/location canon
  Rationale: adaptation must be traceable back to canon.

50. Adaptation freshness warnings
    Must warn when source story/canon changed after adaptation artifact was last synchronized.
    Rationale: this is one of the most useful features for this repo type.

51. Medium-specific policies
    I need different rules for:

* BD
* comics
* manga
  if the repo models them separately.
  Rationale: pacing and structural expectations differ.

## 3.12 Planning and backlog stewardship

52. Backlog artifact support
    The CLI must support a structured backlog model.
    Rationale: the repo needs operational planning, not only content storage.

53. Dependency-aware plan computation
    The CLI should compute next items based on:

* priority
* blockers
* required predecessors
* artifact completeness
* workflow status
  Rationale: I want useful next-step guidance.

54. Missing-foundation detection
    Example:

* cannot sensibly draft arc if premise is missing
* cannot finalize chapter if key canon references do not exist
  Rationale: sequencing matters.

55. Milestone or phase views
    Should support grouping by:

* worldbuilding
* canon completion
* arc development
* review
* adaptation preparation
  Rationale: progress should be visible at the right level.

## 3.13 Content quality and hygiene

56. `check`
    Must provide a single canonical workflow entry that runs the relevant validations.
    Rationale: there should be one obvious command.

57. Duplicate detection
    Should detect probable duplicate artifacts or overlapping concepts.
    Rationale: worldbuilding repos tend to accumulate parallel versions.

58. Unused tag / uncontrolled vocabulary checks
    Rationale: taxonomy drift hurts search and reporting.

59. Incomplete placeholder detection
    Should detect TODO-like placeholders in governed artifacts.
    Rationale: half-finished canon should be visible.

60. Archive detection and enforcement
    The CLI should support:

* archive zones
* deprecated drafts
* no-longer-active alternatives
* exclusion from active indexes where appropriate
  Rationale: the active corpus must remain clean.

## 3.14 Safe modification capabilities

61. `fix`
    Must automatically remediate safe issues where possible.
    Examples:

* sort metadata lists
* normalize headings
* insert missing generated sections
* refresh timestamps
* update indexes
  Rationale: repetitive maintenance should be automatable.

62. Dry-run mode
    Must show exact changes before applying them.
    Rationale: maintainers need confidence.

63. Minimal-diff editing
    The CLI must avoid rewriting entire files when only one section changes.
    Rationale: git review quality matters.

64. Idempotent generation
    Re-running the same command without semantic changes should not churn files.
    Rationale: deterministic maintenance is essential.

## 3.15 CI and machine usage

65. Machine-readable output formats
    Must support at least:

* human-readable text
* JSON
  Rationale: AI agents and CI systems need structured output.

66. Exit code discipline
    Must return meaningful exit codes by failure severity.
    Rationale: CI integration depends on it.

67. Scoped command execution
    Must support:

* whole repo
* specific path
* specific artifact type
* specific rule group
  Rationale: targeted workflows are necessary in large repos.

68. Performance on mature repos
    Must remain usable as the artifact count grows.
    Rationale: story universes expand.

## 3.16 Explainability and maintainability of the CLI itself

69. Config-driven, not magic-driven
    Behavior should come from explicit config rather than hidden assumptions.
    Rationale: maintainers need confidence and predictability.

70. Rule explainability
    Every enforcement should be traceable to a clear configured policy.
    Rationale: debugging configuration should be straightforward.

71. Good error messages
    Errors must state:

* what failed
* where
* why
* what policy applied
* how to remediate
  Rationale: this is critical for real-world usability.

## 4. Exact configurability I would require

This is the most important part. The CLI is only good enough for this repo if I can express my intent precisely in configuration.

## 4.1 Repo model configuration

I need to configure:

* repository name
* repository purpose
* top-level structure contract
* active artifact families
* optional artifact families
* generated paths
* ignored paths
* archive paths
* asset paths

Rationale: the CLI must know the structural model of the repo.

## 4.2 Artifact type configuration

For each artifact type, I need to configure:

* type name
* allowed target paths
* filename pattern
* ID pattern
* title requirements
* required metadata fields
* optional metadata fields
* allowed status values
* allowed tags or vocabularies
* required markdown sections
* optional sections
* section order
* allowed references to other types
* generated sections
* template path
* indexing behavior

Rationale: artifact-type governance is the heart of the repo contract.

## 4.3 Rule targeting and inheritance

I need rules that can target:

* repo root
* exact path
* glob pattern
* artifact type
* folder subtree
* filename pattern

And I need override / precedence rules so that:

* explicit target beats generic target
* deeper scope beats higher scope
* specific artifact type beats broad default

Rationale: this repo needs shared conventions with scoped exceptions.

## 4.4 Lifecycle and workflow configuration

I need to configure:

* allowed statuses per artifact type
* allowed transitions
* required preconditions for transitions
* review gates
* publication gates
* canonization gates
* adaptation readiness gates

Rationale: “status” without transition logic is weak.

## 4.5 Dependency and relationship configuration

I need to configure allowed relationships such as:

* chapter references character/location/plot-thread
* plot-thread depends on event or reveal
* adaptation issue sourced from chapter
* canon event appears on timeline
* backlog item linked to target artifact

I also need optional cardinality rules such as:

* chapter must reference at least one plot-thread
* adaptation outline must reference at least one source chapter
* canon event must appear in timeline index

Rationale: relationship rules are how the CLI understands meaning.

## 4.6 Index configuration

I need to configure:

* which indexes exist
* source artifact query for each index
* sort order
* grouping
* columns
* output format
* target file
* target section
* include/exclude archived items
* include/exclude draft items
* stale-item highlighting

Rationale: indexes are repo-specific.

## 4.7 Workflow-plan configuration

I need to configure how “next” and “plan” are computed.

This should include:

* priority fields
* blocker fields
* dependency fields
* stale thresholds
* readiness criteria
* missing-foundation heuristics
* per-artifact “done enough” rules
* preferred sequencing logic

Rationale: plan guidance must reflect how this repo is actually maintained.

## 4.8 Vocabulary and taxonomy configuration

I need controlled vocabularies for:

* tags
* themes
* statuses
* artifact subtypes
* canon levels
* continuity risk
* adaptation medium
* owner / responsibility if used

Rationale: taxonomy drift reduces discoverability.

## 4.9 Generated content policy configuration

I need to configure:

* which sections are fully generated
* which sections are partially managed
* which fields are auto-maintained
* whether timestamps update automatically
* whether backlinks are generated
* whether indexes are authoritative or advisory

Rationale: authored and generated content must be clearly separated.

## 4.10 Ignore / archive / deprecation configuration

I need to configure:

* ignored paths
* generated paths
* archived paths
* deprecated artifact handling
* retconned artifact handling
* whether deprecated items remain indexed
* whether references to deprecated items are warnings or errors

Rationale: not all old content should be active content.

## 4.11 Output and UX configuration

I need to configure:

* default output format
* verbosity
* whether to show matches
* whether to show descriptions
* whether to show rule origin
* whether to show remediation hints
* severity thresholds for failure

Rationale: different contexts need different output density.

## 5. Exact validation rules I would want for this repo specifically

For a story/lore/adaptation repo, I would explicitly want the CLI to be able to enforce these rule families.

## 5.1 Canon artifact rules

* every canon artifact must have stable ID
* every canon artifact must declare its type
* every canon artifact must have a valid status
* every canon artifact must contain required sections
* every canon artifact must appear in at least one relevant index
* every canon artifact must be linkable from a discoverability surface
* canon artifacts must not live in draft-only zones unless configured

Rationale: canon must be authoritative and findable.

## 5.2 Story artifact rules

* each arc must have overview metadata
* each chapter must belong to an arc if that model is used
* each chapter must reference relevant canon entities
* each chapter must include continuity constraints
* each chapter should identify plot-thread involvement
* chapters marked final should satisfy review gates

Rationale: story artifacts must remain connected to canon.

## 5.3 Plot-thread rules

* plot-thread IDs unique
* open/closed status explicit
* required stakes and dependency fields present
* referenced payoff target or resolution condition present
* closed threads not referenced as unresolved without warning
* threads used in chapters appear in plot-thread index

Rationale: plot-thread loss is one of the biggest long-form story failures.

## 5.4 Timeline rules

* timeline events must have sequenceable placement
* event references must point to existing artifacts
* impossible ordering flagged
* if character age/date logic exists, it should be checkable
* master timeline index must stay synchronized

Rationale: chronology must remain coherent.

## 5.5 Adaptation rules

* adaptation artifacts must reference source story/canon
* adaptation artifacts must live under adaptation paths
* source-last-updated versus adaptation-last-synced check available
* adaptation items cannot silently become canon artifacts
* medium-specific required sections supported

Rationale: adaptation needs traceability and separation.

## 5.6 Planning rules

* all active backlog items must point to real target artifacts or scopes
* blocked items must name blockers
* completed items should satisfy completion conditions
* next-item recommendations should exclude blocked or invalid candidates unless explicitly requested

Rationale: plan views must be trustworthy.

## 6. Workflow support I would explicitly require

I would want the CLI to make these workflows first-class.

### 6.1 New canon entity workflow

The CLI should support:

* scaffold artifact
* assign ID
* apply template
* validate completeness
* insert into indexes
* show missing follow-up items
  Rationale: creating canon should be guided and consistent.

### 6.2 New arc / chapter workflow

The CLI should support:

* create under correct parent
* link to relevant plot threads
* link to relevant canon
* validate status
* surface missing continuity fields
  Rationale: narrative work should stay grounded in canon.

### 6.3 Canon change workflow

The CLI should support:

* update canonical artifact
* run impact analysis
* show downstream stale artifacts
* optionally create follow-up backlog items
  Rationale: canon change must be controlled.

### 6.4 Retcon / deprecation workflow

The CLI should support:

* mark deprecated or retconned
* require replacement or explanation
* surface impacted references
* update active indexes appropriately
  Rationale: evolution must be explicit.

### 6.5 Adaptation preparation workflow

The CLI should support:

* identify chapters ready for adaptation
* validate all required source links
* scaffold issue/page-beat artifacts
* warn on missing visual canon or unresolved dependencies
  Rationale: adaptation should only happen from stable enough source material.

### 6.6 Review workflow

The CLI should support:

* list artifacts awaiting review
* show why they are in review
* show blockers to promotion
  Rationale: review must not be opaque.

### 6.7 Maintainer planning workflow

The CLI should support:

* show current repo health
* show missing foundational artifacts
* show stale items
* show highest-priority next tasks
* show work by milestone or phase
  Rationale: I want operational control.

## 7. Non-functional requirements I would also demand

These are not optional.

1. Deterministic behavior
   Same input and config must produce same result.

2. Minimal diffs
   Generated updates must not rewrite unrelated content.

3. Clear precedence rules
   Config resolution must be predictable.

4. Good performance
   The CLI must stay responsive on growing content repos.

5. Scriptability
   Commands must work well in CI and automation.

6. Explainability
   Every failure must be understandable.

7. Safe defaults
   Auto-fix must not silently make risky semantic changes.

8. Strong dry-run support
   I need confidence before write operations.

9. Machine-readable results
   JSON output is required.

10. Idempotency
    Re-running maintenance commands should not churn files.

## 8. What I would consider the minimum acceptable feature set

If I had to define the minimum version that is still valid for this repo, it would include:

* init
* adopt
* validate-structure
* validate-metadata
* validate-sections
* validate-links
* check-names
* index
* outline
* show
* find
* trace
* status
* plan
* next
* scaffold
* fix
* explain
* canon/timeline/plot-thread validation support
* adaptation freshness/linkage validation
* JSON output
* dry-run
* deterministic section editing
* fully configurable artifact-type rules

Anything less than that would not yet be enough to properly steward this repo.

## 9. The exact standard I would hold the CLI to

For this repo, I would require the CLI to function as:

* structure contract enforcer
* artifact schema validator
* continuity guardrail
* discoverability engine
* workflow guide
* planning assistant
* deterministic repo maintainer

That is the real target.

I do not merely want a CLI that tells me what is wrong.

I want a CLI that makes this repository operable as a long-lived structured story universe system, where canon, narrative, planning, and adaptation remain coherent, discoverable, and enforceable over time.
