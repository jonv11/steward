---
type: audit
status: Historical
last_updated: 2026-04-18
standalone: true
---

# Fresh-Eyes Onboarding Re-Audit — 2026-04-18

## Executive Summary

Steward is **credibly usable** for its target audience: .NET developers willing to build from source and read a detailed README. The core value loop — `init` → `check` → `explain` → fix → `maintain` — works cleanly end-to-end and produces genuinely useful output on real, non-trivial repositories. The README has been substantially improved since the prior audit (same date), with install section naming, PATH setup instructions, post-init warning context, `--json-envelope` documentation, and NuGet install instructions all addressed. However, six friction points remain that would impair a cold first-hour experience, centered on README length and density, noisy first-check output after init, absence of a quickstart, `.NET 10 SDK` adoption barrier, and a hard-coded version string in install commands.

**Verdict: Credibly usable** — a motivated .NET 10 developer can reach first value in under 10 minutes. Remaining issues are polish, not blockers.

---

## Environment Used

- Platform: Windows 11 Pro (x64), Windows Terminal (PowerShell)
- .NET SDK: 10.0.202
- Shell: PowerShell 7
- Git: present, clean working tree
- Steward built from source at v0.15.0
- Local tool install tested at `./.tools/steward/`
- PATH set via `$env:PATH = "$PWD\.tools\steward;$env:PATH"`

---

## Repo Chosen for Real Usage and Why

**Primary target: `d:\git\docflux`** — a .NET library project with 655 files, 41 Markdown documents, multiple docs/ directories, CI workflows, and no prior `.steward/` configuration. This is a realistic adoption target: a mid-size repo with existing documentation that a maintainer wants to bring under governance. It exercises both the zero-config and post-init paths.

**Secondary target: `d:\git\steward`** (the Steward repo itself) — used to validate the mature-config experience. Has full `.steward/` policy, 5 artifact families, maintained artifacts, and 345 files. This confirms the tool works at its best.

**Tertiary target: `c:\temp\test-steward-empty`** — a fresh empty git repo, used to test the exact `init` → `check` golden path and verify what a new user sees on day zero.

---

## README Promise Summary

The README opens with: *"A configurable repository stewardship CLI for humans and AI agents. Steward helps maintain documentation structure, enforce governance policies, and keep repository artifacts in sync — all driven by declarative YAML configuration."*

**Assessment:** This is a credible, scoped claim. The subsequent content promises:

1. Two-role model (Maintainer, Contributor) with distinct workflows
2. Three install methods: from source, local tool install, NuGet
3. Numbered Maintainer getting-started (6 steps)
4. Numbered Contributor getting-started (7 steps)
5. 17 validation rules with IDs and explain commands
6. Three built-in profiles (software, docs, minimal)
7. JSON output for automation
8. Maintenance, refactoring, search, and structural editing

**Promises met:** Items 1, 3–7 are fully delivered. Item 2 is substantially met (NuGet instructions present, though dependent on actual publication). Item 8 is fully delivered.

---

## Exact First-Hour Journey Timeline

### T+0: Clone and read README

Opened README.md cold. First paragraph is clear and compelling. Two-role model introduced cleanly in the second section. Feature list scans well. **Passed.**

**Observation:** The README is 601 lines long. For a first-time reader trying to reach first value, this is dense. There is no TL;DR or quickstart block at the top. A user who just wants to try Steward must scroll past Features to reach Installation at line ~38.

### T+2m: Installation — From source

```bash
git clone https://github.com/jonv11/steward.git
cd steward
dotnet build
```

Build succeeded in 1.4 seconds. `dotnet run --project src/Steward.Cli -- version` printed version correctly. **Passed.**

### T+3m: Installation — Build and install locally

```bash
dotnet pack src/Steward.Cli -c Release
dotnet tool install --tool-path ./.tools/steward --add-source ./src/Steward.Cli/bin/Release Steward --version 0.15.0
```

Pack and install both succeeded. README now correctly labels this section "Build and install locally" (not "global tool"). PATH setup instructions are present and correct for both Unix and PowerShell. **Passed.**

