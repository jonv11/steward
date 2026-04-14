using System.CommandLine;
using Steward.Cli.Commands;

namespace Steward.Cli.Tests.Helpers;

internal static class CliTestHelper
{
    private static readonly Lock ConsoleLock = new();

    public static RootCommand CreateRootCommand()
    {
        var rootCommand = new RootCommand("Repository Steward");
        GlobalOptionsSetup.AddGlobalOptions(rootCommand);
        rootCommand.Add(VersionCommand.Create());
        rootCommand.Add(OrientCommand.Create());
        rootCommand.Add(OutlineCommand.Create());
        return rootCommand;
    }

    public static int Invoke(params string[] args)
    {
        var rootCommand = CreateRootCommand();
        return rootCommand.Parse(args).Invoke();
    }

    public static (int ExitCode, string Output, string Error) InvokeCapture(params string[] args)
    {
        lock (ConsoleLock)
        {
            var stdOut = new StringWriter();
            var stdErr = new StringWriter();

            var originalOut = Console.Out;
            var originalErr = Console.Error;

            Console.SetOut(stdOut);
            Console.SetError(stdErr);

            try
            {
                var exitCode = Invoke(args);
                return (exitCode, stdOut.ToString(), stdErr.ToString());
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
            }
        }
    }
}
