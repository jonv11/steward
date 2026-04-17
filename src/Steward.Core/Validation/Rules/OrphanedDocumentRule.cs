using Steward.Core;
using Steward.Core.Abstractions;

namespace Steward.Core.Validation.Rules;

/// <summary>
/// STWD-013: Detects Markdown files that are not referenced by any start_here entry,
/// artifact declaration, index, or internal link from another file.
/// Files with frontmatter 'standalone: true' are suppressed.
/// </summary>
public sealed class OrphanedDocumentRule : IValidationRule
{
    public string RuleId => "STWD-013";
    public string Category => "discoverability";
    public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Info;
    public string Description => "Markdown files should be reachable from at least one navigation surface.";

    public Task<IReadOnlyList<Diagnostic>> EvaluateAsync(ValidationContext context)
    {
        var diagnostics = new List<Diagnostic>();

        // Collect all referenced paths
        var referencedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. start_here entries
        if (context.Policy?.Governance?.StartHere != null)
        {
            foreach (var s in context.Policy.Governance.StartHere)
                referencedPaths.Add(PathHelper.NormalizeSeparators(s));
        }

        // 2. Artifact paths
        if (context.Policy?.Artifacts != null)
        {
            foreach (var a in context.Policy.Artifacts)
            {
                if (!string.IsNullOrWhiteSpace(a.Path))
                    referencedPaths.Add(PathHelper.NormalizeAndTrim(a.Path));
            }
        }

        // 3. Internal links from all Markdown files
        var mdFiles = context.TargetFiles
            .Where(f => f.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var file in mdFiles)
        {
            var fullPath = Path.Combine(context.RepositoryRoot, file.RelativePath);
            if (!context.FileSystem.FileExists(fullPath))
                continue;

            var content = context.FileSystem.ReadAllText(fullPath);
            var links = BrokenInternalLinkRule.ExtractInternalLinks(content);

            foreach (var (target, _) in links)
            {
                var resolved = BrokenInternalLinkRule.ResolveLinkTarget(file.RelativePath, target);
                if (resolved != null)
                    referencedPaths.Add(resolved);
            }
        }

        // 4. Check each Markdown file for orphan status
        foreach (var file in mdFiles)
        {
            var relPath = PathHelper.NormalizeSeparators(file.RelativePath);

            if (referencedPaths.Contains(relPath))
                continue;

            // Check for standalone suppression in frontmatter
            var fullPath = Path.Combine(context.RepositoryRoot, file.RelativePath);
            if (context.FileSystem.FileExists(fullPath))
            {
                var content = context.FileSystem.ReadAllText(fullPath);
                if (HasStandaloneFrontmatter(content))
                    continue;
            }

            diagnostics.Add(new Diagnostic(
                RuleId,
                DefaultSeverity,
                Category,
                relPath,
                null,
                $"File '{relPath}' is not referenced by any start_here entry, artifact, or internal link.",
                "Link to this file from an index or navigation document, or add 'standalone: true' to its frontmatter.",
                null));
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }

    private static bool HasStandaloneFrontmatter(string content)
    {
        if (!content.StartsWith("---"))
            return false;

        var endIdx = content.IndexOf("---", 3, StringComparison.Ordinal);
        if (endIdx < 0)
            return false;

        var yaml = content[3..endIdx];
        foreach (var line in yaml.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("standalone:", StringComparison.OrdinalIgnoreCase))
            {
                var value = trimmed["standalone:".Length..].Trim();
                return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
            }
        }

        return false;
    }
}
