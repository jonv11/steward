---
type: guide
status: Active
last_updated: 2026-06-06
---

# Using Steward with AI Agents

Steward is designed to work well in agent-driven workflows. AI coding agents can use Steward's CLI to orient themselves, validate changes, interpret failures, and remediate issues in a structured loop.

## Why agents benefit from Steward

- **Structured diagnostics.** Every violation includes a rule ID, file path, severity, message, and remediation guidance.
- **JSON output.** The main command surface supports `--output json` with a standard envelope, machine-readable error codes, and suggested next steps; remaining universal expected-failure-path cleanup is tracked as later pre-1.0 work.
- **Deterministic behavior.** Same input produces same output. No network calls, no non-determinism.
- **Scoped validation.** `--scope changed`, `--scope staged`, and merge-base-aware `--since <ref>` validation keep feedback tight.
- **Explainability.** `steward explain <rule-id>` and `steward explain path <file>` give agents the context needed to fix issues without guessing.

## The agent validation loop

The expected agent workflow is:

```
1. Orient: understand repo structure and governance
2. Inspect: check what rules apply to files being modified
3. Change: make edits
4. Validate: run steward check
5. Diagnose: parse failures and look up rule guidance
6. Remediate: fix issues or auto-fix where possible
7. Re-validate: confirm clean check
```

### Step 1: Orient

```bash
steward orient --output json
steward status --coverage --output json
```

This gives the agent a classified map of the repository, including start-here entry points, artifact roles, and governance state.

### Step 2: Inspect before editing

```bash
steward explain path docs/my-doc.md --output json
```

Before modifying a file, the agent can check what rules apply, what frontmatter is required, and what naming patterns are enforced.

### Step 3: Validate

```bash
steward check --scope changed --output json
```

For pull-request automation, validate the branch diff against its merge base:

```bash
steward check --since origin/main --output json
```

### Step 4: Diagnose failures

The JSON output includes structured diagnostics:

```json
{
  "diagnostics": [
    {
      "ruleId": "STWD-003",
      "severity": "error",
      "filePath": "docs/my-doc.md",
      "message": "Required frontmatter field 'status' is missing.",
      "remediation": "Add 'status' to the YAML frontmatter block at the top of the file."
    }
  ]
}
```

For more detail on any rule:

```bash
steward explain STWD-003 --output json
```

### Step 5: Remediate

For auto-fixable rules (STWD-003, STWD-007, STWD-012, STWD-018):

```bash
steward check --fix --apply
```

For other rules, use the remediation guidance from `steward explain` to make manual fixes.

CI systems that consume static-analysis results can use:

```bash
steward check --since origin/main --output sarif
```

SARIF 2.1.0 is available only from `steward check`. Use JSON for structured output from other commands.

## Exit codes

| Code | Meaning | Agent action |
|------|---------|-------------|
| 0 | Clean | Proceed |
| 1 | Validation failures | Parse diagnostics, fix issues, re-check |
| 2 | Usage error | Fix command invocation |
| 3 | Internal error | Report to user |

## Setting up Steward for agent use in your repo

If you are a maintainer setting up Steward for a repository where agents will operate:

1. **Initialize Steward:** `steward init --profile software`
2. **Configure governance** in `.steward/policy.yaml` — agents benefit from clear rules
3. **Enable JSON output by default** in `.steward/config.yaml`:

```yaml
output:
  format: json
```

1. **Add an AGENTS.md** to your repo that tells agents to use Steward:

```markdown
## Validation

Before finishing any change, run:

    steward check

Fix all errors. Review warnings. Use `steward explain <rule-id>` for guidance.
```

1. **Use `steward check` in CI** so agents get the same feedback as the CI gate.

## Markdown structural operations

Agents can use Steward's Markdown commands for structured editing without fragile text manipulation:

```bash
# Query a section
steward md query README.md "heading[Features]" --output json

# Add a section
steward md edit ensure-section README.md --heading "FAQ" --under "Commands" --apply

# Extract a section to a new file
steward md edit extract-section README.md --selector "heading[Features]" --to docs/features.md --apply

# Update frontmatter
steward md edit fm-set docs/my-doc.md --field status --value Active --apply
```

## Limitations agents should know

- **`search --role` only matches explicit artifact declarations**, not family-classified files. To find all files in a family, use glob patterns or `steward orient --full --output json`.
- **4 of 21 rules support auto-fix.** Most violations require the agent to make manual edits.
- **Text-mode output** includes remediation guidance but not structured next-step hints. Use `--output json` for richer machine-readable diagnostics.
- **No network calls.** Steward cannot fetch remote content or interact with hosting platforms.

## Note on AGENTS.md in this repository

The `AGENTS.md` file in the Steward source repository is about contributing to Steward itself, not about using Steward as a product. If you are setting up Steward for your own repo, create your own `AGENTS.md` tailored to your repo's governance.
