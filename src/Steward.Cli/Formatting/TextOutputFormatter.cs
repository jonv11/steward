namespace Steward.Cli.Formatting;

using Steward.Core.Formatting;

public sealed class TextOutputFormatter : IOutputFormatter
{
    private const string Red = "\x1b[31m";
    private const string Yellow = "\x1b[33m";
    private const string Green = "\x1b[32m";
    private const string Reset = "\x1b[0m";

    private readonly TextWriter _stdout;
    private readonly bool _useColor;

    public TextOutputFormatter(TextWriter stdout, bool useColor)
    {
        _stdout = stdout;
        _useColor = useColor;
    }

    public void WriteObject<T>(T value)
    {
        _stdout.WriteLine(value?.ToString());
    }

    public void WriteMessage(string message)
    {
        _stdout.WriteLine(message);
    }

    public void WriteError(string message)
    {
        if (_useColor)
        {
            Console.Error.Write(Red);
            Console.Error.Write(message);
            Console.Error.WriteLine(Reset);
        }
        else
        {
            Console.Error.WriteLine(message);
        }
    }

    public void WriteSuccess(string message)
    {
        if (_useColor)
        {
            _stdout.Write(Green);
            _stdout.Write(message);
            _stdout.WriteLine(Reset);
        }
        else
        {
            _stdout.WriteLine(message);
        }
    }

    public void WriteDiagnostic(string severity, string message)
    {
        if (_useColor)
        {
            var color = severity switch
            {
                "error" => Red,
                "warn" => Yellow,
                _ => null
            };
            if (color != null)
            {
                _stdout.Write(color);
                _stdout.Write(message);
                _stdout.WriteLine(Reset);
                return;
            }
        }
        _stdout.WriteLine(message);
    }
}
