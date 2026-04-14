using FluentAssertions;
using Steward.Cli.Tests.Helpers;
using Xunit;

namespace Steward.Cli.Tests;

[Collection("Console")]
public class ExitCodeTests
{
    [Fact]
    public void UnknownCommand_ReturnsUsageErrorExitCode()
    {
        var (exitCode, _, _) = CliTestHelper.InvokeCapture("nonexistent-command");

        exitCode.Should().Be(2);
    }

    [Fact]
    public void UnknownOption_ReturnsUsageErrorExitCode()
    {
        var (exitCode, _, _) = CliTestHelper.InvokeCapture("check", "--not-a-real-option");

        exitCode.Should().Be(2);
    }
}
