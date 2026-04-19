using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Steward.Core;
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

        // Use AllDiscoveredFiles for the existence set so that scoped runs
        // (--scope changed/staged) do not report links to unchanged files as broken.
        var allFiles = context.AllDiscoveredFiles ?? context.TargetFiles;
        var existingPaths = new HashSet<string>(
            allFiles.Select(f => PathHelper.NormalizeSeparators(f.RelativePath)),
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
                        null,
                        new Dictionary<string, object> { ["targetPath"] = resolvedPath }));
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
        return
        [
            .. ExtractInternalLinkReferences(content)
                .Select(reference => (reference.Target, reference.Line))
        ];
    }

    public static List<MarkdownLinkReference> ExtractInternalLinkReferences(string content)
    {
        var results = new List<MarkdownLinkReference>();
        var document = Markdig.Markdown.Parse(content, Pipeline);

        foreach (var link in document.Descendants<LinkInline>())
        {
            var rawTarget = link.Url;
            if (string.IsNullOrEmpty(rawTarget))
                continue;

            var (target, fragment) = SplitTarget(rawTarget);
            if (target.Length == 0 || !IsInternalLink(target))
                continue;

            results.Add(new MarkdownLinkReference(
                rawTarget,
                target,
                fragment,
                ExtractLinkText(link),
                link.Line + 1));
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
        var sourceDir = PathHelper.NormalizeSeparators(Path.GetDirectoryName(PathHelper.NormalizeSeparators(sourceFile)) ?? "");
        var combined = sourceDir.Length > 0 ? $"{sourceDir}/{target}" : target;

        // Normalize path segments
        var parts = PathHelper.NormalizeSeparators(combined).Split('/').ToList();
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

    private static (string Target, string? Fragment) SplitTarget(string rawTarget)
    {
        var queryIndex = rawTarget.IndexOf('?');
        if (queryIndex >= 0)
            rawTarget = rawTarget[..queryIndex];

        string? fragment = null;
        var fragmentIndex = rawTarget.IndexOf('#');
        if (fragmentIndex >= 0)
        {
            fragment = rawTarget[(fragmentIndex + 1)..];
            rawTarget = rawTarget[..fragmentIndex];
        }

        return (rawTarget, string.IsNullOrWhiteSpace(fragment) ? null : fragment);
    }

    private static string ExtractLinkText(LinkInline link)
    {
        var builder = new System.Text.StringBuilder();
        AppendInlineText(link.FirstChild, builder);
        return builder.ToString().Trim();
    }

    private static void AppendInlineText(Inline? inline, System.Text.StringBuilder builder)
    {
        while (inline != null)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    builder.Append(literal.Content.ToString());
                    break;
                case CodeInline code:
                    builder.Append(code.Content);
                    break;
                case LineBreakInline:
                    builder.Append(' ');
                    break;
                case ContainerInline container:
                    AppendInlineText(container.FirstChild, builder);
                    break;
            }

            inline = inline.NextSibling;
        }
    }
}

public sealed record MarkdownLinkReference(
    string RawTarget,
    string Target,
    string? Fragment,
    string LinkText,
    int Line);
