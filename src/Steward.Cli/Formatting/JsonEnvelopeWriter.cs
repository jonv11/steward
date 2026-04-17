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
}
