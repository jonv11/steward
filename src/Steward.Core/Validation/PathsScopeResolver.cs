using Steward.Core.Discovery;

namespace Steward.Core.Validation;

/// <summary>
/// Returns only files matching explicitly provided paths.
/// </summary>
public sealed class PathsScopeResolver : IScopeResolver
{
    private readonly IReadOnlyList<string> _paths;

    public PathsScopeResolver(IReadOnlyList<string> paths)
    {
        _paths = paths;
    }

    public IReadOnlyList<DiscoveredFile> Resolve(IReadOnlyList<DiscoveredFile> allFiles, string repositoryRoot)
    {
        var pathSet = new HashSet<string>(_paths.Select(PathHelper.NormalizeSeparators), StringComparer.OrdinalIgnoreCase);
        return allFiles.Where(f => pathSet.Contains(f.RelativePath)).ToList();
    }
}
