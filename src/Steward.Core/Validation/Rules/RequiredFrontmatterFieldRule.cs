using DotNet.Globbing;
using Steward.Core;
using Steward.Core.Configuration;
using Steward.Core.Markdown;
using Steward.Core.Validation;

namespace Steward.Core.Validation.Rules;

/// <summary>
/// STWD-003: Frontmatter must satisfy the declared family schema.
/// Covers required fields, controlled vocabularies, unexpected fields (allowed_fields),
/// and deprecated field migration (deprecated_fields).
/// Implements IFixableRule to insert missing fields and rename/remove deprecated fields.
/// </summary>
public sealed class RequiredFrontmatterFieldRule : IValidationRule, IFixableRule
{
    public string RuleId => "STWD-003";
    public string Category => "frontmatter";
    public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Error;
    public string Description => "Frontmatter must satisfy the declared family schema.";

    public Task<IReadOnlyList<Diagnostic>> EvaluateAsync(ValidationContext context)
    {
        var diagnostics = new List<Diagnostic>();

        var globalRequired = context.Policy?.Governance?.Frontmatter?.RequiredFields ?? [];

        // Compile path-scoped frontmatter requirements
        var scopedRequirements = CompileScopedRequirements(
            context.Policy?.Validation?.FrontmatterRequirements);
        var generatedIndexRequirements = CompileGeneratedIndexRequirements(
            context.Policy?.Maintenance?.Artifacts);

        // Build family classifier
        var familyClassifier = new ArtifactFamilyClassifier(context.Policy?.ArtifactFamilies);

        var hasFamilies = context.Policy?.ArtifactFamilies is { Count: > 0 };

        if (globalRequired.Count == 0 && scopedRequirements.Count == 0 && generatedIndexRequirements.Count == 0 && !hasFamilies)
            return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);

