using System.Text.Json;
using System.Text.Json.Serialization;

namespace Steward.Cli.Formatting;

using Steward.Core.Formatting;

public sealed class JsonOutputFormatter : IOutputFormatter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly TextWriter _stdout;

    public JsonOutputFormatter(TextWriter stdout)
    {
        _stdout = stdout;
    }

    public void WriteObject<T>(T value)
    {
        var json = JsonSerializer.Serialize(value, Options);
        _stdout.WriteLine(json);
    }

    public void WriteMessage(string message)
    {
        // JSON mode: messages go to stderr
        Console.Error.WriteLine(message);
    }

    public void WriteError(string message)
    {
        Console.Error.WriteLine(message);
    }

    public void WriteSuccess(string message)
    {
        Console.Error.WriteLine(message);
    }

    public void WriteDiagnostic(string severity, string message)
    {
        Console.Error.WriteLine(message);
    }
}
