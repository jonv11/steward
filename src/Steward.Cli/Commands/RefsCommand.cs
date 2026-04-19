using System.CommandLine;
using Steward.Cli.Formatting;
using Steward.Core;
using Steward.Core.Discovery;
using Steward.Core.Markdown;
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
            if (!CommandSetup.TryBuild(parseResult, out var ctx, "refs"))
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

            var linkInstances = BuildReferenceLinks(ctx!.Files!, ctx.FileSystem, ctx.RootPath);
            var graph = BuildReferenceGraph(linkInstances);

            if (ctx.OutputFormat == OutputFormat.Json)
            {
                var response = new RefsResponse
                {
                    Path = targetPath,
                    Outbound = showFrom ? GetOutbound(graph, targetPath) : [],
                    Inbound = showTo ? GetInbound(graph, targetPath) : [],
                    OutboundLinks = showFrom ? GetOutboundLinks(linkInstances, targetPath) : [],
                    InboundLinks = showTo ? GetInboundLinks(linkInstances, targetPath) : []
                };
                JsonEnvelopeWriter.Write(ctx.Formatter, "refs", true, ExitCodes.Success, response);
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
        IReadOnlyList<ReferenceLink> linkInstances)
    {
        return linkInstances
            .GroupBy(link => link.SourcePath, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(link => link.ResolvedPath)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);
    }

    internal static List<ReferenceLink> BuildReferenceLinks(
        IReadOnlyList<DiscoveredFile> files,
        Core.Abstractions.IFileSystem fileSystem,
        string repositoryRoot)
    {
        var links = new List<ReferenceLink>();

        foreach (var file in files)
        {
            if (!file.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                continue;

            var fullPath = Path.Combine(repositoryRoot, file.RelativePath);
            if (!fileSystem.FileExists(fullPath))
                continue;

            var content = fileSystem.ReadAllText(fullPath);
            var relPath = PathHelper.NormalizeSeparators(file.RelativePath);
            var document = MarkdownParser.Parse(relPath, content);
            var extractedLinks = BrokenInternalLinkRule.ExtractInternalLinkReferences(content);

            foreach (var link in extractedLinks)
            {
                var resolved = BrokenInternalLinkRule.ResolveLinkTarget(relPath, link.Target);
                if (resolved == null)
                    continue;

                string? selector = null;
                if (MarkdownHeadings.TryFindSectionAtLine(document.Sections, link.Line, out var section, out var headingPath) &&
                    section != null)
                {
                    selector = MarkdownHeadings.TryCreateSafeSelector(document, headingPath, section);
                }

                links.Add(new ReferenceLink
                {
                    SourcePath = relPath,
                    SourceLine = link.Line,
                    LinkText = link.LinkText,
                    RawTarget = link.RawTarget,
                    ResolvedPath = resolved,
                    Fragment = link.Fragment,
                    MdQuerySelector = selector
                });
            }
        }

        return links;
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

    internal static List<ReferenceLink> GetOutboundLinks(IReadOnlyList<ReferenceLink> links, string sourcePath)
    {
        return
        [
            .. links
                .Where(link => string.Equals(link.SourcePath, sourcePath, StringComparison.OrdinalIgnoreCase))
                .OrderBy(link => link.SourceLine)
                .ThenBy(link => link.ResolvedPath, StringComparer.OrdinalIgnoreCase)
        ];
    }

    internal static List<ReferenceLink> GetInboundLinks(IReadOnlyList<ReferenceLink> links, string targetPath)
    {
        return
        [
            .. links
                .Where(link => string.Equals(link.ResolvedPath, targetPath, StringComparison.OrdinalIgnoreCase))
                .OrderBy(link => link.SourcePath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(link => link.SourceLine)
        ];
    }

    internal sealed class RefsResponse
    {
        public required string Path { get; init; }
        public List<string> Outbound { get; init; } = [];
        public List<string> Inbound { get; init; } = [];
        public List<ReferenceLink> OutboundLinks { get; init; } = [];
        public List<ReferenceLink> InboundLinks { get; init; } = [];
    }

    internal sealed class ReferenceLink
    {
        public required string SourcePath { get; init; }
        public required int SourceLine { get; init; }
        public string LinkText { get; init; } = "";
        public required string RawTarget { get; init; }
        public required string ResolvedPath { get; init; }
        public string? Fragment { get; init; }
        public string? MdQuerySelector { get; init; }
    }
}
