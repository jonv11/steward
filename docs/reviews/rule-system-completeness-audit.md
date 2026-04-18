---
type: review
status: Complete
last_updated: 2026-04-18
---

# Rule-System Completeness Audit — Steward CLI

**Date:** 2026-04-18
**Scope:** All 17 validation rules (STWD-001 through STWD-017), the validation engine, the rule registry, config doctor, config validate, the explain surface, and the broader governance contract.
**Baseline version:** v0.15.0
**Method:** Source code inspection of all rule implementations, diagnostic model, validation engine, explain command, config doctor/validate, test suite analysis, live `steward check` and `steward config doctor` execution against the self-dogfooded repository.
**Reviewer mindset:** Rule system as product. Each rule must justify its existence through clear user value. Diagnostics are only useful if understandable and actionable. Missing governance coverage is as important as incorrect existing coverage.

---

## Executive Summary

The Steward rule system at 17 rules is structurally sound and meaningfully complete for its stated pre-1.0 scope. The registry model is clean (single `RuleRegistry.CreateAllRules()` source of truth), the `Diagnostic` record schema is well-designed for both human and machine consumers, severity layering is sensible, and the configuration surface (disabled rules, severity overrides, path overrides) provides adequate control. The system passes its own `steward check` cleanly on its own repository.

However, this audit identifies five categories of deficiency that constrain broader release confidence:

