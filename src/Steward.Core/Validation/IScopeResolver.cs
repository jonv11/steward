using Steward.Core.Discovery;

namespace Steward.Core.Validation;

/// <summary>
/// Resolves the set of files to validate based on the requested scope.
/// </summary>
public interface IScopeResolver
{
    /// <summary>
    /// Returns the effective file list for validation.
    /// </summary>
    IReadOnlyList<DiscoveredFile> Resolve(IReadOnlyList<DiscoveredFile> allFiles, string repositoryRoot);
}
