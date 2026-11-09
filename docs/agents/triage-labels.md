# Triage Labels

> Part of the [Agent skills](../../AGENTS.md#agent-skills) configuration for this repository.

The skills speak in terms of five canonical triage roles. This file maps those roles to the actual label strings used in this repo's issue tracker.

Because this repo tracks issues as local markdown (see [issue-tracker.md](issue-tracker.md)),
the label string is written on a `Status:` line near the top of the issue file rather
than applied through a tracker API.

| Label in mattpocock/skills | Label in our tracker | Meaning                                  |
| -------------------------- | -------------------- | ---------------------------------------- |
| `needs-triage`             | `needs-triage`       | Maintainer needs to evaluate this issue  |
| `needs-info`               | `needs-info`         | Waiting on reporter for more information |
| `ready-for-agent`          | `ready-for-agent`    | Fully specified, ready for an AFK agent  |
| `ready-for-human`          | `ready-for-human`    | Requires human implementation            |
| `wontfix`                  | `wontfix`            | Will not be actioned                     |

When a skill mentions a role (e.g. "apply the AFK-ready triage label"), use the corresponding label string from this table.

## Two values the skills do not name

A label-based tracker gets these from its own machinery. A markdown tracker has to spell
them, so the `Status:` line takes two further values beyond the five roles:

| Value      | Meaning                                                    |
| ---------- | ---------------------------------------------------------- |
| `claimed`  | Transient. A session is working this ticket right now.      |
| `done`     | Terminal. The ticket's work landed. `/wayfinder` calls this `resolved`; here it is `done`, so there is one terminal success word. |

`wontfix` is the other terminal value. See [issue-tracker.md](issue-tracker.md) for how a
ticket is claimed and closed.

A spec's `Status:` line uses the same seven values.

## Category roles

Independent of state. Every triaged ticket carries exactly one, on a `Category:` line:

| Category      | Meaning                       |
| ------------- | ----------------------------- |
| `bug`         | Something is broken           |
| `enhancement` | New feature or improvement    |
