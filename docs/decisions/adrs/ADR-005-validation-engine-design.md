---
type: adr
status: Accepted
category: Architecture
description: Defines the validation engine, rule registry, diagnostics model, and fixable-rule contract
---

# ADR-005: Validation Engine Design

---

## Context

Validation is the primary contract-enforcement surface. The engine must support multiple rule categories, deterministic evaluation, machine-readable diagnostics, scoped execution, and be extensible for new rule types across milestones.

## Decision

### Rule-based architecture

The validation engine uses a **rule registry** pattern:

```csharp
public interface IValidationRule
{
    string RuleId { get; }
    string Category { get; }           // "path-policy", "frontmatter", etc.
    DiagnosticSeverity DefaultSeverity { get; }
    string Description { get; }
    Task<IReadOnlyList<Diagnostic>> EvaluateAsync(ValidationContext context);
}

public sealed class ValidationContext
{
    public RepositoryInfo Repository { get; }
    public EffectivePolicy Policy { get; }
    public IReadOnlyList<string> TargetPaths { get; }  // Scope-resolved paths
    public IFileSystem FileSystem { get; }
    public CancellationToken CancellationToken { get; }
}
```

### Rule registration

Rules are registered in the DI container and discovered by the engine:

```csharp
public interface IValidationEngine
{
    Task<ValidationResult> ValidateAsync(ValidationScope scope, CancellationToken ct);
}
```

The engine:

1. Resolves the target paths based on scope (full, changed, staged, explicit paths).
2. Loads the effective policy.
3. Runs all applicable rules against the context.
4. Collects diagnostics.
5. Returns a `ValidationResult` with summary and diagnostics.

### Rule categories and progression

Rules are added incrementally across milestones:

| Milestone | Rules added |
|-----------|------------|
| v0.3.0 | Path policy rules (required, forbidden, naming) |
| v0.4.0 | Frontmatter rules, basic structural rules |
| v0.6.0 | Managed-scope rules, ownership rules |
| v0.7.0 | Stale-artifact rules, broken-reference rules |
| v0.9.0 | Completion-policy rules |

### Diagnostic model

```csharp
public sealed class Diagnostic
{
    public required string RuleId { get; init; }
    public required DiagnosticSeverity Severity { get; init; }
    public required string Category { get; init; }
    public required string Path { get; init; }
    public int? Line { get; init; }
    public required string Message { get; init; }
    public string? Remediation { get; init; }
    public string? Source { get; init; }
}

public enum DiagnosticSeverity { Error, Warning, Info }
```

### Scope resolution

| Scope | Resolution |
|-------|------------|
| `full` | All files in repository (respecting .gitignore and excludes) |
| `changed` | Files changed vs. merge base or HEAD~1 (via git) |
| `staged` | Files in git staging area |
| `paths` | Explicitly specified file/directory paths |

Scope resolution is performed once before rule evaluation. Rules receive the resolved set of target paths.

### Fix support

Rules that support deterministic auto-fix implement an additional interface:

```csharp
public interface IFixableRule : IValidationRule
{
    Task<IReadOnlyList<FileEdit>> ComputeFixesAsync(ValidationContext context);
}

public sealed class FileEdit
{
    public required string Path { get; init; }
    public required string OriginalContent { get; init; }
    public required string NewContent { get; init; }
}
```

`--fix` applies all computed fixes. `--dry-run` reports what `--fix` would change.

### Secret filtering

The output pipeline applies a `SecretFilter` to all diagnostic messages and snippets before they are emitted. The filter:

- Redacts strings matching common patterns (e.g., `[A-Za-z0-9]{32,}` adjacent to keywords like `key`, `token`, `secret`, `password`).
- Redacts content from paths matching configured sensitive patterns.
- Is best-effort; not a substitute for secret management.

## Alternatives considered

1. **Roslyn-analyzer-style architecture:** Overly complex for repository-level checks. Roslyn analyzers are designed for C# source code, not repository structure.
2. **Pipeline/middleware pattern:** Considered, but rules are independent and don't benefit from ordering. A flat registry with parallel execution is simpler.
3. **External rule plugins (MEF/loading DLLs):** Deferred beyond v1.0.0. Internal rules are sufficient for the planned scope. The `IValidationRule` interface is designed to support future plugin loading.

## Consequences

- Clean, testable rule interface.
- New rules are added by implementing `IValidationRule` and registering it.
- Scope resolution is centralized and consistent.
- Diagnostics have a stable schema for machine consumption.
- Fix support is opt-in per rule.
- Secret filtering is applied at the output boundary.
