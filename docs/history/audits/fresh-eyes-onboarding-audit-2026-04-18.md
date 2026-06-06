---
type: audit
status: Historical
last_updated: 2026-04-18
standalone: true
---

# Fresh-Eyes Onboarding Audit — 2026-04-18

## Executive Summary

Steward is **conditionally ready** for early adopters who are .NET developers and are willing to read carefully. The core value proposition is real, the tool builds and runs cleanly, and the first-time maintainer golden path works without errors. However, five friction points materially impair the first-hour experience for a new user who has no prior knowledge of the repo: the Installation section uses a misleading section header, there is no copy-paste-safe public install command, the tool scans its own installation directory when installed via `--tool-path`, the README uses undocumented CLI flags in prose examples, and the Getting Started sequence jumps the contributor to a command (`orient`) that only has value after a maintainer has already configured the repo. These issues collectively erode confidence before the user has produced any output.

**Verdict: Conditionally ready** — usable by a motivated .NET developer who reads carefully, not yet ready for broad adoption or public announcement.

---

## Environment Used

- Platform: Windows 11 Pro (x64), Windows Terminal
- .NET SDK: 10.0.6
- Shell: bash (Git Bash)
- Git: present, clean working tree
- Steward built from source at commit `ce7bbc0` (v0.15.0)
- Install path tested: `--tool-path ./.tools/steward` (per README instructions)

---

## Repo Chosen for Real Usage and Why

Primary target: the **Steward repository itself** (`d:/git/steward`). This is a legitimate choice: it is the most actively dogfooded target, it has a mature `.steward/` policy with all features exercised, and it is non-trivial (402 files, 5 artifact families, multiple maintained artifacts). The audit also exercises a **fresh empty git repo** (`/tmp/test-steward-onboard`) to test the `init` → `check` flow from scratch, and a **directory with no `.steward/`** (`/tmp/test-no-config`) to test graceful degradation.

No external public repo was available in the test environment. The Hadoop directory at `d:/tmp/hadoop-Jonathan/` was present but empty. The steward repo is sufficient for assessing a non-trivial scenario.

---

## README Promise Summary

The README opens with: *"A configurable repository stewardship CLI for humans and AI agents. Steward helps maintain documentation structure, enforce governance policies, and keep repository artifacts in sync — all driven by declarative YAML configuration."*

This is a credible, reasonably scoped claim. The README goes on to promise:

1. A two-role model (Maintainer, Contributor) with distinct golden paths
2. Three install methods: from source, local tool install, and public NuGet/binary
3. A numbered Maintainer getting-started sequence ending in `steward check`
4. A numbered Contributor getting-started sequence with scoped pre-commit checking
5. 17 documented validation rules with rule IDs and explain commands
6. Three built-in profiles (`software`, `docs`, `minimal`)
7. JSON output for automation
8. Deterministic maintenance, `refactor move`, `md query`, and link detection

The promises in items 1, 3–5, 6, and 7 are substantially met. Items 2 and 8 have partial or misleading coverage as documented below.

---

## Exact First-Hour Journey Timeline

### T+0: Clone and read README

Opened README.md cold. First paragraph is clear. The two-role model ("Maintainer" and "Contributor") is introduced cleanly. **Passed**.

### T+3m: Installation — From source

```bash
git clone https://github.com/jonv11/steward.git
cd steward
dotnet build
```

Command succeeds. Runner: `dotnet run --project src/Steward.Cli -- version` prints version correctly. **Passed**.

### T+5m: Installation — "Build and install as a global tool"

```bash
dotnet pack src/Steward.Cli -c Release
dotnet tool install --tool-path ./.tools/steward --add-source ./src/Steward.Cli/bin/Release Steward --version 0.15.0
```

**BLOCKER (documented, not blocking):** The section header says "Build and install as a global tool" but the command uses `--tool-path`, which is a *local* install. `dotnet tool install --global` would install system-wide; `--tool-path` installs to a local directory with no PATH modification. The section header is factually incorrect.

After install, the tool runs at `./.tools/steward/steward`. All subsequent README examples show `steward <command>` with no qualifying note that this requires PATH setup or aliasing. A new user would hit a `command not found` error on any example after this section.

**Workaround applied:** Used full path `./.tools/steward/steward` for all subsequent commands. Noted as installation section UX gap.

### T+8m: No public install command

