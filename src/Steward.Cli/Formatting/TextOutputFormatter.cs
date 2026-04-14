namespace Steward.Cli.Formatting;

using Steward.Core.Formatting;

public sealed class TextOutputFormatter : IOutputFormatter
{
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
            Console.Error.Write("\x1b[31m");
            Console.Error.Write(message);
            Console.Error.WriteLine("\x1b[0m");
        }
        else
        {
            Console.Error.WriteLine(message);
        }
    }
}
