using DotNet.Globbing;
using Steward.Core.Configuration;
using Steward.Core.Markdown;
using Steward.Core.Validation;

namespace Steward.Core.Validation.Rules;

/// <summary>
/// STWD-003: Checks that required frontmatter fields exist in Markdown files.
/// Supports both global requirements and path-scoped requirements.
/// </summary>
public sealed class RequiredFrontmatterFieldRule : IValidationRule
{
    public string RuleId => "STWD-003";
    public string Category => "frontmatter";
    public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Error;
    public string Description => "Required frontmatter fields must be present in Markdown files.";

    public Task<IReadOnlyList<Diagnostic>> EvaluateAsync(ValidationContext context)
    {
        var diagnostics = new List<Diagnostic>();

        var requiredFromPolicy = context.Policy?.Validation?.RequiredFrontmatterFields ?? [];

        // Also check governance.frontmatter.required_fields (RFC-002 canonical location)
        var requiredFromGovernance = context.Policy?.Governance?.Frontmatter?.RequiredFields ?? [];

        // Merge both sources, removing duplicates
        var globalRequired = requiredFromPolicy.Union(requiredFromGovernance, StringComparer.OrdinalIgnoreCase).ToList();

        // Compile path-scoped frontmatter requirements
        var scopedRequirements = CompileScopedRequirements(
            context.Policy?.Validation?.FrontmatterRequirements);

        if (globalRequired.Count == 0 && scopedRequirements.Count == 0)
            return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);

        foreach (var file in context.TargetFiles.Where(f =>
            f.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)))
        {
            // Determine effective required fields for this path
            var effectiveFields = GetEffectiveRequiredFields(file.RelativePath, globalRequired, scopedRequirements);
            var effectiveAllowedValues = GetEffectiveAllowedValues(file.RelativePath, scopedRequirements);

            if (effectiveFields.Count == 0 && effectiveAllowedValues.Count == 0)
                continue;

            try
            {
                var doc = context.DocumentCache?.GetOrParse(file.RelativePath)
                    ?? MarkdownParser.Parse(file.RelativePath,
                        context.FileSystem.ReadAllText(Path.Combine(context.RepositoryRoot, file.RelativePath)));

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
                            Message: $"File '{file.RelativePath}' is missing frontmatter block.",
                            Remediation: "Add a YAML frontmatter block enclosed by --- markers at the top of the file.",
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
                            Message: $"Required frontmatter field '{field}' is missing in '{file.RelativePath}'.",
                            Remediation: $"Add '{field}' to the frontmatter block.",
                            Source: "policy.yaml"));
                    }
                }

                // Check allowed values for scoped requirements
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
                                Message: $"Frontmatter field '{field}' has value '{value}' which is not in the allowed set [{string.Join(", ", allowedValues)}] in '{file.RelativePath}'.",
                                Remediation: $"Set '{field}' to one of: {string.Join(", ", allowedValues)}.",
                                Source: "policy.yaml"));
                        }
                    }
                }
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
            }
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }

    private static List<string> GetEffectiveRequiredFields(
        string relativePath,
        List<string> globalRequired,
        List<CompiledFrontmatterReq> scopedRequirements)
    {
        var fields = new HashSet<string>(globalRequired, StringComparer.OrdinalIgnoreCase);

        foreach (var req in scopedRequirements)
        {
            if (req.Glob.IsMatch(relativePath) && req.RequiredFields != null)
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
            if (req.Glob.IsMatch(relativePath) && req.AllowedValues != null)
            {
                foreach (var (field, values) in req.AllowedValues)
                    result[field] = values;
            }
        }

        return result;
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
                req.AllowedValues));
        }
        return compiled;
    }

    private sealed record CompiledFrontmatterReq(
        Glob Glob,
        List<string>? RequiredFields,
        Dictionary<string, List<string>>? AllowedValues);
}
