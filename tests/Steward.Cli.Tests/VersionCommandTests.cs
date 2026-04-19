using System.Text.Json;
using FluentAssertions;
using Steward.Cli.Tests.Helpers;
using Xunit;

namespace Steward.Cli.Tests;

[Collection("Console")]
public class VersionCommandTests
{
    [Fact]
    public void Version_TextOutput_ContainsStewardVersion()
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("version");

        output.Should().Contain("steward ");
        output.Should().Contain("Runtime:");
        output.Should().Contain("OS:");
    }

    [Fact]
    public void Version_TextOutput_ReturnsExitCode0()
    {
        var (exitCode, _, _) = CliTestHelper.InvokeCapture("version");

        exitCode.Should().Be(0);
    }

    [Fact]
    public void Version_JsonOutput_ReturnsValidJson()
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("version", "--output", "json");

        exitCode.Should().Be(0);
        output.Should().NotBeNullOrWhiteSpace();

        var doc = JsonDocument.Parse(output);
        var data = doc.RootElement.GetProperty("data");
        data.GetProperty("version").GetString().Should().NotBeNullOrWhiteSpace();
        data.GetProperty("runtimeVersion").GetString().Should().NotBeNullOrWhiteSpace();
        data.GetProperty("osPlatform").GetString().Should().NotBeNullOrWhiteSpace();
        data.GetProperty("architecture").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Version_JsonOutput_UsesCamelCase()
    {
        var (_, output, _) = CliTestHelper.InvokeCapture("version", "--output", "json");

        output.Should().Contain("\"runtimeVersion\"");
        output.Should().Contain("\"osPlatform\"");
        output.Should().NotContain("\"RuntimeVersion\"");
        output.Should().NotContain("\"OsPlatform\"");
    }
}
