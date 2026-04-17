using Steward.Core.Discovery;

namespace Steward.Core.Validation;

/// <summary>
/// Returns only files in the git staging area.
/// </summary>
public sealed class StagedScopeResolver : IScopeResolver
{
    public IReadOnlyList<DiscoveredFile> Resolve(IReadOnlyList<DiscoveredFile> allFiles, string repositoryRoot)
    {
        var stagedPaths = GitDiffHelper.GetStagedFiles(repositoryRoot);
        if (stagedPaths == null)
            return allFiles; // Fallback to full if git is unavailable.

        var stagedSet = new HashSet<string>(stagedPaths, StringComparer.OrdinalIgnoreCase);
        return allFiles.Where(f => stagedSet.Contains(f.RelativePath)).ToList();
    }
}
