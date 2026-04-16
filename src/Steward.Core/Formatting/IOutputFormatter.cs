namespace Steward.Core.Formatting;

public interface IOutputFormatter
{
    void WriteObject<T>(T value);
    void WriteMessage(string message);
    void WriteError(string message);
    /// <summary>
    /// Writes a success/pass message. Implementations may render this in green when color is enabled.
    /// </summary>
    void WriteSuccess(string message);
    /// <summary>
    /// Writes a diagnostic line with optional severity coloring: error=red, warn=yellow, info=default.
    /// </summary>
    void WriteDiagnostic(string severity, string message);
}
