---
type: review
status: Complete
date: 2026-04-18
author: stress-test-pass
---

# Config Expressiveness Stress Test

**Date:** 2026-04-18
**Scope:** Three real public repositories, each representing a distinct archetype
**Method:** Attempt to design credible Steward policy/config for each repo; evaluate authoring experience and expected runtime behavior; map the boundary between "awkward but possible" and "not credibly expressible"

---

## Executive Summary

Steward's configuration model is **meaningfully expressive for documentation-centric governance** and handles the well-structured use case (named ADR/RFC families, frontmatter enforcement, naming patterns, required artifacts) credibly. However, across three different repo shapes, a consistent set of gaps emerged that materially limit adoption credibility beyond that sweet spot:

1. **No cross-artifact relationship enforcement.** References between governed artifacts — changelog entry ↔ release tag, ADR supersedes/resolves chain, changelog section ↔ release version — cannot be expressed or validated. This is a first-class maintainer need in every repo tested.

2. **No conditional or context-sensitive governance.** Rules that apply only when another condition holds — "require CHANGELOG entry if version bumped", "require SECURITY.md if any CVE is referenced" — cannot be modeled. The policy is always-on and uniform.

3. **No multi-file or cross-directory family scoping.** `artifact_families` matches files within a path pattern but cannot express "the ADR index must reference every file in docs/decisions/adrs/". Referential integrity between a governing index and its governed collection is a critical and missing concept.

4. **No graduated/maturity-gradient policy.** The only importance levels are `required`, `recommended`, `optional`. There is no mechanism to express "this area is under construction — flag but don't fail" or "this team is exempt from this rule while onboarding".

5. **Artifact families cannot express directory-scoped exceptions cleanly.** Path overrides apply to specific rules; families apply to specific path patterns. The intersection — "apply this family's schema everywhere except this one subdirectory" — requires awkward duplication or leaves a policy gap.

6. **No lifecycle or state-transition rules.** Status field validation prevents invalid values but cannot enforce that `Accepted` cannot revert to `Draft`, or that a `Deprecated` ADR must have a `superseded_by` field. The policy is vocabulary-only, not lifecycle-aware.

7. **The audit/non-governed-directory gap.** No mechanism to declare "files in this directory are intentionally ungoverned" without suppressing all warnings globally or listing every suppression individually.

---

## Repos Chosen and Why

| Repo | Archetype | Rationale |
|------|-----------|-----------|
| `microsoft/vscode` | Large monorepo | Enterprise-scale, layered architecture, CODEOWNERS, AI-aware governance docs, no centralized changelog |
| `sphinx-doc/sphinx` | Docs-heavy open-source | Documentation as primary artifact, versioned per-file changelog, heavy extension ecosystem, i18n |
| `psf/requests` | Small focused library | Minimal, named-maintainer governance, flat changelog, PSF coordination for security, explicit release process docs |

**Why not other repos:** These three provide meaningfully different governance shapes and are well-known enough that a realistic maintainer intent can be inferred without access to the actual repo. The research agent confirmed their structure. A fourth candidate (e.g., a monorepo with microservices like `kubernetes/kubernetes`) was considered but judged redundant — vscode already exercises the hardest monorepo concerns. The stress test value comes from breadth of archetype coverage, not number of repos.

---

## Repo 1: microsoft/vscode

### Repo Overview (microsoft/vscode)

- ~14,600 files, 5,800+ TypeScript files
- Highly layered: `src/vs/base/`, `src/vs/platform/`, `src/vs/editor/`, `src/vs/workbench/`, `src/vs/code/`
- Extensions as first-class subdirectory: `extensions/{name}/`
- Governance artifacts: `CONTRIBUTING.md` (minimal, wiki-delegated), `SECURITY.md`, `.github/CODEOWNERS` (path-based ownership), `.github/copilot-instructions.md` (AI agent guidance), `.github/endgame/` (milestone docs)
- No centralized CHANGELOG; releases via GitHub Releases monthly
- No RFC/ADR directory; decisions via GitHub Discussions
- Naming conventions: PascalCase types, camelCase methods, platform-scoped filenames (`.browser.ts`, `.electron-main.ts`, `.node.ts`, `.common.ts`)

### Governance Intent a Maintainer Would Reasonably Want (microsoft/vscode)

1. **Layering enforcement** — `base/` must not import `workbench/`, `platform/` must not import `editor/`. Cross-layer dependency violations are caught by a custom validator (`valid-layers-check`).
2. **CODEOWNERS coverage** — Key directories (CI workflows, release scripts) have mandatory reviewers; ungoverned important paths should be flagged.
3. **Platform-scoped file naming** — Files in `browser/`, `node/`, `common/` subdirs should follow the `.browser.ts`/`.node.ts`/`.common.ts` suffix convention.
4. **Extension governance** — Each extension directory must have a `package.json` and a `README.md`. Extensions without these are structurally incomplete.
5. **AI governance doc freshness** — `.github/copilot-instructions.md` and `AGENTS.md` should not go stale relative to architecture changes.
6. **Endgame milestone docs** — `.github/endgame/` docs should follow a predictable naming pattern (e.g., `{version}.md`).
7. **Missing CHANGELOG** — No centralized changelog is a governance gap; a maintainer might want Steward to flag this or at least not penalize its absence.

### Config/Policy Attempt (microsoft/vscode)

**What can be expressed:**

