namespace Steward.Core.Abstractions;

public interface IFileSystem
{
    bool FileExists(string path);
    bool DirectoryExists(string path);
    string ReadAllText(string path);
    string[] ReadAllLines(string path);
    IEnumerable<string> GetFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);
    IEnumerable<string> GetDirectories(string path);
    long GetFileSize(string path);
    Stream OpenRead(string path);
    DateTime GetLastWriteTimeUtc(string path);
}
