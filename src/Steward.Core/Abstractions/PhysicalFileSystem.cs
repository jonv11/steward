namespace Steward.Core.Abstractions;

public sealed class PhysicalFileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public string ReadAllText(string path) => File.ReadAllText(path);

    public string[] ReadAllLines(string path) => File.ReadAllLines(path);

    public IEnumerable<string> GetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        => Directory.EnumerateFiles(path, searchPattern, CreateEnumerationOptions(searchOption));

    public IEnumerable<string> GetDirectories(string path)
        => Directory.EnumerateDirectories(path, "*", CreateEnumerationOptions(SearchOption.TopDirectoryOnly));

    public long GetFileSize(string path) => new FileInfo(path).Length;

    public Stream OpenRead(string path) => File.OpenRead(path);

    public DateTime GetLastWriteTimeUtc(string path) => File.GetLastWriteTimeUtc(path);

    private static EnumerationOptions CreateEnumerationOptions(SearchOption searchOption)
    {
        return new EnumerationOptions
        {
            RecurseSubdirectories = searchOption == SearchOption.AllDirectories,
            IgnoreInaccessible = true,
            ReturnSpecialDirectories = false,
            AttributesToSkip = 0
        };
    }
}
