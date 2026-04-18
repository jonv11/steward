using Steward.Core;
using Steward.Core.Abstractions;

namespace Steward.Core.Validation.Rules;

/// <summary>
/// STWD-011: Checks that all .md files in a declared index_of directory
/// are referenced via Markdown link from the index artifact.
/// </summary>
public sealed class IndexCompletenessRule : IValidationRule
{
    public string RuleId => "STWD-011";
    public string Category => "index-completeness";
    public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Warning;
    public string Description => "All Markdown files in an indexed directory should be linked from the index artifact.";

    public Task<IReadOnlyList<Diagnostic>> EvaluateAsync(ValidationContext context)
    {
        var diagnostics = new List<Diagnostic>();

        if (context.Policy?.Artifacts is null)
            return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);

        var indexArtifacts = context.Policy.Artifacts
            .Where(a => !string.IsNullOrWhiteSpace(a.IndexOf) && !string.IsNullOrWhiteSpace(a.Path))
            .ToList();

        if (indexArtifacts.Count == 0)
            return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);

        foreach (var artifact in indexArtifacts)
        {
            var indexPath = PathHelper.NormalizeSeparators(artifact.Path!);
            var sourceDir = PathHelper.NormalizeAndTrim(artifact.IndexOf!) + "/";

            var fullIndexPath = Path.Combine(context.RepositoryRoot, indexPath);
            if (!context.FileSystem.FileExists(fullIndexPath))
                continue;

            var content = context.FileSystem.ReadAllText(fullIndexPath);
            var links = BrokenInternalLinkRule.ExtractInternalLinks(content);

            var linkedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (target, _) in links)
            {
                var resolved = BrokenInternalLinkRule.ResolveLinkTarget(indexPath, target);
                if (resolved != null)
                    linkedPaths.Add(resolved);
            }

            // Use AllDiscoveredFiles so scoped runs (--scope changed/staged) don't
            // miss files outside the scope when checking index completeness.
            var allFiles = context.AllDiscoveredFiles ?? context.TargetFiles;
            var filesInScope = allFiles
                .Where(f => PathHelper.NormalizeSeparators(f.RelativePath).StartsWith(sourceDir, StringComparison.OrdinalIgnoreCase))
                .Where(f => f.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                .Where(f => !PathHelper.NormalizeSeparators(f.RelativePath).Equals(indexPath, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var file in filesInScope)
            {
                var normalizedPath = PathHelper.NormalizeSeparators(file.RelativePath);
                if (!linkedPaths.Contains(normalizedPath))
                {
                    diagnostics.Add(new Diagnostic(
                        RuleId,
                        DefaultSeverity,
                        Category,
                        normalizedPath,
                        null,
                        $"File is not referenced from index '{indexPath}'.",
                        $"Add a link to '{normalizedPath}' in '{indexPath}', or exclude the file from the indexed directory.",
                        null));
                }
            }
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }
}
