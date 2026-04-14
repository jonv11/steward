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

/// <summary>
/// Returns all discovered files (default scope).
/// </summary>
public sealed class FullScopeResolver : IScopeResolver
{
    public IReadOnlyList<DiscoveredFile> Resolve(IReadOnlyList<DiscoveredFile> allFiles, string repositoryRoot)
        => allFiles;
}

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
        var pathSet = new HashSet<string>(_paths.Select(p => p.Replace('\\', '/')), StringComparer.OrdinalIgnoreCase);
        return allFiles.Where(f => pathSet.Contains(f.RelativePath)).ToList();
    }
}

/// <summary>
/// Helper to invoke git commands for change detection.
/// </summary>
internal static class GitDiffHelper
{
    /// <summary>
    /// Returns files changed relative to HEAD (unstaged + staged), or null if git is unavailable.
    /// </summary>
    public static IReadOnlyList<string>? GetChangedFiles(string repositoryRoot)
    {
        return RunGitDiff(repositoryRoot, "diff --name-only HEAD");
    }

    /// <summary>
    /// Returns files in the staging area, or null if git is unavailable.
    /// </summary>
    public static IReadOnlyList<string>? GetStagedFiles(string repositoryRoot)
    {
        return RunGitDiff(repositoryRoot, "diff --cached --name-only");
    }

    private static IReadOnlyList<string>? RunGitDiff(string workingDirectory, string arguments)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("git", arguments)
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(psi);
            if (process == null) return null;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(10_000);

            if (process.ExitCode != 0)
                return null;

            return output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim().Replace('\\', '/'))
                .Where(p => p.Length > 0)
                .ToList();
        }
        catch
        {
            return null; // git not available
        }
    }
}
