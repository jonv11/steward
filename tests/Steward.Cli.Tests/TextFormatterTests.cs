using FluentAssertions;
using Steward.Cli.Formatting;
using Xunit;

namespace Steward.Cli.Tests;

[Collection("Console")]
public class TextFormatterTests
{
    [Fact]
    public void WriteMessage_WritesToStdout()
    {
        var writer = new StringWriter();
        var formatter = new TextOutputFormatter(writer, useColor: false);

        formatter.WriteMessage("hello world");

        writer.ToString().Trim().Should().Be("hello world");
    }

    [Fact]
    public void WriteError_NoColor_NoAnsiCodes()
    {
        var stdErr = new StringWriter();
        var originalErr = Console.Error;
        Console.SetError(stdErr);

        try
        {
            var formatter = new TextOutputFormatter(new StringWriter(), useColor: false);
            formatter.WriteError("error message");

            var errorOutput = stdErr.ToString();
            errorOutput.Should().NotContain("\x1b[");
            errorOutput.Should().Contain("error message");
        }
        finally
        {
            Console.SetError(originalErr);
        }
    }

    [Fact]
    public void WriteError_WithColor_ContainsAnsiCodes()
    {
        var stdErr = new StringWriter();
        var originalErr = Console.Error;
        Console.SetError(stdErr);

        try
        {
            var formatter = new TextOutputFormatter(new StringWriter(), useColor: true);
            formatter.WriteError("error message");

            var errorOutput = stdErr.ToString();
            errorOutput.Should().Contain("\x1b[31m");
            errorOutput.Should().Contain("\x1b[0m");
        }
        finally
        {
            Console.SetError(originalErr);
        }
    }
}
