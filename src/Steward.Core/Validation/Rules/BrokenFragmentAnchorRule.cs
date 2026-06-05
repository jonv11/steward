using System.Text.RegularExpressions;
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
public sealed class BrokenFragmentAnchorRule : IValidationRule, IFixableRule
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

            foreach (var (rawTarget, fragment, rawFragment, line) in fragmentLinks)
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
                            ["rawFragment"] = rawFragment,
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
    /// Returns tuples of (fileTarget, fragmentSlug, rawFragment, lineNumber).
    /// fileTarget is the path part (empty string for fragment-only links like #heading).
    /// rawFragment is the un-normalized URL fragment text (used for fix replacement).
    /// </summary>
    internal static List<(string FileTarget, string Fragment, string RawFragment, int Line)> ExtractFragmentLinks(string content)
    {
        var results = new List<(string, string, string, int)>();
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

            results.Add((filePart, slug, rawFragment, link.Line + 1));
        }

        return results;
    }

    public async Task<IReadOnlyList<Fix>> ComputeFixesAsync(ValidationContext context)
    {
        var diagnostics = await EvaluateAsync(context);
        if (diagnostics.Count == 0)
            return [];

        // Group diagnostics by source file so we apply all fixes in one pass per file.
        var byFile = diagnostics
            .Where(d => d.Path != null && d.Details != null)
            .GroupBy(d => d.Path!);

        var fixes = new List<Fix>();

        foreach (var group in byFile)
        {
            var sourceRelPath = group.Key;
            var sourcePath = Path.Combine(context.RepositoryRoot, sourceRelPath);
            if (!context.FileSystem.FileExists(sourcePath))
                continue;

            var content = context.FileSystem.ReadAllText(sourcePath);
            var modified = content;
            var appliedAny = false;
            var descriptions = new List<string>();

            foreach (var diag in group)
            {
                if (!diag.Details!.TryGetValue("fragment", out var fragmentObj) ||
                    !diag.Details.TryGetValue("rawFragment", out var rawFragmentObj) ||
                    !diag.Details.TryGetValue("targetFile", out var targetFileObj))
                    continue;

                var fragment = fragmentObj.ToString()!;
                var rawFragment = rawFragmentObj.ToString()!;
                var targetFile = targetFileObj.ToString()!;

                var slugs = GetHeadingSlugs(targetFile, context);
                if (slugs == null || slugs.Count == 0)
                    continue;

                var bestMatch = StringSimilarity.BestMatch(fragment, slugs);
                if (bestMatch == null)
                    continue;

                // Replace #rawFragment → #bestMatch inside Markdown link URLs.
                // Pattern: matches the fragment inside [...](path#rawFragment) or (#rawFragment)
                var escapedRaw = Regex.Escape(rawFragment);
                var pattern = $@"(\[[^\]]*\]\([^)]*?)#{escapedRaw}([\)#?])";
                var replacement = $"$1#{bestMatch}$2";
                var next = Regex.Replace(modified, pattern, replacement, RegexOptions.IgnoreCase);
                if (next == modified)
                    continue;

                modified = next;
                appliedAny = true;
                descriptions.Add($"'#{fragment}' → '#{bestMatch}'");
            }

            if (!appliedAny)
                continue;

            fixes.Add(new Fix(
                RuleId,
                sourceRelPath,
                $"Update broken fragment anchor(s) in {sourceRelPath}: {string.Join(", ", descriptions)}",
                modified));
        }

        return fixes;
    }
}
