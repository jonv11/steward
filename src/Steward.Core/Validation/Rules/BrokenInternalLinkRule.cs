using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Steward.Core.Abstractions;
using Steward.Core.Markdown;

namespace Steward.Core.Validation.Rules;

/// <summary>
/// STWD-008: Detects internal Markdown links that point to non-existent files.
/// </summary>
public sealed class BrokenInternalLinkRule : IValidationRule
{
    public string RuleId => "STWD-008";
    public string Category => "broken-link";
    public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Warning;
    public string Description => "Internal Markdown links should resolve to existing files.";

    public Task<IReadOnlyList<Diagnostic>> EvaluateAsync(ValidationContext context)
    {
        var diagnostics = new List<Diagnostic>();
        var existingPaths = new HashSet<string>(
            context.TargetFiles.Select(f => f.RelativePath.Replace('\\', '/')),
            StringComparer.OrdinalIgnoreCase);

        foreach (var file in context.TargetFiles)
        {
            if (!file.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                continue;

            var fullPath = Path.Combine(context.RepositoryRoot, file.RelativePath);
            if (!context.FileSystem.FileExists(fullPath))
                continue;

            var content = context.FileSystem.ReadAllText(fullPath);
            var links = ExtractInternalLinks(content);

            foreach (var (target, line) in links)
            {
                var resolvedPath = ResolveLinkTarget(file.RelativePath, target);
                if (resolvedPath != null && !existingPaths.Contains(resolvedPath))
                {
                    diagnostics.Add(new Diagnostic(
                        RuleId,
                        DefaultSeverity,
                        Category,
                        file.RelativePath,
                        line,
                        $"Broken link to '{target}' — file not found.",
                        $"Verify the link target exists or update the reference.",
                        null));
                }
            }
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UsePreciseSourceLocation()
        .Build();

    public static List<(string Target, int Line)> ExtractInternalLinks(string content)
    {
        var results = new List<(string, int)>();
        var document = Markdig.Markdown.Parse(content, Pipeline);

        foreach (var link in document.Descendants<LinkInline>())
        {
            var url = link.Url;
            if (string.IsNullOrEmpty(url)) continue;

            // Strip fragment
            var fragmentIdx = url.IndexOf('#');
            if (fragmentIdx >= 0)
                url = url[..fragmentIdx];

            // Strip query string
            var queryIdx = url.IndexOf('?');
            if (queryIdx >= 0)
                url = url[..queryIdx];

            if (url.Length > 0 && IsInternalLink(url))
            {
                results.Add((url, link.Line + 1)); // Markdig lines are 0-based
            }
        }

        return results;
    }

    private static bool IsInternalLink(string target)
    {
        // Skip external URLs, mailto, etc.
        if (target.Contains("://", StringComparison.Ordinal)) return false;
        if (target.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)) return false;
        if (target.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)) return false;
        if (target.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return false;
        return true;
    }

    public static string? ResolveLinkTarget(string sourceFile, string target)
    {
        if (string.IsNullOrWhiteSpace(target)) return null;

        // Resolve relative to the source file's directory
        var sourceDir = Path.GetDirectoryName(sourceFile.Replace('\\', '/'))?.Replace('\\', '/') ?? "";
        var combined = sourceDir.Length > 0 ? $"{sourceDir}/{target}" : target;

        // Normalize path segments
        var parts = combined.Replace('\\', '/').Split('/').ToList();
        var normalized = new List<string>();

        foreach (var part in parts)
        {
            if (part == "." || part == "") continue;
            if (part == ".." && normalized.Count > 0)
            {
                normalized.RemoveAt(normalized.Count - 1);
                continue;
            }
            normalized.Add(part);
        }

        return normalized.Count > 0 ? string.Join('/', normalized) : null;
    }
}
