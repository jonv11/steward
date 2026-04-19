using System.CommandLine;
using System.Runtime.InteropServices;
using Steward.Cli.Formatting;
using Steward.Core;
using Steward.Core.Models;

namespace Steward.Cli.Commands;

public static class VersionCommand
{
    public static Command Create()
    {
        var command = new Command("version", "Print version and runtime information");
        command.SetAction((parseResult) =>
        {
            var output = parseResult.GetValue(GlobalOptionsSetup.OutputOption);
            var noColor = parseResult.GetValue(GlobalOptionsSetup.NoColorOption);

            var formatter = CommandSetup.CreateFormatter(output, noColor);

            var info = new VersionInfo
            {
                Version = typeof(VersionCommand).Assembly.GetName().Version?.ToString(3) ?? "0.0.0",
                RuntimeVersion = RuntimeInformation.FrameworkDescription,
                OsPlatform = RuntimeInformation.OSDescription,
                Architecture = RuntimeInformation.OSArchitecture.ToString()
            };

            if (output == OutputFormat.Json)
            {
                JsonEnvelopeWriter.Write(formatter, "version", true, ExitCodes.Success, info);
            }
            else
            {
                formatter.WriteMessage($"steward {info.Version}");
                formatter.WriteMessage($"Runtime: {info.RuntimeVersion}");
                formatter.WriteMessage($"OS: {info.OsPlatform} ({info.Architecture})");
            }

            return ExitCodes.Success;
        });

        return command;
    }
}
