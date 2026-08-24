# Contributing to Steward

Thank you for contributing. This file covers working on the Steward source code repository itself. If you are a contributor working in a repository that *uses* Steward, see the [Contributor Guide](docs/guide/contributor-guide.md) instead.

## Workflow Guide

The canonical reference for how work should be done in this repo is [docs/project/workflow-guide.md](docs/project/workflow-guide.md). It covers all workflow types — features, bug fixes, documentation, reviews, releases, and more — with specific steps, validation requirements, and definitions of done.

Start there for process questions. The rest of this file covers setup and quick-reference commands.

## Using Steward In This Repo

When contributing to Steward, use Steward as the primary navigation and validation surface:

```bash
steward orient --signals
steward status --coverage
steward check
```

Agent guidance lives in [AGENTS.md](AGENTS.md). Steward CLI operational guidance for agents lives in [.agents/skills/steward-self-cli/SKILL.md](.agents/skills/steward-self-cli/SKILL.md).

For the strongest repo-specific orientation flow, start with `README.md`, then [docs/README.md](docs/README.md), [project status](docs/project/status.md), [roadmap](docs/project/roadmap.md), and the [workflow guide](docs/project/workflow-guide.md). Open `steward.sln` when you are ready to enter the code. If you are changing repo guidance or stewardship behavior, inspect `.steward/policy.yaml` next. After structural moves or new documentation, refresh generated artifacts with `steward maintain --apply`.

## Development Workflow

1. Install repo-local dev dependencies when you need CI-equivalent Markdown checks: `npm ci`
2. Lint Markdown: `npm run lint:md`
3. Build: `dotnet build steward.sln`
4. Test: `dotnet test steward.sln`
5. Validate repo governance: `steward check`
6. Pack for local install: `dotnet pack src/Steward.Cli -c Release`

## Pull Requests

- One logical change per PR.
- Use [Conventional Commits](docs/project/workflow-guide.md#commit-conventions) for all commit messages.
- `npm run lint:md` must pass when your change touches Markdown or workflow docs.
- All `dotnet test` must pass.
- `steward check` must exit 0 before submitting.
- Add a CHANGELOG.md entry under the appropriate version heading.
- Follow the [shared finalization checklist](docs/project/workflow-guide.md#shared-finalization-checklist) before submitting.

## Release Process

Pre-`1.0.0` releases are documented in [docs/project/release-process.md](docs/project/release-process.md). Tag-driven GitHub Actions handle NuGet publication automatically.