```yaml
# policy.yaml

repository:
  name: vscode
  type: software

artifacts:
  - path: CONTRIBUTING.md
    role: governance
    required: true
    importance: required

  - path: SECURITY.md
    role: governance
    required: true
    importance: required

  - path: .github/CODEOWNERS
    role: governance
    importance: recommended

  - path: .github/copilot-instructions.md
    role: governance
    importance: recommended
    freshness:
      max_age_days: 90

  - path: .github/AGENTS.md
    role: governance
    importance: optional
    freshness:
      max_age_days: 90

artifact_families:
  - family: extension
    display_name: Built-in Extension
    match:
      path_pattern: "extensions/*/README.md"
    role: documentation
    importance: recommended

  - family: endgame-doc
    display_name: Milestone Endgame Document
    match:
      path_pattern: ".github/endgame/*.md"
    role: workflow
    importance: optional
    frontmatter_schema:
      required: []

governance:
  start_here:
    - CONTRIBUTING.md
    - .github/copilot-instructions.md
    - README.md

validation:
  path_overrides:
    - pattern: "extensions/**"
      disabled_rules: [STWD-007]   # extensions don't have freshness expectations
    - pattern: ".github/endgame/**"
      disabled_rules: [STWD-003]   # endgame docs have no frontmatter requirements
```

```yaml
# path-policy.yaml

rulesets:
  - name: platform-scoped-ts-naming
    description: TypeScript files in common/browser/node subdirs should use scoped suffixes
    rules:
      - pattern: "src/vs/**/common/*.ts"
        category: recommended
        must_match: "^[a-zA-Z0-9]+(?:[A-Z][a-zA-Z0-9]*)*(?:\\.common)?\\.ts$"
      - pattern: "src/vs/**/browser/*.ts"
        category: recommended
        must_match: "^[a-zA-Z0-9]+(?:[A-Z][a-zA-Z0-9]*)*(?:\\.browser)?\\.ts$"
      - pattern: "src/vs/**/node/*.ts"
        category: recommended
        must_match: "^[a-zA-Z0-9]+(?:[A-Z][a-zA-Z0-9]*)*(?:\\.node)?\\.ts$"
```

### What Steward Expressed Well (microsoft/vscode)

- Required governance file presence (CONTRIBUTING, SECURITY, CODEOWNERS)
- Freshness expectations on AI governance docs (copilot-instructions, AGENTS.md)
- Convention-based extension README governance via `artifact_families`
- Naming pattern enforcement on endgame docs and TypeScript platform files
- `start_here` orientation to key contributor entry points
- Scoped rule suppression via `path_overrides`

### What Was Awkward but Possible (microsoft/vscode)

- **Extension package.json presence:** The `artifact_families` match on `extensions/*/README.md` captures the README requirement, but there is no clean way to also require `extensions/*/package.json` from the same family declaration. Two families with different path patterns can each require their file, but the conceptual unit is "an extension needs both" — not two separate files in two separate families. **Workaround: two families, duplicated semantic intent.**

- **Endgame doc naming:** The `must_match` regex in path-policy covers naming pattern. But the pattern applies to all `.md` files in the endgame directory — if a `README.md` lives there too, it would be flagged. **Workaround: a more specific regex that exempts README, or a separate `ignored` rule.**

- **Freshness on AI governance docs:** Works, but the freshness comparison is git-based timestamp — it cannot understand whether the architecture described in `copilot-instructions.md` is actually current. Steward flags stale by time, not by content drift. That is probably acceptable but is a weaker signal than a maintainer might want.

### What Was Not Credibly Expressible (microsoft/vscode)

- **Layer dependency enforcement.** Steward cannot validate that `src/vs/workbench/` does not import from `src/vs/base/` in the wrong direction. This is a source-code dependency concern. It is clearly not Steward's domain, but the vscode maintainer's primary governance concern is this rule — meaning Steward's governance coverage of this repo is shallow by design.

- **CODEOWNERS coverage validation.** A maintainer would want to know which important paths lack CODEOWNERS entries. Steward cannot parse or validate CODEOWNERS semantics.

- **Per-extension governance completeness.** "Every directory under `extensions/` must have both a README.md and a package.json" cannot be expressed as a single coherent rule. You can approach it with two families or two artifact entries with globs, but the co-presence constraint (both must exist for the same extension) is inexpressible.

- **No-CHANGELOG governance choice.** There is no way to tell Steward "this repo intentionally has no CHANGELOG; do not suggest or warn about its absence." The `minimal` profile suppresses this, but the suppression is global, not targeted. A repo that *wants* Steward's governance except for the CHANGELOG absence has no clean way to declare that intent.

- **Milestone doc lifecycle.** There is no way to declare that an endgame doc `{version}.md` should be closed/archived when the version ships.

### False-Positive / False-Negative Concerns (microsoft/vscode)

- **False positives (high):** The `platform-scoped-ts-naming` path-policy rules will produce a large volume of violations in a codebase of this size. Many existing files use non-suffixed names for legitimate reasons. Without per-file or per-directory exemption mechanism at scale, the policy is unusable in practice. `path_overrides` can suppress by rule per directory but cannot easily express "all existing files are grandfathered; only new files need the suffix".

- **False negatives (medium):** Extension governance via family only catches READMEs — it is silent on missing `package.json` files, extensions with incorrect structure, or extensions added without either file.

- **Runtime noise:** A 14,600-file repo will surface many rule violations from any non-trivial policy. Steward's scoped validation (`--scope changed`) is critical here, but even the check baseline could be overwhelming. Steward currently has no "policy phased rollout" mechanism.