The "Public feed install" subsection has no `dotnet tool install` command. It explains *why* no command is given (avoids implying a package exists before a release publishes), but it does not tell the user how to get a binary from GitHub Releases either. The "GitHub Releases" subsection mentions `.nupkg` and self-contained bundles but provides no download command or URL.

A new user landing on this page who does not want to build from source has no copy-paste install path. **Friction point**.

### T+10m: Getting Started — Maintainer path

```bash
steward init --profile software
```

On the fresh git repo: succeeds. Creates `.steward/config.yaml` and `.steward/policy.yaml`. Scaffolds `LICENSE` placeholder. Prints next steps clearly. **Worked as expected**.

```bash
steward config suggest
```

On the fresh repo: outputs two suggestions (README.md, LICENSE). Correct output. Note: the suggestions are already in the scaffolded policy.yaml, so this step adds no value on a fresh init. **Worked but confusing** — the README says "review the suggestions and edit policy.yaml" but the policy already contains these declarations.

```bash
steward config validate
steward config doctor
```

`config validate`: exits 0 and prints "Configuration is valid." **Passed**.

`config doctor`: reports two `[missing-artifact]` issues for `CHANGELOG.md` and `CONTRIBUTING.md` — both are in the scaffolded policy.yaml but were not created by `init`. The README troubleshooting says "as of v0.12.0, `steward init --profile software` scaffolds placeholders for required artifacts." But `CHANGELOG.md` and `CONTRIBUTING.md` are **not** required (`required: false`) — they are merely declared. This creates a false impression that `steward init` left the repo in a broken state. A new user would wonder whether to delete those entries or create those files.

**Friction point** — the post-init state requires explanation that is not in the Getting Started section.

```bash
steward check
```

Exits 0 (PASS) with two STWD-009 warnings about the non-required declared artifacts. **Worked but confusing** — a new user seeing warnings immediately after init without explanation is a credibility loss moment.

```bash
steward maintain
steward maintain --apply
```

"No maintenance artifacts configured." on the fresh repo. **Correct behavior**, but the Getting Started section says "If you configured maintenance artifacts" without explaining what that means or where to look. A new user would either skip this step (correct) or wonder if they did something wrong.

### T+20m: Getting Started — Maintainer on steward repo

Switched to the steward repo (non-trivial target with full policy).

```bash
steward orient --signals
steward status --coverage
steward check
```

All three run without error. `orient --signals` shows one stale STRUCTURE.md warning. `status` shows comprehensive artifact coverage. `check` output has 15 warnings, 2 infos — all are STWD-008 broken links coming from `.tools/steward/.store/steward/0.15.0/steward/0.15.0/README.md`.

**BUG (critical UX):** When Steward is installed via `--tool-path ./.tools/steward`, the `.tools/` directory ends up inside the repo and gets scanned by the file discovery engine. The tool then finds its own embedded README.md (from the NuGet package store) and reports 14 broken-link warnings against it. These warnings are noise — they appear because the embedded README references files relative to the original repo root, not the current working directory.

The README's `config.yaml` reference block shows `discovery.exclude` with examples like `"node_modules/"` and `.vs/` but does not warn the user that installing Steward inside the repo will cause self-scanning. The steward repo's own `.steward/config.yaml` adds `.tools/steward/` to nothing — in fact it does not exclude `.tools/**` at all, and the issue manifests.

**Result:** A new user running `steward check` immediately after following the README install path sees 14 spurious warnings against the tool's own files. This is a serious credibility leak.

```bash
steward explain STWD-008
steward explain path README.md
```

Both work correctly. Output is clear and useful. **Passed**.

```bash
steward maintain --diff
```

Shows the stale `STRUCTURE.md` diff. Output is readable. **Passed**.

### T+30m: Contributor path

```bash
steward orient
steward check --scope changed
steward check --scope staged
```

`orient` works correctly on the steward repo. On the fresh empty repo (no `.steward/` at all), `orient` produces minimal output but does not error. **Passed**.

`steward check --scope changed` on a clean tree: correctly reports 0 files checked, plus the persistent STWD-007 stale artifact warning. **Passed** — consistent with v0.11.0 fix noted in troubleshooting.

```bash
steward check --fix
steward check --fix --apply
```

`--fix` (preview): correctly shows one fix available (STWD-007 stale structure). Does not apply. **Passed**.

### T+35m: Advanced commands

```bash
steward md query README.md "#who-is-steward-for"
steward refs README.md
steward refactor move README.md README2.md --preview
steward search "validation" --mode headings
```

