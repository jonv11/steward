namespace Steward.Core.Discovery;

/// <summary>
/// Determines whether a file or directory should be ignored during discovery.
/// </summary>
public interface IIgnoreFilter
{
    bool IsIgnored(string relativePath, bool isDirectory);
}
