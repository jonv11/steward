using System.Collections.Concurrent;
using Steward.Core.Abstractions;

namespace Steward.Core.Markdown;

/// <summary>
/// Thread-safe cache that avoids redundant MarkdownParser.Parse calls
/// when multiple rules or maintainers process the same file.
/// </summary>
public sealed class DocumentCache
{
    private readonly ConcurrentDictionary<string, StructuredDocument> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly IFileSystem _fileSystem;
    private readonly string _repositoryRoot;

    public DocumentCache(IFileSystem fileSystem, string repositoryRoot)
    {
        _fileSystem = fileSystem;
        _repositoryRoot = repositoryRoot;
    }

    public StructuredDocument GetOrParse(string relativePath)
    {
        return _cache.GetOrAdd(relativePath, key =>
        {
            var fullPath = Path.Combine(_repositoryRoot, key);
            var content = _fileSystem.ReadAllText(fullPath);
            return MarkdownParser.Parse(key, content);
        });
    }
}