### Verdict: **Materially Limited** (microsoft/vscode)

For a repo of this shape, Steward can handle documentation governance adequately but cannot touch the primary governance concerns (layer dependencies, CODEOWNERS coverage, co-located multi-file extension contract). The policy model expresses useful but peripheral intent. The false-positive risk at scale is high without a grandfathering or phase-in mechanism.

---

## Repo 2: sphinx-doc/sphinx

### Repo Overview (sphinx-doc/sphinx)

- ~774 Python files, ~471 RST docs
- `sphinx/` package: `builders/`, `domains/`, `ext/`, `directives/`, `environment/`, `util/`, `writers/`, `transforms/`, `themes/`, `locale/`
- `doc/`: `changes/{version}.rst` (one file per release, 0.1 through 9.1), `development/`, `internals/`, `extdev/`, `usage/`, `tutorial/`
- `tests/roots/test-{feature}/` — fixture directories per feature
- `CHANGES.rst` stub, `CONTRIBUTING.rst` minimal, `CODE_OF_CONDUCT.rst` full inline, `AUTHORS.rst`
- Changelog: per-version RST files, structured sections: Features added, Bugs fixed, Deprecations, Dependencies, Incompatible changes

### Governance Intent a Maintainer Would Reasonably Want (sphinx-doc/sphinx)

1. **Changelog completeness per release.** Every release version must have a corresponding `doc/changes/{version}.rst` file following the standard structure (required headings: Features added, Bugs fixed).
2. **Extension module structure.** Built-in extensions under `sphinx/ext/` should follow consistent naming and have corresponding test roots under `tests/roots/`.
3. **Domain module naming.** Domain modules under `sphinx/domains/` should follow lowercase-underscore conventions.
4. **AUTHORS tracking.** `AUTHORS.rst` and `CONTRIBUTING.rst` should not go stale.
5. **i18n locale completeness.** `sphinx/locale/` has ~51 locales — a governance policy might want to flag new locales added without documentation.
6. **Test fixture naming.** `tests/roots/test-{feature}/` directories should follow the `test-{slug}` naming convention.
7. **Changelog section structure.** Each release changelog file should contain the expected section headings.
8. **RST documentation file governance.** Key doc files in `doc/development/`, `doc/internals/` should be tracked as governed artifacts.

### Config/Policy Attempt (sphinx-doc/sphinx)

```yaml
# policy.yaml

repository:
  name: sphinx
  type: software

artifacts:
  - path: CONTRIBUTING.rst
    role: governance
    required: true
    importance: required
    freshness:
      max_age_days: 180

  - path: CODE_OF_CONDUCT.rst
    role: governance
    required: true
    importance: required

  - path: AUTHORS.rst
    role: governance
    importance: recommended
    freshness:
      max_age_days: 365

  - path: CHANGES.rst
    role: changelog
    required: true
    importance: required

  - path: doc/development/
    role: documentation
    importance: optional

  - path: doc/internals/
    role: documentation
    importance: optional

artifact_families:
  - family: release-changelog
    display_name: Per-Release Changelog
    match:
      path_pattern: "doc/changes/*.rst"
    role: changelog
    importance: recommended
    required_sections:
      - "Features added"
      - "Bugs fixed"
    directory_expectations:
      min_count: 1
      description: At least one release changelog must exist

  - family: sphinx-extension
    display_name: Built-in Sphinx Extension
    match:
      path_pattern: "sphinx/ext/*.py"
    role: documentation
    importance: optional

  - family: sphinx-domain
    display_name: Sphinx Language Domain
    match:
      path_pattern: "sphinx/domains/*.py"
    role: documentation
    importance: optional

  - family: test-fixture-root
    display_name: Test Fixture Root
    match:
      path_pattern: "tests/roots/test-*"
    role: supporting
    importance: optional

governance:
  start_here:
    - CONTRIBUTING.rst
    - doc/development/
    - doc/internals/contributing.rst

validation:
  path_overrides:
    - pattern: "tests/**"
      disabled_rules: [STWD-003, STWD-007]
    - pattern: "sphinx/locale/**"
      disabled_rules: [STWD-003, STWD-007, STWD-010]
```

```yaml
# path-policy.yaml

rulesets:
  - name: changelog-file-naming
    description: Per-release changelog files follow semver naming
    rules:
      - pattern: "doc/changes/*.rst"
        category: recommended
        must_match: "^[0-9]+\\.[0-9]+(?:\\.[0-9]+)?\\.rst$"

  - name: test-fixture-naming
    description: Test fixture roots follow test-{slug} convention
    rules:
      - pattern: "tests/roots/*"
        category: recommended
        must_match: "^test-.+$"

  - name: domain-module-naming
    description: Domain modules use lowercase underscore naming
    rules:
      - pattern: "sphinx/domains/*.py"
        category: recommended
        must_match: "^[a-z][a-z0-9_]*\\.py$"
```

### What Steward Expressed Well (sphinx-doc/sphinx)

- Required governance file presence (CONTRIBUTING, CODE_OF_CONDUCT, CHANGES.rst)
- Freshness expectations on AUTHORS and CONTRIBUTING
- Convention-based changelog file family (`release-changelog`) with required sections
- `directory_expectations.min_count` to flag if no changelog exists
- Naming conventions on changelog files (semver naming), test fixture roots (`test-{slug}`), and domain modules
- `path_overrides` to suppress noise in tests/ and locale/
- `start_here` orientation for contributor entry points