**Minor issue:** The `--version 0.15.0` is hardcoded. If a user reads this after a newer version ships, the command will fail or install an old version with no guidance on how to find the current version.

### T+5m: PATH setup and first command

```powershell
$env:PATH = "$PWD\.tools\steward;$env:PATH"
steward version
```

Worked immediately. Output: `steward 0.15.0`, `.NET 10.0.6`, OS info. **Passed.**

### T+6m: Getting Started — Contributor path on docflux (no config)

```bash
steward orient
```

**Worked as expected.** Printed classified file list: LICENSE, README.md, docs/README.md, CONTRIBUTING.md, SECURITY.md, .github/ workflows. Clean, useful output without any configuration needed.

```bash
steward outline docs/
```

**Worked as expected.** Clean tree view of the docs directory.

```bash
steward check
```

**Worked as expected.** Reported 712 files checked, 0 errors, 0 warnings, 28 info (all STWD-013 orphan detection and 1 STWD-004). Exit code 0 (PASS).

**Observation:** 26 of the 28 info messages were for test fixture Markdown files (`tests/DocFlux.Core.Tests/Fixtures/`). These are obviously test data, not governance-relevant documents. Without a `.steward/config.yaml` discovery.exclude, there's no way to suppress them. For a zero-config first impression, this is noisy.

### T+10m: Getting Started — Maintainer path on docflux

```bash
steward init --profile software
```

**Worked as expected.** Created `.steward/config.yaml` and `.steward/policy.yaml`. Scaffolded `README.md` and `LICENSE` (already existed, so no conflict behavior observed). Printed clear next steps.

```bash
steward config suggest
```

**Worked but partially redundant.** Suggested 7 artifacts including README.md and LICENSE (already in scaffolded policy), plus CONTRIBUTING.md, CHANGELOG.md, CODE_OF_CONDUCT.md, SECURITY.md, and docs/README.md (not in scaffolded policy). On an existing repo with real files, this adds value. The user must manually copy suggestions into policy.yaml — no `--apply` option exists.

```bash
steward config validate
steward config doctor
```

Both passed. "Configuration is valid." and "No configuration issues found." **Passed.**

```bash
steward check
```

After init: 715 files checked, 0 errors, 0 warnings, 28 info. Exit code 0 (PASS). The STWD-009 warnings that appear on a fresh empty repo (for CHANGELOG.md and CONTRIBUTING.md) did **not** appear here because docflux already has those files. **Passed.**

### T+15m: Testing on fresh empty repo

```bash
mkdir c:\temp\test-steward-empty; cd c:\temp\test-steward-empty; git init
steward init --profile software
```

Scaffolded `.steward/`, `README.md`, `LICENSE`. Next steps printed.

```bash
steward check
```

**Worked but confusing.** Output:
```
[warn ] STWD-009 CHANGELOG.md: Policy artifact 'CHANGELOG.md' (role: changelog) does not exist.
[warn ] STWD-009 CONTRIBUTING.md: Policy artifact 'CONTRIBUTING.md' (role: governance) does not exist.
Files checked: 5  Errors: 0  Warnings: 2  Info: 0
Result: PASS
```

Two warnings on the user's very first check. The README now includes a callout note explaining this ("After a fresh `steward init`..."), but the note appears *after* step 4, while the user hits the warnings *during* step 4. The init could either (a) scaffold these files too, or (b) not declare them in the default policy. Current state: **worked but confidence-reducing**.

### T+20m: Advanced commands on docflux

```bash
steward explain STWD-013
steward explain path README.md
steward refs README.md
steward md outline README.md
steward search "README"
steward check --fix
steward check --output json
steward --help
```

**All passed.** Highlights:
- `explain` output is concise and actionable with rule ID, category, severity, and remediation.
- `explain path` shows per-file governance view including artifact membership and applicable rules.
- `refs` output cleanly separates inbound/outbound links — immediately useful.
- `md outline` shows heading hierarchy with line counts — a strong discovery feature.
- JSON output is clean, machine-parseable, with summary stats at top.
- Error handling is good: `steward explain STWD-999` returns "Unknown rule ID" with helpful pointer.

