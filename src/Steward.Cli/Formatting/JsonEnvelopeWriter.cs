using Steward.Core.Formatting;

namespace Steward.Cli.Formatting;

public static class JsonEnvelopeWriter
{
    private static readonly string ToolVersion =
        typeof(JsonEnvelopeWriter).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    public static void Write<T>(
        IOutputFormatter formatter,
        string command,
        bool success,
        int exitCode,
        T data)
    {
        formatter.WriteObject(new
        {
            schemaVersion = "steward-json/v1",
            command,
            toolVersion = ToolVersion,
            success,
            exitCode,
            data
        });
    }

    /// <summary>
    /// Writes a structured JSON error on stdout, wrapped in the standard envelope.
    /// The error is also echoed to stderr for human readability.
    /// </summary>
    public static void WriteError(
        IOutputFormatter formatter,
        string command,
        int exitCode,
        string kind,
        string message,
        Dictionary<string, object>? details = null,
        bool retryable = false,
        string? suggestedNextStep = null)
    {
        var error = new JsonErrorResponse
        {
            Kind = kind,
            Message = message,
            Details = details,
            Retryable = retryable,
            SuggestedNextStep = suggestedNextStep
        };

        Write(formatter, command, false, exitCode, new { error });

        // Also echo to stderr for human context
        Console.Error.WriteLine(message);
    }
}
