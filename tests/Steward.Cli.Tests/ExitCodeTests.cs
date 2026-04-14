using FluentAssertions;
using Steward.Cli.Tests.Helpers;
using Xunit;

namespace Steward.Cli.Tests;

[Collection("Console")]
public class ExitCodeTests
{
    [Fact]
    public void UnknownCommand_ReturnsNonZeroExitCode()
    {
        var (exitCode, _, _) = CliTestHelper.InvokeCapture("nonexistent-command");

        exitCode.Should().NotBe(0);
    }
}
