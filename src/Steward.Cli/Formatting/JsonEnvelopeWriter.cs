using Steward.Core;
using Steward.Core.Formatting;

namespace Steward.Cli.Formatting;

/// <summary>
/// Writes JSON output in either legacy (direct payload) or standard (wrapped envelope) mode.
/// Standard envelope: { schemaVersion, command, toolVersion, success, exitCode, data }
/// </summary>
public static class JsonEnvelopeWriter
{
    private static readonly string ToolVersion =
        typeof(JsonEnvelopeWriter).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    public static void Write<T>(
        IOutputFormatter formatter,
        JsonEnvelopeMode mode,
        string command,
        bool success,
        int exitCode,
        T data)
    {
        if (mode == JsonEnvelopeMode.Standard)
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
        else
        {
            formatter.WriteObject(data);
        }
    }

    /// <summary>
    /// Writes a structured JSON error on stdout. In standard envelope mode, the error
    /// is wrapped in the envelope with success=false. In legacy mode, the error object
    /// is emitted directly. The error is also echoed to stderr for human readability.
    /// </summary>
    public static void WriteError(
        IOutputFormatter formatter,
        JsonEnvelopeMode mode,
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

        Write(formatter, mode, command, false, exitCode, new { error });

        // Also echo to stderr for human context
        Console.Error.WriteLine(message);
    }
}
