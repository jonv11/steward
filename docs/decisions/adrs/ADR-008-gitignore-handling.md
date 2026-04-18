---
type: adr
status: Accepted
category: Technical
description: Defines Steward's custom .gitignore handling and early-pruning discovery behavior
---

# ADR-008: .gitignore Handling

---

## Context

.gitignore awareness is core behavior (REQ-GITIGNORE-003). All discovery, orientation, search, outline, maintenance, and validation operations must respect .gitignore semantics. The implementation must handle nested .gitignore files, negation patterns, and directory-vs-file distinctions correctly.

## Decision

### Approach: Custom implementation using .gitignore spec

Implement .gitignore parsing and matching in-house within `Steward.Core.Discovery`, following the [gitignore specification](https://git-scm.com/docs/gitignore).

### Rationale

- .gitignore semantics are well-specified and bounded.
- Existing .NET libraries for .gitignore are either unmaintained, incomplete, or add unwanted dependencies.
- The implementation is small (~200-400 lines) and testable.
- Full control over behavior, performance, and edge cases.

### Implementation

```csharp
public interface IIgnoreFilter
{
    bool IsIgnored(string relativePath, bool isDirectory);
}

public sealed class GitIgnoreFilter : IIgnoreFilter
{
    // Loads .gitignore files from the repository root and nested directories
    public static GitIgnoreFilter Load(string repositoryRoot);
    public bool IsIgnored(string relativePath, bool isDirectory);
}
```

**Features:**
- Reads `.gitignore` at repository root and all nested directories.
- Supports negation patterns (`!pattern`).
- Supports directory-only patterns (`pattern/`).
- Supports `**` for recursive matching.
- Respects `.gitignore` file hierarchy (nested overrides parent).
- Supports `.steward/config.yaml` `discovery.exclude` patterns merged on top.

**Not implemented (deferred):**
- `.git/info/exclude` (local excludes).
- Global gitignore (`core.excludesFile`). These are user-specific and not repository-portable.

### Performance

- .gitignore files are loaded once during discovery initialization and cached.
- Pattern matching is optimized for common patterns (literal prefix, simple globs).
- Directory traversal prunes ignored directories early (skip entire subtrees).

### Integration

`IIgnoreFilter` is injected into:
- `FileDiscoveryService` (used by all commands)
- `SearchEngine`
- `OrientationEngine`
- `OutlineEngine`
- `MaintenanceEngine`

All file-listing operations go through `FileDiscoveryService`, which applies ignore filtering. No command directly walks the filesystem.

## Alternatives considered

1. **MAB.DotIgnore NuGet package:** Exists but has limited maintenance and incomplete spec compliance.
2. **Calling `git check-ignore`:** Correct but slow (process spawn per file or batch). Not offline-friendly if git is not installed.
3. **Ignore .gitignore and only use policy excludes:** Rejected—violates REQ-GITIGNORE-001 through REQ-GITIGNORE-003.

## Consequences

- Full .gitignore compliance for the common specification subset.
- No external dependency for ignore handling.
- Consistent filtering across all commands via `IIgnoreFilter`.
- Testable with unit tests against known gitignore patterns.
- Easy to extend if global gitignore support is needed later.
