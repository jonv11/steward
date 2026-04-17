using Steward.Core;
using Steward.Core.Abstractions;
using Steward.Core.Discovery;
using Steward.Core.Validation.Rules;

namespace Steward.Core.Maintenance;

/// <summary>
/// Computes the set of file edits required to safely move/rename a file
/// while updating all internal Markdown references.
/// </summary>
public static class MoveEngine
{
    public sealed record MoveEdit(string FilePath, string OldContent, string NewContent);

    public sealed record MovePlan(
        string OldPath,
        string NewPath,
        IReadOnlyList<MoveEdit> Edits);

    public static MovePlan ComputeMove(
        string oldPath,
        string newPath,
        IReadOnlyList<DiscoveredFile> files,
        IFileSystem fileSystem,
        string repositoryRoot)
    {
        oldPath = PathHelper.NormalizeSeparators(oldPath);
        newPath = PathHelper.NormalizeSeparators(newPath);

        var edits = new List<MoveEdit>();

        // For each Markdown file, check if it links to oldPath and rewrite
        foreach (var file in files)
        {
            if (!file.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                continue;

            var relPath = PathHelper.NormalizeSeparators(file.RelativePath);
            var fullPath = Path.Combine(repositoryRoot, file.RelativePath);
            if (!fileSystem.FileExists(fullPath))
                continue;

            var content = fileSystem.ReadAllText(fullPath);
            var links = BrokenInternalLinkRule.ExtractInternalLinks(content);

            var newContent = content;
            // Process links in reverse order to preserve positions
            var rewriteTargets = new List<(string originalTarget, string newTarget)>();

            foreach (var (target, _) in links)
            {
                var resolved = BrokenInternalLinkRule.ResolveLinkTarget(relPath, target);
                if (resolved != null && string.Equals(resolved, oldPath, StringComparison.OrdinalIgnoreCase))
                {
                    // Compute new relative path from this file to the new location
                    var newRelative = ComputeRelativePath(relPath, newPath);
                    rewriteTargets.Add((target, newRelative));
                }
            }

            if (rewriteTargets.Count > 0)
            {
                foreach (var (originalTarget, newTarget) in rewriteTargets)
                {
                    newContent = newContent.Replace($"]({originalTarget})", $"]({newTarget})");
                    // Also handle links with fragments
                    newContent = ReplaceWithFragment(newContent, originalTarget, newTarget);
                }

                if (newContent != content)
                {
                    edits.Add(new MoveEdit(relPath, content, newContent));
                }
            }
        }

        return new MovePlan(oldPath, newPath, edits);
    }

    internal static string ComputeRelativePath(string fromFile, string toFile)
    {
        var fromDir = PathHelper.NormalizeSeparators(Path.GetDirectoryName(PathHelper.NormalizeSeparators(fromFile)) ?? "");
        var toNormalized = PathHelper.NormalizeSeparators(toFile);

        if (string.IsNullOrEmpty(fromDir))
            return toNormalized;

        var fromParts = fromDir.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var toParts = toNormalized.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // Find common prefix
        int common = 0;
        while (common < fromParts.Length && common < toParts.Length &&
               string.Equals(fromParts[common], toParts[common], StringComparison.OrdinalIgnoreCase))
        {
            common++;
        }

        var parts = new List<string>();
        for (int i = common; i < fromParts.Length; i++)
            parts.Add("..");
        for (int i = common; i < toParts.Length; i++)
            parts.Add(toParts[i]);

        return string.Join('/', parts);
    }

    private static string ReplaceWithFragment(string content, string oldTarget, string newTarget)
    {
        // Replace references like ](oldTarget#fragment) with ](newTarget#fragment)
        var searchPrefix = $"]({oldTarget}#";
        var idx = content.IndexOf(searchPrefix, StringComparison.Ordinal);
        while (idx >= 0)
        {
            content = content.Remove(idx + 2, oldTarget.Length).Insert(idx + 2, newTarget);
            idx = content.IndexOf(searchPrefix, idx + newTarget.Length, StringComparison.Ordinal);
        }
        return content;
    }
}
