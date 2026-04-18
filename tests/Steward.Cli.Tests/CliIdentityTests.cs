using FluentAssertions;
using Steward.Cli.Tests.Helpers;
using Xunit;

namespace Steward.Cli.Tests;

[Collection("Console")]
public class CliIdentityTests
{
    [Fact]
    public void MdQueryHelp_UsesPublicExecutableName()
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("md", "query", "--help");

        exitCode.Should().Be(0);
        output.Should().Contain("Usage:");
        output.Should().Contain("steward md query");
        output.Should().NotContain("Steward.Cli md query");
    }
}
