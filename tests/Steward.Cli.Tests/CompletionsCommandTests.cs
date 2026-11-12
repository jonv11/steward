using System.Text.Json.Nodes;
using FluentAssertions;
using Steward.Cli.Commands;
using Steward.Cli.Tests.Helpers;
using Xunit;

namespace Steward.Cli.Tests;

[Collection("Console")]
public class CompletionsCommandTests
{
    [Theory]
    [InlineData("bash", "complete -o default -F _steward_complete steward")]
    [InlineData("zsh", "compdef _steward steward")]
    [InlineData("pwsh", "Register-ArgumentCompleter -Native -CommandName steward")]
    public void Completions_PrintsAScriptThatCallsTheSuggestDirective(string shell, string registration)
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("completions", shell);

        exitCode.Should().Be(0);
        output.Should().Be(CompletionsCommand.Script(shell));
        output.Should().Contain(registration);
        output.Should().Contain("[suggest:");
    }

    [Fact]
    public void Completions_Json_WrapsTheScriptInTheEnvelope()
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture("completions", "bash", "--output", "json");

        exitCode.Should().Be(0);
        var data = JsonNode.Parse(output)!["data"]!;
        data["shell"]!.GetValue<string>().Should().Be("bash");
        data["script"]!.GetValue<string>().Should().Be(CompletionsCommand.Script("bash"));
    }

    [Fact]
    public void Completions_UnknownShell_IsAUsageError()
    {
        var (exitCode, _, _) = CliTestHelper.InvokeCapture("completions", "fish");

        exitCode.Should().Be(2);
    }

    [Theory]
    [InlineData("steward ch", "check")]
    [InlineData("steward check --sc", "--scope")]
    [InlineData("steward check --scope ", "staged")]
    [InlineData("steward md ed", "edit")]
    [InlineData("steward completions ", "pwsh")]
    public void SuggestDirective_CompletesFromTheParser(string line, string expected)
    {
        var (exitCode, output, _) = CliTestHelper.InvokeCapture($"[suggest:{line.Length}]", line);

        exitCode.Should().Be(0);
        output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Should().Contain(expected);
    }
}
