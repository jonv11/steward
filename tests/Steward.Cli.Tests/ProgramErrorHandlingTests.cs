using System.Text.Json;
using FluentAssertions;
using Steward.Cli.Tests.Helpers;
using Steward.Core;
using Xunit;

namespace Steward.Cli.Tests;

[Collection("Console")]
public class ProgramErrorHandlingTests
{
    [Fact]
    public void TopLevel_UnauthorizedAccess_ReturnsStructuredJsonUsageError()
    {
        var (exitCode, output, error) = CliTestHelper.InvokeCapture(
            static _ => Task.FromException<int>(new UnauthorizedAccessException("Access to '/repo/locked' is denied.")),
            "orient",
            "--output",
            "json");

        exitCode.Should().Be(ExitCodes.UsageError);

        var root = JsonDocument.Parse(output).RootElement;
        root.GetProperty("command").GetString().Should().Be("orient");
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("exitCode").GetInt32().Should().Be(ExitCodes.UsageError);
        root.GetProperty("data").GetProperty("error").GetProperty("kind").GetString().Should().Be("access-denied");
        root.GetProperty("data").GetProperty("error").GetProperty("details").GetProperty("exceptionType").GetString()
            .Should().Be(nameof(UnauthorizedAccessException));
        error.Should().Contain("Access denied:");
    }

    [Fact]
    public void TopLevel_UnexpectedException_ReturnsInternalError_TextOutput()
    {
        var (exitCode, output, error) = CliTestHelper.InvokeCapture(
            static _ => Task.FromException<int>(new InvalidOperationException("Boom")),
            "status");

        exitCode.Should().Be(ExitCodes.InternalError);
        output.Should().BeEmpty();
        error.Should().Contain("Internal error: Boom");
        error.Should().Contain("Please report it if it persists.");
    }
}
