using Steward.Core.Markdown;

namespace Steward.Core.Validation.Rules;

/// <summary>
/// STWD-017: Requires headings within a Markdown file to be unique after anchor normalization.
/// This keeps heading selectors and Markdown-style anchor links deterministic.
/// </summary>
public sealed class UniqueHeadingTextRule : IValidationRule
{
    public string RuleId => "STWD-017";
    public string Category => "structure";
    public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Warning;
    public string Description => "Heading text within a Markdown file should be unique after Markdown-anchor normalization.";

    public Task<IReadOnlyList<Diagnostic>> EvaluateAsync(ValidationContext context)
    {
        var diagnostics = new List<Diagnostic>();

        foreach (var file in context.TargetFiles.Where(file =>
                     file.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)))
        {
            var fullPath = Path.Combine(context.RepositoryRoot, file.RelativePath);
            if (!context.FileSystem.FileExists(fullPath))
                continue;

            StructuredDocument? document;
            try
            {
                document = context.DocumentCache?.GetOrParse(file.RelativePath)
                    ?? MarkdownParser.Parse(file.RelativePath, context.FileSystem.ReadAllText(fullPath));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            var firstByKey = new Dictionary<string, Section>(StringComparer.OrdinalIgnoreCase);
            foreach (var section in MarkdownHeadings.Flatten(document.Sections))
            {
                var normalizedKey = GetNormalizedHeadingKey(section.Heading);
                if (!firstByKey.TryAdd(normalizedKey, section))
                {
                    var original = firstByKey[normalizedKey];
                    diagnostics.Add(new Diagnostic(
                        RuleId,
                        DefaultSeverity,
                        Category,
                        file.RelativePath,
                        section.Range.Start,
                        $"Heading '{section.Heading}' duplicates heading text already used on line {original.Range.Start} in '{file.RelativePath}'. Normalized anchor slug: '{normalizedKey}'.",
                        $"Rename one of the headings so the normalized anchor slug is unique within the file. Current slug: '{normalizedKey}'.",
                        "policy.yaml"));
                }
            }
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }

    private static string GetNormalizedHeadingKey(string heading)
    {
        var anchorSlug = MarkdownHeadings.ToAnchorSlug(heading);
        if (!string.IsNullOrWhiteSpace(anchorSlug))
            return anchorSlug;

        return heading.Trim().ToLowerInvariant();
    }
}