### T+25m: Contributor experience on steward repo (mature config)

```bash
steward orient --signals
steward status --coverage
steward check
steward check --scope changed
```

**All passed.** `status --coverage` is particularly impressive: shows required artifacts, recommended artifacts, state documents, maintained artifacts, artifact families with match counts, and 100% governance coverage. This is genuinely valuable output. `check --scope changed` correctly reported 0 files on a clean tree.

### T+30m: End of first-hour session

Total time to genuine first value: **~6 minutes** (build + orient on docflux). Total time to full maintainer setup on an external repo: **~15 minutes**. Total time to explore all major commands: **~30 minutes**.

---

## What Worked Well

1. **Core check/validate/explain loop** — The `init` → `config validate` → `check` → `explain` → fix → `maintain` cycle works end-to-end without errors on the first try. No crashes, no confusing errors, no stale behavior.

2. **Zero-config value** — `steward orient`, `steward outline`, and `steward check` all work without any `.steward/` configuration. A contributor can use Steward on any repository immediately after installing. This is undersold in the README.

3. **`orient` output** — Clean, role-tagged, actionable. `--signals` adds a quick status layer. This is a genuinely useful "what's in this repo?" command.

4. **`status --coverage`** — Comprehensive at-a-glance health view. Shows artifact completeness, family match counts, and governance coverage percentage. This is the kind of output that sells the tool.

5. **`explain` command** — Both `explain <rule-id>` and `explain path <file>` produce clear, actionable output. Remediation guidance is included by default. This is unusually good for a pre-1.0 tool.

6. **`refs` command** — Inbound/outbound link analysis for a file is immediately useful and clearly formatted.

7. **`md outline`** — Heading hierarchy with line counts is a strong discovery feature that works on any Markdown file.

8. **JSON output** — `steward check --output json` produces clean, machine-parseable JSON with summary statistics. Ready for CI integration.

9. **Error handling** — Invalid rule IDs, missing files, and bad arguments all produce clear error messages with actionable suggestions. Exit codes are consistent.

10. **Init scaffolding** — `init` creates required artifact placeholders, prints clear next steps, and guards against re-initialization. The software profile provides a sensible starting point.

11. **Prior audit fixes** — The README has been visibly improved since the prior same-day audit: install section naming, PATH setup, post-init context note, `--json-envelope` documentation, and NuGet instructions are all present. This demonstrates responsive maintenance.

---

## Friction Points and Blockers

### FP-1 — README is 601 lines with no quickstart [Severity: Medium]

**Evidence:** A new user must read through Features (clear but long), Installation (3 methods), and then choose Maintainer vs Contributor path before reaching their first command. No TL;DR block, no "just try this" shortcut. Comparable tools (e.g., markdownlint, commitlint, pre-commit) have a 3-5 line quickstart block near the top.

**Classification:** Documentation friction. The content is good; the structure penalizes impatient users.

### FP-2 — Post-init check on empty repo shows 2 warnings [Severity: Medium]

**Evidence:** After `steward init --profile software` on a fresh git repo, `steward check` immediately shows 2 STWD-009 warnings for CHANGELOG.md and CONTRIBUTING.md (declared but not scaffolded). The README has a context note, but it appears after the step, not before. The user's very first `check` is not clean.

**Root cause:** The software profile declares 4 artifacts (README.md, LICENSE, CHANGELOG.md, CONTRIBUTING.md) but only scaffolds 2 (the required ones). The non-required declarations trigger STWD-009 warnings because the files don't exist.

**Classification:** Product/UX issue — the tool's defaults create a non-clean first impression.

### FP-3 — `config suggest` has no `--apply` flag [Severity: Low]

**Evidence:** After `steward config suggest` lists 7 artifact suggestions, the user must manually copy each one into policy.yaml. The README says "Apply them by editing .steward/policy.yaml." For a first-time user unfamiliar with the policy.yaml schema, this is a manual and error-prone step.

