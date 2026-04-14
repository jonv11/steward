using Steward.Core.Abstractions;

namespace Steward.TestFixtures;

public sealed class InMemoryFileSystem : IFileSystem
{
    private readonly Dictionary<string, string> _files = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _directories = new(StringComparer.OrdinalIgnoreCase);

    public InMemoryFileSystem AddFile(string path, string content = "")
    {
        var normalized = NormalizePath(path);
        _files[normalized] = content;
        EnsureParentDirectories(normalized);
        return this;
    }

    public InMemoryFileSystem AddDirectory(string path)
    {
        var normalized = NormalizePath(path);
        _directories.Add(normalized);
        EnsureParentDirectories(normalized);
        return this;
    }

    public bool FileExists(string path) => _files.ContainsKey(NormalizePath(path));

    public bool DirectoryExists(string path) => _directories.Contains(NormalizePath(path));

    public string ReadAllText(string path)
    {
        var normalized = NormalizePath(path);
        if (!_files.TryGetValue(normalized, out var content))
            throw new FileNotFoundException($"File not found: {path}", path);
        return content;
    }

    public string[] ReadAllLines(string path) => ReadAllText(path).Split('\n');

    public IEnumerable<string> GetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        var normalized = NormalizePath(path).TrimEnd('/') + "/";

        foreach (var file in _files.Keys)
        {
            if (!file.StartsWith(normalized, StringComparison.OrdinalIgnoreCase))
                continue;

            var remainder = file[normalized.Length..];

            if (searchOption == SearchOption.TopDirectoryOnly && remainder.Contains('/'))
                continue;

            if (searchPattern == "*" || MatchesSimplePattern(Path.GetFileName(file), searchPattern))
                yield return file;
        }
    }

    public IEnumerable<string> GetDirectories(string path)
    {
        var normalized = NormalizePath(path).TrimEnd('/') + "/";

        foreach (var dir in _directories)
        {
            if (!dir.StartsWith(normalized, StringComparison.OrdinalIgnoreCase))
                continue;

            var remainder = dir[normalized.Length..];
            if (remainder.Length > 0 && !remainder.Contains('/'))
                yield return dir;
        }
    }

    public long GetFileSize(string path)
    {
        var content = ReadAllText(path);
        return System.Text.Encoding.UTF8.GetByteCount(content);
    }

    public Stream OpenRead(string path)
    {
        var content = ReadAllText(path);
        return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
    }

    public DateTime GetLastWriteTimeUtc(string path)
    {
        if (!FileExists(path))
            throw new FileNotFoundException($"File not found: {path}", path);
        return DateTime.UtcNow;
    }

    private static string NormalizePath(string path)
        => path.Replace('\\', '/').TrimEnd('/');

    private void EnsureParentDirectories(string normalizedPath)
    {
        var parts = normalizedPath.Split('/');
        for (var i = 1; i < parts.Length; i++)
        {
            var dir = string.Join('/', parts[..i]);
            if (dir.Length > 0)
                _directories.Add(dir);
        }
    }

    private static bool MatchesSimplePattern(string name, string pattern)
    {
        if (pattern == "*") return true;
        if (pattern.StartsWith("*."))
            return name.EndsWith(pattern[1..], StringComparison.OrdinalIgnoreCase);
        return name.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }
}
