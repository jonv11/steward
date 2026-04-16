using Steward.Core.Abstractions;
using Steward.Core.Discovery;

namespace Steward.Core.Orientation;

public sealed class OutlineEngine
{
    private readonly IFileSystem _fileSystem;

    public OutlineEngine(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public OutlineResult BuildOutline(
        string rootPath,
        List<DiscoveredFile> files,
        int maxDepth = int.MaxValue,
        bool includeSizes = false,
        bool includeLines = false,
        bool includeCounts = false)
    {
        var entries = new List<OutlineEntry>();
        var directoryStats = includeSizes || includeCounts
            ? BuildDirectoryStats(files)
            : null;

        foreach (var file in files)
        {
            var depth = file.RelativePath.Count(c => c == '/');
            if (depth >= maxDepth) continue;

            int? lineCount = null;
            if (includeLines && !file.IsDirectory)
            {
                try
                {
                    var fullPath = Path.Combine(rootPath, file.RelativePath);
                    var lines = _fileSystem.ReadAllLines(fullPath);
                    lineCount = lines.Length;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    lineCount = null;
                }
            }

            entries.Add(new OutlineEntry
            {
                Path = file.RelativePath,
                IsDirectory = file.IsDirectory,
                Depth = depth,
                Size = includeSizes
                    ? file.IsDirectory
                        ? directoryStats?.GetValueOrDefault(file.RelativePath)?.TotalSize
                        : file.Size
                    : null,
                LineCount = lineCount,
                FileCount = includeCounts && file.IsDirectory
                    ? directoryStats?.GetValueOrDefault(file.RelativePath)?.FileCount
                    : null,
                DirectoryCount = includeCounts && file.IsDirectory
                    ? directoryStats?.GetValueOrDefault(file.RelativePath)?.DirectoryCount
                    : null
            });
        }

        return new OutlineResult
        {
            RootPath = rootPath,
            Entries = entries
        };
    }

    public static string FormatSize(long bytes)
    {
        return bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{(bytes / 1024.0).ToString("F1", System.Globalization.CultureInfo.InvariantCulture)} KB",
            _ => $"{(bytes / (1024.0 * 1024.0)).ToString("F1", System.Globalization.CultureInfo.InvariantCulture)} MB"
        };
    }

    private static Dictionary<string, DirectoryStats> BuildDirectoryStats(IEnumerable<DiscoveredFile> files)
    {
        var stats = new Dictionary<string, DirectoryStats>(StringComparer.OrdinalIgnoreCase);

        foreach (var directory in files.Where(static file => file.IsDirectory))
            stats[directory.RelativePath] = new DirectoryStats();

        foreach (var file in files.Where(static file => !file.IsDirectory))
        {
            foreach (var ancestor in EnumerateAncestorDirectories(file.RelativePath))
            {
                if (!stats.TryGetValue(ancestor, out var current))
                {
                    current = new DirectoryStats();
                    stats[ancestor] = current;
                }

                current.FileCount++;
                current.TotalSize += file.Size;
            }
        }

        foreach (var directory in files.Where(static file => file.IsDirectory))
        {
            foreach (var ancestor in EnumerateAncestorDirectories(directory.RelativePath))
            {
                if (!stats.TryGetValue(ancestor, out var current))
                {
                    current = new DirectoryStats();
                    stats[ancestor] = current;
                }

                current.DirectoryCount++;
            }
        }

        return stats;
    }

    private static IEnumerable<string> EnumerateAncestorDirectories(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/');
        var current = Path.GetDirectoryName(normalized)?.Replace('\\', '/');

        while (!string.IsNullOrWhiteSpace(current))
        {
            yield return current;
            current = Path.GetDirectoryName(current)?.Replace('\\', '/');
        }
    }

    private sealed class DirectoryStats
    {
        public int FileCount { get; set; }
        public int DirectoryCount { get; set; }
        public long TotalSize { get; set; }
    }
}
