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

    [Fact]
    public void Version_SourceBuild_SaysSoAndReportsReleaseBuildFalse()
    {
        var (_, text, _) = CliTestHelper.InvokeCapture("version");
        var (_, json, _) = CliTestHelper.InvokeCapture("version", "--output", "json");

        text.Should().Contain("(source build, not a release)");
        var data = JsonDocument.Parse(json).RootElement.GetProperty("data");
        data.GetProperty("releaseBuild").GetBoolean().Should().BeFalse();
        data.GetProperty("version").GetString().Should().MatchRegex(@"^\d+\.\d+\.\d+$");
    }

    [Theory]
    [InlineData(true, null, "steward 1.2.3")]
    [InlineData(false, "abc1234", "steward 1.2.3+abc1234 (source build, not a release)")]
    [InlineData(false, null, "steward 1.2.3 (source build, not a release)")]
    public void Describe_NamesTheCommitOnlyForSourceBuilds(bool releaseBuild, string? commit, string expected)
    {
        var info = new Steward.Core.Models.VersionInfo
        {
            Version = "1.2.3",
            RuntimeVersion = "r",
            OsPlatform = "o",
            Architecture = "a",
            Commit = commit,
            ReleaseBuild = releaseBuild,
        };

        Steward.Cli.Commands.VersionCommand.Describe(info).Should().Be(expected);
    }
}
