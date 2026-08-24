# Triage Labels

> Part of the [Agent skills](../../AGENTS.md#agent-skills) configuration for this repository.

The skills speak in terms of canonical triage roles. This file maps those roles to the actual strings used in this repo's issue tracker.

Because this repo tracks issues as [local markdown](issue-tracker.md), these are not labels applied through an API. They are the allowed values of the `Status:` line near the top of each ticket file.

| Canonical role    | Value in our tracker | Meaning                                  |
| ----------------- | -------------------- | ---------------------------------------- |
| `needs-triage`    | `needs-triage`       | Maintainer needs to evaluate this ticket |
| `needs-info`      | `needs-info`         | Waiting on reporter for more information |
| `ready-for-agent` | `ready-for-agent`    | Fully specified, ready for an AFK agent  |
| `ready-for-human` | `ready-for-human`    | Requires human implementation            |
| `wontfix`         | `wontfix`            | Will not be actioned                     |
| —                 | `done`               | Work has landed and acceptance criteria are met |

When a skill mentions a role (e.g. "apply the AFK-ready triage label"), write the corresponding value into the ticket's `Status:` line.

`done` has no counterpart in the skills' vocabulary — the skills hand a ticket off and stop caring — but it is the dominant terminal state in practice, since ticket state is committed repo state rather than an external board. Set it when the work lands, in the same commit.

## Neighbouring vocabularies

Two other `Status:` vocabularies share the same line syntax and are **not** interchangeable with the roles above:

- **Effort status**, in `<effort>/effort.md`: `active`, `paused`, `done`, `abandoned`. Scoped to the whole effort, not a ticket.
- **Wayfinder status**, on map child tickets: `claimed`, `resolved`. See the wayfinding section of [issue-tracker.md](issue-tracker.md).
