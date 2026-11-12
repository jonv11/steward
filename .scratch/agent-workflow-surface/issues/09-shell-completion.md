# 09 — Shell completion

Status: done
Type: task
Category: enhancement
Blocked by: None — can start immediately
Map: `.scratch/agent-workflow-surface/map.md`

## What to build

The sibling `jvcode` CLI prints completion scripts for bash, zsh and PowerShell; steward printed
none, so its long command and option surface had to be typed from `--help`. Add
`steward completions <shell>` without a hand-maintained word list that would drift from the
parser.

## Acceptance criteria

- [x] `steward completions bash|zsh|pwsh` prints a script; an unknown shell is a usage error.
- [x] The scripts complete commands, subcommands, options and enum option values from the parser.
- [x] Tests cover each script and the completions the parser returns; `--output json` matches its schema.
- [x] The README and the CHANGELOG `[Unreleased]` section document how to load the scripts.

## Answer

Each script calls System.CommandLine's built-in `[suggest:<position>]` directive with the
current command line and filters by the word being completed. Verified by hand in bash 3.2, zsh
and PowerShell 7 (`TabExpansion2`) as well as by `CompletionsCommandTests`.