**Classification:** Product/UX gap — nice-to-have, not a blocker.

### FP-4 — Hardcoded `--version 0.15.0` in install commands [Severity: Low]

**Evidence:** README lines 53 and 86 both use `--version 0.15.0`. After a version bump, these become stale. A user who copies the NuGet install command after a new release would install an old version or get an error.

**Classification:** Documentation maintenance burden. Could be mitigated by removing `--version` from the tool-path install (use latest) or adding a "check the latest version" note.

### FP-5 — Test fixture Markdown files trigger STWD-013 noise [Severity: Low]

**Evidence:** On docflux, 26 of 28 info messages from `steward check` (without config) were STWD-013 orphan warnings for test fixture Markdown files under `tests/`. These are test data, not documentation. Without discovery.exclude configured, there's no way to suppress them.

**Classification:** Product/UX — the zero-config path works but produces noisy output on repos with test fixtures. The init-generated `config.yaml` doesn't include `tests/` patterns in `discovery.exclude`.

### FP-6 — .NET 10 SDK is a hard prerequisite with no fallback guidance [Severity: Medium]

**Evidence:** All csproj files target `net10.0`. There is no `global.json`, so the SDK version is implied. A user with .NET 8 or 9 would get a build error ("The current .NET SDK does not support targeting .NET 10.0") with no README guidance. .NET 10 is a current-generation SDK; many developers are still on .NET 8 LTS.

**Classification:** Documentation gap — the prerequisite is listed but the failure mode and resolution are not.

### FP-7 — `steward` with no command exits with error [Severity: Low]

**Evidence:** Running `steward` alone prints "Required command was not provided." followed by full help. Exit code 1. Comparable tools (git, docker, kubectl) show help at exit code 0 or run a default command.

**Classification:** Product/UX preference — missed opportunity for a better first impression.

### FP-8 — NuGet install section creates uncertainty [Severity: Low]

**Evidence:** The NuGet section says "Check the GitHub Releases page to confirm the package is available before running this command." This undermines confidence — is the package published or not? A new user shouldn't need to verify the install method works before trying it.

**Classification:** Documentation/UX — if the version isn't published, the section should say so clearly.

### FP-9 — "Dependency posture" and "Using Steward In This Repo" are noise for new users [Severity: Low]

**Evidence:** Two README sections are relevant only to Steward contributors, not to users adopting Steward for their own repos. "Dependency posture" discusses internal tradeoffs. "Using Steward In This Repo" covers the steward repo's own workflow. Both should live in CONTRIBUTING.md or similar.

**Classification:** Documentation structure — not wrong, but adds to the 601-line length.

---

## Credibility Leaks Ranked by Severity

| Rank | Issue | Impact Point |
| ---- | ----- | ------------ |
| 1 | Post-init check shows 2 warnings on empty repo (not a clean first impression) | T+15m, first check after init |
| 2 | No quickstart — user must read 60+ lines before first command | T+0, opening README |
| 3 | .NET 10 SDK required with no fallback guidance (excludes .NET 8/9 users) | T+2m, build attempt |
| 4 | Hardcoded `--version 0.15.0` will go stale | T+3m, install attempt after version bump |
| 5 | `config suggest` lists recommendations but has no `--apply` | T+12m, manual copy step |
| 6 | Test fixture .md files generate noise in zero-config check | T+8m, first check on repo with test data |
| 7 | `steward` with no command exits 1 instead of showing help at exit 0 | T+5m, first accidental invocation |
| 8 | NuGet section hedges on whether the package actually exists | T+3m, reading install options |
| 9 | Contributor-facing sections in end-user README add length | Throughout README |

---

## Top 10 Actionable Improvements

### Must Fix Before Broader Adoption

**1. Add a 5-line quickstart at the top of the README**

After the intro paragraph, add a fenced block like:

```markdown
### Quick Start
\`\`\`bash
dotnet tool install --global Steward --version 0.15.0   # or build from source below
cd your-repo
steward orient          # see what the repo contains
steward init --profile software   # set up governance
steward check           # validate against policy
\`\`\`
```