1. **Diagnostic message quality is uneven.** Several rules produce messages that omit context a user needs to act without consulting `policy.yaml`. The worst offenders are STWD-007 (stale artifact messages don't always identify the specific drift), STWD-009 (generic "does not exist" without role context in the path field), and STWD-012 (no artifact name or description in the message). STWD-004's remediation ("consider splitting") is not actionable for agents.

2. **Coverage gaps exist at configuration-to-repo seams.** Key integrity checks are missing from the `check` pass: duplicate artifact paths in policy, dangling `depends_on` references in maintenance configs, `index_of` pointing at non-existent directories, and circular index references. Some of these are partially addressed by `config doctor`, but doctor runs are advisory — they don't block CI and are not run automatically.

3. **STWD-006 is structurally underspecified.** It detects two proxy anomalies (empty managed regions and headings inside steward-owned regions) but does not detect the actual ownership violation it claims to enforce: content modification by a non-owner. The rule is not wrong, but it is substantially weaker than its `Description` promises.

4. **The `IFixableRule` interface is nearly unused.** Only STWD-007 implements auto-fix. Rules with deterministic, safe fixes (STWD-003 missing frontmatter field insertion, STWD-005 missing end-marker, STWD-012 `last_updated` field update) do not implement it, leaving significant value on the table for both human and agent workflows.

5. **The explain and remediation surfaces are disconnected.** The `ExplainCommand.GetRemediation()` method duplicates remediation strings that are also hard-coded in each rule's `Diagnostic` construction. These two surfaces can drift, and there is no test asserting consistency between them.

**Overall maturity:** Pre-production ready. Sufficient for self-dogfooding and early adopters. The highest-value improvements are achievable within one or two milestones without architectural change.

---

## Rule-by-Rule Review Table

| Rule | Name | Default Severity | Intent Clarity | Message Quality | Remediation Quality | FP/FN Risk | Recommendation | Priority |
|------|------|-----------------|----------------|-----------------|--------------------|-----------:|----------------|----------|
| STWD-001 | RequiredArtifactRule | Error/Warning | Strong | Good | Good | Low | Good as-is; surface artifact description | Low |
| STWD-002 | ForbiddenPathRule | Error | Strong | Good | Weak | Low | Improve remediation specificity | Low |
| STWD-003 | RequiredFrontmatterFieldRule | Error | Strong | Good | Good | Medium | Fix allowed-values null edge; add `--fix` | Medium |
| STWD-004 | SectionSizeRule | Info | Acceptable | Acceptable | Weak | Medium-High | Clarify remediation; add threshold guidance | Medium |
| STWD-005 | ManagedRegionIntegrityRule | Error | Strong | Good | Adequate | Low | Consider `--fix` for missing end-marker | Low |
| STWD-006 | ManagedScopeViolationRule | Warning | Weak | Weak | Weak | High | Redesign or narrow scope statement | High |
| STWD-007 | StaleArtifactRule | Warning | Strong | Weak | Good | Low | Surface diff detail in message | Medium |
| STWD-008 | BrokenInternalLinkRule | Warning | Strong | Good | Good | Medium | Validate fragment anchors; scoped-mode risk | Medium |
| STWD-009 | BrokenArtifactReferenceRule | Warning | Acceptable | Weak | Adequate | Low | Improve message; clarify STWD-001 overlap | Medium |
| STWD-010 | NamingConventionRule | Warning | Strong | Good | Good | Low | Silent skip of invalid regex is risky | Medium |
| STWD-011 | IndexCompletenessRule | Warning | Strong | Acceptable | Good | Medium | Scoped-mode FP; depth-of-scan gap | Medium |
| STWD-012 | FreshnessRule | Warning | Strong | Weak | Weak | Medium | Fix message (missing artifact name); improve remediation | High |
| STWD-013 | OrphanedDocumentRule | Info | Strong | Good | Good | Medium | Self-link false negative; scalability note | Low |
| STWD-014 | RequiredSectionsRule | Warning | Strong | Good | Good | Low | Good as-is | Low |
| STWD-015 | FamilyMinCountRule | Warning | Strong | Good | Good | Low | Good as-is | Low |
| STWD-016 | FamilyNamingPatternRule | Warning | Strong | Good | Good | Low | Silent skip of invalid regex is risky | Low |
| STWD-017 | UniqueHeadingTextRule | Warning | Strong | Good | Good | Low | Good as-is; minor diagnostic wording | Low |

---

## Detailed Notes Per Rule

### STWD-001 — RequiredArtifactRule

**Purpose:** Ensures required and recommended artifacts declared in policy exist on disk.

**Strengths:**

- Correctly uses `AllDiscoveredFiles` to avoid scoped false positives (the B6 regression fix).
- Differentiates required (Error) from recommended (Warning) via `ResolveImportance()`.
- Skips optional artifacts entirely — correct behavior.
- Directory artifact support (`path.EndsWith('/')`) is well-handled.

**Weaknesses:**

- Message `"Required artifact 'X' is missing."` does not include the artifact's `role` or `description` from policy. For a user seeing this in CI, knowing the role (e.g., "changelog", "readme") would make the diagnostic self-contained without consulting `policy.yaml`.
- Remediation `"Create the file 'X' as specified in the repository policy."` is generic. For artifacts with a `description` field, surfacing it would improve actionability.

**Actionable message quality:** Good. Precise path, correct severity, clear what is wrong.
**Remediation quality:** Adequate. Clear action but generic context.

**Edge cases / risks:**

- Directory artifact checks depend on `f.IsDirectory` being populated by file discovery. If discovery does not emit directory entries, this passes silently. This is a discovery-contract dependency, not a rule defect.
- `importance: "recommended"` produces Warning, which does not fail CI. This is correct but not prominently documented for users expecting Error-only CI gates.

**Recommendation:** Good as-is. Low-priority improvement: include `role` and `description` in diagnostic message.

---

### STWD-002 — ForbiddenPathRule

**Purpose:** Prevents files matching forbidden patterns from existing.

**Strengths:**

- Clean implementation using `PathPolicyEngine.Evaluate()`.
- Message includes the matched pattern: `"Path 'X' matches a forbidden pattern 'Y'."` — this is excellent for debugging.
- Uses `TargetFiles` (scoped), which is correct for file-presence rules.

**Weaknesses:**

- Remediation `"Remove or rename the file to comply with repository policy."` does not explain *what* the pattern was intended to prevent or *why* the pattern is forbidden. The `path-policy.yaml` `description` field (at the ruleset level) is not surfaced.
- Only 4 tests in the test suite. Missing: multi-ruleset matching, interactions with other path-policy categories, edge cases with glob patterns at repository root.

**Actionable message quality:** Good. Both the violating path and the matched pattern are reported.
**Remediation quality:** Weak. Generic. Should surface the ruleset description or forbidden-pattern purpose.

**Edge cases / risks:**

- No false-positive risk identified.
- No false-negative risk for file paths — but directory-level forbidden patterns are not tested.

**Recommendation:** Good as-is directionally. Improve remediation by surfacing why the pattern is forbidden.

---

### STWD-003 — RequiredFrontmatterFieldRule

**Purpose:** Enforces required frontmatter fields (global, path-scoped, and family-level) and allowed value constraints.

**Strengths:**

- Three-tier requirement merging (global, scoped, family) is well-structured.
- Family context shown in messages: `[family: name]` — excellent for disambiguation.
- Handles missing frontmatter block vs. missing individual fields separately with different messages.
- Special handling for `description` field when required by generated indexes (with tailored remediation).
- Allowed-value enforcement with clear error listing.

**Weaknesses:**

- If a frontmatter field has value `null` (explicitly set to `~` in YAML), the allowed-values check calls `.ToString()` on it. The null guard (`rawValue != null`) protects against this, but it means a field set to `null` passes the allowed-values check silently — it is treated as "present but not constrained." This may surprise users who expect `null` to be treated as missing.
- No `IFixableRule` implementation. Inserting a missing frontmatter field (e.g., `type: draft`) is deterministic and safe for simple cases. This is a missed auto-fix opportunity.
- The legacy path (`validation.required_frontmatter_fields`) and canonical path (`governance.frontmatter.required_fields`) are merged additively. `config doctor` warns about this, but the merge semantics are implicit — there is no way for a user to understand the effective set without `config show --effective`.

**Actionable message quality:** Good. Path, field name, family context, and allowed values are all reported.
**Remediation quality:** Good. Specific actions with correct field names.

**Edge cases / risks:**

- **Null-value gap:** As noted above. A field set to `null` satisfies the "present" check but may not satisfy the "allowed values" check depending on how YAML serialization works.
- Medium FP risk: files that match a family path pattern but are not actually family members (e.g., a README in the ADR directory) get family frontmatter requirements applied. The `explicitArtifactPaths` exclusion mitigates this for declared artifacts, but not for other non-family files.

**Recommendation:** Directionally right, medium priority. Fix the null-value edge case. Consider `IFixableRule` for simple field insertion.

---

### STWD-004 — SectionSizeRule

**Purpose:** Flags sections exceeding a configurable line count threshold.

**Strengths:**

- Recursively checks nested section hierarchy — correct depth handling.
- Configurable threshold with sensible default (500 lines).
- Info severity is appropriate for advisory guidance.

**Weaknesses:**

- Remediation `"Consider splitting this section into smaller subsections."` is vague and unactionable for automated workflows. An agent cannot determine *where* to split or *how* without more guidance.
- The rule counts all lines in a section (including child sections' lines). This means a parent section that contains several large children will fire even if the parent's own content is small. This is architecturally correct (the heading's "span" is large) but can produce surprising diagnostics.
- No distinction between a section that is genuinely too large and one that is large because it contains an expected number of subsections.

**Actionable message quality:** Acceptable. Reports section heading, line count, and threshold. Missing: which file the section is in (though `Path` is set on the diagnostic).
**Remediation quality:** Weak. "Consider splitting" gives no actionable direction. Would benefit from noting the section's depth level and suggesting a minimum subsection size.

**Edge cases / risks:**

- **Medium-High FP risk:** Generated content (e.g., large tables, code blocks, indexes) can trigger this rule even when splitting would be inappropriate. There is no way to suppress for individual sections.
- A section with exactly `threshold` lines does not fire (uses `>`, not `>=`). This is correct but undocumented.

**Recommendation:** Directionally right but weak. Improve remediation. Consider adding `<!-- steward:ignore STWD-004 -->` inline suppression as a future feature.

---

### STWD-005 — ManagedRegionIntegrityRule

**Purpose:** Validates that managed region markers are well-formed and properly paired.

**Strengths:**

- Detects three distinct error types: missing `id` attribute, orphaned end markers, unclosed begin markers.
- Stack-based pairing is correct for nested regions.
- Error severity is appropriate — malformed markers break the maintenance engine.

**Weaknesses:**

- Does not validate that the `id` value on an end marker matches the corresponding begin marker's `id`. The current implementation just pops the stack on any `<!-- steward:end -->`. If two regions are accidentally swapped, this passes silently.
- Does not validate the `owner` attribute's presence or value (e.g., whether the declared owner is a known maintainer type). This is arguably out of scope for structural integrity, but it means `owner` values can be arbitrary strings.
- Remediation for missing end-marker (`"Add a <!-- steward:end --> marker to close the region."`) is correct and specific.

**Actionable message quality:** Good. Reports the specific structural defect and the affected line.
**Remediation quality:** Adequate. Specific enough for all three error types.

**Edge cases / risks:**

- **End-marker ID mismatch:** As noted, swapped or misattributed end markers are not detected.
- Low FP risk overall.
- Could implement `IFixableRule` for the "missing end-marker" case by appending `<!-- steward:end -->` at the appropriate location.

**Recommendation:** Good as-is. Low-priority improvement: validate end-marker ID matches begin-marker ID. Consider `IFixableRule` for the missing end-marker case.

---

### STWD-006 — ManagedScopeViolationRule

**Purpose (stated):** Content in managed regions must only be modified by the declared owner.

**Strengths:**

- Detects empty managed regions (markers present but no content between them).
- Detects headings manually inserted inside steward-owned regions.
- Warning severity is appropriate for advisory ownership checks.

**Weaknesses:**

- **The rule does not detect the violation it claims to enforce.** The description says "Content in managed regions must only be modified by the declared owner." But the implementation only checks for (a) empty regions and (b) headings inside steward-owned regions. It does not compare current content to expected content — that is STWD-007's job. The rule name and description overpromise relative to what it detects.
- The "headings inside steward-owned regions" check is a proxy heuristic. It assumes any heading inside a steward-managed region is evidence of manual insertion. But a heading could be part of the legitimately generated content if the maintenance engine produces headings. This creates a **false-positive risk** when a maintainer generates content that includes headings.
- The empty-region check overlaps with STWD-007 (stale artifact detection). An empty managed region is likely a stale artifact that `steward maintain --apply` would populate. Having both rules fire on the same symptom is redundant.
- **No tests exist for this rule.** This is the only rule in the system without a dedicated test file, which is a critical coverage gap given the rule's complexity.

**Actionable message quality:** Weak. The messages are clear about the detected anomaly but do not explain why it matters or how it connects to the ownership model.
**Remediation quality:** Weak. `"Run 'steward maintain --apply' to regenerate"` is correct for steward-owned regions but doesn't cover non-steward owners. The rule's remediation is the same as STWD-007's.

**Edge cases / risks:**

- **High FP risk:** Headings in legitimately generated content trigger false positives.
- **High FN risk:** Actual content modifications (edits to text, deleted lines, added paragraphs) within managed regions are not detected.
- The `owner` field is checked but only for `"steward"` — non-steward owners get the empty-region check but not the heading check.

**Recommendation:** Needs redesign. Either:
(a) Narrow the scope statement to match what the rule actually checks ("managed regions should not be empty; steward-managed regions should not contain manually inserted headings"), or
(b) Implement actual content-diff ownership checking (which is architecturally complex and may belong post-1.0).
Option (a) is the pragmatic pre-1.0 path. Also: add tests immediately.

---

### STWD-007 — StaleArtifactRule

**Purpose:** Maintained artifacts should match what `steward maintain` would produce.

**Strengths:**

- **Only rule implementing `IFixableRule`** — demonstrates the auto-fix pattern correctly.
- Delegates to `MaintenanceEngine.Evaluate()` — reuses the same logic as `steward maintain`, ensuring consistency.
- Supports both whole-file and section-level (managed region) staleness.
- Uses `AllDiscoveredFiles` for full maintenance planning — correct for repo-wide obligations.
- Remediation correctly mentions both `steward maintain --apply` and `steward check --fix`.

**Weaknesses:**

- Message format `"Maintained artifact 'X' is stale. Y"` appends a description from the maintenance action, but that description can be generic (e.g., `"Content does not match expected output"`). The message does not include a diff summary or line-count delta.
- For managed-section staleness, the diagnostic path is set to the file containing the managed section, but the message does not specify which managed section is stale. In a file with multiple managed sections, the user must run `steward maintain --diff` to identify the specific section.

**Actionable message quality:** Weak-to-Acceptable. The artifact ID is identified, but the nature of the drift is not always clear from the message alone.
**Remediation quality:** Good. `"Run 'steward maintain --apply'"` is correct, safe, and deterministic.

**Edge cases / risks:**

- Low FP/FN risk because the rule delegates to the same engine that would perform the fix.
- Performance concern: the rule evaluates the full maintenance plan on every `check` run. For repos with many maintenance artifacts, this could be expensive. Not a problem at current scale.

**Recommendation:** Directionally right. Medium priority: improve message specificity by including the managed section ID and a brief description of the drift (e.g., "3 lines added, 2 removed").

---

### STWD-008 — BrokenInternalLinkRule

**Purpose:** Internal Markdown links should resolve to existing files.

**Strengths:**

- Uses Markdig for precise link extraction with source location.
- Correctly strips fragments and query strings before resolution.
- Filters external URLs, `mailto:`, `tel:`, `data:` schemes.
- Resolves relative paths with `.` and `..` support.
- Reports line number — excellent for quick navigation.
- `Details` dictionary includes `targetPath` for structured access.

**Weaknesses:**

- **Fragment validation is not performed.** A link to `docs/foo.md#nonexistent-heading` passes as long as `docs/foo.md` exists. The fragment anchor is stripped before checking. This means broken section references within existing files are invisible.
- Uses `context.TargetFiles` for the existence set, not `context.AllDiscoveredFiles`. This means in scoped mode (`--scope changed`), a link to an unchanged file that exists may be reported as broken if the target file is not in the scoped set. However, the `BrokenInternalLinkRule` uses `context.TargetFiles` for *both* the set of files to scan *and* the set of files to check existence against. If scoped to only changed files, targets outside the scope appear broken. **This is a known scoped-mode false-positive risk.**
- Does not detect links to non-Markdown files (images, PDFs, etc.) that are missing. The rule only checks Markdown link targets. Links to `images/logo.png` that don't exist are not flagged.

**Actionable message quality:** Good. `"Broken link to 'X' — file not found."` is clear, with line number and target path.
**Remediation quality:** Good. `"Verify the link target exists or update the reference."` is correct.

**Edge cases / risks:**

- **Medium FP risk in scoped mode** as noted above.
- **Medium FN risk for fragment anchors** — broken `#heading` references pass silently.
- Links to directories (e.g., `docs/planning/`) are not validated.

**Recommendation:** Directionally right. Medium priority improvements: (1) use `AllDiscoveredFiles` for the existence check set to fix scoped FP, (2) add fragment anchor validation as a future enhancement (could be a separate rule or an extension of this one).

---

### STWD-009 — BrokenArtifactReferenceRule

**Purpose:** Policy-declared artifact paths should resolve to existing files.

**Strengths:**

- Complements STWD-001 correctly: STWD-001 fires on `required` artifacts (Error), this fires on non-required artifacts (Warning).
- Uses `AllDiscoveredFiles` to avoid scoped false positives.
- Includes artifact role in message: `"(role: X)"`.

**Weaknesses:**

- The overlap with STWD-001 is implicit. The rule skips artifacts where `ResolveImportance() == "required"` to avoid double-reporting, but this coupling is not documented in the rule's description. A user seeing STWD-009 might not understand why some missing artifacts are errors (STWD-001) and others are warnings (STWD-009).
- Message `"Policy artifact 'X' (role: Y) does not exist."` reports on the `Path` field, which means the `Path` property of the `Diagnostic` record is also set to the artifact path. However, the message uses "does not exist" which could mean "was never created" or "was deleted." The distinction matters for remediation.
- Remediation `"Create the artifact, remove it from policy.yaml, or mark it as required if it is mandatory."` lists three options but does not help the user decide which is correct.

**Actionable message quality:** Weak-to-Acceptable. The artifact path and role are present, but the message doesn't explain why the artifact is declared or what it was expected to contain.
**Remediation quality:** Adequate. Three options are listed but prioritization guidance is missing.

**Edge cases / risks:**

- Low FP/FN risk.
- The `optional` importance artifacts are always skipped (no diagnostic). This means a typo in an optional artifact's path goes completely undetected. This is a silent false-negative gap — even optional artifacts should have their path validated if they are declared.

**Recommendation:** Directionally right but weak. Medium priority: (1) also check optional artifact paths (perhaps at Info severity), (2) improve message to explain the STWD-001/STWD-009 relationship, (3) surface artifact `description` for context.

---

### STWD-010 — NamingConventionRule

**Purpose:** Files in governed directories must match declared naming conventions from `path-policy.yaml`.

**Strengths:**

- Correctly compiles glob patterns and regex matchers.
- Applies `must_match` regex to filename only (not full path) — correct per documentation.
- Message includes both the violating filename and the expected pattern.
- `Details` dictionary includes `expectedPattern` for structured access.

**Weaknesses:**

- **Silent skip of invalid regex patterns.** If a `must_match` value is an invalid regex, the rule silently ignores it during compilation. This means a typo in `path-policy.yaml` causes the naming rule to simply not apply, with no warning to the user. The assumption is that `config validate` catches this, but config validate and check are separate passes — a user running only `check` would never know.
- Regex has a 1-second timeout (`TimeSpan.FromSeconds(1)`) which is appropriate for DoS protection, but timeout exceptions are not caught in the evaluation loop (they are caught during compilation but not during `IsMatch` calls). A catastrophic backtracking regex could cause an unhandled exception during evaluation.

**Actionable message quality:** Good. `"File 'X' does not match naming convention 'Y' required for pattern 'Z'."` — all three pieces of context are present.
**Remediation quality:** Good. `"Rename the file to match the pattern: Y"` is specific.

**Edge cases / risks:**

- **Medium risk from silent regex skip** as noted.
- **Low risk of regex timeout during evaluation** — should be caught and converted to a diagnostic.
- Glob matching uses `DotNet.Glob.Glob.Parse()` — the semantics of this library's globbing may differ subtly from user expectations (e.g., `**` behavior).

**Recommendation:** Good as-is directionally. Medium priority: (1) emit a warning diagnostic (not just skip) when a regex pattern is invalid, (2) catch `RegexMatchTimeoutException` during evaluation.

---

### STWD-011 — IndexCompletenessRule

**Purpose:** All Markdown files in an indexed directory should be linked from the index artifact.

**Strengths:**

- Correctly reuses `BrokenInternalLinkRule.ExtractInternalLinks()` for link extraction — avoids reimplementing link parsing.
- Excludes the index file itself from the "must be linked" requirement.
- Normalizes paths for comparison.

**Weaknesses:**

- Uses `context.TargetFiles` for the set of files in the indexed directory, not `context.AllDiscoveredFiles`. In scoped mode, files outside the scope would not be checked for index membership, creating a false-negative in scoped runs.
- Only checks immediate children of the `index_of` directory. If the index is expected to cover files in subdirectories (e.g., `docs/decisions/adrs/`), those are included via the `StartsWith` comparison, which is correct. However, the depth behavior is not configurable.
- Does not validate that the index artifact's links are still valid (that's STWD-008's job). But the combination means there's no single rule that checks "index completeness AND index correctness" — a user must understand the STWD-008/STWD-011 relationship.

**Actionable message quality:** Acceptable. `"File is not referenced from index 'X'."` — clear but does not name the specific file in the message text (it's in the `Path` field of the diagnostic, but the message itself is generic).
**Remediation quality:** Good. `"Add a link to 'X' in 'Y', or exclude the file from the indexed directory."` — two clear options.

**Edge cases / risks:**

- **Medium FN risk in scoped mode** as noted.
- False positive if a file is intentionally not indexed (no suppression mechanism specific to STWD-011).

**Recommendation:** Directionally right. Medium priority: use `AllDiscoveredFiles` for the indexed-directory file set in scoped mode.

---

### STWD-012 — FreshnessRule

**Purpose:** State documents with freshness declarations should be updated within the declared time window.

**Strengths:**

- Prefers `last_updated` frontmatter field over filesystem mtime — correct prioritization.
- Falls back to role-based default freshness days when no explicit `max_age_days` is set.
- Calculates age in days with clear threshold comparison.

**Weaknesses:**

- **Message omits the artifact's name and role.** `"File is X days old (max: Y days)."` does not identify the file by anything other than the `Path` field of the diagnostic. For a user seeing multiple freshness violations, the message provides no context about *why* the file has a freshness requirement.
- **Remediation `"Update the document content and its 'last_updated' frontmatter field."` is not specific enough.** It doesn't say *what* the current date should be set to, or whether simply touching the `last_updated` field without a content update is acceptable. For agents, this ambiguity is a blocker.
- **No `IFixableRule` implementation.** Updating the `last_updated` frontmatter field to today's date is a deterministic, safe operation that could be auto-fixed. This is a missed opportunity.
- Uses `DateTime.UtcNow` at evaluation time. Freshness evaluations are not reproducible — the same check on the same file can produce different results depending on when it runs. This is inherent to the feature but means snapshot testing is difficult.

**Actionable message quality:** Weak. Missing artifact identity in message text.
**Remediation quality:** Weak. Ambiguous about what "update" means and whether touching `last_updated` alone is sufficient.

**Edge cases / risks:**

- **Medium FP risk** if a file was meaningfully updated but the `last_updated` field was not touched.
- Filesystem mtime fallback can produce surprising results on cloned repos where all files have the clone timestamp.
- If `last_updated` is set to a future date, the rule will never fire — there is no check for unreasonable date values.

**Recommendation:** Needs improvement. High priority: (1) include artifact name/description in message, (2) implement `IFixableRule` for `last_updated` field update, (3) clarify remediation text, (4) consider warning on future-dated `last_updated` values.

---

### STWD-013 — OrphanedDocumentRule

**Purpose:** Markdown files should be reachable from at least one navigation surface.

**Strengths:**

- Collects references from multiple sources: `start_here`, artifact declarations, and internal Markdown links.
- Supports `standalone: true` frontmatter suppression — well-designed escape hatch.
- Info severity is appropriate for an advisory, discoverability-focused rule.

**Weaknesses:**

- **Self-references are counted as "referenced."** If `docs/orphan.md` contains a link to itself, it is not reported as orphaned. This is a false-negative for files that are only self-referential.
- The link extraction reuses `BrokenInternalLinkRule.ExtractInternalLinks()` which only finds Markdown links — HTML `<a>` tags or reference-style links are not extracted. (Note: Markdig's `LinkInline` does handle reference-style links, but raw HTML links are not parsed.)
- At scale (many Markdown files), the rule reads every Markdown file to extract links, then checks every file for orphan status. This is O(n²) in the number of files. For large repos, this could be expensive.

**Actionable message quality:** Good. `"File 'X' is not referenced by any start_here entry, artifact, or internal link."` — clear about what was checked and why it failed.
**Remediation quality:** Good. Two options: link from navigation or add `standalone: true`.

**Edge cases / risks:**

- **Medium FN risk from self-reference** as noted.
- **Low FP risk** — the rule is deliberately lenient (Info severity, multiple ways to be "referenced").

**Recommendation:** Good as-is. Low priority improvement: exclude self-references from the "referenced" set.

---

### STWD-014 — RequiredSectionsRule

**Purpose:** Files in an artifact family must contain all required section headings.

**Strengths:**

- Case-insensitive heading matching — correct for Markdown.
- Recursively collects all headings at all levels — a `## Context` satisfies a requirement for `Context` regardless of nesting depth.
- Explicitly excludes files that are declared as explicit artifacts (not family members).
- Family context included in message: `[family: name]`.

**Weaknesses:**

- Heading matching is purely textual. `"Context and Background"` does not satisfy a requirement for `"Context"`. This is correct but could surprise users who expect substring matching. The documentation should clarify this.
- No heading-level enforcement. A family could require `## Context` but a `#### Context` deep in the document satisfies it. This is intentional (flexible) but may be too lenient for some governance use cases.

**Actionable message quality:** Good. `"Required section 'X' is missing in 'Y' [family: Z]."` — all context present.
**Remediation quality:** Good. `"Add a heading '## X' (or equivalent level) to the document."` — specific and correct.

**Edge cases / risks:**

- Low FP/FN risk.
- No false positive for files that are explicit artifacts (correctly excluded from family rules).

**Recommendation:** Good as-is. No changes needed.

---

### STWD-015 — FamilyMinCountRule

**Purpose:** Artifact families with `min_count` must have at least the declared minimum number of matched files.

**Strengths:**

- Uses `AllDiscoveredFiles` — correct for repo-wide obligation checks.
- Includes `DisplayName` and `Description` from the family definition in the message when available.
- Excludes explicit artifact paths from the count — correct behavior.
- Well-tested with 9 test methods including AllDiscoveredFiles fallback.

**Weaknesses:**

- The diagnostic has no `Path` field (`null`), which means structured consumers cannot associate the diagnostic with a specific location. This is arguably correct (it's a repo-level obligation, not a file-level issue) but makes JSON output harder to filter.
- Classifies files by path pattern only (`frontmatterFields: null`), which means files that match the path glob but have wrong frontmatter are counted. This could overcount if the family definition relies on frontmatter matching for accurate classification.

**Actionable message quality:** Good. `"Artifact family 'X' has Y matched file(s) but requires at least Z."` — precise and informative.
**Remediation quality:** Good. `"Add at least N more file(s) matching the 'X' family pattern."` — specific.

**Edge cases / risks:**

- Low FP/FN risk.
- The path-only classification for counting could lead to overcounting, but in practice families are typically defined with sufficiently specific path patterns.

**Recommendation:** Good as-is. No changes needed.

---

### STWD-016 — FamilyNamingPatternRule

**Purpose:** Files matched by an artifact family must satisfy the family's `naming_pattern` regex.

**Strengths:**

- Case-insensitive regex matching (explicit `RegexOptions.IgnoreCase`).
- Applies regex to filename only — correct per documentation.
- Excludes explicit artifact paths — correct behavior.
- Regex compilation with timeout protection.
- `Details` dictionary includes `expectedPattern`.

**Weaknesses:**

- **Same silent-skip problem as STWD-010.** Invalid regex patterns in `naming_pattern` are silently ignored during compilation. A typo in policy means the naming rule simply doesn't apply to that family, with no user notification.
- Regex timeout (`TimeSpan.FromSeconds(1)`) is set during compilation but not caught during `IsMatch`. Same risk as STWD-010.

**Actionable message quality:** Good. `"File 'X' does not match the naming pattern 'Y' required for family 'Z'."` — all context present.
**Remediation quality:** Good. `"Rename the file to match the pattern: Y"` — specific.

**Edge cases / risks:**

- Same regex-related risks as STWD-010.
- Well-tested with 8 test methods including invalid regex handling.

**Recommendation:** Good as-is. Low priority: share the regex-safety improvements with STWD-010 (emit diagnostic for invalid patterns, catch timeout during evaluation).

---

### STWD-017 — UniqueHeadingTextRule

**Purpose:** Heading text within a Markdown file should be unique after anchor-style normalization.

**Strengths:**

- Uses `MarkdownHeadings.ToAnchorSlug()` for normalization — aligns with GitHub-style anchor generation.
- Reports both the duplicate heading and the original's line number — excellent for navigation.
- Flattens all heading levels for comparison — correct for Markdown anchor uniqueness.

**Weaknesses:**

- Many Markdown renderers auto-disambiguate duplicate anchors (e.g., GitHub appends `-1`, `-2`). This means duplicate headings are a usability issue, not a correctness issue. Warning severity may be too strong for some users.
- Only 3 tests in the test suite. Missing: headings with special characters, emoji, HTML entities, very long headings, headings that differ only in whitespace.
- The fallback for empty anchor slugs (`heading.Trim().ToLowerInvariant()`) could cause false positives for headings like `---` or `***` that normalize to unusual values.

**Actionable message quality:** Good. Message includes the duplicate heading text, the original's line number, and the normalized slug.
**Remediation quality:** Good. `"Rename one of the headings so the normalized anchor slug is unique within the file."` — clear.

**Edge cases / risks:**

- Low FP risk in practice (most duplicate headings are genuinely problematic).
- The rule fires per-file only. Cross-file heading uniqueness is not checked (nor should it be).

**Recommendation:** Good as-is. Low priority: expand test coverage.

---

## Diagnostic Quality Findings

### Diagnostic Strengths

- The `Diagnostic` record schema is well-designed: `RuleId`, `Severity`, `Category`, `Path`, `Line`, `Message`, `Remediation`, `Source`, `Details` — all fields a consumer needs.
- Most rules populate `Path` and `Line` correctly, enabling precise navigation.
- The `Details` dictionary (used by STWD-003, STWD-008, STWD-010, STWD-016) provides structured data for JSON consumers beyond the human-readable message.
- Severity model is clear and well-layered: Error (blocks CI), Warning (advisory, prominent), Info (background signal).

### Diagnostic Weaknesses

- **Message self-containment varies.** STWD-001, STWD-009, and STWD-012 produce messages that require consulting `policy.yaml` to understand the full context. Diagnostics should be self-contained enough to act on without opening another file.
- **Remediation duplication.** The `ExplainCommand.GetRemediation()` method duplicates remediation strings that also exist in each rule's diagnostic construction. These can drift. No test asserts they are consistent.
- **No structured error codes within messages.** While `RuleId` is available, the message text itself does not include it. Users reading plain-text output see the rule ID in the output formatting, but the `Message` field alone does not self-identify.
- **`Source` field inconsistency.** Some rules set `Source` to `"policy.yaml"`, others to the file path, others to `null`. The semantics of `Source` are undocumented.

### Diagnostic Recommendations

1. **High priority:** Add a test that asserts `ExplainCommand.GetRemediation(ruleId)` is consistent with the remediation text produced by each rule's diagnostics (or document that they serve different purposes).
2. **Medium priority:** Enrich messages for STWD-001, STWD-009, and STWD-012 with artifact role/description.
3. **Low priority:** Document the `Source` field semantics and standardize usage.

---

## Remediation Quality Findings

### Remediation Strengths

- All 17 rules produce non-null remediation text.
- Most remediation text is actionable and specific: it names the file, the field, the pattern, or the command to run.
- STWD-007's remediation correctly mentions both `steward maintain --apply` and `steward check --fix`.
- STWD-013's remediation offers two options (link or `standalone: true`) — good for different user contexts.

### Remediation Weaknesses

- **STWD-002:** Remediation does not explain *why* the path is forbidden.
- **STWD-004:** "Consider splitting" is not actionable for automated workflows.
- **STWD-006:** Remediation is identical to STWD-007 — does not address the ownership model.
- **STWD-009:** Lists three options without prioritization guidance.
- **STWD-012:** Does not clarify whether touching `last_updated` alone is sufficient.

### Remediation Recommendation

Focus remediation improvements on rules where the text is the primary user guidance (STWD-002, STWD-004, STWD-012). For these rules, remediation is the main way users learn what to do — it must be precise.

---

## False-Positive / False-Negative Risks

| Rule | FP Risk | FP Scenario | FN Risk | FN Scenario |
|------|---------|-------------|---------|-------------|
| STWD-001 | Low | — | Low | — |
| STWD-002 | Low | — | Low | Directory-level forbidden patterns untested |
| STWD-003 | Medium | Non-family files matching family path glob get family FM rules | Medium | Null-valued fields pass allowed-values check |
| STWD-004 | Medium-High | Generated content (tables, indexes) exceeds threshold | Low | — |
| STWD-005 | Low | — | Low | Mismatched end-marker IDs not caught |
| STWD-006 | High | Headings in legitimately generated content | High | Actual text modifications in managed regions not detected |
| STWD-007 | Low | — | Low | — |
| STWD-008 | Medium | Scoped mode: targets outside scope appear broken | Medium | Fragment anchors not validated |
| STWD-009 | Low | — | Medium | Optional artifact path typos go undetected |
| STWD-010 | Low | — | Medium | Invalid regex silently skips enforcement |
| STWD-011 | Low | — | Medium | Scoped mode misses files outside scope |
| STWD-012 | Medium | File updated but `last_updated` not touched | Low | — |
| STWD-013 | Low | — | Medium | Self-referencing files pass as "referenced" |
| STWD-014 | Low | — | Low | — |
| STWD-015 | Low | — | Low | — |
| STWD-016 | Low | — | Medium | Invalid regex silently skips enforcement |
| STWD-017 | Low | — | Low | — |

---

## Coverage Gaps

The following governance concerns are not addressed by any current rule:

### Gap 1: Duplicate artifact paths in policy

**Problem:** Two `artifacts[]` entries with the same `path` are accepted without warning. This can cause STWD-001 to report a missing artifact twice, or STWD-009 to report it with conflicting roles.
**Severity of gap:** Medium. Causes confusing double-diagnostics.
**Where it belongs:** `config validate` or a new validation rule.

### Gap 2: Dangling `depends_on` references in maintenance config

**Problem:** If a maintenance artifact declares `depends_on` another artifact ID that doesn't exist, the maintenance engine silently ignores the dependency. There is no validation that the referenced ID is valid.
**Severity of gap:** Medium. Maintenance ordering could be silently wrong.
**Where it belongs:** `config validate`.

### Gap 3: `index_of` pointing at non-existent directories

**Problem:** An artifact with `index_of: docs/nonexistent/` is accepted without warning. STWD-011 simply finds no files to check, producing no diagnostics — a silent pass.
**Severity of gap:** Medium. A typo in `index_of` silently disables index completeness checking.
**Where it belongs:** `config doctor` (already checks some patterns) or `config validate`.

### Gap 4: Fragment anchor validation

**Problem:** Links like `docs/foo.md#nonexistent-heading` pass STWD-008 as long as `docs/foo.md` exists. The fragment is stripped before checking. Broken section references within existing files are invisible.
**Severity of gap:** High. This is a common source of broken documentation — heading renames break anchored links silently.
**Where it belongs:** Extension of STWD-008 or a new companion rule.

### Gap 5: Circular or self-referencing index artifacts

**Problem:** An artifact with `index_of: docs/` that resides at `docs/index.md` could reference itself. The rule excludes the index file from the "must be linked" check, but does not validate that the index does not point at itself in a cycle.
**Severity of gap:** Low. Edge case, unlikely in practice.
**Where it belongs:** `config doctor`.

### Gap 6: Unreachable artifacts (declared but excluded by discovery)

**Problem:** If a `discovery.exclude` pattern in `config.yaml` excludes a path that is also declared as an artifact in `policy.yaml`, the artifact is never discovered. STWD-001 fires (missing), but the cause is the exclude pattern, not the absence of the file. The user gets a confusing diagnostic.
**Severity of gap:** Medium. Causes puzzling diagnostics for maintainers.
**Where it belongs:** `config doctor`.

### Gap 7: Cross-validation of scoped frontmatter requirements vs. family schemas

**Problem:** A file can match both a `validation.frontmatter_requirements` pattern and an `artifact_families` entry. The requirements are merged additively. If the two sources declare conflicting `allowed_values` for the same field (e.g., `status` allowed as `[Draft, Active]` in one and `[Proposed, Accepted]` in the other), the stricter set wins — but neither source is aware of the conflict. The user gets a diagnostic that may cite the wrong source.
**Severity of gap:** Medium. Causes confusion about which governance layer is driving the requirement.
**Where it belongs:** `config doctor`.

### Gap 8: Non-Markdown file link targets

**Problem:** STWD-008 only validates links to files that exist in `TargetFiles`. Links to non-Markdown files (images, PDFs, binaries) are not checked unless those files appear in the target set. Missing images or assets referenced from Markdown are invisible.
**Severity of gap:** Medium. Common in documentation repos.
**Where it belongs:** Extension of STWD-008 to check all internal link targets against the filesystem, not just the Markdown-file target set.

---

## Redundancies or Overlaps

### STWD-001 / STWD-009 Overlap

STWD-001 fires on missing `required` artifacts (Error). STWD-009 fires on missing `recommended` artifacts (Warning). They share nearly identical detection logic but differ in severity and the importance filter. The overlap is intentional and correctly handled (STWD-009 skips `required` artifacts), but the relationship is not documented in rule descriptions. A user seeing both rules in `steward explain` output does not understand they are complementary halves of the same check.

**Recommendation:** Add a note in `steward explain STWD-009` that explicitly references STWD-001 and explains the division.

### STWD-006 / STWD-007 Overlap

STWD-006 detects empty managed regions. STWD-007 detects stale managed regions (content doesn't match expected). An empty managed region is a special case of a stale managed region. Both rules can fire on the same file for the same managed section.

**Recommendation:** Either have STWD-006 skip the empty-region check when STWD-007 would fire on the same region, or merge the empty-region detection into STWD-007.

### STWD-010 / STWD-016 Overlap

STWD-010 enforces `must_match` patterns from `path-policy.yaml`. STWD-016 enforces `naming_pattern` from artifact families in `policy.yaml`. A file can match both if it falls under a path-policy glob AND belongs to a family. Both rules fire independently, potentially with different patterns.

**Recommendation:** Document the interaction. Consider having STWD-016 skip files already covered by STWD-010 for the same effective pattern, or accept the overlap as intentional (different governance layers).

---

## Proposed New Rules

### Proposed: STWD-018 — Broken Fragment Anchor Rule

**Why it matters:** Links to `file.md#heading-name` are common in documentation. When headings are renamed, the anchors break silently. This is one of the most frequent documentation rot patterns.
**What it would check:** For each internal Markdown link with a fragment, verify that the target file contains a heading whose anchor slug matches the fragment.
**Why core, not domain-specific:** Fragment anchors are a universal Markdown concern. Every documentation repo benefits from this check.
**Priority:** High.

### Proposed: STWD-019 — Duplicate Artifact Path Rule

**Why it matters:** Two artifact declarations with the same path cause double-reporting from STWD-001 and STWD-009. The user sees confusing duplicate diagnostics.
**What it would check:** All `artifacts[].path` values in policy must be unique (case-insensitive).
**Why core, not domain-specific:** This is a configuration integrity check. Any repo using Steward can trigger it with a copy-paste error.
**Priority:** Medium. Could alternatively be enforced by `config validate`.

### Proposed: Config-validate enhancement — Invalid regex detection

**Why it matters:** STWD-010 and STWD-016 silently skip invalid regex patterns. A typo in a naming convention or naming pattern silently disables enforcement. The user gets zero feedback.
**What it would check:** All `must_match` values in path-policy and all `naming_pattern` values in artifact families must be valid .NET regex patterns.
**Why core:** This is already partially in the validation path but needs to surface as a diagnostic, not a silent skip.
**Priority:** Medium.

---

## Proposed Rule Redesigns

### STWD-006 — ManagedScopeViolationRule (Redesign)

**Current state:** Detects two proxy anomalies (empty regions, headings in steward-owned regions). Does not detect actual content modification by non-owners.
**Proposed redesign:** Narrow the Description and rule name to match actual behavior:

- **New description:** "Managed regions should not be empty, and steward-managed regions should not contain manually inserted content."
- **New approach:** Drop the ownership claim. The rule checks structural anomalies in managed regions, not content ownership. If content-ownership checking is desired in the future, it should be a separate rule.
- **Immediate action:** Add a dedicated test file for this rule.

### STWD-004 — SectionSizeRule (Improvement)

**Current state:** Advisory rule with vague remediation.
**Proposed improvement:** Enhance remediation to include the section's heading depth level and approximate recommended maximum (e.g., "This is a level-2 section with 600 lines. Consider splitting into level-3 subsections of 100-200 lines each."). Add support for per-section suppression via inline comment.

### STWD-012 — FreshnessRule (Improvement)

**Current state:** Message omits artifact identity; remediation is ambiguous.
**Proposed improvement:** Include artifact `description` or `role` in the message. Implement `IFixableRule` to update `last_updated` to today's date. Clarify that the fix updates the timestamp only — content review is the user's responsibility.

---

## Proposed Severity/Message/Remediation Improvements

| Rule | Change Type | Current | Proposed |
|------|-------------|---------|----------|
| STWD-001 | Message | `"Required artifact 'X' is missing."` | `"Required artifact 'X' (role: Y) is missing. Z"` where Z is the artifact description |
| STWD-002 | Remediation | `"Remove or rename the file..."` | `"Remove or rename the file. This path is forbidden because: [ruleset description]."` |
| STWD-004 | Remediation | `"Consider splitting this section into smaller subsections."` | `"Section has N lines at heading level L. Consider splitting into subsections of ~200 lines or suppress with a path override."` |
| STWD-006 | Description | `"Content in managed regions must only be modified by the declared owner."` | `"Managed regions should not be empty and should not contain manually inserted content."` |
| STWD-007 | Message | `"Maintained artifact 'X' is stale. Y"` | Include managed section ID when applicable; include line-count delta |
| STWD-009 | Message | `"Policy artifact 'X' (role: Y) does not exist."` | Add note: `"This is a non-required artifact; see STWD-001 for required artifacts."` |
| STWD-009 | Coverage | Skips optional artifacts entirely | Check optional artifacts at Info severity |
| STWD-012 | Message | `"File is X days old (max: Y days)."` | `"Artifact 'NAME' (role: ROLE) is X days old (max: Y days)."` |
| STWD-012 | Remediation | `"Update the document content and its 'last_updated' frontmatter field."` | `"Update the document content to reflect current state and set 'last_updated: YYYY-MM-DD' in frontmatter. If only the timestamp needs refreshing, use 'steward check --fix --apply'."` |

---

## Final Assessment of Rule System Maturity

### Maturity Level: Pre-production Ready (3 of 5)

The rule system is well-architected, consistently structured, and meaningfully complete for its stated scope. The clean separation between rules, the validation engine, the diagnostic model, and the CLI surface is a genuine strength. The configuration surface (disabled rules, severity overrides, path overrides) provides adequate control for maintainers.

**What works well:**

- Registry model is clean and extensible.
- Diagnostic schema supports both human and machine consumers.
- Severity model is clear and well-layered.
- Most rules have good test coverage and precise detection logic.
- The `IFixableRule` pattern is well-designed even though underutilized.
- `config doctor` fills an important niche for configuration hygiene.

**What must improve before broader release confidence:**

- STWD-006 must be redesigned or its scope narrowed to match its behavior.
- STWD-008 needs scoped-mode false-positive fix (use `AllDiscoveredFiles`).
- STWD-012 message and remediation must be improved.
- `IFixableRule` should be implemented for at least STWD-003 (field insertion) and STWD-012 (`last_updated` update).
- Remediation text duplication between rules and `ExplainCommand.GetRemediation()` must be tested for consistency or unified.
- Fragment anchor validation (proposed STWD-018) is the single most valuable new rule for documentation integrity.

**What can wait for post-1.0:**

- Content-ownership verification (true STWD-006 redesign).
- Cross-file heading uniqueness.
- Non-Markdown link target validation (images, assets).
- Inline per-section/per-block suppression comments.
- Advanced index depth-of-scan configuration.

---

## Highest-Value Next Governance Improvements

These items are framed for direct conversion to roadmap tasks, ADRs, or RFCs.

### 1. [HIGH] Implement fragment anchor validation (STWD-018 or STWD-008 extension)

**Value:** Broken `#heading` references are the most common silent documentation rot. This is the single highest-value rule addition.
**Effort:** Medium. Requires reading target files and extracting heading slugs.
**Scope:** Core rule. Applies to all Markdown repos.

### 2. [HIGH] Redesign STWD-006 scope statement and add tests

**Value:** The current rule overpromises and has no tests. Narrowing the scope eliminates user confusion; adding tests prevents regression.
**Effort:** Low. Rename + rewrite description + add 5-8 test methods.
**Scope:** Bug fix / refinement.

### 3. [HIGH] Fix STWD-008 scoped-mode false positives

**Value:** Scoped validation (`--scope changed`) is a primary contributor workflow. False broken-link reports in scoped mode undermine trust in `steward check`.
**Effort:** Low. Change `context.TargetFiles` to `context.AllDiscoveredFiles` for the existence set.
**Scope:** Bug fix.

### 4. [HIGH] Improve STWD-012 message and remediation

**Value:** Freshness violations are a key governance signal. The current message is the weakest in the system and the remediation is ambiguous.
**Effort:** Low. Enrich message with artifact name/role; rewrite remediation text.
**Scope:** Quality improvement.

### 5. [MEDIUM] Implement IFixableRule for STWD-003 and STWD-012

**Value:** Auto-fixing missing frontmatter fields and stale `last_updated` timestamps reduces friction for both humans and agents. These are deterministic, safe fixes.
**Effort:** Medium. Requires frontmatter editing logic (already exists in `FrontmatterEditor`).
**Scope:** Feature enhancement.

### 6. [MEDIUM] Add ExplainCommand remediation consistency test

**Value:** The explain surface and rule diagnostics produce different remediation text with no test enforcing consistency. Drift erodes trust.
**Effort:** Low. One test method that instantiates all rules, generates diagnostics, and compares remediation text with `GetRemediation()`.
**Scope:** Test gap.

### 7. [MEDIUM] Emit diagnostic for invalid regex patterns in STWD-010 and STWD-016

**Value:** Silent enforcement skips are dangerous. A typo in a naming pattern should produce a visible warning, not a silent pass.
**Effort:** Low. Catch `RegexParseException` and emit a Warning diagnostic instead of skipping.
**Scope:** Quality improvement.

### 8. [MEDIUM] Add duplicate artifact path detection to config validate

**Value:** Prevents confusing double-reporting from STWD-001/STWD-009.
**Effort:** Low. Hash-set check during policy loading.
**Scope:** Config integrity.

### 9. [LOW] Enrich STWD-001 and STWD-009 messages with artifact role/description

**Value:** Makes diagnostics self-contained. Users can act without consulting `policy.yaml`.
**Effort:** Low. String interpolation changes.
**Scope:** Quality improvement.

### 10. [LOW] Document STWD-001/STWD-009 relationship in explain output

**Value:** Reduces user confusion about why some missing artifacts are errors and others are warnings.
**Effort:** Low. Text addition to `GetRemediation()`.
**Scope:** Documentation.
