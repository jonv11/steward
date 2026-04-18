---
type: audit
status: Active
last_updated: 2026-04-18
---

# Rule-System Completeness Audit

**Date:** 2026-04-18  
**Scope:** All 16 validation rules (STWD-001 through STWD-016), the validation engine, the rule registry, and the broader governance contract.  
**Baseline version reviewed:** v0.15.0  
**Reviewer mindset:** Rule system as product. Each rule must justify its existence through clear user value. Diagnostics are only useful if understandable and actionable. Missing governance coverage is as important as incorrect existing coverage.

---

## Executive Summary

The Steward rule system is structurally sound and well-designed. The registry model is clean, the diagnostic schema is machine-readable, the severity model is sensible, and the configuration surface is expressive. At 16 rules the system is already meaningfully complete for its stated pre-1.0 scope.

However, the audit surfaces four categories of deficiency that together constrain release confidence:

1. **Message quality is uneven.** Several rules produce vague or path-absent messages that reduce actionability. The worst offenders are STWD-007, STWD-009, and STWD-012.

2. **Coverage gaps exist at the seams.** Key seam checks are missing: dangling `depends_on` references in maintenance configs, families with `path_pattern` that match no files (partially detected by config doctor but not enforced by check), duplicate artifact paths in policy, and `index_of` self-reference cycles.

3. **STWD-006 is structurally underspecified.** The rule detects two anomalies (empty regions and headings inside steward-owned regions) but its detection logic is a proxy for the real ownership violation it was designed to find. The rule is not wrong, but it is weaker than its stated purpose.

4. **The `IFixableRule` interface is nearly unused.** Only STWD-007 implements auto-fix. Rules with deterministic, safe fixes available (STWD-003 field insertion, STWD-005 missing end-marker) do not implement it, leaving value on the table for both human and agent-assisted workflows.

**Overall maturity:** Pre-production ready. The system is sufficient for self-dogfooding and early adopters but has identifiable gaps that will surface with broader use. The highest-value improvements are achievable within one or two milestones without architectural change.

---

## Rule-by-Rule Review Table

| Rule | Name | Severity | Intent Clarity | Message Quality | Remediation Quality | FP/FN Risk | Recommendation | Priority |
|------|------|----------|---------------|-----------------|--------------------|-----------:|----------------|----------|
| STWD-001 | RequiredArtifactRule | Error/Warning | Strong | Good | Good | Low | Good as-is; minor message clarification | Low |
| STWD-002 | ForbiddenPathRule | Error | Strong | Good | Weak | Low | Improve remediation specificity | Low |
| STWD-003 | RequiredFrontmatterFieldRule | Error | Strong | Good | Good | Medium | Fix allowed-values null gap; add `--fix` support | Medium |
| STWD-004 | SectionSizeRule | Info | Acceptable | Acceptable | Weak | Medium-High | Threshold guidance; clarify info vs warning confusion | Medium |
| STWD-005 | ManagedRegionIntegrityRule | Error | Strong | Good | Adequate | Low | Consider `--fix` for missing end-marker | Low |
| STWD-006 | ManagedScopeViolationRule | Warning | Weak | Weak | Weak | High | Redesign or narrow scope statement | High |
| STWD-007 | StaleArtifactRule | Warning | Strong | Weak | Good | Low | Fix path-absent diagnostic; surface diff detail | Medium |
| STWD-008 | BrokenInternalLinkRule | Warning | Strong | Good | Good | Medium | Scoped validation false positive risk | Medium |
| STWD-009 | BrokenArtifactReferenceRule | Warning | Acceptable | Weak | Adequate | Low | Improve message; clarify overlap with STWD-001 | Medium |
| STWD-010 | NamingConventionRule | Warning | Strong | Good | Good | Low | Silent skip of invalid regex is risky | Medium |
| STWD-011 | IndexCompletenessRule | Warning | Strong | Acceptable | Good | Medium | Scoped FP; depth-of-scan gap | Medium |
| STWD-012 | FreshnessRule | Warning | Strong | Weak | Weak | Medium | Fix message (missing artifact name); improve remediation | High |
| STWD-013 | OrphanedDocumentRule | Info | Strong | Good | Good | Medium | Self-link false negative; large-repo scalability | Low |
| STWD-014 | RequiredSectionsRule | Warning | Strong | Good | Good | Low | Good as-is | Low |
| STWD-015 | FamilyMinCountRule | Warning | Strong | Good | Good | Low | Good as-is | Low |
| STWD-016 | FamilyNamingPatternRule | Warning | Strong | Good | Good | Low | Silent skip of invalid regex is risky | Low |

---

## Detailed Notes Per Rule

### STWD-001 — RequiredArtifactRule

**Purpose:** Ensures required (and recommended) artifacts declared in policy exist on disk.

**Strengths:**
- Correctly uses `AllDiscoveredFiles` to avoid scoped false positives.
- Differentiates required (Error) from recommended (Warning) via `ResolveImportance()`.
- Skips optional artifacts correctly.
- Directory artifact support (`path.EndsWith('/')`) is a good extension.

**Weaknesses:**
- The `Message` for a required artifact is `"Required artifact 'foo' is missing."` — it does not state what role or description the artifact has, which would help the reader understand why it is required without consulting `policy.yaml` directly.
- `Remediation` says only `"Create the file..."`. For role-driven defaults this is sufficient, but for files with meaningful descriptions in policy, surfacing that description in the message would make the diagnostic self-contained.

**Actionable message quality:** Good. Precise path, correct severity, clear what is wrong.

**Remediation quality:** Adequate. The action is clear but generic. Including the artifact's `description` field (if set) would improve context.

**Edge cases / risks:**
- A directory artifact check relies on `f.IsDirectory` being populated by file discovery. If discovery does not surface directory entries, this silently passes. Not a rule defect but a dependency on discovery contract.
- `importance: "recommended"` produces a Warning, but downstream users expecting Error-only CI gates would miss recommended violations. This is correct behavior but should be documented prominently.

**Recommendation:** Good as-is. Optional improvement: include `artifact.Description` in message when set.

**Priority:** Low.

---

### STWD-002 — ForbiddenPathRule

**Purpose:** Detects files matching forbidden path patterns declared in `path-policy.yaml`.

**Strengths:**
- Uses `PathPolicyEngine` for consistent pattern matching.
- Reports the matched pattern, making it clear which rule triggered.
- Clean integration with the path policy model.

**Weaknesses:**
- `Remediation` says only `"Remove or rename the file to comply with repository policy."` This is correct but does not explain *why* the path is forbidden. Path-policy rules can have many different intents (security, naming, structure) and users with no context cannot tell which applies.
- The rule iterates over `TargetFiles` (not `AllDiscoveredFiles`). This is correct for content-scanning rules but means that a forbidden file that was present before the scope window will not be detected in scoped runs. This is a known trade-off but it means the rule is not a reliable gating check in scoped CI pipelines.

**Actionable message quality:** Good. Path and matched pattern are both shown.

**Remediation quality:** Weak. Should surface the policy rationale or at least the ruleset name if available.

**Edge cases / risks:**
- No false-positive risk identified. False negatives are possible in scoped runs as noted above.

**Recommendation:** Improve remediation to surface ruleset name or description from the policy. Document scoped-validation gap explicitly.

**Priority:** Low.

---

### STWD-003 — RequiredFrontmatterFieldRule

**Purpose:** Enforces required frontmatter fields (global, path-scoped, and family-level) in Markdown files.

**Strengths:**
- Three-tier requirement merging (global, scoped, family) is powerful and correctly implemented.
- Family name surfaced in message as `[family: name]` is a significant diagnostic quality improvement.
- `AllowedValues` enforcement integrates cleanly with the family schema.
- Explicit artifact path exclusion from family rules is correct.

