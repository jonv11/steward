using System.CommandLine;
using System.Globalization;
using Steward.Core;
using Steward.Core.Abstractions;
using Steward.Core.Formatting;
using Steward.Core.Markdown;

namespace Steward.Cli.Commands;

public static class MdCommand
{
    public static Command Create()
    {
        var command = new Command("md", "Markdown structural operations");

        command.Add(CreateQueryCommand());
        command.Add(CreateOutlineCommand());
        command.Add(MdEditCommand.Create());

        return command;
    }

    private static Command CreateQueryCommand()
    {
        var command = new Command("query", "Extract content using an MdPath selector (e.g., 'heading[Status]', 'frontmatter', 'heading[## Usage]')");

        var fileArg = new Argument<string?>("file") { Description = "Path to the Markdown file", Arity = ArgumentArity.ZeroOrOne };
        var selectorArg = new Argument<string?>("selector") { Description = "MdPath selector expression", Arity = ArgumentArity.ZeroOrOne };
        var patternOpt = new Option<string?>("--pattern") { Description = "Glob pattern to query multiple files (e.g. 'docs/**/*.md')" };

        command.Add(fileArg);
        command.Add(selectorArg);
        command.Add(patternOpt);

        command.SetAction((parseResult) =>
        {
            var output = parseResult.GetValue(GlobalOptionsSetup.OutputOption);
            var noColor = parseResult.GetValue(GlobalOptionsSetup.NoColorOption);
            var file = parseResult.GetValue(fileArg);
            var selector = parseResult.GetValue(selectorArg);
            var pattern = parseResult.GetValue(patternOpt);

            var formatter = CommandSetup.CreateFormatter(output, noColor);
            var fileSystem = new PhysicalFileSystem();

            // Disambiguate: when --pattern is used, the single positional arg is the
            // selector (not a file), because System.CommandLine assigns positionals
            // left-to-right and "file" (ZeroOrOne) captures first.
            if (pattern != null && selector == null && file != null)
            {
                selector = file;
                file = null;
            }

            if (string.IsNullOrWhiteSpace(selector))
            {
                formatter.WriteError("A selector expression is required.");
                return ExitCodes.UsageError;
            }

            // Batch mode: --pattern glob
            if (pattern != null)
            {
                return ExecuteBatchQuery(formatter, fileSystem, output, selector, pattern);
            }

            if (file == null)
            {
                formatter.WriteError("Provide a file argument or --pattern for batch query.");
                return ExitCodes.UsageError;
            }

            var fullPath = Path.GetFullPath(file);
            if (!fileSystem.FileExists(fullPath))
            {
                formatter.WriteError($"File not found: {file}");
                return ExitCodes.UsageError;
            }

            var content = fileSystem.ReadAllText(fullPath);
            var doc = MarkdownParser.Parse(fullPath, content);
            var result = MdPathSelector.Evaluate(doc, selector);

            if (result.IsError)
            {
                formatter.WriteError(result.ErrorMessage!);
                return ExitCodes.UsageError;
            }

            if (output == OutputFormat.Json)
            {
                formatter.WriteObject(new
                {
                    selector = result.Selector,
                    matchCount = result.Matches.Count,
                    matches = result.Matches.Select(m => new
                    {
                        kind = m.Kind.ToString().ToLowerInvariant(),
                        label = m.Label,
                        range = new { start = m.Range.Start, end = m.Range.End },
                        content = m.Content
                    }).ToArray()
                });
            }
            else
            {
                if (result.IsEmpty)
                {
                    formatter.WriteMessage($"No matches for selector '{result.Selector}'.");
                }
                else
                {
                    foreach (var match in result.Matches)
                    {
                        formatter.WriteMessage(match.Content);
                    }
                }
            }

            return ExitCodes.Success;
        });

        return command;
    }