### What Was Awkward but Possible (sphinx-doc/sphinx)

- **RST files as governed artifacts.** Steward governs Markdown natively. RST files are supported by discovery and path-policy (filename matching works), but `required_sections` in `artifact_families` checks Markdown headings — **RST section headings (underline-delimited) will not be recognized.** This means the `required_sections` declaration for release changelog files will silently fail to validate RST content. **Workaround: none within current config model. Governance intent is expressed but not enforced at runtime for RST files.**

- **CHANGES.rst as both index and stub.** The file exists and can be declared as a required artifact, but Steward cannot understand that it's a stub pointing to versioned files. The `index_of` concept would be semantically correct here, but `index_of` in Steward means "Steward auto-maintains this as an index of directory contents" — not "this file points to a set of governed artifacts." **Workaround: declare it as `role: changelog`, `importance: required`, and leave the semantic relationship unmodeled.**

- **Test fixture directories.** `artifact_families` matches files, but test fixture roots are directories. The family entry for `test-fixture-root` matches `tests/roots/test-*` — path-policy can validate directory naming, but the family declaration's `required_sections` and `frontmatter_schema` only apply to files. The family match on a directory glob is conceptually odd. **Workaround: use path-policy exclusively for directory naming, remove from artifact_families.**

- **Extension ↔ test-root correspondence.** A maintainer might want to ensure that every extension `sphinx/ext/{name}.py` has a corresponding `tests/roots/test-{name}/` fixture. This cross-directory referential integrity is inexpressible.

### What Was Not Credibly Expressible (sphinx-doc/sphinx)

- **RST content validation.** Steward is Markdown-native. RST files can be discovered, named-checked, and declared as artifacts, but their content — headings, sections, structural completeness — cannot be validated. For a repo where RST is the primary documentation format, this is a fundamental coverage gap. The governance config accurately declares intent but Steward cannot enforce it at the content level.

- **Versioned changelog completeness.** There is no way to declare "for every release version X.Y.Z there must exist a file `doc/changes/X.Y.Z.rst`". The `release-changelog` family can validate that individual changelog files have the right structure, but cannot cross-reference against actual release versions.

- **Changelog section order/completeness per file.** `required_sections` checks that headings exist. It cannot validate ordering, that sections are non-empty, or that "Incompatible changes" is only present when changes actually exist.

- **Per-locale completeness.** There is no way to validate that all 51 locale directories have consistent internal structure. Declaring 51 individual artifacts is unreasonable; there is no mechanism to express "each directory under `sphinx/locale/` must contain `LC_MESSAGES/sphinx.po`".

- **Extension ecosystem integrity.** Steward cannot validate that registered extensions in `sphinx/ext/__init__.py` have corresponding module files, or that extension names referenced in docs match actual module names.

### False-Positive / False-Negative Concerns (sphinx-doc/sphinx)

- **False positives (medium-high):** The `domain-module-naming` rule will flag `__init__.py` and `__pycache__` items unless the path pattern is precise enough. Glob precision matters greatly for Python package structures.
- **False positives (high for RST):** If `required_sections` silently fails on RST files and no error is raised, the maintainer may believe governance is enforced when it is not. This is a silent false-negative, not a noisy false-positive — arguably worse.
- **False negatives (high):** The RST content governance gap means Steward provides weaker validation for the most important artifact type in this repo (release changelogs in RST).

### Verdict: **Workable with Friction** (sphinx-doc/sphinx)

Steward can express meaningful governance for file presence, naming conventions, and family-level classification. The RST content gap is significant for a docs-heavy RST repo but does not block the entire config. The authoring experience for the parts that work is reasonable. The gap between declared intent and enforced reality (RST sections) is a credibility risk.

---

## Repo 3: psf/requests

### Repo Overview (psf/requests)

- ~36 core Python files, 16 RST doc files
- `src/requests/`: flat package — `adapters.py`, `auth.py`, `models.py`, `sessions.py`, `utils.py`, `cookies.py`, `exceptions.py`, `api.py`
- `docs/`: `user/` (quickstart, API, auth), `dev/` (contributing, authors), `community/` (FAQ, release-process, vulnerabilities)
- `tests/`: `test_{feature}.py` + test servers + fixtures
- Governance: `AUTHORS.rst`, `LICENSE`, `HISTORY.md`, `.pre-commit-config.yaml`, `.readthedocs.yaml`
- Changelog: `HISTORY.md` flat chronological, version entries with release dates and CVE references
- Named maintainers governance: maintainer list in contributing docs

### Governance Intent a Maintainer Would Reasonably Want (psf/requests)

1. **HISTORY.md currency.** Every release must add a new version entry to HISTORY.md. Freshness constraint: should be updated within 90 days of last release.
2. **Governance artifact presence.** AUTHORS.rst, LICENSE, SECURITY (or equivalent), CONTRIBUTING-equivalent (docs/dev/contributing.rst) must exist.
3. **Changelog section structure.** Each HISTORY.md version entry should contain structured sections (Bugfixes, Improvements, Deprecations). This is a soft convention currently.
4. **Module naming.** Core modules in `src/requests/` should follow lowercase convention.
5. **Release process doc.** `docs/community/release-process.rst` should be governed and kept current.
6. **Security vulnerability doc.** `docs/community/vulnerabilities.rst` must exist.
7. **Test naming.** `tests/test_{feature}.py` naming convention.
8. **PSF contributor count tracking.** AUTHORS.rst completeness (named contributor list).

