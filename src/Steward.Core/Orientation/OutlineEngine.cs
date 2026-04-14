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

    public OutlineResult BuildOutline(string rootPath, List<DiscoveredFile> files, int maxDepth = int.MaxValue, bool includeSizes = false, bool includeLines = false)
    {
        var entries = new List<OutlineEntry>();

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
                Size = includeSizes ? file.Size : null,
                LineCount = lineCount
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
}
