using DotNet.Globbing;

namespace Steward.Core.Maintenance;

/// <summary>
/// Centralizes how maintenance <c>source</c> values match repository-relative paths.
/// A source can be an exact path, a directory/prefix, or a glob.
/// </summary>
public static class MaintenanceSourceMatcher
{
    public static bool Matches(string? source, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(relativePath))
            return false;

        var normalizedSource = PathHelper.NormalizeSeparators(source);
        var normalizedPath = PathHelper.NormalizeSeparators(relativePath);

        if (LooksLikeGlob(normalizedSource))
            return Glob.Parse(normalizedSource).IsMatch(normalizedPath);

        var normalizedPrefix = PathHelper.NormalizeAndTrim(normalizedSource);
        if (IsDirectoryLikeSource(normalizedSource, normalizedPrefix))
        {
            return normalizedPath.Equals(normalizedPrefix, StringComparison.OrdinalIgnoreCase) ||
                   normalizedPath.StartsWith(normalizedPrefix + "/", StringComparison.OrdinalIgnoreCase);
        }

        return normalizedPath.Equals(normalizedPrefix, StringComparison.OrdinalIgnoreCase);
    }

    public static bool MatchesAny(string? source, IEnumerable<string> relativePaths)
    {
        return !string.IsNullOrWhiteSpace(source) &&
               relativePaths.Any(path => Matches(source, path));
    }

    private static bool LooksLikeGlob(string source)
        => source.IndexOfAny(['*', '?', '[']) >= 0;

    private static bool IsDirectoryLikeSource(string rawSource, string normalizedSource)
    {
        return rawSource.EndsWith("/", StringComparison.Ordinal) ||
               !Path.HasExtension(normalizedSource);
    }
}