        foreach (var file in context.TargetFiles.Where(f =>
            f.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)))
        {
            // Determine effective required fields for this path (global + scoped)
            var effectiveFields = GetEffectiveRequiredFields(file.RelativePath, globalRequired, scopedRequirements, generatedIndexRequirements);
            var effectiveAllowedValues = GetEffectiveAllowedValues(file.RelativePath, scopedRequirements);
            var requiresGeneratedIndexDescription = generatedIndexRequirements.Any(req =>
                req.RequiredFields?.Contains("description", StringComparer.OrdinalIgnoreCase) == true &&
                req.AppliesTo(file.RelativePath));

            // We may need to parse the document even if no global/scoped requirements apply,
            // because family matching requires frontmatter fields.
            var needsParse = effectiveFields.Count > 0 || effectiveAllowedValues.Count > 0 || hasFamilies;
            if (!needsParse)
                continue;

            StructuredDocument? doc = null;
            try
            {
                doc = context.DocumentCache?.GetOrParse(file.RelativePath)
                    ?? MarkdownParser.Parse(file.RelativePath,
                        context.FileSystem.ReadAllText(Path.Combine(context.RepositoryRoot, file.RelativePath)));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                diagnostics.Add(new Diagnostic(
                    RuleId: RuleId,
                    Severity: DiagnosticSeverity.Warning,
                    Category: Category,
                    Path: file.RelativePath,
                    Line: null,
                    Message: $"Could not read file '{file.RelativePath}': {ex.Message}",
                    Remediation: "Check file permissions and encoding.",
                    Source: "policy.yaml"));
                continue;
            }

            // Apply family schema even when the file is also declared as an explicit artifact.
            // Explicit artifacts keep their explicit role/classification, but family governance
            // still contributes frontmatter requirements and allowed values.
            string? matchedFamilyName = null;
            ArtifactFamilyDefinition? matchedFamily = null;
            if (hasFamilies)
            {
                var frontmatterFields = doc.Frontmatter?.Fields != null
                    ? (IReadOnlyDictionary<string, object?>)doc.Frontmatter.Fields
                        .ToDictionary(kv => kv.Key, kv => (object?)kv.Value, StringComparer.OrdinalIgnoreCase)
                    : null;

                matchedFamily = familyClassifier.Classify(file.RelativePath, frontmatterFields);
                if (matchedFamily != null)
                {
                    matchedFamilyName = matchedFamily.Family;

                    // Merge family-required fields
                    foreach (var field in matchedFamily.FrontmatterSchema?.Required ?? [])
                    {
                        if (!effectiveFields.Contains(field, StringComparer.OrdinalIgnoreCase))
                            effectiveFields = [.. effectiveFields, field];
                    }

                    // Merge family allowed_values with any scoped overrides so explicit
                    // path-specific requirements can broaden or specialize a family schema.
                    if (matchedFamily.FrontmatterSchema?.AllowedValues != null)
                    {
                        foreach (var (field, values) in matchedFamily.FrontmatterSchema.AllowedValues)
                            MergeAllowedValues(effectiveAllowedValues, field, values);
                    }
                }
            }

            var hasNewFamilyChecks = matchedFamily?.FrontmatterSchema?.AllowedFields != null
                || matchedFamily?.FrontmatterSchema?.DeprecatedFields != null;

            if (effectiveFields.Count == 0 && effectiveAllowedValues.Count == 0 && !hasNewFamilyChecks)
                continue;

            var familyContext = matchedFamilyName != null ? $" [family: {matchedFamilyName}]" : "";

            if (doc.Frontmatter == null)
            {
                if (effectiveFields.Count > 0)
                {
                    diagnostics.Add(new Diagnostic(
                        RuleId: RuleId,
                        Severity: DefaultSeverity,
                        Category: Category,
                        Path: file.RelativePath,
                        Line: 1,
                        Message: $"File '{file.RelativePath}' is missing frontmatter block.{familyContext}",
                        Remediation: requiresGeneratedIndexDescription
                            ? "Add a YAML frontmatter block enclosed by --- markers at the top of the file, including a non-empty 'description' field so generated indexes can include it."
                            : "Add a YAML frontmatter block enclosed by --- markers at the top of the file.",
                        Source: "policy.yaml"));
                }
                continue;
            }

            foreach (var field in effectiveFields)
            {
                if (!doc.Frontmatter.Fields.ContainsKey(field))
                {
                    diagnostics.Add(new Diagnostic(
                        RuleId: RuleId,
                        Severity: DefaultSeverity,
                        Category: Category,
                        Path: file.RelativePath,
                        Line: doc.Frontmatter.Range.Start,
                        Message: $"Required frontmatter field '{field}' is missing in '{file.RelativePath}'.{familyContext}",
                        Remediation: string.Equals(field, "description", StringComparison.OrdinalIgnoreCase) && requiresGeneratedIndexDescription
                            ? "Add 'description' to the frontmatter block so generated directory indexes can describe this file."
                            : $"Add '{field}' to the frontmatter block.",
                        Source: "policy.yaml",
                        Details: new Dictionary<string, object> { ["missingField"] = field }));
                    continue;
                }

                if (string.Equals(field, "description", StringComparison.OrdinalIgnoreCase) &&
                    requiresGeneratedIndexDescription &&
                    string.IsNullOrWhiteSpace(doc.Frontmatter.Fields[field]?.ToString()))
                {
                    diagnostics.Add(new Diagnostic(
                        RuleId: RuleId,
                        Severity: DefaultSeverity,
                        Category: Category,
                        Path: file.RelativePath,
                        Line: doc.Frontmatter.Range.Start,
                        Message: $"Required frontmatter field 'description' is blank in '{file.RelativePath}'.{familyContext}",
                        Remediation: "Set 'description' to a concise non-empty summary so generated directory indexes can include this file.",
                        Source: "policy.yaml"));
                }
            }

            // Check allowed values
            foreach (var (field, allowedValues) in effectiveAllowedValues)
            {
                if (doc.Frontmatter.Fields.TryGetValue(field, out var rawValue) && rawValue != null)
                {
                    var value = rawValue.ToString();
                    if (value != null && !allowedValues.Any(v => string.Equals(v, value, StringComparison.OrdinalIgnoreCase)))
                    {
                        diagnostics.Add(new Diagnostic(
                            RuleId: RuleId,
                            Severity: DefaultSeverity,
                            Category: Category,
                            Path: file.RelativePath,
                            Line: doc.Frontmatter.Range.Start,
                            Message: $"Frontmatter field '{field}' has value '{value}' which is not in the allowed set [{string.Join(", ", allowedValues)}] in '{file.RelativePath}'.{familyContext}",
                            Remediation: $"Set '{field}' to one of: {string.Join(", ", allowedValues)}.",
                            Source: "policy.yaml"));
                    }
                }
            }

            // Check allowed_fields (closed schema) — Warning severity
            if (matchedFamily?.FrontmatterSchema?.AllowedFields != null)
            {
                var allowedSet = new HashSet<string>(
                    matchedFamily.FrontmatterSchema.AllowedFields, StringComparer.OrdinalIgnoreCase);

                // Fields in governance.frontmatter.auto_fields are implicitly allowed in every family
                foreach (var k in context.Policy?.Governance?.Frontmatter?.AutoFields?.Keys
                                   ?? Enumerable.Empty<string>())
                    allowedSet.Add(k);

                // Deprecated fields take precedence — don't double-flag as unexpected
                var deprecatedKeys = new HashSet<string>(
                    matchedFamily.FrontmatterSchema.DeprecatedFields?.Keys ?? Enumerable.Empty<string>(),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var key in doc.Frontmatter.Fields.Keys)
                {
                    if (allowedSet.Contains(key) || deprecatedKeys.Contains(key)) continue;

                    diagnostics.Add(new Diagnostic(
                        RuleId: RuleId,
                        Severity: DiagnosticSeverity.Warning,
                        Category: Category,
                        Path: file.RelativePath,
                        Line: doc.Frontmatter.Range.Start,
                        Message: $"Frontmatter field '{key}' is not declared in allowed_fields for family '{matchedFamily.Family}' in '{file.RelativePath}'.{familyContext}",
                        Remediation: $"Remove '{key}' from frontmatter, or add it to the family's allowed_fields in policy.yaml.",
                        Source: "policy.yaml",
                        Details: new Dictionary<string, object> { ["unexpectedField"] = key }));
                }
            }

            // Check deprecated_fields — Warning (or Error when both deprecated and replacement coexist)
            if (matchedFamily?.FrontmatterSchema?.DeprecatedFields != null)
            {
                foreach (var (deprecated, replacement) in matchedFamily.FrontmatterSchema.DeprecatedFields)
                {
                    if (!doc.Frontmatter.Fields.ContainsKey(deprecated)) continue;

                    if (replacement != null && doc.Frontmatter.Fields.ContainsKey(replacement))
                    {
                        diagnostics.Add(new Diagnostic(
                            RuleId: RuleId,
                            Severity: DiagnosticSeverity.Error,
                            Category: Category,
                            Path: file.RelativePath,
                            Line: doc.Frontmatter.Range.Start,
                            Message: $"Frontmatter field '{deprecated}' is deprecated in favor of '{replacement}', and both are present in '{file.RelativePath}'.{familyContext}",
                            Remediation: $"Remove the deprecated field '{deprecated}'. The replacement '{replacement}' is already present.",
                            Source: "policy.yaml",
                            Details: new Dictionary<string, object>
                            {
                                ["deprecatedField"] = deprecated,
                                ["conflict"] = (object)true
                            }));
                    }
                    else
                    {
                        var msg = replacement != null
                            ? $"Frontmatter field '{deprecated}' is deprecated in family '{matchedFamily.Family}'; use '{replacement}' instead in '{file.RelativePath}'.{familyContext}"
                            : $"Frontmatter field '{deprecated}' is deprecated in family '{matchedFamily.Family}' and should be removed from '{file.RelativePath}'.{familyContext}";
                        var remediation = replacement != null
                            ? $"Rename '{deprecated}' to '{replacement}'."
                            : $"Remove the deprecated field '{deprecated}'.";
                        diagnostics.Add(new Diagnostic(
                            RuleId: RuleId,
                            Severity: DiagnosticSeverity.Warning,
                            Category: Category,
                            Path: file.RelativePath,
                            Line: doc.Frontmatter.Range.Start,
                            Message: msg,
                            Remediation: remediation,
                            Source: "policy.yaml",
                            Details: new Dictionary<string, object>
                            {
                                ["deprecatedField"] = deprecated
                            }));
                    }
                }
            }
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }

    public async Task<IReadOnlyList<Fix>> ComputeFixesAsync(ValidationContext context)
    {
        var diagnostics = await EvaluateAsync(context);
        var fixes = new List<Fix>();

        // Collect missing-field diagnostics grouped by file
        var missingByFile = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var diag in diagnostics)
        {
            if (diag.Path == null) continue;
            if (diag.Details == null || !diag.Details.TryGetValue("missingField", out var fieldObj))
                continue;

            var field = fieldObj?.ToString();
            if (string.IsNullOrWhiteSpace(field)) continue;

            if (!missingByFile.TryGetValue(diag.Path, out var fields))
            {
                fields = [];
                missingByFile[diag.Path] = fields;
            }
            fields.Add(field);
        }

        // Collect deprecated-field diagnostics grouped by file (Warning only — Error conflicts are not fixable)
        var deprecatedByFile = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var diag in diagnostics)
        {
            if (diag.Path == null) continue;
            if (diag.Severity != DiagnosticSeverity.Warning) continue;
            if (diag.Details == null || !diag.Details.TryGetValue("deprecatedField", out var depObj)) continue;

            var deprecated = depObj?.ToString();
            if (string.IsNullOrWhiteSpace(deprecated)) continue;

            if (!deprecatedByFile.TryGetValue(diag.Path, out var entries))
            {
                entries = [];
                deprecatedByFile[diag.Path] = entries;
            }
            entries.Add(deprecated);
        }

        // Process all files that need any fix — one Fix object per file to avoid write conflicts
        var allFiles = new HashSet<string>(missingByFile.Keys, StringComparer.OrdinalIgnoreCase);
        allFiles.UnionWith(deprecatedByFile.Keys);

        // Build family classifier once for deprecated-field lookups
        var familyClassifier = new ArtifactFamilyClassifier(context.Policy?.ArtifactFamilies);

        foreach (var relPath in allFiles)
        {
            var fullPath = Path.Combine(context.RepositoryRoot, relPath);
            if (!context.FileSystem.FileExists(fullPath))
                continue;

            var content = context.FileSystem.ReadAllText(fullPath);
            var doc = context.DocumentCache?.GetOrParse(relPath)
                ?? MarkdownParser.Parse(relPath, content);

            if (doc.Frontmatter == null) continue;

            // Clone the current fields dict (preserve ordering)
            var newFields = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in doc.Frontmatter.Fields)
                newFields[kv.Key] = kv.Value;

            var descriptionParts = new List<string>();

            // Step A: Apply deprecated-field renames first so their values are available
            if (deprecatedByFile.TryGetValue(relPath, out var deprecatedKeys))
            {
                // Re-classify to find the matched family's DeprecatedFields map
                var frontmatterFields = doc.Frontmatter.Fields
                    .ToDictionary(kv => kv.Key, kv => (object?)kv.Value, StringComparer.OrdinalIgnoreCase);
                var matchedFamily = familyClassifier.Classify(relPath, frontmatterFields);
                var deprecatedMap = matchedFamily?.FrontmatterSchema?.DeprecatedFields;

                var renamed = new List<string>();
                var removed = new List<string>();

                foreach (var deprecated in deprecatedKeys)
                {
                    if (!newFields.TryGetValue(deprecated, out var oldValue)) continue;

                    string? replacement = null;
                    deprecatedMap?.TryGetValue(deprecated, out replacement);

                    newFields.Remove(deprecated);

                    if (replacement != null && !newFields.ContainsKey(replacement))
                    {
                        newFields[replacement] = oldValue;
                        renamed.Add($"'{deprecated}'→'{replacement}'");
                    }
                    else
                    {
                        removed.Add($"'{deprecated}'");
                    }
                }

                if (renamed.Count > 0)
                    descriptionParts.Add($"Renamed deprecated field(s): {string.Join(", ", renamed)}.");
                if (removed.Count > 0)
                    descriptionParts.Add($"Removed deprecated field(s): {string.Join(", ", removed)}.");
            }

            // Step B: Insert missing fields (only if not already present after renames)
            if (missingByFile.TryGetValue(relPath, out var missingFields))
            {
                var inserted = new List<string>();
                foreach (var field in missingFields)
                {
                    if (!newFields.ContainsKey(field))
                    {
                        newFields[field] = "";
                        inserted.Add(field);
                    }
                }
                if (inserted.Count > 0)
                    descriptionParts.Add($"Inserted missing field(s) [{string.Join(", ", inserted)}] with placeholder values.");
            }

            if (descriptionParts.Count == 0) continue;

            var description = string.Join(" ", descriptionParts);
            var result = FrontmatterEditor.ReplaceAllFields(doc, newFields, description);
            if (result.HasChanges)
            {
                fixes.Add(new Fix(RuleId, relPath, description, result.NewContent));
            }
        }

        return fixes;
    }

    private static List<string> GetEffectiveRequiredFields(
        string relativePath,
        List<string> globalRequired,
        List<CompiledFrontmatterReq> scopedRequirements,
        List<CompiledFrontmatterReq> generatedIndexRequirements)
    {
        var fields = new HashSet<string>(globalRequired, StringComparer.OrdinalIgnoreCase);

        foreach (var req in scopedRequirements)
        {
            if (req.AppliesTo(relativePath) && req.RequiredFields != null)
            {
                foreach (var f in req.RequiredFields)
                    fields.Add(f);
            }
        }

        foreach (var req in generatedIndexRequirements)
        {
            if (req.AppliesTo(relativePath) && req.RequiredFields != null)
            {
                foreach (var f in req.RequiredFields)
                    fields.Add(f);
            }
        }

        return [.. fields];
    }

    private static Dictionary<string, List<string>> GetEffectiveAllowedValues(
        string relativePath,
        List<CompiledFrontmatterReq> scopedRequirements)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var req in scopedRequirements)
        {
            if (req.AppliesTo(relativePath) && req.AllowedValues != null)
            {
                foreach (var (field, values) in req.AllowedValues)
                    MergeAllowedValues(result, field, values);
            }
        }

        return result;
    }

    private static void MergeAllowedValues(
        Dictionary<string, List<string>> target,
        string field,
        IEnumerable<string> values)
    {
        if (!target.TryGetValue(field, out var existing))
        {
            target[field] = values
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            return;
        }

        var merged = existing
            .Union(values, StringComparer.OrdinalIgnoreCase)
            .ToList();

        target[field] = merged;
    }

    private static List<CompiledFrontmatterReq> CompileScopedRequirements(
        List<FrontmatterRequirement>? requirements)
    {
        if (requirements == null || requirements.Count == 0)
            return [];

        var compiled = new List<CompiledFrontmatterReq>();
        foreach (var req in requirements)
        {
            if (string.IsNullOrWhiteSpace(req.Pattern))
                continue;

            compiled.Add(new CompiledFrontmatterReq(
                Glob.Parse(req.Pattern),
                req.RequiredFields,
                req.AllowedValues,
                null));
        }
        return compiled;
    }

    private static List<CompiledFrontmatterReq> CompileGeneratedIndexRequirements(
        List<MaintenanceArtifactDef>? maintenanceArtifacts)
    {
        if (maintenanceArtifacts == null || maintenanceArtifacts.Count == 0)
            return [];

        var compiled = new List<CompiledFrontmatterReq>();
        foreach (var artifact in maintenanceArtifacts)
        {
            if (!string.Equals(artifact.Type, "directory-index", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(artifact.Source))
            {
                continue;
            }

            compiled.Add(new CompiledFrontmatterReq(
                Glob.Parse(artifact.Source),
                ["description"],
                null,
                artifact.Path));
        }

        return compiled;
    }

    private sealed record CompiledFrontmatterReq(
        Glob Glob,
        List<string>? RequiredFields,
        Dictionary<string, List<string>>? AllowedValues,
        string? ExcludedPath)
    {
        public bool AppliesTo(string relativePath)
        {
            if (!Glob.IsMatch(relativePath))
                return false;

            if (!string.IsNullOrWhiteSpace(ExcludedPath) &&
                string.Equals(PathHelper.NormalizeSeparators(ExcludedPath), relativePath, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }
    }
}
