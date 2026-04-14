using Steward.Core.Abstractions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Markdown;

namespace Steward.Core.Search;

public sealed class SearchEngine
{
    private readonly IFileSystem _fileSystem;
    private readonly string _repositoryRoot;

    public SearchEngine(IFileSystem fileSystem, string repositoryRoot)
    {
        _fileSystem = fileSystem;
        _repositoryRoot = repositoryRoot;
    }

    public SearchResult Search(
        string query,
        IReadOnlyList<DiscoveredFile> files,
        SearchMode mode = SearchMode.All,
        string? scopeRole = null,
        RepositoryPolicy? policy = null,
        int maxResults = 100)
    {
        var filteredFiles = FilterByScope(files, scopeRole, policy);
        var matches = new List<SearchMatch>();
        var totalMatches = 0;

        foreach (var file in filteredFiles.Where(f => !f.IsDirectory))
        {
            if (matches.Count >= maxResults) break;

            try
            {
                var fullPath = Path.Combine(_repositoryRoot, file.RelativePath);
                if (!_fileSystem.FileExists(fullPath)) continue;

                var lines = _fileSystem.ReadAllLines(fullPath);
                var isMd = file.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase);

                if (mode is SearchMode.All or SearchMode.Content)
                {
                    SearchContent(query, file.RelativePath, lines, isMd, matches, ref totalMatches, maxResults);
                }

                if (isMd && mode is SearchMode.All or SearchMode.Headings)
                {
                    SearchHeadings(query, file.RelativePath, lines, matches, ref totalMatches, maxResults);
                }
            }
            catch
            {
                // Skip unreadable files
            }
        }

        return new SearchResult
        {
            Query = query,
            Mode = mode,
            Matches = matches,
            TotalMatches = totalMatches,
            Truncated = totalMatches > maxResults
        };
    }

    private static void SearchContent(
        string query, string path, string[] lines, bool isMd,
        List<SearchMatch> matches, ref int total, int max)
    {
        // Precompute heading context for Markdown files
        string? currentHeading = null;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            if (isMd && line.StartsWith('#'))
            {
                currentHeading = line.TrimStart('#').Trim();
            }

            var col = line.IndexOf(query, StringComparison.OrdinalIgnoreCase);
            if (col >= 0)
            {
                total++;
                if (matches.Count < max)
                {
                    matches.Add(new SearchMatch
                    {
                        Path = path,
                        Line = i + 1, // 1-based
                        Column = col + 1,
                        Snippet = line.Trim(),
                        Kind = SearchMatchKind.Content,
                        HeadingContext = isMd ? currentHeading : null
                    });
                }
            }
        }
    }

    private static void SearchHeadings(
        string query, string path, string[] lines,
        List<SearchMatch> matches, ref int total, int max)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (!line.StartsWith('#')) continue;

            var headingText = line.TrimStart('#').Trim();
            if (headingText.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                total++;
                if (matches.Count < max)
                {
                    matches.Add(new SearchMatch
                    {
                        Path = path,
                        Line = i + 1,
                        Column = 1,
                        Snippet = line.Trim(),
                        Kind = SearchMatchKind.Heading,
                        HeadingContext = headingText
                    });
                }
            }
        }
    }

    private static IReadOnlyList<DiscoveredFile> FilterByScope(
        IReadOnlyList<DiscoveredFile> files,
        string? scopeRole,
        RepositoryPolicy? policy)
    {
        if (string.IsNullOrEmpty(scopeRole) || policy?.Artifacts == null)
            return files;

        var rolePaths = new HashSet<string>(
            policy.Artifacts
                .Where(a => string.Equals(a.Role, scopeRole, StringComparison.OrdinalIgnoreCase) && a.Path != null)
                .Select(a => a.Path!),
            StringComparer.OrdinalIgnoreCase);

        if (rolePaths.Count == 0) return files;

        return files.Where(f => rolePaths.Contains(f.RelativePath)).ToList();
    }
}
