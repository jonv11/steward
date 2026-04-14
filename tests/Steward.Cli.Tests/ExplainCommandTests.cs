using FluentAssertions;
using Steward.Cli.Commands;
using System.CommandLine;
using Xunit;

namespace Steward.Cli.Tests;

[Collection("Console")]
public class ExplainCommandTests
{
    private static (int ExitCode, string Output, string Error) InvokeExplain(params string[] args)
    {
        var rootCommand = new RootCommand("Repository Steward");
        GlobalOptionsSetup.AddGlobalOptions(rootCommand);
        rootCommand.Add(ExplainCommand.Create());

        var stdOut = new StringWriter();
        var stdErr = new StringWriter();
        var origOut = Console.Out;
        var origErr = Console.Error;

        Console.SetOut(stdOut);
        Console.SetError(stdErr);

        try
        {
            var exitCode = rootCommand.Parse(args).Invoke();
            return (exitCode, stdOut.ToString(), stdErr.ToString());
        }
        finally
        {
            Console.SetOut(origOut);
            Console.SetError(origErr);
        }
    }

    [Fact]
    public void Explain_KnownRule_ShowsDetails()
    {
        var (exitCode, output, _) = InvokeExplain("explain", "STWD-001");

        exitCode.Should().Be(0);
        output.Should().Contain("STWD-001");
        output.Should().Contain("Category:");
        output.Should().Contain("Remediation:");
    }

    [Fact]
    public void Explain_UnknownRule_ReportsError()
    {
        var (exitCode, _, _) = InvokeExplain("explain", "STWD-999");

        exitCode.Should().Be(2);
    }

    [Fact]
    public void Explain_JsonOutput()
    {
        var (exitCode, output, _) = InvokeExplain("explain", "STWD-003", "--output", "json");

        exitCode.Should().Be(0);
        output.Should().Contain("\"ruleId\"");
        output.Should().Contain("\"remediation\"");
        output.Should().Contain("STWD-003");
    }

    [Fact]
    public void Explain_AllRules_Listed()
    {
        var (exitCode, output, _) = InvokeExplain("explain", "STWD-008");

        exitCode.Should().Be(0);
        output.Should().Contain("STWD-008");
        output.Should().Contain("broken-link");
    }

    [Fact]
    public void Explain_EachRuleHasRemediation()
    {
        foreach (var ruleId in new[] { "STWD-001", "STWD-002", "STWD-003", "STWD-004",
                                        "STWD-005", "STWD-006", "STWD-007", "STWD-008" })
        {
            var remediation = ExplainCommand.GetRemediation(ruleId);
            remediation.Should().NotBeNullOrWhiteSpace($"Rule {ruleId} should have remediation guidance");
            remediation.Should().NotBe("No specific remediation guidance available.",
                $"Rule {ruleId} should have specific remediation");
        }
    }
}