**Weaknesses:**
- When a file has frontmatter but the specific field is absent, the line number reported is `doc.Frontmatter.Range.Start` — i.e., line 1 of the frontmatter block. This is acceptable but slightly inaccurate: reporting the first line of frontmatter for a missing field means the user must scan the block manually. Consider reporting the line *after* the last frontmatter field, or simply noting "in frontmatter block" in the message.
- When `allowedValues` check fires and `rawValue` is null/missing, the rule silently skips. A null field that is in the `allowed_values` map is not flagged as missing — only as having a bad value. But if the field is not in `effectiveFields`, it won't be required. This means `allowed_values` without a corresponding `required_fields` entry can produce a silent gap: the field is neither required nor checked if absent.
- `io/permissions` error handling during parse downgrades to `DiagnosticSeverity.Warning`. This means a file that cannot be read will generate a Warning rather than an Error — which is arguably correct (we cannot know the intent) but may cause confusion.
- No `IFixableRule` implementation. Adding a missing required field has a well-defined fix (append to frontmatter with a placeholder value). This is a high-value auto-fix opportunity.

**Actionable message quality:** Good. Path, field name, and family context are all present.

**Remediation quality:** Good. `"Add 'field' to the frontmatter block."` is clear.

**Edge cases / risks:**
- Family classification requires frontmatter parsing *before* classification, creating a circular dependency: we need frontmatter to classify, but classification determines which fields are required. Current implementation handles this correctly (parse first, classify second), but any future lazy-parse optimization must preserve this order.
- If a file matches multiple `scopedRequirements` patterns, last-writer wins for `allowed_values` across overlapping patterns. This is the documented FIFO behavior but may surprise users who expect additive union semantics.

**Recommendation:** Fix the allowed-values null gap (require the field if it is in `allowed_values` and not in `required_fields`). Consider `IFixableRule` implementation. Document the last-writer-wins precedence for overlapping scoped requirements.

**Priority:** Medium.

---

### STWD-004 — SectionSizeRule

**Purpose:** Warns when a Markdown section exceeds a line-count threshold, signaling it should be split.

**Strengths:**
- Default threshold of 500 is reasonable for most documentation repos.
- Recursive section traversal covers nested sections.
- Reports line number for navigation.
- Severity `Info` is the correct default: this is an advisory observation, not a policy violation.

**Weaknesses:**
- The rule fires on every Markdown file, including generated artifacts, test fixtures, and files where section size is not meaningful (e.g., changelog entries, generated manifests). There is no exclude mechanism except the global `disabled_rules` or path-level override, forcing users to either disable globally or write per-path suppressions.
- `Remediation` says `"Consider splitting this section into smaller subsections."` — correct but the most generic possible guidance. For a rule that is `Info` severity, this may be acceptable, but if a team intends this to be actionable, the guidance should say what a well-structured split looks like.
- The rule is category `governance` but the diagnostic table in RFC-003 lists `structure` as the section-checking category. Minor inconsistency.
- The threshold of 500 applies to every section in the document, including top-level sections that naturally contain multiple subsections. A top-level section with 5 subsections of 100 lines each will correctly not fire, but a flat long section will fire even if it cannot meaningfully be split (e.g., a glossary or reference list).

**Actionable message quality:** Acceptable. Reports section title, actual line count, and threshold.

**Remediation quality:** Weak. Generic advice. Should suggest specific approaches: extract to separate document, introduce subsections, or suppress with `disabled_rules`.

**Edge cases / risks:**
- False positive risk on generated documents, long glossaries, reference appendices.
- No mechanism to suppress per-section (e.g., a frontmatter flag or inline comment).

**Recommendation:** Add support for a per-artifact or per-family `exclude_from_size_check` flag, or at minimum document the path-level override mechanism in the remediation message. Improve remediation text with concrete options.

**Priority:** Medium.

---

### STWD-005 — ManagedRegionIntegrityRule

**Purpose:** Ensures managed region markers (`<!-- steward:begin ... -->` / `<!-- steward:end -->`) are properly paired.

**Strengths:**
- Stack-based detection correctly handles nested regions.
- Reports both orphaned-end and unclosed-begin cases.
- Detects missing `id` attribute on begin markers.
- Severity `Error` is correct: unpaired markers indicate a structural corruption that will break maintenance tools.

**Weaknesses:**
- The `Source` field is `null` in all diagnostics. This is the rule with the clearest source (the inline marker location in the file), yet it provides no source reference. Providing the marker text would improve traceability.
- `ExtractAttribute` uses `IndexOf` and is not robust to multiple attributes in arbitrary order (e.g., `id="x" owner="steward"`). The current marker format may have stricter conventions, but this is not validated — a marker with `owner="steward" id="x"` would fail the extraction.
- For the unclosed-begin case, the reported line number is the begin marker, which is correct. For the orphaned-end case, the line number is the end marker — also correct. Good.
- No `IFixableRule`. The missing-end-marker case has a mechanically obvious fix (append `<!-- steward:end -->`), though the exact placement requires understanding the intended scope of the region.

**Actionable message quality:** Good. Specific line numbers and region IDs are reported.

**Remediation quality:** Adequate. Instructions are actionable but do not explain where to add the marker.

**Edge cases / risks:**
- End-marker matching is not ID-aware: a `<!-- steward:end -->` closes the most recent open region regardless of its ID. This means mismatched begin/end pairs (e.g., nested regions where one is closed out of order) will not be detected as such — only that the final balance is wrong. This could lead to false-negative scenarios where a region is technically "balanced" but semantically mismatched.
- The line-split uses `'\n'` only, so Windows CRLF files will have `'\r'` appended to each line. The rule does `.TrimEnd('\r')` which handles this, but only on the marker lines — it does not normalize the full content. This is a minor robustness gap.

**Recommendation:** Make `ExtractAttribute` order-independent. Consider implementing `IFixableRule` for the missing-end case. Document the CRLF handling behavior. Add ID-aware end-marker matching to detect cross-nested mismatches.

**Priority:** Low.

---

### STWD-006 — ManagedScopeViolationRule

**Purpose:** Detects unauthorized modifications to content inside managed/generated regions.

**Strengths:**
- The concept is valuable: protecting machine-managed content from human edits is a meaningful governance concern.
- Empty-region detection is useful as a secondary check.
- Uses the document cache for efficient parse.

**Weaknesses:**
- The rule's stated purpose — "Content in managed regions must only be modified by the declared owner" — cannot actually be enforced by content inspection alone. Git blame or diff history is needed to determine *who* modified content. The rule instead detects structural anomalies as proxies for violations.
- Empty region detection: an empty region may be intentional (freshly scaffolded, awaiting `maintain` run). The diagnostic says to run `steward maintain` but does not clarify that this is not necessarily an error — it may be expected state immediately after init.
- Heading-in-managed-region detection: this only checks `owner == "steward"`. Non-steward owners (other tools or agents) are not checked at all. The rule description says "only by the declared owner" but the implementation only enforces this for `owner: steward`.
- The rule's `Source` field is set to `file.RelativePath` (the file being checked) rather than the policy source. While this is informative, it differs from other rules where `Source` identifies the policy file that created the expectation.
- No coverage for: detecting content hash changes inside a managed region (would require baseline comparison), detecting manual edits to generated lists, detecting format corruption inside a steward-managed table.

**Actionable message quality:** Weak. The "may have been manually inserted" phrasing is hedged. A heading inside a steward region is either a violation or it is not — the hedge adds confusion.

**Remediation quality:** Weak. "Avoid manually editing" is advisory, not actionable. Users who encounter this need to know: was my edit wrong, or does the maintenance system need to be run?

**Edge cases / risks:**
- High false-positive risk for the heading check: any heading that was generated by a previous `steward maintain` run and then a subsequent maintain run produced different output would still appear to be "inside a managed region" and trigger the rule even when no human violation occurred. This happens when maintain output includes headings.
- The empty-region check fires only when `region.Range.End - region.Range.Start <= 1`. A region with a single blank line would pass as "not empty" even though it has no meaningful content.

**Recommendation:** Narrow the rule's stated purpose to match what it can actually detect. Replace "Content in managed regions must only be modified by the declared owner" with "Managed regions must have non-empty content and steward-owned regions must not contain manually-inserted headings." Consider removing the heading check entirely (it has high false-positive risk and unclear business value) and replacing it with a content-hash-based comparison once that infrastructure exists.

**Priority:** High.

---

### STWD-007 — StaleArtifactRule