All four work correctly. Output is useful and clearly formatted. `refactor move --preview` correctly identifies one file with reference updates. **All passed**.

### T+40m: Help and discoverability

```bash
steward --help
steward init --help
steward check --help
```

`--help` shows all commands. However, the global option table in `--help` shows `-c, --config` with **no type annotation** — the help string says "Path to .steward configuration directory" but the option is not shown as accepting an argument. The README shows `--config <path>` correctly. This is a `System.CommandLine` rendering issue but appears as inconsistency to a new user.

`--json-envelope <Legacy|Standard>` appears in every command's `--help` output but is **not documented anywhere in the README**. A new user sees this flag without explanation.

---

## What Worked Well

1. **Core check/validate/explain loop** — The `init` → `config validate` → `check` → `explain <rule>` → `check --fix --apply` cycle works end-to-end on the first try. No crashes, no confusing errors.
2. **orient output** — Clean, actionable, role-tagged. `--signals` provides immediately useful status at a glance.
3. **explain command** — `steward explain STWD-009` gives a concise rule description and remediation. `steward explain path <file>` gives a per-file governance view. Both are unusually good for a pre-1.0 tool.
4. **md query with anchor slug** — `steward md query README.md "#who-is-steward-for"` just works. This is a strong feature that is undersold in the README.
5. **refactor move** — Preview mode with reference count is immediately useful. Clear `--preview` / `--apply` pattern.
6. **JSON output** — `steward check --output json` produces clean JSON that a CI pipeline or AI agent can consume directly.
7. **init idempotency guard** — `steward init` on an already-initialized directory exits with a clear error rather than silently overwriting. Exit code 2 is correct.
8. **No `.steward/` graceful degradation** — `steward check` and `steward orient` both run without error in a directory with no policy. This is correct and useful behavior, though not documented.
9. **Error messages** — Rule violations include both the fix instruction and the rule ID. The format is consistent and parseable.

---

## Friction Points and Blockers

### FP-1 — Section header says "global tool" but installs locally [Severity: High]

**Evidence:** README section 3 header is "Build and install as a global tool" but the command uses `--tool-path`, which is a local install to `./.tools/steward`. The binary is not on PATH after this step. All subsequent README examples use bare `steward` which would fail with `command not found` for any user who follows the README exactly.

**Classification:** Failed due to documentation.

### FP-2 — Tool scans its own installation directory [Severity: High]

**Evidence:** After installing via `--tool-path ./.tools/steward`, `steward check` reports 14 STWD-008 warnings from `.tools/steward/.store/steward/0.15.0/steward/0.15.0/README.md`. These are relative-link checks against a file inside the NuGet package store that cannot pass in this context. The steward repo's own `discovery.exclude` does not suppress this.

**Classification:** Failed due to product/UX issue. The tool should either (a) auto-exclude its own installation directory, or (b) the install instructions should direct users to add `.tools/**` to `discovery.exclude`.

### FP-3 — No public install command [Severity: Medium]

**Evidence:** "Public feed install" subsection contains no command. "GitHub Releases" mentions self-contained bundles exist but gives no URL or download instruction. A new user who does not want to build from source has no path forward.

**Classification:** Failed due to documentation (intentional limitation, but not clearly explained for the user perspective).

### FP-4 — Post-init STWD-009 warnings without explanation [Severity: Medium]

**Evidence:** After `steward init --profile software` on a fresh git repo, `steward check` immediately shows two STWD-009 warnings for `CHANGELOG.md` and `CONTRIBUTING.md`. These are non-required artifacts in the scaffolded policy that do not yet exist. The Getting Started section does not prepare the user for this.

**Classification:** Worked but confusing — a new user's first check output is not clean, and no explanation is given in context.

### FP-5 — `--json-envelope` flag undocumented [Severity: Medium]

**Evidence:** `--help` shows `--json-envelope <Legacy|Standard>` on every command. The README has zero mentions of this flag. A user discovering this via `--help` has no explanation of what it controls or when to use which value.

**Classification:** Failed due to documentation.

### FP-6 — `config suggest` adds no value immediately after `init` [Severity: Low]

**Evidence:** The Getting Started Maintainer step 2 says "run `steward config suggest` and review the suggestions." On a fresh init, the suggestions are identical to what `init` already scaffolded. The user reviews suggestions that are already applied.

**Classification:** Worked but confusing — the command order implies `suggest` reveals new information, but it doesn't on a fresh init.

