# Steward Rules in Plain Words

Use this to explain the proposed contract to the maintainer. **Setting** is what turns the rule on; rules marked *always* run on every governed file. Default severity: `E` error (fails CI), `W` warning, `I` info. `steward explain <id>` is the source of truth if this table disagrees.

| Rule | Default | Setting | In plain words |
|------|---------|---------|----------------|
| STWD-001 | E | `artifacts[]` with `required: true` | This file must exist. |
| STWD-002 | E | `path-policy.yaml` rule, `category: forbidden` | Files matching this pattern must never exist. |
| STWD-003 | E | `frontmatter_schema`, `frontmatter_requirements`, `governance.frontmatter.required_fields` | These files must carry these metadata fields, with allowed values only. Auto-fix fills missing fields only when a frontmatter block exists. |
| STWD-004 | I | *always*; threshold `governance.section_size_warning_threshold` (500 lines) | A section is too long; consider splitting. |
| STWD-005 | E | *always* | Generated-content markers must open and close correctly. |
| STWD-006 | W | `maintenance` (managed sections) | A generated section is still empty: run `steward maintain --apply`. |
| STWD-007 | W | `maintenance` | A generated file is out of date or was hand-edited. Auto-fixable. |
| STWD-008 | W | *always* | A link to another file in the repo is broken. |
| STWD-009 | W | `artifacts[]` | A file the policy names doesn't exist. |
| STWD-010 | W | `path-policy.yaml` rule with `must_match` | Filenames in this directory must follow the naming convention. |
| STWD-011 | W | `artifacts[].index_of` | Every document in this directory must be linked from its index. |
| STWD-012 | W | `artifacts[].freshness.max_age_days` | This document hasn't been updated within its window. Auto-fixable (date bump only). |
| STWD-013 | I | *always* | Nothing links to this document. `standalone: true` in its frontmatter silences it. |
| STWD-014 | W | family `required_sections` | Documents of this kind must contain these headings. |
| STWD-015 | W | family `directory_expectations.min_count` | There must be at least N documents of this kind. |
| STWD-016 | W | family `naming_pattern` | Documents of this kind must follow the filename convention. |
| STWD-017 | W | *always* | Two headings in one file produce the same link anchor. |
| STWD-018 | W | *always* | A link to a heading (`file.md#section`) points at a heading that doesn't exist. Auto-fixable. |
| STWD-019 | W | family `title_pattern` | The H1 title must follow the pattern (matched against rendered text, without Markdown syntax). |
| STWD-020 | W | family `section_pattern` | Every H2 heading must follow the pattern. |
| STWD-021 | W | family `section_schema` | H2 sections must be the expected set, in the expected order. |

Severity changes go in `validation.severity_overrides`; switching a rule off for a glob goes in `validation.path_overrides`.