This lets impatient users reach first value in 30 seconds instead of 5 minutes of scrolling.

**2. Scaffold CHANGELOG.md and CONTRIBUTING.md placeholders in init**

The software profile declares 4 artifacts but only scaffolds 2. Either scaffold all 4 with placeholder content (like README.md gets `> TODO: Add content.`) or remove the non-required declarations from the default policy. The user's first `steward check` should be clean.

**3. Add .NET 10 SDK version requirement and failure guidance**

Change the prerequisite from just `.NET 10 SDK` (plain text, no link) to:

> **.NET 10 SDK** (10.0 or later) — [Download](https://dotnet.microsoft.com/download). Steward targets `net10.0`; earlier SDK versions (.NET 8, .NET 9) will not work. Run `dotnet --version` to verify.

**4. Remove hardcoded `--version 0.15.0` or add a "latest version" note**

Either use a dynamic reference ("replace `0.15.0` with the latest version from the Releases page") or remove the `--version` flag entirely from the tool-path install (which installs the latest available in the source).

### Nice to Improve

**5. Add `tests/**` to the default `discovery.exclude` in init scaffolding**

The generated `config.yaml` includes `node_modules/` and `.vs/` but not `tests/`. For repos with test fixture Markdown files, this creates noise. Adding a commented-out `# - "tests/**"` line would prompt users to consider it.

**6. Add `config suggest --apply` to auto-merge suggestions into policy.yaml**

Currently the user must manually copy suggestions. A `--apply` flag (or even `--dry-run` / `--apply` pattern matching other commands) would reduce friction on the maintainer onboarding path.

**7. Change `steward` with no command to exit 0 and show friendlier output**

Either run `orient` as a default command or show help with a welcoming message and exit 0, matching conventions of git, docker, and kubectl.

**8. Clarify NuGet section: is the package published or not?**

If v0.15.0 is published to NuGet, say so definitively. If not, change the section to: "NuGet publication is not yet available. Build from source or download from GitHub Releases."

**9. Move "Dependency posture" and "Using Steward In This Repo" to CONTRIBUTING.md**

These sections are relevant only to Steward project contributors. Moving them shortens the README by ~30 lines and reduces noise for adopters.

**10. Add a "zero-config usage" callout in Contributor Getting Started**

The README mentions in a blockquote that orient/outline/check work without config, but it's easy to miss. A more prominent placement — perhaps as the very first item in Contributor Getting Started — would help users discover immediate value.

---

## Concrete Doc Changes Recommended

| Location | Current State | Recommended Change |
| -------- | ------------- | ------------------ |
| README.md top | Jump from intro to "Who Is Steward For?" | Add 5-line quickstart block after intro paragraph |
| README.md prerequisites | ".NET 10 SDK" with link only | Add version requirement, failure guidance, and `dotnet --version` check instruction |
| README.md line ~53 | `--version 0.15.0` hardcoded in tool-path install | Add note: "replace with latest version" or remove `--version` |
| README.md line ~86 | `--version 0.15.0` hardcoded in NuGet install | Same as above |
| README.md NuGet section | "Check the GitHub Releases page to confirm..." | State definitively whether the package is published |
| README.md "Dependency posture" | In end-user README | Move to CONTRIBUTING.md |
| README.md "Using Steward In This Repo" | In end-user README | Move to CONTRIBUTING.md |
| README.md Contributor Getting Started | Zero-config note in blockquote | Promote to prominent callout or first item |
| Init-generated config.yaml | Excludes `node_modules/`, `.vs/`, `*.user` | Add commented `# - "tests/**"` example |

---

## Concrete Product/UX Changes Recommended

| Priority | Change | Rationale |
| -------- | ------ | --------- |
| P1 | Scaffold all declared artifacts in `init` (including CHANGELOG.md, CONTRIBUTING.md) | Eliminates warnings on first check |
| P1 | Add `global.json` to pin .NET 10 SDK or provide a clear build error | Prevents silent failure on wrong SDK |
| P2 | Add `config suggest --apply` | Reduces manual onboarding step |
| P2 | `steward` with no command → exit 0, show help or orient | Better first impression |
| P3 | Add `tests/**` or `tests/fixtures/**` to default discovery.exclude | Reduces noise on zero-config check |
| P3 | Consider `steward init --profile software --apply-suggestions` one-liner | Reduces maintainer onboarding to 2 commands instead of 4 |

---

## Final Verdict

**Credibly usable.**

The tool delivers genuine value on real repositories. The core command loop (`init` → `check` → `explain` → `maintain`) works without errors and produces actionable output. Zero-config commands (`orient`, `outline`, `check`) provide immediate value on any repo. The `explain` command with per-rule remediation guidance and `explain path` per-file governance view are standout features. JSON output is production-quality. The two-role model (Maintainer/Contributor) is clear and the workflows are distinct.

The tool is **credibly usable** because:

1. A .NET 10 developer can build, install, and reach meaningful first value in under 10 minutes following the README exactly
2. The `orient` → `check` → `explain` loop is intuitive and produces useful output on the first try
3. Commands that worked: `version`, `orient`, `outline`, `init`, `config suggest`, `config validate`, `config doctor`, `check`, `check --scope changed`, `check --fix`, `check --output json`, `explain`, `explain path`, `search`, `refs`, `md outline`, `maintain`, `status --coverage`, `--help` — every command tested worked correctly
4. Error handling is clean and consistent across all tested failure modes

The tool is **not yet ready for broad public announcement** because:

1. The first `steward check` after init shows warnings, undermining the "clean start" promise
2. .NET 10 is a hard prerequisite that excludes .NET 8 LTS users without clear guidance
3. The README is long and has no quickstart for impatient users

These are polish issues, not fundamental design problems. Fix items 1–3 from the improvements list and the tool is ready for public early-adopter testing.

---

## Prioritized Action Checklist

- [ ] **P0** — Add a quickstart block to the top of the README (5 lines, immediate time-to-value improvement)
- [ ] **P1** — Scaffold all declared artifacts in `init` so first `check` is clean
- [ ] **P1** — Add .NET 10 version requirement and failure guidance to prerequisites
- [ ] **P1** — Remove or parameterize hardcoded `--version 0.15.0` in install commands
- [ ] **P2** — Clarify NuGet section: state definitively whether the package is published
- [ ] **P2** — Move "Dependency posture" and "Using Steward In This Repo" to CONTRIBUTING.md
- [ ] **P2** — Add `tests/**` to default discovery.exclude or init scaffolding
- [ ] **P2** — Add `config suggest --apply` to reduce manual onboarding friction
- [ ] **P2** — Promote zero-config usage note in Contributor Getting Started
- [ ] **P3** — `steward` with no command → exit 0 with friendlier output
- [ ] **P3** — Add `global.json` to pin .NET 10 SDK minimum version

---

## Delta from Prior Audit (same date)

The prior audit (`fresh-eyes-onboarding-audit-2026-04-18.md`) found 9 friction points. Of those:

| Prior Finding | Status |
| ------------- | ------ |
| FP-1: "Global tool" section header but local install | **Fixed** — section renamed to "Build and install locally" |
| FP-2: Tool scans its own `.tools/.store` files | **Fixed** — discovery.exclude now includes `.tools/steward/` |
| FP-3: No public install command | **Fixed** — NuGet install command added |
| FP-4: Post-init STWD-009 warnings without explanation | **Partially fixed** — context note added to README, but warnings still appear |
| FP-5: `--json-envelope` flag undocumented | **Fixed** — added to Global Options table |
| FP-6: `config suggest` redundant after init | **Not fixed** — still a minor friction point |
| FP-7: `-c, --config` missing type annotation | **Not fixed** — still shows without `<path>` |
| FP-8: Maintain step gated confusingly | **Not fixed** — low severity, still present |
| FP-9: `steward` with no command prints error | **Not fixed** — still exits with code 1 |

The prior verdict was "Conditionally ready." With the fixes applied, the tool has moved to **Credibly usable.** The remaining issues are polish.
