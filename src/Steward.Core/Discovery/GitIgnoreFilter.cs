using Steward.Core;
using Steward.Core.Abstractions;

namespace Steward.Core.Discovery;

public sealed class GitIgnoreFilter : IIgnoreFilter
{
    private readonly List<IgnoreRuleSet> _ruleSets;

    private GitIgnoreFilter(List<IgnoreRuleSet> ruleSets)
    {
        _ruleSets = ruleSets;
    }

    public static GitIgnoreFilter Load(string repositoryRoot, IFileSystem fileSystem, IReadOnlyList<string>? additionalExcludes = null)
    {
        var ruleSets = new List<IgnoreRuleSet>();
        LoadRecursive(repositoryRoot, repositoryRoot, fileSystem, ruleSets);

        if (additionalExcludes is { Count: > 0 })
        {
            var rules = additionalExcludes.Select(p => IgnoreRule.Parse(p)).Where(r => r != null).ToList();
            if (rules.Count > 0)
                ruleSets.Add(new IgnoreRuleSet("", rules!));
        }

        return new GitIgnoreFilter(ruleSets);
    }

    public bool IsIgnored(string relativePath, bool isDirectory)
    {
        var normalized = PathHelper.NormalizeSeparators(relativePath).TrimStart('/');
        if (normalized.Length == 0) return false;

        // Always ignore .git
        if (normalized == ".git" || normalized.StartsWith(".git/"))
            return true;

        bool? result = null;

        foreach (var ruleSet in _ruleSets)
        {
            var pathForRuleSet = GetRelativeToRuleSet(normalized, ruleSet.BasePath);
            if (pathForRuleSet == null) continue;

            foreach (var rule in ruleSet.Rules)
            {
                if (rule.DirectoryOnly && !isDirectory) continue;

                if (rule.Matches(pathForRuleSet, normalized, isDirectory))
                {
                    result = !rule.Negated;
                }
            }
        }

        return result ?? false;
    }

    private static string? GetRelativeToRuleSet(string normalizedPath, string ruleSetBasePath)
    {
        if (ruleSetBasePath.Length == 0) return normalizedPath;
        if (!normalizedPath.StartsWith(ruleSetBasePath + "/", StringComparison.Ordinal))
            return null;
        return normalizedPath[(ruleSetBasePath.Length + 1)..];
    }

    private static void LoadRecursive(string currentDir, string repositoryRoot, IFileSystem fileSystem, List<IgnoreRuleSet> ruleSets)
    {
        var gitignorePath = Path.Combine(currentDir, ".gitignore");
        if (fileSystem.FileExists(gitignorePath))
        {
            try
            {
                var content = fileSystem.ReadAllText(gitignorePath);
                var basePath = GetRelativePath(repositoryRoot, currentDir);
                var rules = ParseGitignore(content);
                if (rules.Count > 0)
                    ruleSets.Add(new IgnoreRuleSet(basePath, rules));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Skip unreadable .gitignore files so discovery can continue.
            }
        }

        if (!fileSystem.DirectoryExists(currentDir)) return;

        foreach (var subDir in EnumerateDirectoriesSafe(currentDir, fileSystem))
        {
            var dirName = Path.GetFileName(subDir);
            if (dirName is ".git" or ".hg" or ".svn") continue;
            LoadRecursive(subDir, repositoryRoot, fileSystem, ruleSets);
        }
    }

    private static string GetRelativePath(string root, string dir)
    {
        if (root == dir) return "";
        var rel = PathHelper.NormalizeSeparators(Path.GetRelativePath(root, dir));
        return rel == "." ? "" : rel;
    }