### Config/Policy Attempt (psf/requests)

```yaml
# policy.yaml

repository:
  name: requests
  type: software

artifacts:
  - path: HISTORY.md
    role: changelog
    required: true
    importance: required
    freshness:
      max_age_days: 90

  - path: AUTHORS.rst
    role: governance
    required: true
    importance: required

  - path: LICENSE
    role: governance
    required: true
    importance: required

  - path: README.md
    role: authoritative
    required: true
    importance: required

  - path: docs/dev/contributing.rst
    role: governance
    importance: required
    freshness:
      max_age_days: 365

  - path: docs/community/release-process.rst
    role: workflow
    importance: recommended
    freshness:
      max_age_days: 365

  - path: docs/community/vulnerabilities.rst
    role: governance
    importance: required

  - path: docs/community/
    role: documentation
    importance: optional

  - path: docs/user/
    role: documentation
    importance: optional

artifact_families:
  - family: test-module
    display_name: Test Module
    match:
      path_pattern: "tests/test_*.py"
    role: supporting
    importance: optional

  - family: core-module
    display_name: Core Requests Module
    match:
      path_pattern: "src/requests/*.py"
    role: authoritative
    importance: optional

governance:
  start_here:
    - README.md
    - HISTORY.md
    - docs/dev/contributing.rst
    - docs/community/release-process.rst

validation:
  path_overrides:
    - pattern: "tests/**"
      disabled_rules: [STWD-003, STWD-007, STWD-010]
    - pattern: "docs/**"
      disabled_rules: [STWD-003]
```

```yaml
# path-policy.yaml

rulesets:
  - name: test-naming
    description: Test modules follow test_{feature} convention
    rules:
      - pattern: "tests/test_*.py"
        category: recommended
        must_match: "^test_[a-z][a-z0-9_]*\\.py$"

  - name: core-module-naming
    description: Core package modules use lowercase underscore naming
    rules:
      - pattern: "src/requests/*.py"
        category: recommended
        must_match: "^[a-z][a-z0-9_]*\\.py$"
```

### What Steward Expressed Well (psf/requests)

- Required artifact presence: HISTORY.md, AUTHORS.rst, LICENSE, README.md, security docs
- Freshness on HISTORY.md (90 days) and contributing/release docs (365 days)
- `start_here` orientation to maintainer-facing entry points
- Test module naming enforcement
- Core module naming enforcement
- Family-based classification of test modules and core modules
- Required governance files with distinct importance levels

### What Was Awkward but Possible (psf/requests)

- **HISTORY.md versioned entry structure.** HISTORY.md is Markdown, so `required_sections` in a family *could* theoretically enforce that every HISTORY.md has certain heading patterns. But HISTORY.md is a single file with many versioned entries — the family concept applies at file-match granularity, not at section-within-file granularity. There is no way to declare "the top section of HISTORY.md should be a recent version entry." **Workaround: only freshness enforcement; structural validation of the changelog is not possible.**

- **docs/dev/contributing.rst as required artifact.** RST files can be declared as required artifacts (presence check works), but their content cannot be validated. A maintainer might want to check that the contributing guide still references the correct maintainer list or has specific sections. **Workaround: presence only.**

