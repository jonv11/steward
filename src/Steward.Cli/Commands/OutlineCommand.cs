using System.CommandLine;
using Steward.Core;
using Steward.Core.Abstractions;
using Steward.Core.Discovery;
using Steward.Core.Formatting;
using Steward.Core.Markdown;
using Steward.Core.Orientation;

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

                    // Show heading outline for .md files when --headings is enabled
                    if (headings && !entry.IsDirectory && entry.Path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                    {
                        var fullFilePath = Path.Combine(rootPath, entry.Path);
                        WriteInlineHeadings(fullFilePath, fileSystem, formatter, entry.Depth + 1);
                    }
                }
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
                formatter.WriteMessage($"  [frontmatter] ({fmLines} lines)");
            }

            WriteSectionsText(formatter, doc.Sections, 0);
            formatter.WriteMessage("");
            formatter.WriteMessage($"Total: {doc.TotalLines} lines");
        }

        return ExitCodes.Success;
    }

    private static void WriteInlineHeadings(string fullPath, IFileSystem fileSystem,
        IOutputFormatter formatter, int baseIndent)
    {
        try
        {
            var content = fileSystem.ReadAllText(fullPath);
            var doc = MarkdownParser.Parse(fullPath, content);
            WriteSectionsText(formatter, doc.Sections, baseIndent);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Skip unreadable files silently
        }
    }

    private static void WriteSectionsText(IOutputFormatter formatter,
        IReadOnlyList<Section> sections, int indent)
    {
        foreach (var section in sections)
        {
            var prefix = new string(' ', indent * 2 + 2);
            var hashes = new string('#', section.Level);
            formatter.WriteMessage($"{prefix}{hashes} {section.Heading} ({section.LineCount} lines)");
            WriteSectionsText(formatter, section.Children, indent + 1);
        }
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
