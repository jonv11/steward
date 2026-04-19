using Steward.Core;
using Steward.Core.Abstractions;

namespace Steward.Core.Discovery;

public sealed class FileDiscoveryService
{
    private readonly IFileSystem _fileSystem;
    private readonly IIgnoreFilter _ignoreFilter;

    public FileDiscoveryService(IFileSystem fileSystem, IIgnoreFilter ignoreFilter)
    {
        _fileSystem = fileSystem;
        _ignoreFilter = ignoreFilter;
    }

    public List<DiscoveredFile> Discover(string rootPath)
    {
        var results = new List<DiscoveredFile>();
        WalkDirectory(rootPath, rootPath, results);
        results.Sort((a, b) => string.Compare(a.RelativePath, b.RelativePath, StringComparison.OrdinalIgnoreCase));
        return results;
    }

    private void WalkDirectory(string currentPath, string rootPath, List<DiscoveredFile> results)
    {
        // Add subdirectories
        foreach (var dirPath in EnumerateDirectoriesSafe(currentPath))
        {
            var relativePath = GetRelativePath(rootPath, dirPath);

            if (_ignoreFilter.IsIgnored(relativePath, isDirectory: true))
                continue;

            results.Add(new DiscoveredFile(relativePath, 0, IsDirectory: true));
            WalkDirectory(dirPath, rootPath, results);
        }

        // Add files
        foreach (var filePath in EnumerateFilesSafe(currentPath))
        {
            var relativePath = GetRelativePath(rootPath, filePath);

            if (_ignoreFilter.IsIgnored(relativePath, isDirectory: false))
                continue;

            long size;
            try
            {
                size = _fileSystem.GetFileSize(filePath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            results.Add(new DiscoveredFile(relativePath, size, IsDirectory: false));
        }
    }

    private static string GetRelativePath(string root, string fullPath)
    {
        return PathHelper.NormalizeSeparators(Path.GetRelativePath(root, fullPath));
    }

    private IReadOnlyList<string> EnumerateDirectoriesSafe(string path)
    {
        try
        {
            return _fileSystem.GetDirectories(path).ToArray();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private IReadOnlyList<string> EnumerateFilesSafe(string path)
    {
        try
        {
            return _fileSystem.GetFiles(path).ToArray();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }
}
