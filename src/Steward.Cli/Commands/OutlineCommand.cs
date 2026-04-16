using System.CommandLine;
using Steward.Core;
using Steward.Core.Abstractions;
using Steward.Core.Discovery;
using Steward.Core.Formatting;
using Steward.Core.Markdown;
using Steward.Core.Orientation;
using Steward.Cli.Formatting;

namespace Steward.Cli.Commands;

public static class OutlineCommand
{
    public static Command Create()
    {
        var command = new Command("outline", "Show the repository file tree");

        var pathArgument = new Argument<string>("path")
        {
            Description = "Root path to outline",
            DefaultValueFactory = _ => "."
        };
        command.Add(pathArgument);

        var depthOption = new Option<int>("--depth", "-d")
        {
            Description = "Maximum depth to display (default: unlimited)",
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

        var countsOption = new Option<bool>("--counts")
        {
            Description = "Include recursive file and directory counts for folders"
        };
        command.Add(countsOption);

        var headingsOption = new Option<bool>("--headings")
        {
            Description = "Include Markdown heading outlines for .md files"
        };
        command.Add(headingsOption);

        command.SetAction((parseResult) =>
        {
            var path = parseResult.GetValue(pathArgument);
            var depth = parseResult.GetValue(depthOption);
            var sizes = parseResult.GetValue(sizesOption);
            var lines = parseResult.GetValue(linesOption);
            var counts = parseResult.GetValue(countsOption);
            var headings = parseResult.GetValue(headingsOption);

            var output = parseResult.GetValue(GlobalOptionsSetup.OutputOption);
            var noColor = parseResult.GetValue(GlobalOptionsSetup.NoColorOption);
            var formatter = CommandSetup.CreateFormatter(output, noColor);
            var fileSystem = new PhysicalFileSystem();

            // outline takes an explicit path argument, not necessarily CWD, so we
            // cannot use CommandSetup.Build() (which roots discovery at CWD). We
            // wire up discovery directly against the resolved target path instead.
            var rootPath = Path.GetFullPath(path ?? ".");

            // If the path is a file, delegate to markdown outline for .md files
            // or show a helpful error for other file types.
            if (File.Exists(rootPath))
            {
                if (rootPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                {
                    return ShowMarkdownOutline(rootPath, fileSystem, formatter, output);
                }
                else
                {
                    formatter.WriteError($"Cannot outline a non-Markdown file: {path}");
                    formatter.WriteError("Use 'steward outline <directory>' for directory trees, or 'steward md outline <file>' for Markdown files.");
                    return ExitCodes.UsageError;
                }
            }

            if (!Directory.Exists(rootPath))
            {
                formatter.WriteError($"Path does not exist: {path}");
                return ExitCodes.UsageError;
            }

            var ignoreFilter = GitIgnoreFilter.Load(rootPath, fileSystem);
            var discoveryService = new FileDiscoveryService(fileSystem, ignoreFilter);
            var files = discoveryService.Discover(rootPath);

            var engine = new OutlineEngine(fileSystem);
            var result = engine.BuildOutline(rootPath, files, depth, sizes, lines, counts);

            if (output == OutputFormat.Json)
            {
                formatter.WriteObject(result);
            }
            else
            {
                WriteDirectoryOutline(result.Entries, rootPath, fileSystem, formatter, headings);
            }

            return ExitCodes.Success;
        });

        return command;
    }

    private static int ShowMarkdownOutline(string fullPath, IFileSystem fileSystem,
        IOutputFormatter formatter, OutputFormat output)
    {
        var content = fileSystem.ReadAllText(fullPath);
        var doc = MarkdownParser.Parse(fullPath, content);

        if (output == OutputFormat.Json)
        {
            formatter.WriteObject(new
            {
                file = Path.GetFileName(fullPath),
                totalLines = doc.TotalLines,
                hasFrontmatter = doc.Frontmatter != null,
                sections = FlattenSectionsForJson(doc.Sections)
            });
        }
        else
        {
            if (doc.Frontmatter != null)
            {
                var fmLines = doc.Frontmatter.Range.End - doc.Frontmatter.Range.Start + 1;
                formatter.WriteMessage($"  {OutputStyler.Style(formatter, "[frontmatter]", CliTextStyle.Muted)} ({fmLines} lines)");
            }

            WriteSectionsText(formatter, doc.Sections, "  ", 0);
            formatter.WriteMessage("");
            formatter.WriteMessage($"Total: {doc.TotalLines} lines");
        }

        return ExitCodes.Success;
    }

    private static void WriteDirectoryOutline(
        IReadOnlyList<OutlineEntry> entries,
        string rootPath,
        IFileSystem fileSystem,
        IOutputFormatter formatter,
        bool headings)
    {
        var rootNodes = BuildTree(entries);
        WriteTreeNodes(rootNodes, rootPath, fileSystem, formatter, headings, prefix: "");
    }

    private static void WriteTreeNodes(
        IReadOnlyList<OutlineTreeNode> nodes,
        string rootPath,
        IFileSystem fileSystem,
        IOutputFormatter formatter,
        bool headings,
        string prefix)
    {
        for (var i = 0; i < nodes.Count; i++)
        {
            var isLast = i == nodes.Count - 1;
            var connector = isLast ? "└── " : "├── ";
            var childPrefix = prefix + (isLast ? "    " : "│   ");

            formatter.WriteMessage($"{prefix}{OutputStyler.Style(formatter, connector, CliTextStyle.Muted)}{FormatOutlineLabel(nodes[i].Entry, formatter)}");

            if (headings &&
                !nodes[i].Entry.IsDirectory &&
                nodes[i].Entry.Path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                var fullFilePath = Path.Combine(rootPath, nodes[i].Entry.Path);
                WriteInlineHeadings(fullFilePath, fileSystem, formatter, childPrefix);
            }

            if (nodes[i].Children.Count > 0)
                WriteTreeNodes(nodes[i].Children, rootPath, fileSystem, formatter, headings, childPrefix);
        }
    }

    private static string FormatOutlineLabel(OutlineEntry entry, IOutputFormatter formatter)
    {
        var suffix = entry.IsDirectory ? "/" : "";
        var name = Path.GetFileName(entry.Path) + suffix;
        var styledName = entry.IsDirectory
            ? OutputStyler.Style(formatter, name, CliTextStyle.Directory)
            : name;

        var extras = new List<string>();
        if (entry.FileCount.HasValue)
            extras.Add(FormatCountSummary(entry.FileCount.Value, entry.DirectoryCount ?? 0));
        if (entry.Size.HasValue)
            extras.Add(OutlineEngine.FormatSize(entry.Size.Value));
        if (entry.LineCount.HasValue)
            extras.Add($"{entry.LineCount.Value} lines");

        return extras.Count > 0
            ? $"{styledName} {OutputStyler.Style(formatter, $"({string.Join(", ", extras)})", CliTextStyle.Muted)}"
            : styledName;
    }

    private static string FormatCountSummary(int fileCount, int directoryCount)
    {
        var parts = new List<string> { fileCount == 1 ? "1 file" : $"{fileCount} files" };
        if (directoryCount > 0)
            parts.Add(directoryCount == 1 ? "1 dir" : $"{directoryCount} dirs");
        return string.Join(", ", parts);
    }

    private static void WriteInlineHeadings(string fullPath, IFileSystem fileSystem,
        IOutputFormatter formatter, string prefix)
    {
        try
        {
            var content = fileSystem.ReadAllText(fullPath);
            var doc = MarkdownParser.Parse(fullPath, content);
            WriteSectionsText(formatter, doc.Sections, prefix, 0);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Skip unreadable files silently
        }
    }

    private static void WriteSectionsText(IOutputFormatter formatter,
        IReadOnlyList<Section> sections, string prefix, int indent)
    {
        foreach (var section in sections)
        {
            var sectionIndent = new string(' ', indent * 2);
            var hashes = new string('#', section.Level);
            var line = $"{prefix}{sectionIndent}{OutputStyler.Style(formatter, hashes, CliTextStyle.Accent)} {section.Heading} {OutputStyler.Style(formatter, $"({section.LineCount} lines)", CliTextStyle.Muted)}";
            formatter.WriteMessage(line);
            WriteSectionsText(formatter, section.Children, prefix, indent + 1);
        }
    }

    private static List<OutlineTreeNode> BuildTree(IReadOnlyList<OutlineEntry> entries)
    {
        var rootNodes = new List<OutlineTreeNode>();
        var nodeLookup = new Dictionary<string, OutlineTreeNode>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries.OrderBy(static entry => entry.Path, StringComparer.OrdinalIgnoreCase))
        {
            var node = new OutlineTreeNode(entry);
            nodeLookup[entry.Path] = node;

            var parentPath = Path.GetDirectoryName(entry.Path)?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(parentPath) || !nodeLookup.TryGetValue(parentPath, out var parent))
            {
                rootNodes.Add(node);
            }
            else
            {
                parent.Children.Add(node);
            }
        }

        return rootNodes;
    }

    private sealed class OutlineTreeNode(OutlineEntry entry)
    {
        public OutlineEntry Entry { get; } = entry;
        public List<OutlineTreeNode> Children { get; } = [];
    }

    private static object[] FlattenSectionsForJson(IReadOnlyList<Section> sections)
    {
        var result = new List<object>();
        FlattenSectionsRecursive(sections, result);
        return result.ToArray();
    }

    private static void FlattenSectionsRecursive(IReadOnlyList<Section> sections, List<object> result)
    {
        foreach (var s in sections)
        {
            result.Add(new
            {
                heading = s.Heading,
                level = s.Level,
                lineCount = s.LineCount,
                range = new { start = s.Range.Start, end = s.Range.End }
            });
            FlattenSectionsRecursive(s.Children, result);
        }
    }
}
