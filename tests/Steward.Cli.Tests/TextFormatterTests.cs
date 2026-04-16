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

    [Fact]
    public void WriteSuccess_NoColor_NoAnsiCodes()
    {
        var writer = new StringWriter();
        var formatter = new TextOutputFormatter(writer, useColor: false);

        formatter.WriteSuccess("Result: PASS");

        var output = writer.ToString();
        output.Should().NotContain("\x1b[");
        output.Should().Contain("Result: PASS");
    }

    [Fact]
    public void WriteSuccess_WithColor_ContainsGreenAnsiCode()
    {
        var writer = new StringWriter();
        var formatter = new TextOutputFormatter(writer, useColor: true);

        formatter.WriteSuccess("Result: PASS");

        var output = writer.ToString();
        output.Should().Contain("\x1b[32m");
        output.Should().Contain("\x1b[0m");
        output.Should().Contain("Result: PASS");
    }

    [Theory]
    [InlineData("error", "\x1b[31m")]
    [InlineData("warn", "\x1b[33m")]
    public void WriteDiagnostic_WithColor_ColorCodeMatchesSeverity(string severity, string expectedCode)
    {
        var writer = new StringWriter();
        var formatter = new TextOutputFormatter(writer, useColor: true);

        formatter.WriteDiagnostic(severity, $"[{severity}] some diagnostic");

        var output = writer.ToString();
        output.Should().Contain(expectedCode);
        output.Should().Contain("\x1b[0m");
        output.Should().Contain($"[{severity}]");
    }

    [Fact]
    public void WriteDiagnostic_InfoSeverity_WithColor_NoColorCode()
    {
        var writer = new StringWriter();
        var formatter = new TextOutputFormatter(writer, useColor: true);

        formatter.WriteDiagnostic("info", "[info ] some diagnostic");

        var output = writer.ToString();
        output.Should().NotContain("\x1b[");
        output.Should().Contain("[info ]");
    }

    [Fact]
    public void WriteDiagnostic_NoColor_NoAnsiCodes()
    {
        var writer = new StringWriter();
        var formatter = new TextOutputFormatter(writer, useColor: false);

        formatter.WriteDiagnostic("error", "[error] some diagnostic");

        var output = writer.ToString();
        output.Should().NotContain("\x1b[");
        output.Should().Contain("[error]");
    }

    [Fact]
    public void Style_WithColor_AppliesAnsiCodes()
    {
        var formatter = new TextOutputFormatter(new StringWriter(), useColor: true);

        var output = formatter.Style("Directory", CliTextStyle.Directory);

        output.Should().Contain("\x1b[34m");
        output.Should().Contain("\x1b[0m");
        output.Should().Contain("Directory");
    }

    [Fact]
    public void Style_NoColor_ReturnsPlainText()
    {
        var formatter = new TextOutputFormatter(new StringWriter(), useColor: false);

        var output = formatter.Style("Heading", CliTextStyle.Heading);

        output.Should().Be("Heading");
    }
}
