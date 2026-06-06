namespace Steward.Core.Validation;

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

    /// <summary>
    /// Returns the changed-files result for <paramref name="sinceRef"/> vs HEAD.
    /// <see cref="SinceResult.Paths"/> is non-null on success.
    /// <see cref="SinceResult.GitError"/> is non-null when git ran but rejected the ref
    /// (invalid branch, tag, or commit SHA) — callers should treat this as a usage error.
    /// Both fields are null when git is unavailable or timed out.
    /// </summary>
    public static SinceResult GetChangedFilesSince(string repositoryRoot, string sinceRef)
    {
        return RunGitDiffSince(repositoryRoot, $"diff --name-only {sinceRef}...HEAD");
    }

    private static IReadOnlyList<string>? RunGitDiff(string workingDirectory, string arguments)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("git", arguments)
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(psi);
            if (process == null) return null;

            process.StandardInput.Close();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(10_000))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Best effort only.
                }

                return null;
            }

            System.Threading.Tasks.Task.WaitAll([outputTask, errorTask], 10_000);

            if (process.ExitCode != 0)
                return null;

            return outputTask.Result
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => PathHelper.NormalizeSeparators(p.Trim()))
                .Where(p => p.Length > 0)
                .ToList();
        }
        catch
        {
            return null; // git not available
        }
    }

    private static SinceResult RunGitDiffSince(string workingDirectory, string arguments)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("git", arguments)
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(psi);
            if (process == null) return new SinceResult(null, null);

            process.StandardInput.Close();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(10_000))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Best effort only.
                }

                return new SinceResult(null, null);
            }

            System.Threading.Tasks.Task.WaitAll([outputTask, errorTask], 10_000);

            if (process.ExitCode != 0)
            {
                // git ran but rejected the ref — surface the error so callers can report it.
                var gitErr = errorTask.Result?.Trim();
                return new SinceResult(null, string.IsNullOrEmpty(gitErr) ? "git exited with an error" : gitErr);
            }

            var paths = outputTask.Result
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => PathHelper.NormalizeSeparators(p.Trim()))
                .Where(p => p.Length > 0)
                .ToList();

            return new SinceResult(paths, null);
        }
        catch
        {
            return new SinceResult(null, null); // git not available
        }
    }
}

/// <summary>
/// Result of <see cref="GitDiffHelper.GetChangedFilesSince"/>.
/// </summary>
internal sealed record SinceResult(
    IReadOnlyList<string>? Paths,
    string? GitError);