### FP-7 — `-c, --config` flag help missing type annotation [Severity: Low]

**Evidence:** `--help` shows `-c, --config` without `<path>`. The README correctly shows `--config <path>`. This is a minor `System.CommandLine` rendering artifact but creates a subtle inconsistency.

**Classification:** Worked but confusing.

### FP-8 — `steward maintain` step in Maintainer getting-started is conditional but gated confusingly [Severity: Low]

**Evidence:** Step 5 says "If you configured maintenance artifacts…" but does not explain what that means or where to look. A first-time user would not know whether they have configured maintenance artifacts after a fresh `init`.

**Classification:** Failed due to documentation.

### FP-9 — `steward` with no command prints "Required command was not provided" but no hint [Severity: Low]

**Evidence:** Running `steward` alone prints an error message followed by the full help. The error message "Required command was not provided" is technically accurate but could be replaced with a more welcoming prompt like "Run `steward --help` to see available commands" or show a default action like `orient`.

**Classification:** Product/UX preference — low severity but a missed opportunity for a better first impression.

---

## Credibility Leaks Ranked by Severity

| Rank | Issue | Where It Hits |
| ---- | ----- | ------------- |
| 1 | Tool scans its own `.tools/.store` files — first `steward check` shows 14 spurious warnings | T+20m, first check on real repo |
| 2 | "Global tool" section header but local `--tool-path` install — all bare `steward` examples break | T+5m, reading install section |
| 3 | No copy-paste public install command — public feed and GitHub Releases sections give no actionable command | T+8m |
| 4 | `--json-envelope` flag appears in every `--help` but has zero README documentation | Any `--help` call |
| 5 | Post-init STWD-009 warnings with no contextual explanation — first check is not clean | T+10m |
| 6 | `config suggest` after `init` adds no visible value — step seems redundant | T+12m |
| 7 | `steward maintain` step is gated on "if you configured maintenance" with no pointer | T+15m |

---

## Top 10 Actionable Improvements

### Must Fix Before Broader Adoption

**1. Fix `discovery.exclude` for `.tools/**` in the default software profile or in `init` output**

When Steward is installed inside a repo via `--tool-path`, the `.tools/` directory should be excluded from file discovery. Either:

- The `software` profile's default `discovery.exclude` should include `.tools/**` and `.tools/`
- Or `steward init` should detect a `.tools/` directory and add it to the generated `config.yaml`

This fix removes 14 spurious STWD-008 warnings that appear immediately after the README install path.

### 2. Rename "Build and install as a global tool" or add a PATH setup step

The section header is factually incorrect — `--tool-path` is a local install. Either:

- Rename to "Build and install locally" and add a one-line note explaining the user needs to add `./.tools/steward` to PATH or use an alias to get the bare `steward` command used in all examples
- Or add a `dotnet tool install --global` variant that actually installs globally

### 3. Add a copy-paste public install command or remove the "Public feed install" section

If v0.15.0 published to NuGet.org, add: `dotnet tool install --global Steward --version 0.15.0`

If it did not publish or the package name differs, either remove this section or replace it with an honest "not yet available from public feed" note, and make the GitHub Releases section include the actual download URL pattern.

### 4. Document `--json-envelope` in the README Global Options table

Add a row for `--json-envelope <legacy|standard>` with a one-sentence explanation of the difference. The flag appears in every command's `--help` but nowhere in the README.

### 5. Add a post-init explanation to Getting Started about expected STWD-009 state

After the `steward check` step in Maintainer Getting Started, add a note: "If you see STWD-009 warnings for `CHANGELOG.md` or `CONTRIBUTING.md`, these are non-required declarations in the starter policy. Either create those files or remove their entries from `.steward/policy.yaml`."

### Nice to Improve

### 6. Reorder Maintainer Getting Started: move `config suggest` before `init` or remove it from the primary path

`config suggest` is most useful *before* declaring artifacts so the user knows what the tool detects. On a fresh init, it is redundant. Either explain this clearly ("use `config suggest` on an existing repo before running `init`") or move it to an "Advanced" section.

### 7. Give `steward` with no command a useful default output

Running `steward` alone could show `steward orient` output (like `git status` shows current state) or at minimum show a cleaner first-impression message than "Required command was not provided." This is the first interaction for many users.

### 8. Fix `-c, --config` help to show `<path>` type annotation

The `System.CommandLine` option should be configured with an explicit `ArgumentHelpName` so the rendered help shows `-c, --config <path>` consistently with the README.

