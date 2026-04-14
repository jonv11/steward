namespace Steward.Core.Configuration;

public sealed class StewardConfigException : Exception
{
    public string? FilePath { get; }

    public StewardConfigException(string message, string? filePath, Exception? innerException = null)
        : base(message, innerException)
    {
        FilePath = filePath;
    }
}
