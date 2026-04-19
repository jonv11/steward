using DotNet.Globbing;
using Steward.Core;
using Steward.Core.Configuration;
using Steward.Core.Markdown;
using Steward.Core.Validation;

namespace Steward.Core.Validation.Rules;

/// <summary>
/// STWD-003: Checks that required frontmatter fields exist in Markdown files.
/// Supports global requirements, path-scoped requirements, and artifact-family schema requirements.
/// Implements IFixableRule to insert missing fields with placeholder values.
/// </summary>
public sealed class RequiredFrontmatterFieldRule : IValidationRule, IFixableRule
{
    public string RuleId => "STWD-003";
    public string Category => "frontmatter";
    public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Error;
    public string Description => "Required frontmatter fields must be present in Markdown files.";

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
            if (hasFamilies)
            {
                var frontmatterFields = doc.Frontmatter?.Fields != null
                    ? (IReadOnlyDictionary<string, object?>)doc.Frontmatter.Fields
                        .ToDictionary(kv => kv.Key, kv => (object?)kv.Value, StringComparer.OrdinalIgnoreCase)
                    : null;

                var matchedFamily = familyClassifier.Classify(file.RelativePath, frontmatterFields);
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

            if (effectiveFields.Count == 0 && effectiveAllowedValues.Count == 0)
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
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }

    public async Task<IReadOnlyList<Fix>> ComputeFixesAsync(ValidationContext context)
    {
        var diagnostics = await EvaluateAsync(context);
        var fixes = new List<Fix>();

        // Group missing-field diagnostics by file path so we can batch all missing
        // fields into a single frontmatter edit per file.
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

        foreach (var (relPath, fields) in missingByFile)
        {
            var fullPath = Path.Combine(context.RepositoryRoot, relPath);
            if (!context.FileSystem.FileExists(fullPath))
                continue;

            var content = context.FileSystem.ReadAllText(fullPath);
            var doc = context.DocumentCache?.GetOrParse(relPath)
                ?? MarkdownParser.Parse(relPath, content);

            // Insert all missing fields at once with placeholder values
            var placeholders = fields.ToDictionary(
                f => f,
                f => (string)"",
                StringComparer.OrdinalIgnoreCase);

            var result = FrontmatterEditor.SetFields(doc, placeholders);
            if (result.HasChanges)
            {
                fixes.Add(new Fix(
                    RuleId,
                    relPath,
                    $"Insert missing frontmatter field(s) [{string.Join(", ", fields)}] in '{relPath}' with placeholder values.",
                    result.NewContent));
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
