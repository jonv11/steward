using System.CommandLine;
using Steward.Cli.Formatting;
using Steward.Core;
using Steward.Core.Discovery;
using Steward.Core.Validation.Rules;

namespace Steward.Cli.Commands;

public static class RefsCommand
{
    public static Command Create()
    {
        var command = new Command("refs", "Show inbound and outbound references for a file");

        var pathArg = new Argument<string>("path")
        {
            Description = "The file path to inspect references for"
        };
        var toOption = new Option<bool>("--to")
        {
            Description = "Show only files that link TO this file"
        };
        var fromOption = new Option<bool>("--from")
        {
            Description = "Show only files that this file links TO (outbound)"
        };

        command.Add(pathArg);
        command.Add(toOption);
        command.Add(fromOption);

        command.SetAction(parseResult =>
        {
            if (!CommandSetup.TryBuild(parseResult, out var ctx))
                return ExitCodes.UsageError;

            var targetPath = PathHelper.NormalizeSeparators(parseResult.GetValue(pathArg) ?? "");
            var showTo = parseResult.GetValue(toOption);
            var showFrom = parseResult.GetValue(fromOption);

            // Default: show both
            if (!showTo && !showFrom)
            {
                showTo = true;
                showFrom = true;
            }

            var graph = BuildReferenceGraph(ctx!.Files!, ctx.FileSystem, ctx.RootPath);

            if (ctx.OutputFormat == OutputFormat.Json)
            {
                var response = new RefsResponse
                {
                    Path = targetPath,
                    Outbound = showFrom ? GetOutbound(graph, targetPath) : [],
                    Inbound = showTo ? GetInbound(graph, targetPath) : []
                };
                JsonEnvelopeWriter.Write(ctx.Formatter, ctx.JsonEnvelope, "refs", true, ExitCodes.Success, response);
            }
            else
            {
                if (showFrom)
                {
                    var outbound = GetOutbound(graph, targetPath);
                    ctx.Formatter.WriteMessage($"Outbound links from '{targetPath}':");
                    if (outbound.Count == 0)
                        ctx.Formatter.WriteMessage("  (none)");
                    else
                        foreach (var link in outbound)
                            ctx.Formatter.WriteMessage($"  → {link}");
                }

                if (showTo)
                {
                    var inbound = GetInbound(graph, targetPath);
                    if (showFrom) ctx.Formatter.WriteMessage("");
                    ctx.Formatter.WriteMessage($"Inbound links to '{targetPath}':");
                    if (inbound.Count == 0)
                        ctx.Formatter.WriteMessage("  (none)");
                    else
                        foreach (var link in inbound)
                            ctx.Formatter.WriteMessage($"  ← {link}");
                }
            }

            return ExitCodes.Success;
        });

        return command;
    }

    internal static Dictionary<string, List<string>> BuildReferenceGraph(
        IReadOnlyList<DiscoveredFile> files,
        Core.Abstractions.IFileSystem fileSystem,
        string repositoryRoot)
    {
        // Key: source file, Value: list of resolved target paths
        var graph = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            if (!file.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                continue;

            var fullPath = Path.Combine(repositoryRoot, file.RelativePath);
            if (!fileSystem.FileExists(fullPath))
                continue;

            var content = fileSystem.ReadAllText(fullPath);
            var links = BrokenInternalLinkRule.ExtractInternalLinks(content);
            var relPath = PathHelper.NormalizeSeparators(file.RelativePath);

            var targets = new List<string>();
            foreach (var (target, _) in links)
            {
                var resolved = BrokenInternalLinkRule.ResolveLinkTarget(relPath, target);
                if (resolved != null)
                    targets.Add(resolved);
            }

            if (targets.Count > 0)
                graph[relPath] = targets;
        }

        return graph;
    }

    internal static List<string> GetOutbound(Dictionary<string, List<string>> graph, string path)
    {
        if (graph.TryGetValue(path, out var targets))
            return targets.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToList();
        return [];
    }

    internal static List<string> GetInbound(Dictionary<string, List<string>> graph, string path)
    {
        var inbound = new List<string>();
        foreach (var (source, targets) in graph)
        {
            if (targets.Any(t => string.Equals(t, path, StringComparison.OrdinalIgnoreCase)))
                inbound.Add(source);
        }
        return inbound.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
    }

    internal sealed class RefsResponse
    {
        public required string Path { get; init; }
        public List<string> Outbound { get; init; } = [];
        public List<string> Inbound { get; init; } = [];
    }
}
