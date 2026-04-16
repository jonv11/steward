---
type: rfc
status: Accepted
resolves: >-
  How orient, outline, and search relate; what each surface owns; overlap management
---

# RFC-005: Orientation, Search, and Outline Boundaries

---

## Context

The requirements define three discovery surfaces—orient, outline, and search—as distinct commands. Their responsibilities must be clearly separated to avoid confusion and duplication.

## Decision

### Surface responsibilities

| Surface | Primary question it answers | Output character |
|---------|----------------------------|-----------------|
| `steward orient` | "What is this repository and where should I start?" | Curated, high-level, repository-wide map |
| `steward outline` | "What does this directory or file contain structurally?" | Detailed, scoped, structural view |
| `steward search` | "Where in this repository can I find X?" | Targeted, query-driven, result-list |

### `steward orient`

**Purpose:** Session-start understanding. A human or agent that has never seen the repository runs this first.

**Output includes:**
- Repository name, type, and profile
- Configured start-here entry points (prominently displayed)
- Curated hierarchical map of important directories and files
- Artifact classification tags (authoritative, workflow, generated, supporting)
- Important roots highlighted (policy, roadmap, current state, indexes)
- Optionally: cheap signals (missing required artifacts, stale indexes) via `--signals`

**Output excludes:**
- Full file listings (that's `outline`)
- Search results
- Validation diagnostics (that's `check`)

**Scope:** Always repository-wide. Depth is configurable (`--depth`). Default depth shows top-level structure plus one level of important directories.

**Performance:** Must be fast—no full validation scan. Reads `.steward/policy.yaml` and the file tree; optionally checks file existence for cheap signals.

### `steward outline`

**Purpose:** Structural detail for a given scope—directory or file.

**For a directory:**
- Curated tree view (respects .gitignore and excludes)
- Optional file sizes (`--sizes`)
- Optional line counts (`--lines`)
- Spots oversized files (with configurable thresholds)

**For a Markdown file:**
- Heading hierarchy with optional line counts per section
- Identifies large sections
- Managed regions listed

**Scope:** A specific directory or file. Defaults to repository root.

### `steward search`

**Purpose:** Find content across the repository by query.

**Modes:**
- `--mode content` — Full-text content search
- `--mode headings` — Heading-only search (Markdown files)
- `--mode all` — Both content and headings (default)

**Result fields:**
- File path
- Line number
- Column or character position (when available)
- Snippet (surrounding context)
- Match kind (content, heading)
- Heading context (the nearest parent heading for content matches in Markdown files)

**Filtering:**
- .gitignore-aware (always)
- Policy-aware filtering (respects `discovery.exclude`)
- Scoping by role (`--role requirements`, `--role authoritative`) — maps to policy-defined artifact roles

**Performance:**
- Live-scan-first (REQ-SEARCH-009). Does not require pre-built indexes.
- Optional enrichment from maintained index artifacts when present and fresh.
- Default result limit (`--max`, default 100).

### Overlap management

| Feature | orient | outline | search |
|---------|--------|---------|--------|
| Repository-wide map | ✓ | | |
| Artifact classification | ✓ | | |
| Start-here entries | ✓ | | |
| Directory tree | curated | detailed | |
| File sizes | | ✓ | |
| Line counts | | ✓ | |
| Heading hierarchy | | ✓ (file) | |
| Content search | | | ✓ |
| Heading search | | | ✓ |
| Heading context in results | | | ✓ |
| Cheap health signals | optional | | |
| .gitignore-aware | ✓ | ✓ | ✓ |

Orient is a high-level map. Outline is a detailed structural view. Search is a targeted query. They complement each other without overlapping responsibilities.

### Unconfigured repository behavior

All three surfaces work on unconfigured repositories using convention-based fallback:
- Orient uses heuristic artifact detection (e.g., README.md, LICENSE, docs/, src/).
- Outline uses .gitignore and universal excludes.
- Search uses .gitignore filtering and conservative defaults.

## Alternatives considered

1. **Merge orient and outline into one command:** Rejected—they serve different use cases (session-start vs. structural detail) and different scopes (repo-wide vs. directory/file).
2. **Make search a subcommand of orient:** Rejected—search is query-driven and result-list-oriented, fundamentally different from map/orientation.
3. **No default result limit on search:** Rejected—unbounded results harm performance and usability, especially for agents.

## Consequences

- Three distinct discovery surfaces with clear responsibilities.
- Agents can choose the right tool: orient for context, outline for structure, search for finding things.
- No duplication of output between commands.
- All surfaces respect .gitignore and policy exclusions consistently.
