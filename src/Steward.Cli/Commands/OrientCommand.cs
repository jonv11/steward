using System.CommandLine;
using Steward.Core;
using Steward.Core.Formatting;
using Steward.Core.Orientation;

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
            var depth = parseResult.GetValue(depthOption);
            var ctx = CommandSetup.Build(parseResult);

            var engine = new OrientationEngine();
            var result = engine.Orient(ctx.RootPath, ctx.Files!, depth);

            if (ctx.OutputFormat == OutputFormat.Json)
            {
                ctx.Formatter.WriteObject(result);
            }
            else
            {
                foreach (var entry in result.Entries)
                {
                    var indent = new string(' ', entry.Depth * 2);
                    var icon = entry.IsDirectory ? "📁" : "📄";
                    var label = $"[{entry.Classification}]";
                    ctx.Formatter.WriteMessage($"{indent}{icon} {entry.Path} {label}");
                }
            }

            return ExitCodes.Success;
        });

        return command;
    }
}
