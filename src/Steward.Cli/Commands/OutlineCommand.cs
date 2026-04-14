using System.CommandLine;
using Steward.Core;
using Steward.Core.Abstractions;
using Steward.Core.Discovery;
using Steward.Core.Formatting;
using Steward.Core.Orientation;

namespace Steward.Cli.Commands;

public static class OutlineCommand
{
    public static Command Create()
    {
        var command = new Command("outline", "Show directory file tree");

        var pathArgument = new Argument<string>("path")
        {
            Description = "Root path to outline",
            DefaultValueFactory = _ => "."
        };
        command.Add(pathArgument);

        var depthOption = new Option<int>("--depth", "-d")
        {
            Description = "Maximum depth to display",
            DefaultValueFactory = _ => int.MaxValue
        };
        command.Add(depthOption);

        var sizesOption = new Option<bool>("--sizes")
        {
            Description = "Include file sizes"
        };
        command.Add(sizesOption);

        var linesOption = new Option<bool>("--lines")
        {
            Description = "Include line counts"
        };
        command.Add(linesOption);

        command.SetAction((parseResult) =>
        {
            var path = parseResult.GetValue(pathArgument);
            var depth = parseResult.GetValue(depthOption);
            var sizes = parseResult.GetValue(sizesOption);
            var lines = parseResult.GetValue(linesOption);

            var output = parseResult.GetValue(GlobalOptionsSetup.OutputOption);
            var noColor = parseResult.GetValue(GlobalOptionsSetup.NoColorOption);
            var formatter = CommandSetup.CreateFormatter(output, noColor);
            var fileSystem = new PhysicalFileSystem();

            // outline takes an explicit path argument, not necessarily CWD, so we
            // cannot use CommandSetup.Build() (which roots discovery at CWD). We
            // wire up discovery directly against the resolved target path instead.
            var rootPath = Path.GetFullPath(path ?? ".");

            var ignoreFilter = GitIgnoreFilter.Load(rootPath, fileSystem);
            var discoveryService = new FileDiscoveryService(fileSystem, ignoreFilter);
            var files = discoveryService.Discover(rootPath);

            var engine = new OutlineEngine(fileSystem);
            var result = engine.BuildOutline(rootPath, files, depth, sizes, lines);

            if (output == OutputFormat.Json)
            {
                formatter.WriteObject(result);
            }
            else
            {
                foreach (var entry in result.Entries)
                {
                    var indent = new string(' ', entry.Depth * 2);
                    var suffix = entry.IsDirectory ? "/" : "";
                    var name = Path.GetFileName(entry.Path) + suffix;

                    var extras = new List<string>();
                    if (entry.Size.HasValue)
                        extras.Add(OutlineEngine.FormatSize(entry.Size.Value));
                    if (entry.LineCount.HasValue)
                        extras.Add($"{entry.LineCount.Value} lines");

                    var info = extras.Count > 0 ? $" ({string.Join(", ", extras)})" : "";
                    formatter.WriteMessage($"{indent}{name}{info}");
                }
            }

            return ExitCodes.Success;
        });

        return command;
    }
}
