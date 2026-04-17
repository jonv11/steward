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
}
