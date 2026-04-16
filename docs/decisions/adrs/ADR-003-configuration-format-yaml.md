---
type: adr
status: Accepted
category: Technical
---

# ADR-003: Configuration Format — YAML

---

## Context

Configuration and policy files need a format that is human-readable, human-writable, agent-parseable, supports comments, and is consistent with the Markdown frontmatter convention.

## Decision

Use **YAML** for all configuration and policy files. Use **YamlDotNet** as the parsing library.

### Rationale

| Criterion | YAML | TOML | JSON |
|-----------|------|------|------|
| Human readability | Excellent | Good | Fair |
| Comments | Yes | Yes | No |
| Agent familiarity | High | Medium | High |
| Frontmatter consistency | Yes (YAML frontmatter is standard) | No | No |
| Nested structure | Natural | Verbose for deep nesting | Natural |
| .NET library maturity | YamlDotNet (mature, active) | Tomlyn (good) | Built-in |
| Ecosystem prevalence for config | High (k8s, CI, etc.) | Growing | Medium (data, not config) |

YAML wins on human authoring experience, comment support, frontmatter consistency, and ecosystem alignment.

### Library: YamlDotNet

- Mature, well-maintained, actively developed.
- Supports serialization and deserialization to strongly-typed C# objects.
- Supports schema validation via attributes.
- MIT licensed.

### Schema enforcement

- Config and policy YAML are deserialized into strongly-typed C# model classes.
- Unknown fields produce warnings (not errors) to support forward compatibility.
- `steward config validate` performs full schema validation including cross-references.

### YAML conventions

- Use 2-space indentation.
- Use lowercase `snake_case` for all keys.
- No YAML aliases or anchors (keep files simple and portable).
- No multi-document YAML files.

## Alternatives considered

1. **TOML:** Good for simple config, but deep nesting (policy has significant nesting) becomes verbose. Less familiar to agents trained on YAML-heavy ecosystems.
2. **JSON:** No comments, poor human authoring experience. Appropriate for machine output, not human-authored config.
3. **Custom DSL:** Excessive complexity for configuration. YAML is sufficient.

## Consequences

- Consistent format across config, policy, path-policy, and Markdown frontmatter.
- Good human authoring experience with comments.
- Strong .NET library support via YamlDotNet.
- Agents can read and write config reliably.
