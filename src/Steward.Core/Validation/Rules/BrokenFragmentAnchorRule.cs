using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Steward.Core.Markdown;

namespace Steward.Core.Validation.Rules;

/// <summary>
/// STWD-018: Internal Markdown links that include a fragment anchor (#heading)
/// should reference a heading that actually exists in the target file.
/// Heading slugs are normalized using the same algorithm as GitHub Markdown.
/// </summary>
public sealed class BrokenFragmentAnchorRule : IValidationRule
{
    public string RuleId => "STWD-018";
    public string Category => "broken-link";
    public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Warning;
    public string Description => "Internal Markdown links with fragment anchors (#heading) must reference a heading that exists in the target file.";

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UsePreciseSourceLocation()
        .Build();

    public Task<IReadOnlyList<Diagnostic>> EvaluateAsync(ValidationContext context)
    {
        var diagnostics = new List<Diagnostic>();

        // Use AllDiscoveredFiles for the existence set to avoid scoped false-positives.
        var allFiles = context.AllDiscoveredFiles ?? context.TargetFiles;
        var allFilePaths = new HashSet<string>(
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
            var fragmentLinks = ExtractFragmentLinks(content);

            foreach (var (rawTarget, fragment, line) in fragmentLinks)
            {
                // Resolve the file path part
                string resolvedFilePath;
                if (string.IsNullOrEmpty(rawTarget))
                {
                    // Fragment-only link (#section) — refers to the current file
                    resolvedFilePath = PathHelper.NormalizeSeparators(file.RelativePath);
                }
                else
                {
                    var resolved = BrokenInternalLinkRule.ResolveLinkTarget(file.RelativePath, rawTarget);
                    if (resolved == null) continue;
                    resolvedFilePath = resolved;
                }

                // Skip if the target file doesn't exist (STWD-008 reports that)
                if (!allFilePaths.Contains(resolvedFilePath))
                    continue;

                // Only validate fragments in Markdown files
                if (!resolvedFilePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Build heading slug set for the target file
                var slugs = GetHeadingSlugs(resolvedFilePath, context);
                if (slugs == null)
                    continue; // Could not read target file — skip

                if (!slugs.Contains(fragment))
                {
                    diagnostics.Add(new Diagnostic(
                        RuleId,
                        DefaultSeverity,
                        Category,
                        file.RelativePath,
                        line,
                        $"Broken fragment anchor '#{fragment}' in link to '{(string.IsNullOrEmpty(rawTarget) ? file.RelativePath : rawTarget)}' — " +
                        $"no heading with that anchor slug exists in '{resolvedFilePath}'.",
                        $"Verify the heading exists in '{resolvedFilePath}' or update the fragment anchor. " +
                        $"Anchors are lower-cased and space/punctuation is replaced with hyphens.",
                        null,
                        new Dictionary<string, object>
                        {
                            ["fragment"] = fragment,
                            ["targetFile"] = resolvedFilePath
                        }));
                }
            }
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }

    private HashSet<string>? GetHeadingSlugs(string relativeFilePath, ValidationContext context)
    {
        try
        {
            var fullPath = Path.Combine(context.RepositoryRoot, relativeFilePath);
            if (!context.FileSystem.FileExists(fullPath))
                return null;

            var doc = context.DocumentCache?.GetOrParse(relativeFilePath)
                ?? MarkdownParser.Parse(relativeFilePath, context.FileSystem.ReadAllText(fullPath));

            var slugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var section in MarkdownHeadings.Flatten(doc.Sections))
            {
                var slug = MarkdownHeadings.ToAnchorSlug(section.Heading);
                if (!string.IsNullOrEmpty(slug))
                    slugs.Add(slug);
            }
            return slugs;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts internal links that contain fragment anchors.
    /// Returns tuples of (fileTarget, fragmentSlug, lineNumber).
    /// fileTarget is the path part (empty string for fragment-only links like #heading).
    /// </summary>
    internal static List<(string FileTarget, string Fragment, int Line)> ExtractFragmentLinks(string content)
    {
        var results = new List<(string, string, int)>();
        var document = Markdig.Markdown.Parse(content, Pipeline);

        foreach (var link in document.Descendants<LinkInline>())
        {
            var url = link.Url;
            if (string.IsNullOrEmpty(url)) continue;

            // Skip external URLs
            if (url.Contains("://", StringComparison.Ordinal)) continue;
            if (url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)) continue;
            if (url.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)) continue;
            if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) continue;

            // Strip query string
            var queryIdx = url.IndexOf('?');
            if (queryIdx >= 0)
                url = url[..queryIdx];

            var fragmentIdx = url.IndexOf('#');
            if (fragmentIdx < 0) continue; // No fragment — not our concern

            var filePart = url[..fragmentIdx];
            var rawFragment = url[(fragmentIdx + 1)..];

            if (string.IsNullOrEmpty(rawFragment)) continue;

            // Normalize the fragment to a slug for comparison
            var slug = MarkdownHeadings.ToAnchorSlug(rawFragment);
            if (string.IsNullOrEmpty(slug)) continue;

            results.Add((filePart, slug, link.Line + 1));
        }

        return results;
    }
}