**Purpose:** Detects maintained artifacts that are out of sync with what `steward maintain` would produce.

**Strengths:**
- Implements `IFixableRule` — the only rule to do so.
- Uses `AllDiscoveredFiles` to avoid false positives in scoped validation.
- Correctly delegates evaluation to `MaintenanceEngine`, keeping the rule thin.
- Fix computation is deterministic and correct.

**Weaknesses:**
- `Path` in the diagnostic is `action.ArtifactPath` — which is the artifact being maintained. This is the correct path. However, when `ArtifactPath` is null or empty (which should not happen but is not guarded), `Path` would be null, silently producing a pathless diagnostic that cannot be filtered by path-level overrides.
- The `Message` is `"Maintained artifact '{action.ArtifactId}' is stale. {action.Description}"`. The `Description` field comes from `MaintenanceAction.Description`, which may be empty — leaving a trailing space in the message. More critically, the message does not say *how* the artifact is stale (what changed). A diff summary (even just "content differs" vs "section was removed") would make this significantly more actionable.
- `DefaultSeverity` is `Warning`. RFC-003 lists STWD-007 as a "completion policy" rule that is surfaced as part of `steward check`. If a stale maintained artifact indicates that the repo is out of date (a concrete governance violation), the case for `Error` severity is strong. Currently this is advisory.
- No guard for the case where the maintenance engine itself throws. The rule does not catch exceptions from `MaintenanceEngine.Evaluate`, which could cause the entire validation run to fail with an unhandled exception rather than a diagnostic.

**Actionable message quality:** Weak. Reports the artifact ID and a description, but not what specifically is stale or how far the current state has drifted.

**Remediation quality:** Good. `"Run 'steward maintain --apply' or 'steward check --fix'"` is precise.

**Edge cases / risks:**
- Fix correctness depends on `MaintenanceEngine` being deterministic. If the engine has any non-deterministic behavior (timestamps, ordering), the fix could produce different output each time, causing the rule to perpetually fire after `--fix` is applied.

**Recommendation:** Add null guard on `ArtifactPath`. Add exception handling around `MaintenanceEngine.Evaluate`. Improve message to surface what specifically is stale. Consider promoting severity to `Error` for artifacts whose staleness indicates a governance violation.

**Priority:** Medium.

---

### STWD-008 — BrokenInternalLinkRule

**Purpose:** Detects Markdown links that point to files that do not exist.

**Strengths:**
- Uses Markdig for accurate link extraction with source location.
- Correctly strips fragments and query strings before resolution.
- Correctly skips external URLs, mailto, tel, data URIs.
- `ResolveLinkTarget` normalizes `../` traversal correctly.
- `ExtractInternalLinks` is a public static method, reused by STWD-011 and STWD-013.

