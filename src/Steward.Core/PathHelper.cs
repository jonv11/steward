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
}
