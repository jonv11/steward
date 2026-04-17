using Steward.Core.Discovery;

namespace Steward.Core.Validation;

/// <summary>
/// Returns only files changed relative to the merge base (uses git diff).
/// </summary>
public sealed class ChangedScopeResolver : IScopeResolver
{
    public IReadOnlyList<DiscoveredFile> Resolve(IReadOnlyList<DiscoveredFile> allFiles, string repositoryRoot)
    {
        var changedPaths = GitDiffHelper.GetChangedFiles(repositoryRoot);
        if (changedPaths == null)
            return allFiles; // Fallback to full if git is unavailable.

        var changedSet = new HashSet<string>(changedPaths, StringComparer.OrdinalIgnoreCase);
        return allFiles.Where(f => changedSet.Contains(f.RelativePath)).ToList();
    }
}