### 9. Add a "Getting Started without a .steward/ config" note

`steward check`, `steward orient`, and `steward outline` all work without a policy. This is a useful zero-configuration entry point (e.g., use `steward outline docs/` on any repo) but it's not mentioned anywhere. A one-paragraph note would let a contributor try Steward before a maintainer has configured it.

### 10. Add `steward` binary to PATH setup instructions for local installs

After the `dotnet tool install --tool-path` command, add:

```bash
# On Unix:
export PATH="$PWD/.tools/steward:$PATH"
# On Windows (PowerShell):
$env:PATH = "$PWD\.tools\steward;$env:PATH"
```

This lets all subsequent examples use bare `steward` as shown throughout the README.

---

## Concrete Doc Changes Recommended

| Location | Current text | Recommended change |
| -------- | ------------ | ------------------ |
| README.md line 48 | `### Build and install as a global tool` | `### Build and install locally` |
| README.md lines 55–58 | Runs command with no PATH note | Add PATH export instructions after install command |
| README.md lines 62–70 | "Public feed install" — no command | Add `dotnet tool install --global Steward` if published, or note not yet available |
| README.md lines 250–255 | Global Options table | Add `--json-envelope <legacy\|standard>` row |
| README.md — Maintainer step 2 | "Review the suggestions and edit…" | Clarify that on a fresh init the suggestions are already applied; most useful on an existing repo |
| README.md — Maintainer step 4 | Ends after `steward check` | Add note explaining STWD-009 warnings are expected for non-required declared artifacts |
| README.md — Maintainer step 5 | "If you configured maintenance artifacts" | Add pointer: "See `maintenance.artifacts` in your policy.yaml. On a fresh init, no maintenance is configured." |
| README.md config.yaml block | Shows `discovery.exclude` with examples | Add `.tools/**` to the example exclude list or add a note about local tool installs |

---

## Concrete Product/UX Changes Recommended

| Priority | Change | Rationale |
| -------- | ------ | --------- |
| P0 | Auto-exclude `.tools/**` in default discovery or detect and warn | Eliminates spurious warnings from local tool install |
| P1 | Add PATH setup to install output or readme | Makes bare `steward` work after install |
| P2 | `steward` with no args → run `orient` or show cleaner help | Better first impression than error message |
| P2 | Add `--json-envelope` to `--help` description text | Already exposed; just needs documentation |
| P3 | Post-init `steward check` should return exit 0 with a clean summary | STWD-009 for non-required declared artifacts is expected state; should be distinguishable |

---

## Final Verdict

**Conditionally ready.**

The tool's core loop — `init` → `config validate` → `check` → `explain` → `check --fix --apply` → `maintain --apply` — works correctly and without errors. The check output is readable and actionable. `explain` is a standout feature. JSON output is production-quality. The Markdown query, refactor, and refs commands work on the first attempt.

The tool is **not ready for broad adoption** because:

1. Following the README install path produces spurious warnings on the first `check` run (FP-2)
2. All bare `steward` examples in Getting Started fail without PATH setup that the README does not provide (FP-1)
3. There is no public install command for users who don't want to build from source (FP-3)

A motivated .NET developer who builds from source and uses `dotnet run` can reach first value in under 15 minutes. Any other new user will hit at least FP-1 within the first 10 minutes. Fix FP-1 and FP-2 and the tool becomes credibly usable for early public testing.

---

## Prioritized Action Checklist

- [ ] **P0** — Add `.tools/**` to default `discovery.exclude` in software profile or generated `config.yaml`
- [ ] **P0** — Rename install section to "Build and install locally" and add PATH setup commands
- [ ] **P1** — Add `dotnet tool install --global Steward` to Public feed install section (or honest note)
- [ ] **P1** — Document `--json-envelope` in README Global Options table
- [ ] **P1** — Add post-`init` STWD-009 context note to Maintainer Getting Started step 4
- [ ] **P2** — Clarify `config suggest` placement: most useful before first `init` on existing repos
- [ ] **P2** — Add "zero-config usage" note: `orient`, `outline`, `check` work without `.steward/`
- [ ] **P2** — Fix `--config` flag help to show `<path>` type annotation
- [ ] **P3** — Give `steward` with no args a useful default (run `orient` or cleaner help)
- [ ] **P3** — Add Maintainer step 5 pointer: what "configured maintenance" means on fresh init
