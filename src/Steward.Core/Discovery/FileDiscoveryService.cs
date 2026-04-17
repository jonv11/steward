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
        foreach (var dirPath in _fileSystem.GetDirectories(currentPath))
        {
            var relativePath = GetRelativePath(rootPath, dirPath);

            if (_ignoreFilter.IsIgnored(relativePath, isDirectory: true))
                continue;

            results.Add(new DiscoveredFile(relativePath, 0, IsDirectory: true));
            WalkDirectory(dirPath, rootPath, results);
        }

        // Add files
        foreach (var filePath in _fileSystem.GetFiles(currentPath))
        {
            var relativePath = GetRelativePath(rootPath, filePath);

            if (_ignoreFilter.IsIgnored(relativePath, isDirectory: false))
                continue;

            var size = _fileSystem.GetFileSize(filePath);
            results.Add(new DiscoveredFile(relativePath, size, IsDirectory: false));
        }
    }

    private static string GetRelativePath(string root, string fullPath)
    {
        return PathHelper.NormalizeSeparators(Path.GetRelativePath(root, fullPath));
    }
}
