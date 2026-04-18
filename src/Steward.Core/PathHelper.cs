namespace Steward.Core;

public static class PathHelper
{
    /// <summary>
    /// Normalizes path separators to forward slashes for consistent cross-platform comparison.
    /// </summary>
    public static string NormalizeSeparators(string path) => path.Replace('\\', '/');

    /// <summary>
    /// Normalizes separators and strips a trailing slash.
    /// </summary>
    public static string NormalizeAndTrim(string path) => path.Replace('\\', '/').TrimEnd('/');

    /// <summary>
    /// Builds a Markdown-friendly relative path from one repository-relative file to another.
    /// </summary>
    public static string GetRelativeMarkdownPath(string fromFile, string toPath)
    {
        var fromDirectory = Path.GetDirectoryName(NormalizeSeparators(fromFile));
        if (string.IsNullOrWhiteSpace(fromDirectory))
            fromDirectory = ".";

        var relative = Path.GetRelativePath(fromDirectory, NormalizeSeparators(toPath));
        return NormalizeSeparators(relative);
    }
}
