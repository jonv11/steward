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

        var requiredFromPolicy = context.Policy?.Validation?.RequiredFrontmatterFields ?? [];

        if (requiredFromPolicy.Count == 0)
            return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);

        foreach (var file in context.TargetFiles.Where(f =>
            f.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var doc = context.DocumentCache?.GetOrParse(file.RelativePath)
                    ?? MarkdownParser.Parse(file.RelativePath,
                        context.FileSystem.ReadAllText(Path.Combine(context.RepositoryRoot, file.RelativePath)));

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
                    Source: null));
            }
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }
}