    private static IReadOnlyList<string> EnumerateDirectoriesSafe(string path, IFileSystem fileSystem)
    {
        try
        {
            return fileSystem.GetDirectories(path).ToArray();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static List<IgnoreRule> ParseGitignore(string content)
    {
        var rules = new List<IgnoreRule>();
        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r').TrimEnd();
            if (line.Length == 0 || line[0] == '#') continue;

            var rule = IgnoreRule.Parse(line);
            if (rule != null) rules.Add(rule);
        }
        return rules;
    }

    private sealed record IgnoreRuleSet(string BasePath, List<IgnoreRule> Rules);
}

internal sealed class IgnoreRule
{
    public string Pattern { get; }
    public bool Negated { get; }
    public bool DirectoryOnly { get; }
    public bool Anchored { get; }

    private IgnoreRule(string pattern, bool negated, bool directoryOnly, bool anchored)
    {
        Pattern = pattern;
        Negated = negated;
        DirectoryOnly = directoryOnly;
        Anchored = anchored;
    }

    public static IgnoreRule? Parse(string line)
    {
        var trimmed = line;
        var negated = false;

        if (trimmed.StartsWith('!'))
        {
            negated = true;
            trimmed = trimmed[1..];
        }

        // Remove trailing spaces (unless escaped)
        while (trimmed.EndsWith(' ') && !trimmed.EndsWith("\\ "))
            trimmed = trimmed[..^1];

        if (trimmed.Length == 0) return null;

        var directoryOnly = trimmed.EndsWith('/');
        if (directoryOnly) trimmed = trimmed[..^1];

        if (trimmed.Length == 0) return null;

        // A pattern is "anchored" if it contains a slash (except trailing, already removed)
        var anchored = trimmed.Contains('/');

        // Remove leading slash if present (it's just an anchor indicator)
        if (trimmed.StartsWith('/'))
            trimmed = trimmed[1..];

        return new IgnoreRule(trimmed, negated, directoryOnly, anchored);
    }

    public bool Matches(string relativeToRuleSet, string fullRelativePath, bool isDirectory)
    {
        // If pattern contains ** or /, use full matching
        if (Anchored || Pattern.Contains("**"))
        {
            return GlobMatch(Pattern, fullRelativePath) || GlobMatch(Pattern, relativeToRuleSet);
        }
        else
        {
            // Unanchored: match against the filename or any path segment
            var fileName = Path.GetFileName(fullRelativePath);
            if (GlobMatch(Pattern, fileName)) return true;

            // Also try matching against path segments
            return GlobMatch(Pattern, fullRelativePath) || GlobMatch(Pattern, relativeToRuleSet);
        }
    }

    internal static bool GlobMatch(string pattern, string text)
    {
        return GlobMatchInternal(pattern.AsSpan(), text.AsSpan());
    }

    private static bool GlobMatchInternal(ReadOnlySpan<char> pattern, ReadOnlySpan<char> text)
    {
        int pi = 0, ti = 0;
        int starPi = -1, starTi = -1;

        while (ti < text.Length)
        {
            if (pi < pattern.Length && pattern[pi] == '*' && pi + 1 < pattern.Length && pattern[pi + 1] == '*')
            {
                // ** matches zero or more path segments
                // Skip the **
                pi += 2;
                if (pi < pattern.Length && pattern[pi] == '/')
                    pi++; // Skip the trailing /

                // Try matching the rest of the pattern against every possible suffix
                for (int i = ti; i <= text.Length; i++)
                {
                    if (GlobMatchInternal(pattern[pi..], text[i..]))
                        return true;
                }
                return false;
            }
            else if (pi < pattern.Length && pattern[pi] == '*')
            {
                // * matches anything except /
                starPi = pi;
                starTi = ti;
                pi++;
            }
            else if (pi < pattern.Length && (pattern[pi] == text[ti] || pattern[pi] == '?'))
            {
                pi++;
                ti++;
            }
            else if (starPi >= 0)
            {
                pi = starPi + 1;
                starTi++;
                ti = starTi;
                // * should not match /
                if (ti > 0 && text[ti - 1] == '/')
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        // Consume remaining * or ** in pattern
        while (pi < pattern.Length && pattern[pi] == '*')
            pi++;

        return pi == pattern.Length;
    }
}
