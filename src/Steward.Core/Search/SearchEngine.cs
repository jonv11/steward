using System.Text.RegularExpressions;
using DotNet.Globbing;
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
        int maxResults = 100,
        bool useRegex = false)
    {
        var filteredFiles = FilterByScope(files, scopeRole, policy);
        var matches = new List<SearchMatch>();
        var totalMatches = 0;

        Regex? regex = null;
        if (useRegex)
        {
            try
            {
                regex = new Regex(query, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(5));
            }
            catch (RegexParseException)
            {
                return new SearchResult
                {
                    Query = query,
                    Mode = mode,
                    Matches = [],
                    TotalMatches = 0,
                    Truncated = false,
                    Error = $"Invalid regex pattern: '{query}'"
                };
            }
        }

        foreach (var file in filteredFiles.Where(f => !f.IsDirectory))
        {
            if (matches.Count >= maxResults) break;

            try
            {
                var fullPath = Path.Combine(_repositoryRoot, file.RelativePath);
                if (!_fileSystem.FileExists(fullPath)) continue;

                var content = _fileSystem.ReadAllText(fullPath);
                var lines = content.Split('\n');
                var isMd = file.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
                var document = isMd ? MarkdownParser.Parse(file.RelativePath, content) : null;

                if (mode is SearchMode.All or SearchMode.Content)
                {
                    SearchContent(query, file.RelativePath, lines, document, matches, ref totalMatches, maxResults, regex);
                }

                if (isMd && mode is SearchMode.All or SearchMode.Headings)
                {
                    SearchHeadings(query, file.RelativePath, lines, document!, matches, ref totalMatches, maxResults, regex);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
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
        string query, string path, string[] lines, StructuredDocument? document,
        List<SearchMatch> matches, ref int total, int max, Regex? regex)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            int col;
            if (regex != null)
            {
                var match = regex.Match(line);
                col = match.Success ? match.Index : -1;
            }
            else
            {
                col = line.IndexOf(query, StringComparison.OrdinalIgnoreCase);
            }

            if (col >= 0)
            {
                total++;
                if (matches.Count < max)
                {
                    var lineNumber = i + 1;
                    var sectionContext = document != null
                        ? ResolveSectionContext(document, lineNumber)
                        : null;

                    matches.Add(new SearchMatch
                    {
                        Path = path,
                        Line = lineNumber,
                        Column = col + 1,
                        Snippet = line.Trim(),
                        Kind = SearchMatchKind.Content,
                        HeadingContext = sectionContext?.SectionHeading,
                        SectionHeading = sectionContext?.SectionHeading,
                        SectionRange = sectionContext?.SectionRange,
                        MdQuerySelector = sectionContext?.MdQuerySelector
                    });
                }
            }
        }
    }

    private static void SearchHeadings(
        string query, string path, string[] lines, StructuredDocument document,
        List<SearchMatch> matches, ref int total, int max, Regex? regex)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (!line.StartsWith('#')) continue;

            var headingText = line.TrimStart('#').Trim();
            var isMatch = regex != null
                ? regex.IsMatch(headingText)
                : headingText.Contains(query, StringComparison.OrdinalIgnoreCase);
            if (isMatch)
            {
                total++;
                if (matches.Count < max)
                {
                    var lineNumber = i + 1;
                    var sectionContext = ResolveSectionContext(document, lineNumber);

                    matches.Add(new SearchMatch
                    {
                        Path = path,
                        Line = lineNumber,
                        Column = 1,
                        Snippet = line.Trim(),
                        Kind = SearchMatchKind.Heading,
                        HeadingContext = headingText,
                        SectionHeading = sectionContext?.SectionHeading,
                        SectionRange = sectionContext?.SectionRange,
                        MdQuerySelector = sectionContext?.MdQuerySelector
                    });
                }
            }
        }
    }

    private static SearchSectionContext? ResolveSectionContext(StructuredDocument document, int lineNumber)
    {
        if (!MarkdownHeadings.TryFindSectionAtLine(document.Sections, lineNumber, out var section, out var headingPath) ||
            section == null)
        {
            return null;
        }

        return new SearchSectionContext
        {
            SectionHeading = section.Heading,
            SectionRange = section.Range,
            MdQuerySelector = MarkdownHeadings.TryCreateSafeSelector(document, headingPath, section)
        };
    }

    private static IReadOnlyList<DiscoveredFile> FilterByScope(
        IReadOnlyList<DiscoveredFile> files,
        string? scopeRole,
        RepositoryPolicy? policy)
    {
        if (string.IsNullOrEmpty(scopeRole) || policy?.Artifacts == null)
            return files;

        var roleGlobs = policy.Artifacts
            .Where(a => string.Equals(a.Role, scopeRole, StringComparison.OrdinalIgnoreCase) && a.Path != null)
            .Select(a => Glob.Parse(a.Path!))
            .ToList();

        if (roleGlobs.Count == 0) return files;

        return files.Where(f => roleGlobs.Any(g => g.IsMatch(f.RelativePath))).ToList();
    }
}

internal sealed class SearchSectionContext
{
    public required string SectionHeading { get; init; }
    public required LineRange SectionRange { get; init; }
    public string? MdQuerySelector { get; init; }
}
