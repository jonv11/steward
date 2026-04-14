using Steward.Core.Markdown;
using Steward.Core.Validation;

namespace Steward.Core.Validation.Rules;

/// <summary>
/// STWD-003: Checks that required frontmatter fields exist in Markdown files.
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
        var requiredFields = context.Policy?.Governance?.StartHere; // reuse or add dedicated config

        // If no policy defines required frontmatter fields, skip
        if (context.Policy?.Validation?.SeverityOverrides == null &&
            context.Policy?.Governance?.StartHere == null)
            return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);

        // Look for required_frontmatter_fields in validation config
        // Convention: severity_overrides keys starting with "frontmatter." are required fields
        var requiredFromPolicy = context.Policy?.Validation?.SeverityOverrides?
            .Where(kv => kv.Key.StartsWith("frontmatter.", StringComparison.OrdinalIgnoreCase))
            .Select(kv => kv.Key[12..])
            .ToList() ?? [];

        if (requiredFromPolicy.Count == 0)
            return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);

        foreach (var file in context.TargetFiles.Where(f =>
            f.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var content = context.FileSystem.ReadAllText(
                    Path.Combine(context.RepositoryRoot, file.RelativePath));
                var doc = MarkdownParser.Parse(file.RelativePath, content);

                if (doc.Frontmatter == null)
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
                    continue;
                }

                foreach (var field in requiredFromPolicy)
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
            }
            catch
            {
                // Skip files that can't be read
            }
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }
}