    private static int ExecuteBatchQuery(IOutputFormatter formatter, IFileSystem fileSystem,
        OutputFormat output, string selector, string pattern)
    {
        var root = Directory.GetCurrentDirectory();
        var glob = DotNet.Globbing.Glob.Parse(pattern);
        var allFiles = Directory.EnumerateFiles(root, "*.md", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(root, f).Replace('\\', '/'))
            .Where(f => glob.IsMatch(f))
            .OrderBy(f => f)
            .ToList();

        if (allFiles.Count == 0)
        {
            formatter.WriteError($"No files matched pattern '{pattern}'.");
            return ExitCodes.UsageError;
        }

        var allResults = new List<object>();

        foreach (var relativePath in allFiles)
        {
            var fullPath = Path.Combine(root, relativePath);
            var content = fileSystem.ReadAllText(fullPath);
            var doc = MarkdownParser.Parse(fullPath, content);
            var result = MdPathSelector.Evaluate(doc, selector);

            if (result.IsError || result.IsEmpty) continue;

            if (output == OutputFormat.Json)
            {
                allResults.Add(new
                {
                    file = relativePath,
                    selector = result.Selector,
                    matches = result.Matches.Select(m => new
                    {
                        kind = m.Kind.ToString().ToLowerInvariant(),
                        label = m.Label,
                        content = m.Content
                    }).ToArray()
                });
            }
            else
            {
                foreach (var match in result.Matches)
                {
                    formatter.WriteMessage($"--- {relativePath} ---");
                    formatter.WriteMessage(match.Content);
                    formatter.WriteMessage("");
                }
            }
        }

        if (output == OutputFormat.Json)
        {
            formatter.WriteObject(new { pattern, selector, results = allResults });
        }

        return ExitCodes.Success;
    }

    private static Command CreateOutlineCommand()
    {
        var command = new Command("outline", "Show heading hierarchy with line counts");

        var fileArg = new Argument<string>("file") { Description = "Path to the Markdown file" };
        command.Add(fileArg);

        command.SetAction((parseResult) =>
        {
            var output = parseResult.GetValue(GlobalOptionsSetup.OutputOption);
            var noColor = parseResult.GetValue(GlobalOptionsSetup.NoColorOption);
            var file = parseResult.GetValue(fileArg)!;

            var formatter = CommandSetup.CreateFormatter(output, noColor);
            var fileSystem = new PhysicalFileSystem();

            var fullPath = Path.GetFullPath(file);
            if (!fileSystem.FileExists(fullPath))
            {
                formatter.WriteError($"File not found: {file}");
                return ExitCodes.UsageError;
            }

            var content = fileSystem.ReadAllText(fullPath);
            var doc = MarkdownParser.Parse(fullPath, content);

            if (output == OutputFormat.Json)
            {
                formatter.WriteObject(new
                {
                    file = file,
                    totalLines = doc.TotalLines,
                    hasFrontmatter = doc.Frontmatter != null,
                    sections = FlattenForJson(doc.Sections)
                });
            }
            else
            {
                if (doc.Frontmatter != null)
                {
                    var fmLines = doc.Frontmatter.Range.End - doc.Frontmatter.Range.Start + 1;
                    formatter.WriteMessage($"  [frontmatter] ({fmLines} lines)");
                }

                WriteOutlineText(formatter, doc.Sections, 0);
                formatter.WriteMessage("");
                formatter.WriteMessage(string.Format(CultureInfo.InvariantCulture,
                    "Total: {0} lines", doc.TotalLines));
            }

            return ExitCodes.Success;
        });

        return command;
    }

    private static void WriteOutlineText(IOutputFormatter formatter,
        IReadOnlyList<Section> sections, int indent)
    {
        foreach (var section in sections)
        {
            var prefix = new string(' ', indent * 2 + 2);
            var hashes = new string('#', section.Level);
            formatter.WriteMessage($"{prefix}{hashes} {section.Heading} ({section.LineCount} lines)");
            WriteOutlineText(formatter, section.Children, indent + 1);
        }
    }

    private static object[] FlattenForJson(IReadOnlyList<Section> sections)
    {
        var result = new List<object>();
        FlattenForJsonRecursive(sections, result);
        return result.ToArray();
    }

    private static void FlattenForJsonRecursive(IReadOnlyList<Section> sections, List<object> result)
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
            FlattenForJsonRecursive(s.Children, result);
        }
    }

}
