---
type: adr
status: Accepted
category: Technical
---

# ADR-006: Output Formatting Strategy

---

## Context

The CLI must support both human-readable and machine-readable output from the same data model. Output format must be selectable per invocation, and stdout/stderr contract must be stable.

## Decision

### Format selection

All commands support `--output text` (default) and `--output json`.

### Formatter abstraction

```csharp
public interface IOutputFormatter
{
    void WriteDiagnostics(ValidationResult result);
    void WriteOrientation(OrientationResult result);
    void WriteOutline(OutlineResult result);
    void WriteSearchResults(SearchResult result);
    void WriteMaintenancePlan(MaintenancePlan plan);
    void WriteObject<T>(T value);  // Generic fallback for simple outputs
}
```

Two implementations: `TextFormatter` (human-friendly, colored) and `JsonFormatter` (machine-friendly, stable schema).

### Text formatter

- Uses ANSI colors when stdout is a terminal and `--no-color` is not set.
- Indentation and alignment for readability.
- Severity labels: `ERROR`, `WARN`, `INFO`, `✓`.
- Summary lines at the end of command output.
- Progress and verbose messages go to stderr only.

### JSON formatter

- One JSON document per command invocation on stdout.
- Stable, documented schema per command.
- Camel-case property names.
- Uses `System.Text.Json` with source-generated serializers for performance.
- No colorization, no progress indicators.

### stdout / stderr contract

| Stream | Content | Stable |
|--------|---------|--------|
| stdout | Command output (diagnostics, results, maps, queries) | Yes — automation can parse this |
| stderr | Progress messages, verbose/debug logging, internal warnings | No — for human observation only |

### Color handling

1. If `--no-color` is set: no ANSI codes.
2. If `--output json`: no ANSI codes.
3. If stdout is not a terminal (piped): no ANSI codes.
4. Otherwise: ANSI colors enabled.

Detection uses `Console.IsOutputRedirected`.

### System.Text.Json

Use `System.Text.Json` (built-in) instead of Newtonsoft.Json:
- No additional dependency.
- Source generators for AOT-friendly serialization.
- Better performance for large outputs.
- Sufficient for the output schema needs.

## Alternatives considered

1. **Spectre.Console for text output:** Rich terminal UI library. Considered, but its rendering model is heavier than needed. Simple ANSI formatting is sufficient for a CLI tool. Could be adopted later if richer TUI features are needed.
2. **Newtonsoft.Json:** No advantage over System.Text.Json for output serialization. Additional dependency.
3. **YAML output format:** Rejected for v1.0.0. JSON is the standard for machine-readable CLI output. YAML output could be added later.

## Consequences

- Consistent output contract across all commands.
- Agents reliably parse JSON output.
- Humans get readable, colored output by default.
- Color detection is automatic and safe for piping.
- Source-generated JSON serialization enables future AOT compilation.
