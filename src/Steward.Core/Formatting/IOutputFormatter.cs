namespace Steward.Core.Formatting;

public interface IOutputFormatter
{
    void WriteObject<T>(T value);
    void WriteMessage(string message);
    void WriteError(string message);
}