- **LICENSE without extension.** `LICENSE` (no `.md` extension) can be declared as a required artifact and its presence is checked. Steward can govern it for presence; content governance is not applicable here (it's static legal text). This actually works well.

- **AUTHORS.rst currency.** Freshness on AUTHORS.rst via `freshness.max_age_days: 365` works mechanically, but the git-timestamp freshness signal is weak — a revert that touches AUTHORS.rst without actually adding contributors resets the clock. **Limitation is intrinsic to git-timestamp-based freshness, not a config expressiveness issue per se.**

- **Vulnerability doc sections.** `docs/community/vulnerabilities.rst` is RST; section presence validation would not work. For a file this important (security disclosure process), the inability to validate its structure is a genuine gap.

### What Was Not Credibly Expressible (psf/requests)

- **Changelog version entry validation.** Cannot check that a new release version in HISTORY.md matches the actual version in `setup.cfg` or `pyproject.toml`. Cannot validate that the release date is present or in ISO format. Cannot validate that a CVE is properly attributed.

- **Named maintainer governance.** There is no config concept for "this repo has declared maintainers; changes to AUTHORS.rst require at least one maintainer's sign-off." Steward has no CODEOWNERS-equivalent concept.

- **PSF coordination policy.** The repo's vulnerability process requires PSF coordination (out-of-band). There is no way to declare this policy in Steward config and have it surface in governance output.

- **Release gate completeness.** A maintainer might want to declare "before tagging a release, HISTORY.md must have an entry for the new version, AUTHORS.rst must be updated, and docs/community/release-process.rst must be less than 365 days old." This is a composite completion policy that spans multiple artifact freshness rules — not expressible as a single policy item or without multiple independent rules that don't compose into a meaningful gate.

- **Conditional artifact requirements.** "Require a `SECURITY.md` or `docs/community/vulnerabilities.rst`, but not necessarily both" — the policy model has no OR-relationship or conditional-requirement concept. You must declare both as required, or neither.

- **Pre-commit config governance.** `.pre-commit-config.yaml` is a tooling artifact that a maintainer might want to keep fresh. It can be declared as an artifact, but Steward has no concept of "this file's content should reference packages that are not pinned to EOL versions."

### False-Positive / False-Negative Concerns (psf/requests)

- **False positives (low-medium):** For a repo this small, the policy is unlikely to produce excessive noise. The main risk is the `core-module-naming` rule flagging `__init__.py` and `__version__.py`. **Mitigation: tighten the glob pattern to exclude dunder files.**

- **False negatives (medium):** The RST content governance gap is the primary false-negative risk. Declared intent for `docs/community/vulnerabilities.rst` structure is not enforced.

- **False negatives (medium):** No changelog entry validation means a release can happen without a HISTORY.md update — Steward will only flag staleness after 90 days, not immediately after a version bump.

### Verdict: **Workable with Friction** (psf/requests)

For a small focused library, Steward's config model covers the meaningful governance surface well. The main gaps are RST content enforcement and conditional/composite policy. The authoring experience is clean and the policy reads like credible intent. This is the repo archetype where Steward fits best today.

---

## Cross-Repo Recurring Config Pain Points

The following friction points appeared in multiple repos and represent systemic gaps rather than one-repo edge cases:

### 1. RST / Non-Markdown Content Governance

All three repos have important non-Markdown artifacts. Two (sphinx, requests) use RST extensively. Steward can declare these files as governed artifacts, apply naming conventions, and enforce presence — but cannot validate their content or structure. A config that declares `required_sections` on RST files silently produces no enforcement. This creates a credibility gap: the policy *reads* as if it governs RST content, but it does not.

### 2. Intra-Family Referential Integrity

Two repos (vscode, sphinx) have families where elements should reference or correspond to each other. Extension `package.json` + `README.md` must co-exist. Each `sphinx/ext/{name}.py` should have a `tests/roots/test-{name}/` fixture. There is no config mechanism to express "for every file matching pattern A, a corresponding file matching pattern B (with the same slug) must exist." This is a first-class governance need for any repo with a registry/collection pattern.

### 3. Conditional/Composite Artifact Requirements

All three repos have cases where policy should be conditional: "require X only if Y exists" or "require A or B (not necessarily both)". The policy model is always-on and additive. This is not just a power-user need — it's fundamental to expressing real repo governance without false positives.

### 4. No Changelog Entry Validation

All three repos have changelog artifacts. Steward can check that the changelog file exists, is fresh, and (for Markdown) has certain sections. But it cannot validate that:

- A specific version entry exists
- The entry follows the structural convention
- The entry was added in this release cycle

This is a high-value governance signal for any library or tool with versioned releases.

### 5. Per-Directory Ungoverned Zone Declaration

Multiple repos have directories that are intentionally out-of-scope for governance (test fixtures, generated files, locale resources). Currently, the maintainer must list all suppressed rules via `path_overrides` — one override block per directory per rule. There is no mechanism to declare "this directory is intentionally ungoverned; do not apply any artifact-level rules here." The `discovery.exclude` can suppress discovery entirely, but that also removes the path from search and orient results.

### 6. Grandfathering / Phase-In Policy

All three repos have pre-existing files that would violate new naming or structure rules. There is no mechanism for "this rule applies to new files only" or "this rule is advisory-only for this directory." The policy applies uniformly to all matching files. For mature repos adopting Steward, this makes many rules immediately unusable at `recommended` severity without a phased rollout path.

### 7. Cross-Artifact Relationship / Index Integrity

Multiple repos have index-like files that should reference a complete set of governed files. `CHANGES.rst` references per-version changelog files. A planning index references ADRs and RFCs. Steward's `index_of` concept is for *maintained* (auto-generated) indexes — it does not validate that a *human-authored* index contains all expected entries. This leaves referential integrity gaps for any repo with a hand-maintained registry or index.

---

## Missing Policy Model Capabilities

### M-1: RST and Non-Markdown Content Governance

**Problem:** Steward is Markdown-native. Content validation (required sections, structural checks) only works for Markdown.
**Impact:** Any repo with RST, AsciiDoc, or other structured documentation formats cannot use Steward's content governance features. The config can express intent, but enforcement is silently absent.
**Note:** This may be by intentional product scope, but the policy model does not communicate this boundary. A maintainer authoring `required_sections` for an RST file receives no error and no indication that the rule will not be enforced.

### M-2: Intra-Family Co-Presence Constraints

**Problem:** No mechanism to express "for every file in family A, a corresponding file in family B (related by slug or derived name) must exist."
**Impact:** Extension governance, plugin governance, test fixture integrity, and any registry/collection pattern with multi-file units cannot be expressed.
**Types of repos unlocked:** Monorepos, plugin ecosystems, framework extension collections.

### M-3: Conditional Artifact Requirements (OR-logic and IF-THEN-logic)

**Problem:** The policy model is additive and always-on. There is no `requires_if`, `one_of`, or `any_of` concept.
**Impact:** Realistic governance often requires conditional logic: "require SECURITY.md or docs/community/vulnerabilities.rst, not necessarily both." Without this, a maintainer must either over-specify (require both) or under-specify (require neither).
**Types of repos unlocked:** Any repo with alternative-but-equivalent governance artifacts, any repo with context-dependent requirements.

### M-4: Changelog / Version Entry Validation

**Problem:** There is no mechanism to validate that a changelog file contains an entry for a specific version, or that changelog entries follow a structural convention at the entry level (not just the file level).
**Impact:** Changelog governance is the most common documentation governance need for any maintained library or tool. Steward can check presence and freshness but not versioned entry completeness.
**Types of repos unlocked:** Every library, tool, or framework with versioned releases.
**Scope:** Core scope, given the product's documentation-governance positioning.

### M-5: Ungoverned Zone Declaration

**Problem:** No config concept for "this directory is intentionally outside Steward's artifact-level governance."
**Impact:** Maintainers must suppress specific rules for specific paths via `path_overrides`. For broad zones (test fixtures, generated output, vendor code), this produces verbose and brittle suppression lists.
**Types of repos unlocked:** Any repo with mixed governed/ungoverned areas.
**Scope:** Core scope. Workaround is verbose.

### M-6: Grandfathering / Phase-In Policy

**Problem:** Rules apply uniformly to all matching files. There is no "applies only to files created after date X" or "advisory-only for this release cycle."
**Impact:** Any mature repo adopting Steward faces an immediate wall of violations for pre-existing files. Without a phase-in mechanism, the only options are: fix everything now (high cost), suppress everything (defeats the purpose), or use `recommended` severity (but then CI cannot gate on it).
**Types of repos unlocked:** Any repo with existing files that predate the governance policy.
**Scope:** Core scope for adoption credibility. Could be as simple as a `new_files_only: true` family option.

### M-7: Cross-File Referential Integrity (Human-Authored Indexes)

**Problem:** `index_of` in Steward declares a maintained (auto-generated) index. There is no equivalent for human-authored indexes that must reference a complete collection.
**Impact:** A planning index, decision register, or changelog stub that should enumerate all governed files in a directory cannot be validated for completeness. The `index_of` auto-maintenance solves the generated case but leaves the human-curated case unaddressed.
**Types of repos unlocked:** Any repo with hand-maintained indexes, registers, or catalogs.
**Scope:** Advanced scope. Could surface as a `check` diagnostic rather than full maintenance.

### M-8: Named Maintainer / Ownership Governance

**Problem:** Steward has no concept of declared maintainers or owners at the policy level. CODEOWNERS-style concepts are entirely absent.
**Impact:** Governance for "this file requires maintainer approval" or "this directory has designated owners" cannot be expressed.
**Types of repos unlocked:** Any community-governed open-source project with named maintainers.
**Scope:** Advanced scope; likely requires integration with hosting platform concepts. May be intentionally deferred.

### M-9: Lifecycle State Transitions (Not Just Vocabulary)

**Problem:** `allowed_values` in `frontmatter_schema` validates that status fields contain permitted values. It does not enforce that transitions between status values are valid.
**Impact:** A governed ADR can be set from `Accepted` back to `Draft` without any diagnostic. A `Deprecated` ADR can omit a `superseded_by` field. The policy expresses vocabulary but not lifecycle contract.
**Types of repos unlocked:** Any repo with formal document lifecycle (ADRs, RFCs, proposals, specifications).
**Scope:** Advanced scope. Would require new config syntax (e.g., `transitions:` declarations with conditional required fields per status value).

---

## Policy Model Improvements with Highest Leverage

### P-1: Non-Markdown Content Policy Transparency (Core Scope)

**Missing capability:** When `required_sections` or `frontmatter_schema` is declared on a family that matches non-Markdown files, Steward should emit a `config doctor` warning: "family `X` declares section requirements on RST files — section validation is only supported for Markdown files."

**Why real maintainers need it:** Without this, the policy silently fails to enforce declared intent. A maintainer believes governance is in place; it is not. This is a credibility problem, not a nice-to-have.

**Repos unlocked or improved:** sphinx, requests, any RST/AsciiDoc documentation project.

**Scope:** Core scope. Requires no new enforcement capability — only a doctor diagnostic.

---

### P-2: `new_files_only` Option on Rules and Families (Core Scope)

**Missing capability:** A boolean option on `artifact_families`, path-policy rules, or `frontmatter_requirements` entries that causes the rule to apply only to files created (by git history) after the policy was added. Alternatively: a `grandfathered_before: {date}` option.

**Why real maintainers need it:** Any mature repo adopting Steward will have hundreds of pre-existing files violating new naming or structure rules. Without phase-in, adoption is blocked.

**Repos unlocked or improved:** vscode (platform-scoped naming would flag thousands of existing files), sphinx (domain naming, test fixtures), any repo over 1 year old.

**Scope:** Core scope for adoption credibility. Implementation note: requires git-date lookup per file, which is available given Steward's existing git-timestamp freshness infrastructure.

---

### P-3: Ungoverned Zone Declaration (Core Scope)

**Missing capability:** A top-level `ungoverned_zones:` section (or `role: excluded` in an artifact declaration) that marks a directory as intentionally outside artifact-level governance — but keeps it in discovery for search and orient.

**Why real maintainers need it:** Every non-trivial repo has areas that are intentionally out of scope: vendor code, generated output, test fixtures, locale resources. The current `path_overrides` mechanism requires per-rule suppression. A zone-level concept is cleaner and harder to accidentally break.

**Repos unlocked or improved:** vscode (extensions/test/ generated/), sphinx (locale/, tests/roots/), requests (tests/).

**Scope:** Core scope. Simple schema addition; behavior is suppress artifact rules, preserve discovery.

---

### P-4: Intra-Family Co-Presence Constraints (Advanced Scope)

**Missing capability:** A `requires_sibling:` declaration on an artifact family specifying that for each matched file, a corresponding file (with a pattern-derived name) must exist in a parallel location.

Example syntax concept:

```yaml
artifact_families:
  - family: extension
    match:
      path_pattern: "extensions/*/package.json"
    requires_sibling:
      pattern: "extensions/{dir}/README.md"
      severity: warning
```

**Why real maintainers need it:** Extension ecosystems, plugin registries, fixture-backed test suites — any repo where a logical unit spans multiple files in parallel directories.

**Repos unlocked or improved:** vscode (extension multi-file completeness), sphinx (extension ↔ test fixture), any plugin-based framework.

**Scope:** Advanced scope. New concept requiring implementation in the family classification and check engine.

---

### P-5: Conditional Required Artifacts / OR-Logic (Advanced Scope)

**Missing capability:** An `any_of:` or `one_of:` block in the artifacts section expressing that at least one artifact in the group must be present.

Example syntax concept:

```yaml
artifacts:
  - any_of:
      description: Security disclosure process must be documented
      paths:
        - SECURITY.md
        - docs/community/vulnerabilities.rst
        - docs/security.md
      importance: required
```

**Why real maintainers need it:** Real governance rarely has exact-file requirements for all concerns. Security docs, contributing guides, and code-of-conduct docs often have multiple acceptable locations or formats.

**Repos unlocked or improved:** requests (security doc alternatives), sphinx (contributing in multiple locations), any repo with format flexibility in governance artifacts.

**Scope:** Advanced scope. Requires changes to artifact resolution and check reporting.

---

### P-6: Changelog Entry Validation (Core Scope)

**Missing capability:** A `changelog:` governance concept in policy.yaml that validates a changelog artifact's entry structure — specifically, that a Markdown changelog file contains a top-level heading for the current version (derivable from a version file or declared pattern).

Example syntax concept:

```yaml
artifacts:
  - path: HISTORY.md
    role: changelog
    changelog:
      entry_pattern: "## {semver} \\([0-9]{4}-[0-9]{2}-[0-9]{2}\\)"
      check_latest_on_version_bump: true
      version_source: pyproject.toml
```

**Why real maintainers need it:** The most common documentation governance failure in maintained libraries is releasing without updating the changelog. Steward checks freshness but does not validate entry completeness. This is a high-signal, low-noise check.

**Repos unlocked or improved:** requests, sphinx (versioned RST changelogs), any library or tool with versioned releases.

**Scope:** Core scope for library/tool repos. Implementation requires version-file parsing and changelog heading pattern matching — both feasible with Steward's existing Markdown engine.

---

### P-7: Lifecycle State Transition Rules (Future Scope)

**Missing capability:** A `transitions:` declaration per artifact family or frontmatter schema that specifies allowed status transitions and conditional field requirements per status value.

Example syntax concept:

```yaml
artifact_families:
  - family: adr
    frontmatter_schema:
      allowed_values:
        status: [Draft, Proposed, Accepted, Superseded, Deprecated]
      transitions:
        Superseded:
          requires_field: superseded_by
        Deprecated:
          requires_field: deprecated_reason
```

**Why real maintainers need it:** Formal document lifecycle is a real governance concern for ADRs, RFCs, proposals, and specifications. Controlled vocabularies catch invalid states; lifecycle rules catch structurally incomplete transitions.

**Repos unlocked or improved:** Any repo with formal decision records, RFCs, specs, or proposals (including Steward itself).

**Scope:** Future scope. Requires extending the frontmatter validation engine with conditional field requirements.

---

### P-8: Human-Authored Index Integrity Check (Advanced Scope)

**Missing capability:** An `index_validates:` option on a declared artifact that causes `steward check` to verify that the file contains links or references to all files in a given directory pattern — without auto-maintaining them.

Example syntax concept:

```yaml
artifacts:
  - path: docs/planning-index.md
    role: guide
    index_validates:
      pattern: "docs/decisions/rfcs/RFC-*.md"
      severity: warning
      description: Planning index should reference all accepted RFCs
```

**Why real maintainers need it:** Human-curated indexes drift over time as new files are added but the index is not updated. This is one of the most common sources of documentation rot. Auto-maintenance via `index_of` solves the case where Steward owns the index, but many indexes are intentionally human-authored with commentary.

**Repos unlocked or improved:** Steward itself (docs/planning-index.md, decision-index.md), sphinx (doc/ index files), any repo with curated navigation hubs.

**Scope:** Advanced scope. Requires reference-link extraction from Markdown (partially available via Steward's existing link-checking) and cross-matching against discovered files.

---

## Summary Assessment

| Repo | Verdict | Primary Limitation |
|------|---------|-------------------|
| microsoft/vscode | Materially Limited | Layer enforcement out of scope; co-presence inexpressible; phase-in mechanism absent; scale makes uniform rules untenable |
| sphinx-doc/sphinx | Workable with Friction | RST content governance silently unenforced; cross-directory fixture integrity missing; versioned changelog completeness missing |
| psf/requests | Workable with Friction | RST content governance; conditional artifact logic missing; changelog entry validation missing |

**Steward's policy model is strongest for:**

- Documentation-centric repos using Markdown
- Repositories with explicit named artifact collections (ADRs, RFCs, planning docs)
- Repos where governance is primarily about file presence, naming conventions, and frontmatter contracts
- Small-to-medium repos where uniform rule application is feasible

**Steward's policy model is weakest for:**

- Monorepos with co-located multi-file units requiring co-presence constraints
- Repos with RST or non-Markdown primary documentation
- Repos with conditional or graduated governance requirements
- Any repo with a changelog that needs entry-level validation
- Mature repos that need phase-in mechanisms for new naming rules
