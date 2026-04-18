# Contributing to Steward

Thank you for contributing. This file covers everything specific to working on the Steward repository itself.

## Using Steward In This Repo

When contributing to Steward, use Steward as the primary navigation and validation surface:

```bash
steward orient --signals
steward status --coverage
steward check
```

Agent guidance lives in [AGENTS.md](AGENTS.md). Steward CLI operational guidance for agents lives in [.agents/skills/steward-cli/SKILL.md](.agents/skills/steward-cli/SKILL.md).

For the strongest repo-specific orientation flow, start with `README.md`, then [docs/planning-index.md](docs/planning-index.md), [docs/implementation-status.md](docs/implementation-status.md), [docs/planning/implementation-instructions.md](docs/planning/implementation-instructions.md), and [docs/requirements/PRD.md](docs/requirements/PRD.md). Open `steward.sln` when you are ready to enter the code. If you are changing repo guidance or stewardship behavior, inspect `.steward/policy.yaml` next. After structural moves or new documentation, refresh the generated map with `steward maintain --artifact structure --apply`.

## Development Workflow

1. Build: `dotnet build steward.sln`
2. Test: `dotnet test steward.sln`
3. Validate repo governance: `steward check`
4. Pack for local install: `dotnet pack src/Steward.Cli -c Release`

## Pull Requests

- One logical change per PR.
- All `dotnet test` must pass.
- `steward check` must exit 0 before submitting.
- Add a CHANGELOG.md entry under the appropriate version heading.

## Release Process

Pre-`1.0.0` releases are documented in [docs/planning/release-process.md](docs/planning/release-process.md). Tag-driven GitHub Actions handle NuGet publication automatically.
