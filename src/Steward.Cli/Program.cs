using System.CommandLine;
using Steward.Core;
using Steward.Cli.Commands;

namespace Steward.Cli;

public static class Program
{
    public static RootCommand CreateRootCommand()
    {
        var rootCommand = new RootCommand("Repository Steward — a configurable repository stewardship CLI for humans and AI agents");

        GlobalOptionsSetup.AddGlobalOptions(rootCommand);

        rootCommand.Add(VersionCommand.Create());
        rootCommand.Add(OrientCommand.Create());
        rootCommand.Add(OutlineCommand.Create());
        rootCommand.Add(InitCommand.Create());
        rootCommand.Add(ConfigCommand.Create());
        rootCommand.Add(CheckCommand.Create());
        rootCommand.Add(MdCommand.Create());
        rootCommand.Add(SearchCommand.Create());
        rootCommand.Add(MaintainCommand.Create());
        rootCommand.Add(StatusCommand.Create());
        rootCommand.Add(ExplainCommand.Create());
        rootCommand.Add(RefsCommand.Create());
        rootCommand.Add(RefactorCommand.Create());

        return rootCommand;
    }

    public static async Task<int> Main(string[] args)
    {
        var parseResult = CreateRootCommand().Parse(args);
        var exitCode = await parseResult.InvokeAsync(CancellationToken.None);

        if (parseResult.Errors.Count > 0 && exitCode != ExitCodes.Success)
            return ExitCodes.UsageError;

        return exitCode;
    }
}
