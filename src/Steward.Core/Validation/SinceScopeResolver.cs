using Steward.Core.Discovery;

namespace Steward.Core.Validation;

/// <summary>
/// Returns only files changed since the merge-base of a given ref and HEAD.
/// </summary>
public sealed class SinceScopeResolver : IScopeResolver
{
    private readonly string _sinceRef;

    public SinceScopeResolver(string sinceRef) => _sinceRef = sinceRef;

    public IReadOnlyList<DiscoveredFile> Resolve(IReadOnlyList<DiscoveredFile> allFiles, string repositoryRoot)
    {
        var changedPaths = GitDiffHelper.GetChangedFilesSince(repositoryRoot, _sinceRef);
        if (changedPaths == null)
            return allFiles; // Fallback to full if git is unavailable.

        var changedSet = new HashSet<string>(changedPaths, StringComparer.OrdinalIgnoreCase);
        return allFiles.Where(f => changedSet.Contains(f.RelativePath)).ToList();
    }
}