**Weaknesses:**
- **Critical false-positive risk in scoped runs:** The `existingPaths` set is built from `context.TargetFiles`, not `context.AllDiscoveredFiles`. This means that in a `--scope changed` run, a link in a changed file pointing to an unchanged (but existing) file will be reported as broken because the unchanged file is not in `TargetFiles`. This is a significant usability bug — it will produce broken-link noise every time a file with internal links is modified.
- The rule only checks `.md` target files — it does not check links to other file types (`.yaml`, `.json`, images, scripts). This is a documented scope narrowing but is not explicit in the rule's description.
- Fragment-only links (`#heading`) are stripped and resolved to the source file, then checked against existingPaths. If the source file exists (it always does — we're iterating it), this check will always pass, making fragment-only links silently unchecked even when the heading does not exist.
- Line numbers are 0-based in Markdig and the rule correctly adds 1 (`link.Line + 1`).

**Actionable message quality:** Good. Reports source file, target, and line number.

**Remediation quality:** Good. `"Verify the link target exists or update the reference."` is actionable.

**Edge cases / risks:**
- Scoped validation false positives (see critical weakness above).
- Non-`.md` targets are silently skipped without indication.
- Image links and asset links are not checked.

**Recommendation:** Fix `existingPaths` to use `AllDiscoveredFiles ?? TargetFiles` (same pattern as STWD-001). This is the most important fix for this rule. Document the `.md`-only scope limitation.

**Priority:** Medium (fix is simple and high-impact).

---

### STWD-009 — BrokenArtifactReferenceRule

**Purpose:** Detects policy-declared artifact paths that do not resolve to existing files, for artifacts that are not already required (those are handled by STWD-001).

**Strengths:**
- Correctly avoids double-reporting with STWD-001 by checking `ResolveImportance() != "required"`.
- Uses `AllDiscoveredFiles` for existence checks.
- Handles directory artifacts correctly (same logic as STWD-001).

**Weaknesses:**
- The message `"Policy artifact 'path' (role: unspecified) does not exist."` is weak when `artifact.Role` is null, producing a literal `"role: unspecified"` in the output. This is uninformative and slightly unprofessional.
- The rule name and description say "broken reference" but the rule checks policy-declared paths. A genuine "broken reference" in common parlance means a pointer in document A to document B where B no longer exists — that is what STWD-008 checks for links. STWD-009 is more accurately "orphaned policy declaration" — a policy entry pointing to a non-existent file.
- `Remediation` includes `"mark it as required if it is mandatory"` as one of the three options. This will *promote* the diagnostic from Warning to Error (via STWD-001) but will not fix the underlying missing file. The remediation is semantically odd: it suggests escalating a problem rather than solving it.
- The `recommended` importance case is handled differently by STWD-001 (reports as Warning) vs STWD-009 (skips because `ResolveImportance() != "required"`). This means a `recommended` artifact that does not exist gets a Warning from STWD-001, not STWD-009. This is correct but creates an asymmetry that is not obvious from reading either rule in isolation.

**Actionable message quality:** Weak. Artifact path and role are shown, but "unspecified" role is noise.

**Remediation quality:** Adequate. The three options are reasonable but the "mark as required" suggestion is misleading.

**Edge cases / risks:**
- An artifact with `importance: "recommended"` that does not exist will be flagged by STWD-001 (as Warning) and skipped by STWD-009. If a user disables STWD-001, STWD-009 will not catch the case either. There is no fallback.

**Recommendation:** Suppress "role: unspecified" — omit the role clause entirely when null. Remove the "mark as required" option from remediation. Rename the rule conceptually to better reflect "orphaned policy declaration." Consider whether STWD-009 is needed at all given STWD-001's coverage.

**Priority:** Medium.

---

### STWD-010 — NamingConventionRule

**Purpose:** Enforces filename naming conventions declared as `must_match` regex patterns in `path-policy.yaml`.

**Strengths:**
- Regex has a 1-second timeout, preventing catastrophic backtracking.
- Reports both the filename and the expected pattern.
- Remediation includes the pattern to match.
- Correctly uses `Path.GetFileName` (not the full path) as the match target.

**Weaknesses:**
- Invalid regex patterns in `path-policy.yaml` are silently skipped with a comment that "config validate should catch these." However, the silent skip behavior is not visible to the user at runtime — there is no warning that a naming rule was skipped due to invalid regex. A user who writes an invalid regex will see no enforcement and no error.
- The rule checks `file.RelativePath` against the glob pattern, then `Path.GetFileName(file.RelativePath)` against the regex. This means the naming constraint applies to the filename only, not the full path. This is the correct behavior (naming rules are about filenames) but it is not obvious from the configuration — a user might expect `must_match` to apply to the full path.
- The rule iterates `context.TargetFiles`, not `AllDiscoveredFiles`. For a scoped run that adds a new file, the new file will be checked. For a scoped run that does not touch a pre-existing file with a bad name, the violation will not surface. This is expected behavior but means naming violations in existing files are only caught on full-scope runs.

**Actionable message quality:** Good. Shows filename, expected pattern, and governing glob.

**Remediation quality:** Good. Includes the pattern to rename to.

**Edge cases / risks:**
- Silent invalid-regex skip could mask misconfiguration.
- Scoped runs only check files in scope.

**Recommendation:** Emit a `Warning` diagnostic (rather than silently skipping) when a naming rule's regex fails to compile at runtime. The config validator should be the primary detection mechanism, but a runtime fallback prevents silent non-enforcement.

**Priority:** Medium.

---

### STWD-011 — IndexCompletenessRule

**Purpose:** Ensures all `.md` files in a directory declared as `index_of` are linked from the index document.

**Strengths:**
- Reuses `BrokenInternalLinkRule.ExtractInternalLinks` and `ResolveLinkTarget` for consistent link resolution.
- Correctly excludes the index file itself from the check.
- Includes the index path in the remediation message.
- Skips gracefully when the index file does not exist (avoids cascading with STWD-001).

**Weaknesses:**
- **Scoped validation behavior:** `filesInScope` is built from `context.TargetFiles`. In a scoped run, only files in scope are checked for index coverage. A new file added to an indexed directory will correctly be checked. But pre-existing unlinked files will not be caught unless they happen to be in scope. For a rule about index completeness, this creates a false-completeness illusion in CI pipelines running scoped validation.
- The rule only checks direct children and subdirectory files that match the `sourceDir` prefix. There is no depth limit — it will match all `.md` files recursively under `sourceDir`. For a large directory hierarchy with subdirectories, this could produce hundreds of diagnostics if a new directory is created without updating the index. The rule may need a `recursive: false` option.
- If `artifact.IndexOf` points to the same directory as `artifact.Path`, the rule will attempt to check whether the index file itself is linked from itself — which is excluded. Good. But if the index points to a parent directory that contains itself, the path traversal could match the index. This edge case is handled by the explicit exclusion of `indexPath`.
- Fragment links to sections within files are stripped before resolution. This means `[Title](file.md#section)` correctly resolves to `file.md` and counts as a reference. Good.

**Actionable message quality:** Acceptable. Reports the unlisted file and names the index. Could include the index path more prominently.

**Remediation quality:** Good. Specific, actionable, includes both the file to add and the index to update.

**Edge cases / risks:**
- Scoped validation false completeness (unlisted files not caught in scoped runs).
- Potentially large diagnostic volume for new subdirectories.

**Recommendation:** Change `filesInScope` to use `AllDiscoveredFiles ?? TargetFiles` filtered to the `sourceDir`. Add a documented depth-limit option. Document scoped behavior explicitly.

**Priority:** Medium.

---

### STWD-012 — FreshnessRule

**Purpose:** Detects artifacts that have not been updated within the declared `max_age_days` window.

**Strengths:**
- Two-tier timestamp resolution: `last_updated` frontmatter (explicit intent) > filesystem mtime (fallback).
- Role-linked defaults avoid requiring per-artifact freshness configuration.
- Correctly skips artifacts that do not exist (avoiding cascading with STWD-001).
- The `state-document` role default of 30 days is a sensible and opinionated default.

**Weaknesses:**
- **Critical message quality gap:** The message is `"File is {N} days old (max: {max} days)."` — it does not include the artifact's path or name. Diagnostics for this rule always have `Path: artifactPath` set, so the path is available in the structured output. But in the plain-text output (which formats as `WARN  freshness  {path}`), users will see the path separately. However, the message string alone — as used in log aggregation, JSON output snippets, or programmatic processing — is not self-contained.
- **Remediation is generic:** `"Update the document content and its 'last_updated' frontmatter field."` This is correct but gives no guidance on *what* should change or how to assess whether the document's content is actually stale vs. simply not recently touched. For a state document, "update the content" means reviewing it for accuracy — a non-trivial task the remediation should at least acknowledge.
- **Filesystem mtime is unreliable:** On many CI systems, git checkout resets all file modification times to the current time, making filesystem mtime useless as a freshness signal. The rule's fallback to filesystem mtime will produce systematic false negatives (all files appear fresh) or false positives (all files appear stale), depending on the CI environment. The `last_updated` frontmatter field is more reliable, but it requires manual maintenance.
- The rule fires for every artifact with a freshness window, regardless of whether the artifact is required or optional. An optional artifact that is not present is skipped (correct). But an optional artifact that *is* present and old will fire. This may be surprising — optional presence does not necessarily imply freshness obligation.

**Actionable message quality:** Weak. Message is self-contained only if the reader has access to the full structured diagnostic (path is separate).

**Remediation quality:** Weak. Too generic for a rule that requires meaningful judgment about document content.

**Edge cases / risks:**
- CI environment mtime unreliability produces systematic false results.
- Optional artifacts with stale timestamps produce warnings even when staleness has no governance implication.

**Recommendation:** Include artifact path in message text: `"'path' is {N} days old (max: {max} days)."`. Add a note in remediation about git-checkout-mtime unreliability and recommend relying on `last_updated` frontmatter. Consider a config option to disable mtime fallback for environments where it is unreliable.

**Priority:** High.

---

### STWD-013 — OrphanedDocumentRule

**Purpose:** Detects Markdown files not referenced by any navigation surface (start_here, artifact declarations, or internal links).

**Strengths:**
- Three-tier reachability model (start_here, artifacts, links) is well-designed.
- `standalone: true` frontmatter escape hatch is clean and self-documenting.
- `Info` severity is appropriate — orphaning is a discoverability concern, not a policy violation.
- Reuses `BrokenInternalLinkRule` for consistent link extraction.

**Weaknesses:**
- **Self-link false negative:** The rule builds `referencedPaths` from all links found in all Markdown files. If document A links to itself (e.g., via an anchor link `[Title](#anchor)`), `ResolveLinkTarget` will resolve the fragment-stripped target as the source file path, marking it as referenced. This means a file that only self-links will not be reported as orphaned. This is unlikely in practice but is a correctness gap.
- **Scalability:** The rule reads every Markdown file in the target set to extract links, then re-reads every Markdown file to check for standalone frontmatter. For large repos with hundreds of Markdown files, this double-read is significant. The `DocumentCache` helps but only for files already parsed.
- **Scoped validation gap:** In a scoped run, `mdFiles` is filtered to `TargetFiles`. New files added to the repo that are not yet linked anywhere will correctly be flagged. But pre-existing orphaned files outside scope will not be re-evaluated. The rule's `Info` severity means this is not a blocking concern.
- The `HasStandaloneFrontmatter` method is a raw-string YAML parser rather than using the shared frontmatter infrastructure. This creates two parsing paths for the same file, with potential for divergence if frontmatter syntax evolves.

**Actionable message quality:** Good. File path is included and the failure reason is clear.

**Remediation quality:** Good. Two concrete options (add a link, or set standalone).

**Edge cases / risks:**
- Self-link false negative.
- `HasStandaloneFrontmatter` divergence from main parser.
- Large-repo double-read cost.

**Recommendation:** Replace `HasStandaloneFrontmatter` raw parser with the shared `MarkdownParser`/`DocumentCache`. Add a note in the description about self-link edge case.

**Priority:** Low.

---

### STWD-014 — RequiredSectionsRule

**Purpose:** Ensures files matched by an artifact family contain all required headings declared in `required_sections`.

**Strengths:**
- Case-insensitive heading comparison is the right default.
- Flattened heading collection checks all levels, not just top-level.
- Family name included in message for traceability.
- Correctly excludes explicit artifact paths.
- Suggestion to use `## Heading` in remediation is helpful.

**Weaknesses:**
- The rule checks for *presence* of a heading anywhere in the document, regardless of heading level or nesting. A `required_sections: [Summary]` configuration would be satisfied by a heading at any level (H1–H6). This may or may not be the intent — if the governance requires a top-level section, a deeply nested heading would pass incorrectly.
- `IOException`/`UnauthorizedAccessException` are silently swallowed (`continue`) without emitting any diagnostic. This means a file that cannot be read will silently pass STWD-014 but fail (with Warning) in STWD-003. The inconsistency is minor but observable.
- No `required_sections` ordering check. Two documents in the same family could have sections in completely different orders and both pass. For governance purposes this may be acceptable, but for families that define a canonical structure, order matters.

**Actionable message quality:** Good. Missing section name, file path, and family are all included.

**Remediation quality:** Good. Provides an example heading syntax.

**Edge cases / risks:**
- Level-agnostic heading match may satisfy the rule with a deeply buried heading when a top-level section was intended.
- Silent parse failure.

**Recommendation:** Consider an optional `heading_level: 2` constraint on required sections for families where structure matters. Emit a diagnostic (Warning) rather than silently continuing on parse failure. Good as-is for the current use case.

**Priority:** Low.

---

### STWD-015 — FamilyMinCountRule

**Purpose:** Ensures artifact families with `directory_expectations.min_count` contain at least the declared number of matched files.

**Strengths:**
- Uses `AllDiscoveredFiles` correctly for repo-wide count.
- Null-guard on path-pattern matching is correct (family with only frontmatter match still counts correctly when `frontmatterFields: null` is passed for count-only evaluation).
- Wait — this is actually a weakness: see below.
- `DisplayName` is surfaced in the diagnostic for readability.
- `description` from `DirectoryExpectations` appended to message is a nice extensibility point.

**Weaknesses:**
- **Classification with `frontmatterFields: null`:** The classifier is called with `frontmatterFields: null` for the count. A family that uses frontmatter match criteria (e.g., `match: { frontmatter: { type: adr } }`) will *never* match with null frontmatter, causing the count to always be 0 regardless of how many matching files exist. This means STWD-015 silently undercounts for frontmatter-matched families. The family appears to have 0 files and fires min_count violations even when files exist. This is a functional correctness bug.
- The diagnostic has `Path: null` (no specific file path) because this is a directory-level obligation. This is correct but means path-level overrides cannot suppress this rule for specific directories — only global `disabled_rules` can suppress it.
- Remediation says `"Add at least {min_count - actual} more file(s) matching the '{family.Family}' family pattern."` For frontmatter-matched families, "matching the pattern" is ambiguous — there is no path pattern to match.

**Actionable message quality:** Good. Shows actual vs. required count and family name.

**Remediation quality:** Adequate. Clear action but ambiguous for frontmatter-matched families.

**Edge cases / risks:**
- **Functional correctness bug:** frontmatter-matched families always count as 0, causing spurious min_count violations.
- No path-level suppressibility.

**Recommendation:** Fix the frontmatter-only family count: either read frontmatter for counting (adding a parse cost) or document that `min_count` only works for `path_pattern`-based families. This is a correctness bug that should be addressed before broader release.

**Priority:** High (correctness bug for frontmatter-matched families).

---

### STWD-016 — FamilyNamingPatternRule

**Purpose:** Enforces filename `naming_pattern` regex declared on artifact families.

**Strengths:**
- Case-insensitive matching by default (reasonable for file naming).
- Regex timeout (1 second) prevents catastrophic backtracking.
- Reports both the filename and the expected pattern.
- Correctly excludes explicit artifact paths.

**Weaknesses:**
- Same silent-skip behavior as STWD-010: invalid regex patterns are caught and swallowed. No runtime warning is emitted. The config validator should catch these, but silent runtime non-enforcement is a reliability gap.
- The `compiled` list is built by iterating families with `naming_pattern`, then the `classifier` is built from `compiled.Select(c => c.Definition)`. If `compiled` is a subset of `families` (i.e., some families have no naming_pattern), the classifier will not match files for families without a naming_pattern. This is correct — those families are not checked. But if a file matches both a family with a naming_pattern and a family without one, the classifier returns the first match (by declaration order), which may not be the family with the naming constraint. This could produce a false negative.
- Remediation includes the raw regex string. For families with complex regex patterns, this may not be human-readable. A `display_pattern` or example filename would be more helpful.

**Actionable message quality:** Good. Shows filename, expected regex, and family.

**Remediation quality:** Adequate. Pattern is shown but raw regex is not always human-readable.

**Edge cases / risks:**
- Silent invalid-regex skip.
- Declaration-order dependent false negatives for files matching multiple families.

**Recommendation:** Emit a Warning when a naming_pattern fails to compile at runtime (same recommendation as STWD-010). Consider adding an optional `pattern_example` field to family definitions for human-readable remediation.

**Priority:** Low.

---

## Diagnostic Quality Findings

### Cross-cutting message quality issues

1. **Missing path in message text:** STWD-012 does not include the artifact path in the message string. While the structured `Path` field is set, this makes the message non-self-contained for log aggregation and programmatic consumers.

2. **Hedged language in violation messages:** STWD-006 uses "may have been manually inserted" — hedged phrasing in a diagnostic message creates user confusion. A diagnostic should state what the observed condition is, not speculate about its cause.

3. **Role: unspecified noise:** STWD-009 emits `(role: unspecified)` when no role is set, which looks like a placeholder rather than meaningful information.

4. **Inconsistent Source field population:** Most rules set `Source: "policy.yaml"` or `Source: "path-policy.yaml"`. STWD-005, STWD-006 (partially), STWD-007, STWD-008, STWD-009, STWD-011, STWD-012, STWD-013 use `null` or the file path. The RFC-003 schema specifies `source` as optional, but inconsistent population reduces machine-readability and explainability.

5. **Error vs Warning severity ambiguity at the rule level:** RFC-003 defines `error` as "must be fixed (affects exit code)" and `warning` as "recommended." Several rules that govern meaningful policy compliance (STWD-007 stale artifact, STWD-012 freshness) are `Warning` by default. The case for optionally promoting these to `Error` via `severity_overrides` is well-supported, but the default choices may under-signal governance requirements.

### Line number quality

- STWD-003: reports frontmatter block start, not the specific missing field location. Acceptable.
- STWD-004: reports section start. Correct and useful.
- STWD-005: reports marker line. Correct.
- STWD-006: reports region start or section start. Correct.
- STWD-008: reports link line (Markdig 0-based + 1 correction). Correct.
- All others: `null` (no line number). Correct where line-level attribution is not meaningful.

---

## Remediation Quality Findings

### Issues requiring correction

1. **STWD-009 misleading option:** "mark it as required" in remediation escalates rather than solves. Remove.

2. **STWD-012 mtime warning absent:** The remediation does not warn about git-checkout mtime unreliability in CI. Should note that `last_updated` frontmatter is preferred.

3. **STWD-006 unclear remediation:** "Avoid manually editing" does not tell users what to do if they legitimately need to modify content in a managed region. Should explain how to extend maintenance configuration instead.

### Missing `IFixableRule` implementations

The following rules have deterministic, safe auto-fix candidates that do not currently implement `IFixableRule`:

| Rule | Fixable scenario | Fix description |
|------|-----------------|-----------------|
| STWD-003 | Missing required field | Append field with placeholder value to frontmatter block |
| STWD-005 | Unclosed begin marker | Append `<!-- steward:end -->` at end of file or at heuristic position |
| STWD-001 | Missing optional/recommended artifact | Create scaffold file at declared path |

STWD-001 auto-fix is lower value (content must be authored by user). STWD-003 and STWD-005 are high-value because they have unambiguous, safe, idempotent fixes.

---

## False-Positive / False-Negative Risks

| Rule | Risk Type | Scenario | Severity |
|------|-----------|----------|----------|
| STWD-006 | False positive | Heading inside managed region was generated by `maintain`, not manually inserted | High |
| STWD-008 | False positive | Scoped run: link target exists but is outside TargetFiles set | High |
| STWD-010 | False negative | Invalid regex in path-policy silently skips naming enforcement | Medium |
| STWD-011 | False negative | Scoped run: pre-existing unlisted files not in scope | Medium |
| STWD-012 | False positive | CI checkout resets mtime, all files appear stale | High |
| STWD-015 | False positive/negative | Frontmatter-matched families always count as 0 files | High (correctness bug) |
| STWD-016 | False negative | Invalid regex silently skips naming enforcement | Medium |
| STWD-004 | False positive | Generated files, glossaries, or reference lists trigger section size warning | Medium |
| STWD-013 | False negative | Self-linking file is not flagged as orphan | Low |

---

## Coverage Gaps

The following governance checks are not covered by any current rule:

### Gap 1: Dangling `depends_on` references in maintenance config

`MaintenanceArtifactDef.DependsOn` is a list of artifact IDs. No rule checks that these IDs resolve to declared maintenance artifacts. A typo in `depends_on` silently breaks the dependency chain, causing maintenance to run in wrong order or not at all.

**Why it matters:** `depends_on` is a correctness-critical field for multi-step maintenance pipelines. A dangling reference is a silent governance failure.

**Proposed rule:** STWD-XXX (ValidMaintenanceDependsOnRule) — Error severity. Checks that all IDs in `depends_on` lists resolve to declared maintenance artifact IDs in the same policy.

### Gap 2: Duplicate artifact paths in policy.yaml

Multiple `artifacts[]` entries can declare the same `path`. There is no deduplication rule. The second declaration silently shadows or supplements the first, with undefined precedence behavior.

**Why it matters:** Duplicate paths produce confusing diagnostics (same file reported under different contexts) and make policy harder to reason about.

**Proposed rule:** STWD-XXX (DuplicateArtifactPathRule) — Warning severity. Checks for duplicate `path` values in `artifacts[]`.

### Gap 3: Artifact families with no matched files (runtime, not config doctor)

`config doctor` reports families whose path_pattern matches no files. But this check is only available when running `config doctor` explicitly — it is not surfaced during `steward check`. A family configured but matching nothing will silently produce no family-level diagnostics, including no `min_count` enforcement.

**Why it matters:** A silently empty family means governance policies (frontmatter requirements, naming, sections) are not being enforced on the document type they were written for.

**Proposed rule:** STWD-XXX (EmptyFamilyRule) — Warning severity. During check, report any artifact family (with at least one governance rule) that matches zero files. Distinct from STWD-015 which requires `min_count` — this fires even without a `min_count` declaration.

### Gap 4: index_of pointing to a non-existent directory

If `artifact.IndexOf` points to a directory that does not exist, STWD-011 silently skips (no files in scope match the prefix). This is partially handled by STWD-001 (the directory artifact would be missing) but only if the directory is declared as a required artifact. An orphaned `index_of` reference produces no diagnostic.

**Proposed rule:** STWD-XXX (ValidIndexOfReferenceRule) — Warning severity, or extend STWD-011 to explicitly report when `sourceDir` matches no discovered files.

### Gap 5: Config-level semantic validation during `check`

`config validate` catches semantic errors (bad rule IDs, invalid regex, bad `depends_on` references). But `config validate` is a separate command from `check`. Users running only `steward check` will not see config semantic errors. These are currently silent.

**Why it matters:** A user who misconfigures a rule (e.g., writes an invalid severity override value) will see no error from `check` — the override is silently ignored. The only path to detection is running `config validate` separately.

**Proposed approach:** Either integrate config validation into the `check` execution path (emit Info diagnostics for config issues), or ensure that the check summary includes a warning when config has known validation issues.

### Gap 6: Artifact role completeness (unknown roles)

`RoleDefaults` maps 7 known roles. If an artifact declares `role: unknown-type`, `GetDefaultImportance` returns null and the artifact defaults to `optional`. No rule warns about unknown role values. This creates a governance gap: a user who misspells a role (e.g., `role: state_document` vs `role: state-document`) will silently get no role-linked freshness defaults.

**Proposed rule:** STWD-XXX (ValidArtifactRoleRule) — Info/Warning severity. Report artifacts with `role` values not in the known role vocabulary.

### Gap 7: `path_override` suppressing non-existent rule IDs

A `path_overrides` entry that references a non-existent rule ID silently does nothing. `config doctor` detects dead suppressions in `disabled_rules` but may not cover `path_overrides.disabled_rules`. Should be verified.

---

## Redundancies and Overlaps

### STWD-001 and STWD-009

These rules overlap for `recommended` artifacts:
- STWD-001 reports `recommended` artifacts as Warning.
- STWD-009 skips `required` artifacts (handled by STWD-001) but reports non-required artifacts as Warning.

A `recommended` artifact that is missing will be reported by STWD-001 (not STWD-009). An `optional` artifact that is missing will be reported by STWD-009. This is a coherent split but is not obvious from reading either rule.

**Assessment:** The split is intentional but should be documented in the rule descriptions or developer notes to prevent future accidental duplication.

### STWD-010 and STWD-016

Both rules enforce filename naming conventions via regex. STWD-010 is driven by `path-policy.yaml` (path-level naming), STWD-016 by `artifact_families` in `policy.yaml` (family-level naming). A file can theoretically match both and receive two naming diagnostics — from different regex patterns, from different config files.

**Assessment:** Not redundant, but potentially confusing in output. Should be noted in documentation. No change required to rule implementations, but the output could benefit from clearer attribution.

### STWD-006 and STWD-005

STWD-005 validates marker structure; STWD-006 validates marker content integrity. These are complementary, not overlapping. However, a file with a broken STWD-005 marker structure will also potentially trigger STWD-006 for the same region, producing multiple diagnostics for the same root cause. The ordering in `RuleRegistry` (STWD-005 before STWD-006) means the user will see STWD-005 first, but there is no short-circuit to prevent STWD-006 from also running on a structurally broken file.

**Assessment:** Low-priority cleanup. Consider adding a guard in STWD-006 to skip files that have STWD-005 violations.

---

## Proposed New Rules

### P1 — STWD-XXX: ValidMaintenanceDependsOnRule

**Why it matters:** Dangling `depends_on` references silently break maintenance ordering. This is a correctness-critical config integrity check.  
**What it checks:** All IDs in `maintenance.artifacts[*].depends_on` must resolve to another declared `maintenance.artifacts[*].id` in the same policy.  
**Severity:** Error.  
**Belongs in core:** Yes. Maintenance configuration is a first-class Steward feature.

### P2 — STWD-XXX: DuplicateArtifactPathRule

**Why it matters:** Duplicate paths in `artifacts[]` produce undefined behavior and confusing diagnostics.  
**What it checks:** All `artifacts[*].path` values must be unique (case-insensitive).  
**Severity:** Warning.  
**Belongs in core:** Yes. A single enforcement of policy integrity.

### P3 — STWD-XXX: EmptyFamilyRule

**Why it matters:** An artifact family with governance rules but zero matched files means no governance is actually enforced. This often indicates a misconfigured glob pattern.  
**What it checks:** Any artifact family that declares governance rules (`frontmatter_schema`, `required_sections`, `naming_pattern`) but matches zero files in `AllDiscoveredFiles`.  
**Severity:** Warning.  
**Belongs in core:** Yes. A natural complement to STWD-015.

### P4 — STWD-XXX: ValidArtifactRoleRule

**Why it matters:** Unknown roles silently drop role-linked defaults. A misspelled role produces silent governance gaps.  
**What it checks:** All `artifacts[*].role` values must be in the set of known roles (`RoleDefaults.Defaults` keys) or explicitly empty.  
**Severity:** Warning.  
**Belongs in core:** Yes. Follows directly from the role-defaults system.

### P5 — STWD-XXX: ValidIndexOfReferenceRule

**Why it matters:** An `index_of` pointing to a non-existent or empty directory silently produces no index-completeness diagnostics, giving a false impression of coverage.  
**What it checks:** `artifacts[*].index_of` must point to a directory that exists and contains at least one `.md` file.  
**Severity:** Warning.  
**Belongs in core:** Yes. A natural integrity check for the index-completeness feature.

---

## Proposed Rule Redesigns

### STWD-006 — Narrow or Redesign

The current rule conflates two unrelated checks under an over-broad purpose statement. Proposed redesign:

1. **Split into two rules:**
   - STWD-006a: Empty managed regions should contain content (Warning). Rename to `EmptyManagedRegionRule`.
   - STWD-006b: Steward-owned regions must not contain manually-added headings (Warning, optional/suppressible). Rename to `ManualHeadingInManagedRegionRule`.

2. **Or: Narrow STWD-006 to structural integrity only** (remove the heading check, which has high false-positive risk) and document that ownership enforcement requires git history integration (future work).

3. **Fix the hedged language:** Replace "may have been manually inserted" with "contains a heading that was not present in the last `steward maintain` output" — or remove the heading check entirely.

### STWD-008 — Fix AllDiscoveredFiles usage

Replace `existingPaths` construction from `TargetFiles` to `AllDiscoveredFiles ?? TargetFiles`. This is a one-line fix that eliminates the most significant false-positive risk in the rule system.

### STWD-012 — Add path to message

Change message from `$"File is {N} days old (max: {max} days)."` to `$"'{artifactPath}' is {N} days old (max: {max} days)."` for self-contained diagnostics.

---

## Proposed Severity / Message / Remediation Improvements

| Rule | Improvement type | Current | Proposed |
|------|-----------------|---------|----------|
| STWD-009 | Message | `(role: unspecified)` | Omit role clause when null |
| STWD-009 | Remediation | Includes "mark as required" | Remove that option |
| STWD-006 | Message | "may have been manually inserted" | "contains a heading not expected in this steward-managed region" |
| STWD-006 | Remediation | "Avoid manually editing" | "To add content here, update the maintenance configuration that generates this region" |
| STWD-012 | Message | "File is {N} days old" | "'{path}' is {N} days old" |
| STWD-012 | Remediation | Generic update instruction | Add: "Note: git checkout may reset file timestamps in CI; rely on `last_updated` frontmatter for reliable freshness tracking." |
| STWD-007 | Message | "is stale. {action.Description}" | Include what specifically changed or "content has changed" if no detail available |
| STWD-002 | Remediation | "Remove or rename the file" | Include ruleset name/description from policy when available |
| STWD-004 | Remediation | "Consider splitting this section" | Add: "or add a path-level suppression via `validation.path_overrides` in policy.yaml" |
| STWD-010 | Behavior | Silently skips invalid regex | Emit Warning diagnostic: "Naming rule for pattern '{glob}' skipped: invalid regex '{pattern}'" |
| STWD-016 | Behavior | Silently skips invalid regex | Emit Warning diagnostic: "Family '{family}' naming_pattern skipped: invalid regex '{pattern}'" |

---

## Final Assessment: Rule System Maturity

**Category assessments:**

| Aspect | Assessment | Notes |
|--------|-----------|-------|
| Structural integrity | Strong | Registry, engine, interface design are clean and maintainable |
| Coverage breadth | Good | 16 rules cover the core governance surface area for a documentation-heavy repo |
| Severity model | Adequate | Mostly correct defaults; STWD-007 and STWD-012 may want to be promotable to Error more visibly |
| Diagnostic schema | Strong | Structured record, machine-readable, stable ruleId |
| Message quality | Uneven | STWD-012 and STWD-007 weakest; STWD-008, STWD-014, STWD-015 strongest |
| Remediation quality | Uneven | STWD-006, STWD-009 need correction; STWD-007, STWD-003 need improvement |
| Auto-fix coverage | Weak | Only STWD-007; STWD-003 and STWD-005 are high-value missing implementations |
| Scoped validation correctness | Has bugs | STWD-008 and STWD-011 use TargetFiles where AllDiscoveredFiles is needed |
| Configuration integrity checks | Has gaps | depends_on, duplicate paths, unknown roles not enforced |
| False positive risk | Manageable | STWD-006 heading check and STWD-012 mtime fallback are highest-risk |
| Functional correctness | Has a bug | STWD-015 undercounts frontmatter-matched families |
| Test coverage | Good | 14 of 16 rules have dedicated test files; STWD-006 lacks a test file |

**Summary verdict:** The rule system is fit for pre-1.0 release with the current user base. It is not yet fit for broader adoption without addressing the scoped-validation correctness issues in STWD-008 and STWD-011, the STWD-015 frontmatter-count bug, and the STWD-012 message quality issue. The remaining improvements would meaningfully raise the bar from "functional" to "trustworthy."

---

## Highest-Value Next Governance Improvements

Ordered by impact on release confidence and user trust. Each item is scoped as a discrete task suitable for direct roadmap entry, ADR, or RFC.

---

### 1. Fix STWD-008 scoped validation false positives [Bug Fix]

**Problem:** `BrokenInternalLinkRule` checks link targets against `TargetFiles` rather than `AllDiscoveredFiles`. In any scoped run, links to unmodified files report as broken.  
**Fix:** Replace `context.TargetFiles.Select(...)` with `(context.AllDiscoveredFiles ?? context.TargetFiles).Select(...)` in the `existingPaths` construction.  
**Impact:** Eliminates the most disruptive false-positive behavior in the rule system. High-frequency pain point for anyone running `--scope changed` in CI.  
**Scope:** Single line change in `BrokenInternalLinkRule.cs`. Matching fix in `IndexCompletenessRule.cs` for `filesInScope`.

---

### 2. Fix STWD-015 frontmatter-family count bug [Bug Fix]

**Problem:** `FamilyMinCountRule` passes `frontmatterFields: null` to the classifier when counting files, causing families matched by frontmatter criteria to always count as 0.  
**Fix:** For frontmatter-matched families, either (a) read frontmatter during counting using the document cache, or (b) restrict `min_count` to path-pattern-matched families and document the limitation.  
**Impact:** Eliminates spurious min_count violations for all repos using frontmatter-based family classification (the common pattern for ADR/RFC families).  
**Scope:** Medium change in `FamilyMinCountRule.cs`. May require performance consideration for large repos.

---

### 3. Implement `IFixableRule` for STWD-003 [Feature]

**Problem:** Missing frontmatter fields have a deterministic, safe fix (append field with placeholder value) but no auto-fix is available.  
**Fix:** Implement `ComputeFixesAsync` that inserts missing fields into the frontmatter block, using the shared `FrontmatterEditor` infrastructure.  
**Impact:** Enables `steward check --fix` to repair frontmatter violations in bulk. High value for agent-assisted workflows and onboarding.  
**Scope:** New method in `RequiredFrontmatterFieldRule.cs`. Requires testing for frontmatter edge cases (empty block, block with trailing whitespace, allowed-values violations).

---

### 4. Fix STWD-012 message quality and mtime reliability [Diagnostic Quality]

**Problem:** (a) Freshness diagnostic message omits the artifact path, making it non-self-contained. (b) The mtime fallback is unreliable in CI environments.  
**Fix:** (a) Include `artifactPath` in message string. (b) Add documentation note to remediation text about git-checkout mtime behavior. (c) Consider a config option `freshness.require_frontmatter_date: true` to disable mtime fallback.  
**Impact:** Eliminates a systematic false-positive risk in CI and improves diagnostic actionability for the freshness feature.  
**Scope:** Message change + optional config extension.

---

### 5. Add dangling `depends_on` validation [New Rule]

**Problem:** No rule validates that `depends_on` IDs in maintenance configs resolve to declared artifacts. Typos silently break maintenance ordering.  
**Fix:** Add `ValidMaintenanceDependsOnRule` (proposed as STWD-017) checking all `DependsOn` IDs against declared `maintenance.artifacts[*].id` values.  
**Impact:** Closes a correctness gap in the maintenance subsystem. Error severity — this is an unambiguous misconfiguration.  
**Scope:** New rule file, RuleRegistry update, test file.

---

### 6. Redesign or narrow STWD-006 [Rule Redesign]

**Problem:** The rule's stated purpose cannot be fully implemented without git history. The heading-in-managed-region detection has high false-positive risk. The empty-region check is useful but conflated.  
**Fix:** Split into two focused rules, or remove the heading check and narrow the rule to empty-region detection only. Update stated purpose to match actual behavior.  
**Impact:** Reduces false-positive noise. Makes the rule's intent and behavior coherent with each other.  
**Scope:** Rule redesign + test additions.

---

### 7. Add missing test file for STWD-006 [Test Coverage]

**Problem:** `ManagedScopeViolationRule` has no dedicated test file. The rule has known-complex behavior (empty region check, heading-in-region check) that should be covered.  
**Fix:** Create `ManagedScopeViolationRuleTests.cs` covering: empty region, heading inside steward region, non-steward owner (should not flag), correctly populated region.  
**Impact:** Removes a blind spot in the rule test suite. Required before any redesign of the rule.  
**Scope:** New test file.

---

### 8. Add EmptyFamilyRule (STWD-XXX) [New Rule]

**Problem:** A family with governance rules but zero matched files silently produces no enforcement. This usually indicates a misconfigured `path_pattern` glob.  
**Fix:** During `check`, report families that have governance rules but match zero files. Complementary to (but distinct from) STWD-015.  
**Impact:** Catches configuration mistakes that otherwise produce silent non-enforcement — a governance blind spot.  
**Scope:** New rule. Can share classification infrastructure with STWD-015.

---

### 9. Emit runtime warning for invalid regex in STWD-010 and STWD-016 [Reliability]

**Problem:** Both rules silently skip naming enforcement when a regex fails to parse. `config validate` is the intended detector, but users running only `steward check` will see silent non-enforcement.  
**Fix:** Emit a `Warning` diagnostic (with `Path: null`, category `config-error`) when a naming rule's regex fails to compile at runtime.  
**Impact:** Makes misconfiguration visible at check time without requiring a separate config validate run.  
**Scope:** Small change in each rule's `CompileNamingRules` / `CompileFamilies` method.

---

### 10. Add DuplicateArtifactPathRule (STWD-XXX) [New Rule]

**Problem:** Duplicate `path` values in `artifacts[]` produce undefined behavior and confusing diagnostics.  
**Fix:** During `check`, report any artifact paths that appear more than once in `artifacts[]`.  
**Impact:** Low-frequency but high-confusion issue. Simple rule with no false-positive risk.  
**Scope:** New rule. Trivially implemented as a HashSet deduplication check.

---

## Implementation Follow-Up (2026-04-18)

All actionable items from this audit were implemented in a single session. Summary of work done, deviations, and deferred items.

### Implemented

#### A — Core rule correctness fixes

- **STWD-006 narrowed (A1):** Removed the heading-inside-managed-region check entirely. The audit recommended splitting or narrowing; the heading check was removed outright due to high false-positive risk (generated content includes headings). Rule now detects empty managed regions only. Description updated to match. Category changed from `ownership` to `managed-region`.
- **STWD-008 scoped fix (A2):** Fixed `existingPaths` to use `AllDiscoveredFiles ?? TargetFiles`. One-line change. Confirmed by test.
- **STWD-011 scoped fix (A3):** Fixed `filesInScope` to use `AllDiscoveredFiles ?? TargetFiles` filtered to `sourceDir`. Same pattern as STWD-008.
- **STWD-012 IFixableRule (A4):** Implemented `ComputeFixesAsync` using `FrontmatterEditor.SetField`. Message now includes artifact path and role: `"Artifact 'PATH' (role: ROLE) is X days old (max: Y days)."` Remediation includes today's date hint and future-date detection added as a new Warning.
- **STWD-003 IFixableRule (A5):** Implemented `ComputeFixesAsync` using `FrontmatterEditor.SetFields`. Groups missing fields by file, inserts placeholders.

#### B — Diagnostic/remediation quality

- **STWD-001 (B1):** Message now includes `(role: ROLE)` and artifact description when set.
- **STWD-002 (B2):** Remediation now includes "Reason: [description]" from `PathRule.Description` or `PathRuleSet.Description` when available.
- **STWD-004 (B3):** Message includes heading level and threshold. Remediation suggests subsection sizes and path override mechanism.
- **STWD-007 (B4):** Message includes artifact type and description. `Details` dict includes `artifactId`.
- **STWD-009 (B5):** Optional (non-required, non-recommended) artifacts now checked at `Info` severity. Message includes role and description labels without "unspecified" noise. Misleading "mark as required" option removed from remediation.
- **STWD-010/016 (B6):** Invalid regex patterns now emit a `Warning` diagnostic with category `config-error` at runtime instead of silently skipping. `RegexMatchTimeoutException` also caught and reported.

#### C — Config validate and doctor integrity

- **Duplicate artifact paths (C1):** `ConfigLoader.ValidatePolicy` now detects duplicate artifact `path` values (case-insensitive) and throws `StewardConfigException`. Pre-empts runtime confusion from overlapping policy entries.
- **Config doctor checks (C2):** Three new advisory checks added to `RunDoctor`:
  - `dead-index-of-directory` — `index_of` dirs with no discovered files
  - `artifact-excluded-by-discovery` — artifacts whose paths match a `discovery.exclude` glob
  - `conflicting-allowed-values` — different `allowed_values` for the same field between a family definition and a frontmatter requirement with overlapping path coverage

#### D — STWD-018: BrokenFragmentAnchorRule (new rule)

New rule added at `STWD-018`. Checks that `#fragment` anchors in internal Markdown links resolve to a heading that actually exists in the target file. Uses `MarkdownHeadings.ToAnchorSlug()` for GitHub-compatible slug normalization. Fragment-only links (`#heading`) check the current file. Uses `AllDiscoveredFiles ?? TargetFiles` for existence set. STWD-008 handles file existence; STWD-018 handles fragment validity within files that exist. Registered in `RuleRegistry` (18 rules total). `ExplainCommand` updated with remediation entry.

#### E — ExplainCommand drift repair

All 17 existing `GetRemediation` entries updated with more specific/actionable text. STWD-018 entry added. Applicable-rules filter for `explain` updated with STWD-018. Comment labels for STWD-014/015/016 corrected (were misattributed).

#### F — Tests

New test files created: `ManagedScopeViolationRuleTests.cs` (7 tests), `BrokenFragmentAnchorRuleTests.cs` (12 tests), `FreshnessRuleFixTests.cs` (8 tests), `ConfigIntegrityTests.cs` (3 tests). `ExplainRemediationConsistencyTests.cs` (7 tests, in `Steward.Cli.Tests`). Existing test files updated: `BrokenInternalLinkRuleTests.cs` (2 scoped-mode tests), `RuleRegistryTests.cs` (count 17→18, STWD-018 type check), `FamilyNamingPatternRuleTests.cs` (invalid-regex test updated to expect config-error Warning), `BrokenArtifactReferenceRuleTests.cs` (optional artifact severity updated to `Info`), snapshot `CliSnapshotTests.CheckJson_IsStable.verified.txt` (STWD-001 message includes role). Final test result: 703/703 passing.

### Deviations from Audit Recommendations

- **STWD-005 IFixableRule** (unclosed managed region → append end-marker): Not implemented. Risk of placing the marker in the wrong location without content analysis. Deferred to post-1.0.
- **STWD-006 heading check removal vs. split**: Audit recommended "split into two rules or remove heading check." Chose removal without split — the empty-region check stands as STWD-006. A future `ManualHeadingInManagedRegionRule` (if needed) should be separate.
- **STWD-009 severity**: Changed optional artifact check from Warning → Info (not documented in audit). Rationale: optional artifacts are advisory; Info is the appropriate severity for "here is something that might be wrong but is expected to be absent."
- **STWD-015 frontmatter-family count bug**: Not fixed in this pass. The fix requires reading frontmatter during family counting, which adds per-file parse cost. Deferred to a dedicated performance-aware fix.
- **ValidMaintenanceDependsOnRule (P1)**: Not implemented. Requires `maintenance.yaml` schema access during validation. Deferred — not currently a user-facing pain point.
- **EmptyFamilyRule (P3)**: Not implemented. Deferred as low-urgency.
- **ValidArtifactRoleRule (P4)**: Not implemented. Deferred.
- **ValidIndexOfReferenceRule (P5)**: Partially addressed by the new `dead-index-of-directory` doctor check. A runtime rule was not added.

### Remaining Open Items

| Item | Audit ref | Priority | Notes |
| --- | --- | --- | --- |
| STWD-015 frontmatter-count bug | Section: STWD-015 | High | Needs performance-safe frontmatter counting |
| ValidMaintenanceDependsOnRule | Gap 1, P1 | Medium | Requires maintenance config schema access |
| EmptyFamilyRule | Gap 3, P3 | Low | Complement to STWD-015; not blocking |
| ValidArtifactRoleRule | Gap 6, P4 | Low | Role vocabulary is small and stable |
| STWD-006 heading check (future) | Section: STWD-006 | Low | Only valuable with git-history integration |
| STWD-005 IFixableRule | Remediation section | Low | Placement heuristic needed |
| STWD-013 self-link false negative | Section: STWD-013 | Low | Edge case, Info severity |
| Config-level validation in check path | Gap 5 | Low | Config validate is a separate flow by design |
