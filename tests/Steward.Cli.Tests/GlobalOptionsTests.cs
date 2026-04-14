using FluentAssertions;
using Steward.Cli.Tests.Helpers;
using Xunit;

namespace Steward.Cli.Tests;

[Collection("Console")]
public class GlobalOptionsTests
{
    [Theory]
    [InlineData("--output")]
    [InlineData("--verbosity")]
    [InlineData("--no-color")]
    [InlineData("--config")]
    [InlineData("--help")]
    public void Help_ContainsGlobalOption(string optionName)
    {
        var (_, output, _) = CliTestHelper.InvokeCapture("--help");

        output.Should().Contain(optionName);
    }

    [Fact]
    public void Help_ContainsVersionCommand()
    {
        var (_, output, _) = CliTestHelper.InvokeCapture("--help");

        output.Should().Contain("version");
    }
}
