using System.CommandLine;
using System.Globalization;
using Steward.Core;
using Steward.Cli.Commands;

namespace Steward.Cli;

public static class Program
{
    public static RootCommand CreateRootCommand()
    {
        var rootCommand = new RootCommand("Repository Steward — a configurable repository stewardship CLI for humans and AI agents");

        GlobalOptionsSetup.AddGlobalOptions(rootCommand);

        rootCommand.SetAction(parseResult =>
        {
            parseResult.RootCommandResult.Command.Parse(["--help"]).Invoke();
            return ExitCodes.Success;
        });

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

    internal static async Task<int> InvokeAsync(string[] args)
    {
        var parseResult = CreateRootCommand().Parse(args);
        using var helpScope = ShouldRewriteHelpOutput(args, parseResult)
            ? new HelpOutputRewriteScope("Steward.Cli", "steward")
            : null;

        var exitCode = await parseResult.InvokeAsync(CancellationToken.None);

        if (parseResult.Errors.Count > 0 && exitCode != ExitCodes.Success)
            return ExitCodes.UsageError;

        return exitCode;
    }

    public static async Task<int> Main(string[] args)
    {
        // Ensure invariant culture for deterministic output regardless of system locale.
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

        return await InvokeAsync(args);
    }

    private static bool ShouldRewriteHelpOutput(string[] args, ParseResult parseResult)
    {
        return args.Length == 0
            || args.Any(static arg => arg is "--help" or "-h" or "-?")
            || parseResult.Errors.Count > 0;
    }

    private sealed class HelpOutputRewriteScope : IDisposable
    {
        private readonly string internalName;
        private readonly string publicName;
        private readonly TextWriter originalOut = Console.Out;
        private readonly TextWriter originalErr = Console.Error;
        private readonly StringWriter captureOut = new();
        private readonly StringWriter captureErr = new();
        private bool disposed;

        public HelpOutputRewriteScope(string internalName, string publicName)
        {
            this.internalName = internalName;
            this.publicName = publicName;
            Console.SetOut(captureOut);
            Console.SetError(captureErr);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            Console.SetOut(originalOut);
            Console.SetError(originalErr);

            WriteRewritten(originalOut, captureOut.ToString(), internalName, publicName);
            WriteRewritten(originalErr, captureErr.ToString(), internalName, publicName);
        }

        private static void WriteRewritten(TextWriter writer, string content, string internalName, string publicName)
        {
            if (content.Length == 0)
                return;

            var rewritten = content
                .Replace($"Usage: {internalName}", $"Usage: {publicName}", StringComparison.Ordinal)
                .Replace($"{internalName} [", $"{publicName} [", StringComparison.Ordinal)
                .Replace($"{internalName} ", $"{publicName} ", StringComparison.Ordinal);

            writer.Write(rewritten);
            writer.Flush();
        }
    }
}
