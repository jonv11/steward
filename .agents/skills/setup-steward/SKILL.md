---
name: setup-steward
description: "Set up Steward in a repository: investigate its docs and conventions, interview the maintainer, dry-run a proposed `.steward/` contract, explain each rule in plain words, and write it once approved. Run once per repo, or again to rework an existing setup."
disable-model-invocation: true
---

# Setup Steward

Build a `.steward/` contract that is **necessary** (each rule guards a convention the repo already follows or the maintainer asks for) and **sufficient** (every convention worth guarding is covered). Every rule rests on **evidence**: files you read, or an answer the maintainer gave.

Field syntax: the `steward-cli-maintainer` skill if installed, else the [configuration reference](https://github.com/jonv11/steward/blob/dev/docs/guide/configuration-reference.md). Rule meanings and the setting that turns each on: [rules.md](rules.md).

Investigate → ask → dry run → confirm → write.

## 1. Investigate

`steward version` must succeed. If it fails, install (`dotnet tool install --global Steward`, needs the .NET 10 SDK) and continue only once it runs.

Read; don't assume:

- **Existing `.steward/`**: this is a re-run. Its files are the starting draft; keep what the maintainer wrote unless they change it.
- **Orientation**: `steward orient --full` and `steward config suggest`. Suggest is a heuristic and misses document families.
- **Markdown inventory**: `git ls-files '*.md'`, grouped by directory.
- **Document families**: a directory holding 3+ documents of one kind (ADRs, RFCs, runbooks, specs, posts). Read 3+ samples, then measure across *all* its files how many share each candidate convention: filename pattern, frontmatter fields and their value sets (`status`, `type`), H1 pattern, recurring H2 sections and their order. Record **conformance** as counts: "14/16 have `## Decision`".
- **State documents** (status, roadmap, changelog): last change via `git log -1 --format=%cs -- <file>`. Freshness candidates.
- **Index documents**: READMEs or indexes that list a directory's files. Index-completeness or generated-index candidates.
- **Entry points**: README, CONTRIBUTING, `AGENTS.md`/`CLAUDE.md`. Candidates for `start_here`.
- **Noise**: build output, vendored or generated docs, test fixtures. Exclusion candidates.
- **Never-commit files**: `.env`, credentials, stray exports. Forbidden-path candidates. Discovery skips gitignored files, so a tracked one is fixed by untracking and ignoring it, never by lowering severity.
- **CI and existing linters** (markdownlint, Vale): Steward governs structure and governance, not prose style; leave style to them.

Done when every directory holding Markdown is classified as family, state, index, entry point, excluded, or free prose.

## 2. Present findings and ask

Summarise the repo and how its docs are organised in at most six bullets, then ask. Take the sections in order; skip a section the evidence already settles; lead each question with the recommended answer so it can be accepted in a word; explain a choice in one line only when it genuinely branches.

**A. Goals.** What should Steward protect? Multi-select: required files, forbidden paths, family conventions, index completeness, internal links, state-document freshness, generated indexes or `STRUCTURE.md`. Recommend the ones with evidence.

**B. Families**, one question per family: enforce it, and which conventions? Show the conformance counts. Recommend the conventions at least ~80% of files already follow; offer the rest as optional. Ask for the allowed values of lifecycle fields such as `status`.

**C. Required files.** Which files must always exist?

**D. Freshness.** Which state documents go stale, and after how many days? Recommend roughly twice the observed update interval; with too little history, propose 90 days and say it is a guess.

**E. Wiring.** Run `steward check` in CI? Recommend yes: install with the .NET 10 SDK and `dotnet tool install --global Steward --version <current>`; use `--since <base-branch>` when CI runs on pull requests, a full check otherwise. Add a Steward pointer to the existing `AGENTS.md` or `CLAUDE.md`? Skip the question when neither exists.

## 3. Dry run

Build the draft in `.steward-draft/` at the repo root: `cp -R .steward .steward-draft` on a re-run, otherwise `steward init --profile minimal && mv .steward .steward-draft`. Write the answers into its `policy.yaml`; add `path-policy.yaml` only for forbidden paths, or naming in directories no family covers (a family `naming_pattern` and a path-policy `must_match` on one directory report every bad file twice). Path patterns have no exclude: keep an index out of its family with a character class, `docs/adr/[0-9]*.md`.

Pass `-c` as an absolute path: Steward `0.18.1` and earlier crash on a bare relative path, and ignore `-c` in `init`, while an absolute path works in every version. The config directory's parent becomes the repo root.

```bash
steward config validate -c "$PWD/.steward-draft"
steward config doctor   -c "$PWD/.steward-draft"
steward check           -c "$PWD/.steward-draft" -o json
```

Count diagnostics per rule and severity; `steward explain path <file> -c "$PWD/.steward-draft"` shows which family a file landed in. A pattern that matches nothing, or a convention you misread, is a draft bug: fix it and re-run until every diagnostic is real repo **debt**. `doctor` reporting an unreachable family that simply has no files yet is a known false positive; keep the family if it is intended.

Steward has no baseline mode: a rule applies to every existing file at once. Set **gate** severity from the debt:

- Zero violations: `error`, so it fails CI from day one.
- Some violations: fix the files now (`steward check --fix` for auto-fixable rules), narrow the glob, or downgrade to `warning`, per the maintainer's call. Prefer fixing: severity applies to a rule repo-wide, so one bad file downgrades the gate for every family.

## 4. Confirm

Show the maintainer:

1. Your understanding of the repo: the summary, corrected by their answers.
2. A table with one row per active rule: what it checks in plain words, which files, effect (fails CI / warns / informs), current violations.
3. The draft files in full.

Wait for explicit approval. When they change the contract, edit the draft and re-run step 3.

## 5. Write and verify

Replace `.steward/` with `.steward-draft/` and delete the draft. Then:

```bash
steward config validate && steward config doctor && steward check
steward maintain --apply   # only when maintenance artifacts are configured
```

Apply the wiring agreed in E, editing the agent file that already exists.

Done when `steward check` exits 0, every remaining warning is debt the maintainer accepted, and the changes are left uncommitted for review unless they asked for a commit.

Report: the rules that gate CI, the accepted debt (visible with `steward check`), and how to tighten later: raise a rule to `error` in `validation.severity_overrides` once its debt reaches zero.
