---
type: guide
status: Active
last_updated: 2026-11-15
standalone: true
---

# Bounded document contracts

Opt into these checks under `document_contracts` in `.steward/policy.yaml`. Existing policies
retain their behavior. All three rules default to **error**. They generate diagnostics, never
rewrite prose or claim that two differently worded documents mean the same thing.

## Content budgets — STWD-022

```yaml
document_contracts:
  budgets:
    - pattern: "docs/**/*.md"
      max_words: 1000
      max_section_words: 350
      max_table_cell_words: 80
    - pattern: "docs/**/INDEX.md"
      max_words: 400
      max_index_entries: 40
```

Patterns use Steward's glob syntax. Entries apply in order: a later matching non-null limit
replaces that limit, retaining other limits. Every supplied limit must be positive. No limit
means no check for that dimension.

A word is a whitespace-delimited token in the Markdown **source**, excluding YAML front matter.
Code, HTML, tables and link destinations count. Wrapping has no effect. Sections count their
heading and own content, up to the next heading, excluding child sections. Content before the
first heading is checked separately. Pipe-table cells are parsed as Markdown, so escaped pipes
and code spans do not create false cell boundaries. Other table syntaxes still count toward
document and section budgets. Index entries count non-image Markdown links, including navigation
links; declare this limit only on index pages.

Prefer one question or task per document. A size limit is a ceiling, not a target. Delete
repeated material before splitting, and link to the authoritative source instead of copying it.

## Identities and relationships — STWD-023

```yaml
document_contracts:
  identities:
    - pattern: "docs/reference/*.md"
      field: subject
  relations:
    - pattern: "docs/decisions/*.md"
      field: evidence
      target_pattern: "docs/reports/*.md"
```

An identity declaration defines its own uniqueness scope. Its field is required by default;
`required: false` allows absence. Present values must be non-empty YAML scalars interpreted as
strings by Steward's Markdown parser. Values are trimmed and compared case-insensitively.
Missing or duplicate identities fail. Use a controlled subject such as `vessel-frames`, rather
than inventing a new synonym to keep a competing authority.

Relationships are optional metadata fields containing one path or a sequence of paths. Paths
are **repository-relative**, must name discovered files matching `target_pattern`, and cannot
contain parent traversal, backslashes or absolute paths. Use frontmatter requirements to make a
relationship mandatory for a particular document family. Relationships do not replace ordinary
Markdown navigation links.

These checks inspect the full discovered corpus even with `check --paths`, `--scope` or `--since`.
This catches a duplicate elsewhere and a removed evidence target outside the selected scope.
It does not detect semantic duplication: review whether each document has a distinct purpose.

## Frozen source collections — STWD-024

```yaml
document_contracts:
  source_collections:
    - id: original-brief
      parts_pattern: "docs/sources/brief/part-*.md"
      parts:
        - docs/sources/brief/part-01.md
        - docs/sources/brief/part-02.md
      sha256: REPLACE_WITH_64_HEXADECIMAL_CHARACTERS
```

Choose logical chapter or section boundaries, preserving **all** original body text and
whitespace. Parts may have navigation metadata in YAML front matter. Steward removes only the
opening frontmatter block and its closing line ending, concatenates the remaining bodies in
`parts` order with **no inserted separator**, encodes UTF-8 and checks SHA-256. Body line endings
are significant. The collection must contain exactly the discovered files matching
`parts_pattern`; missing, extra, repeated or cross-collection parts fail. A collection requires
an ordered non-empty list, a unique id and a valid digest. These are repository-wide obligations
in scoped checks too.

Record the digest from the original source before migrating it. A changed digest needs reviewed
source provenance; updating the digest merely to silence a failure destroys the guarantee.
Collection indexes must live outside `parts_pattern`.

## Bounded topic indexes

An artifact can declare `index_pattern: "docs/reference/*.md"` instead of `index_of` to check
explicit glob coverage with STWD-011. This supports shallow topic indexes without requiring the
root index to list every descendant. Declare one or the other, never both. Combine it with a
maintenance `source` using the same glob, STWD-007 as error, and an index-entry budget. New source
files then require regenerated indexes, while new branches of documentation must remain reachable
through the navigation checks.

## Adoption and diagnosis

Run `config validate`, `config doctor` and `check` after declaring contracts. Doctor identifies
patterns matching no discovered files. For phased adoption use a temporary baseline, then remove
it once migrated; a baseline is not a permanent size exception. Use `explain STWD-022`,
`explain STWD-023`, `explain STWD-024` or `explain path <file>` for remediation.
