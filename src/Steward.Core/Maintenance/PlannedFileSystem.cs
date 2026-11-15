using System.IO.Enumeration;
using System.Text;
using Steward.Core.Abstractions;

namespace Steward.Core.Maintenance;

/// <summary>A read-through snapshot of earlier planned writes; evaluation never mutates disk.</summary>
internal sealed class PlannedFileSystem(IFileSystem source) : IFileSystem
{
    private readonly Dictionary<string, string> _content = new(StringComparer.OrdinalIgnoreCase);
    private static string Key(string path) => Path.GetFullPath(path);

    public void SetContent(string path, string content) => _content[Key(path)] = content;
    public bool FileExists(string path) => _content.ContainsKey(Key(path)) || source.FileExists(path);
    public bool DirectoryExists(string path) => source.DirectoryExists(path) ||
        _content.Keys.Any(p => p.StartsWith(Key(path).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase));
    public string ReadAllText(string path) => _content.TryGetValue(Key(path), out var content) ? content : source.ReadAllText(path);
    public string[] ReadAllLines(string path) => _content.TryGetValue(Key(path), out var content)
        ? content.Split('\n').Select(l => l.TrimEnd('\r')).ToArray() : source.ReadAllLines(path);
    public long GetFileSize(string path) => _content.TryGetValue(Key(path), out var content)
        ? Encoding.UTF8.GetByteCount(content) : source.GetFileSize(path);
    public Stream OpenRead(string path) => _content.TryGetValue(Key(path), out var content)
        ? new MemoryStream(Encoding.UTF8.GetBytes(content)) : source.OpenRead(path);
    public DateTime GetLastWriteTimeUtc(string path) => source.FileExists(path)
        ? source.GetLastWriteTimeUtc(path) : DateTime.UnixEpoch;

    public IEnumerable<string> GetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        var prefix = Key(path).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var planned = _content.Keys.Where(p => p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Where(p => searchOption == SearchOption.AllDirectories || Path.GetDirectoryName(p) == Key(path))
            .Where(p => FileSystemName.MatchesSimpleExpression(searchPattern, Path.GetFileName(p), ignoreCase: true));
        return (source.DirectoryExists(path) ? source.GetFiles(path, searchPattern, searchOption) : [])
            .Concat(planned).Distinct(StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<string> GetDirectories(string path)
    {
        var prefix = Key(path).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var planned = _content.Keys.Where(p => p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(p => p[prefix.Length..].Split(Path.DirectorySeparatorChar))
            .Where(parts => parts.Length > 1).Select(parts => Path.Combine(path, parts[0]));
        return (source.DirectoryExists(path) ? source.GetDirectories(path) : [])
            .Concat(planned).Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
