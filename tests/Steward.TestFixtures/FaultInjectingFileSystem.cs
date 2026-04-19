using Steward.Core.Abstractions;

namespace Steward.TestFixtures;

public sealed class FaultInjectingFileSystem : IFileSystem
{
    private readonly InMemoryFileSystem _inner = new();
    private readonly HashSet<string> _traversalDeniedPaths = new(StringComparer.OrdinalIgnoreCase);

    public FaultInjectingFileSystem AddFile(string path, string content = "")
    {
        _inner.AddFile(path, content);
        return this;
    }

    public FaultInjectingFileSystem AddDirectory(string path)
    {
        _inner.AddDirectory(path);
        return this;
    }

    public FaultInjectingFileSystem DenyTraversal(string path)
    {
        _traversalDeniedPaths.Add(NormalizePath(path));
        return this;
    }

    public bool FileExists(string path) => _inner.FileExists(path);

    public bool DirectoryExists(string path) => _inner.DirectoryExists(path);

    public string ReadAllText(string path) => _inner.ReadAllText(path);

    public string[] ReadAllLines(string path) => _inner.ReadAllLines(path);

    public IEnumerable<string> GetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        ThrowIfTraversalDenied(path);
        return _inner.GetFiles(path, searchPattern, searchOption);
    }

    public IEnumerable<string> GetDirectories(string path)
    {
        ThrowIfTraversalDenied(path);
        return _inner.GetDirectories(path);
    }

    public long GetFileSize(string path) => _inner.GetFileSize(path);

    public Stream OpenRead(string path) => _inner.OpenRead(path);

    public DateTime GetLastWriteTimeUtc(string path) => _inner.GetLastWriteTimeUtc(path);

    private void ThrowIfTraversalDenied(string path)
    {
        if (_traversalDeniedPaths.Contains(NormalizePath(path)))
            throw new UnauthorizedAccessException($"Access to '{path}' is denied.");
    }

    private static string NormalizePath(string path)
        => path.Replace('\\', '/').TrimEnd('/');
}
