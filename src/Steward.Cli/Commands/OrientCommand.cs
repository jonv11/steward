using System.CommandLine;
using Steward.Core;
using Steward.Core.Abstractions;
using Steward.Core.Discovery;
using Steward.Core.Formatting;
using Steward.Core.Orientation;
using Steward.Cli.Formatting;

namespace Steward.Cli.Commands;

public static class OrientCommand
{
    public static Command Create()
    {
        var command = new Command("orient", "Show classified repository structure");

        var depthOption = new Option<int>("--depth", "-d")
        {
            Description = "Maximum depth to display",
            DefaultValueFactory = _ => 3
        };
        command.Add(depthOption);

        command.SetAction((parseResult) =>
        {
            var output = parseResult.GetValue(GlobalOptionsSetup.OutputOption);
            var noColor = parseResult.GetValue(GlobalOptionsSetup.NoColorOption);
            var depth = parseResult.GetValue(depthOption);

            var formatter = CreateFormatter(output, noColor);
            var fileSystem = new PhysicalFileSystem();
            var rootPath = Directory.GetCurrentDirectory();

            var ignoreFilter = GitIgnoreFilter.Load(rootPath, fileSystem);
            var discoveryService = new FileDiscoveryService(fileSystem, ignoreFilter);
            var files = discoveryService.Discover(rootPath);

            var engine = new OrientationEngine();
            var result = engine.Orient(rootPath, files, depth);

            if (output == OutputFormat.Json)
            {
                formatter.WriteObject(result);
            }
            else
            {
                foreach (var entry in result.Entries)
                {
                    var indent = new string(' ', entry.Depth * 2);
                    var icon = entry.IsDirectory ? "📁" : "📄";
                    var label = $"[{entry.Classification}]";
                    formatter.WriteMessage($"{indent}{icon} {entry.Path} {label}");
                }
            }

            return ExitCodes.Success;
        });

        return command;
    }

    private static IOutputFormatter CreateFormatter(OutputFormat format, bool noColor)
    {
        return format switch
        {
            OutputFormat.Json => new JsonOutputFormatter(Console.Out),
            _ => new TextOutputFormatter(Console.Out, !noColor && !Console.IsOutputRedirected)
        };
    }
}
