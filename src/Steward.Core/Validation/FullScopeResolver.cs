using Steward.Core.Discovery;

namespace Steward.Core.Validation;

/// <summary>
/// Returns all discovered files (default scope).
/// </summary>
public sealed class FullScopeResolver : IScopeResolver
{
    public IReadOnlyList<DiscoveredFile> Resolve(IReadOnlyList<DiscoveredFile> allFiles, string repositoryRoot)
        => allFiles;
}
