using System.CommandLine;
using Steward.Cli.Formatting;
using Steward.Core;
using Steward.Core.Abstractions;
using Steward.Core.Markdown;

namespace Steward.Cli.Commands;

public static class MdSplitCommand
{
    public static Command Create()
    {
        var command = new Command("split", "Non-mutating split planning for large Markdown documents");
        command.Add(CreatePlanCommand());
        return command;
    }

    private static Command CreatePlanCommand()
    {
        var command = new Command("plan", "Analyze a Markdown document and propose candidate sections for extraction");

        var fileArg = new Argument<string>("file") { Description = "Path to the Markdown file to analyze" };
        var maxLinesOpt = new Option<int>("--max-lines")
        {
            Description = "Document line threshold above which extraction is suggested",
            DefaultValueFactory = _ => SplitPlanner.DefaultMaxLines
        };
        var minSectionLinesOpt = new Option<int>("--min-section-lines")
        {
            Description = "Minimum section size to propose for extraction",
            DefaultValueFactory = _ => SplitPlanner.DefaultMinSectionLines
        };

        command.Add(fileArg);
        command.Add(maxLinesOpt);
        command.Add(minSectionLinesOpt);

        command.SetAction(parseResult =>
        {
            var output = parseResult.GetValue(GlobalOptionsSetup.OutputOption);
            var noColor = parseResult.GetValue(GlobalOptionsSetup.NoColorOption);
            var jsonEnvelope = parseResult.GetValue(GlobalOptionsSetup.JsonEnvelopeOption);
            var file = parseResult.GetValue(fileArg)!;
            var maxLines = parseResult.GetValue(maxLinesOpt);
            var minSectionLines = parseResult.GetValue(minSectionLinesOpt);

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
            var plan = SplitPlanner.Plan(doc, maxLines, minSectionLines);

            if (output == OutputFormat.Json)
            {
                JsonEnvelopeWriter.Write(formatter, jsonEnvelope, "md split plan", true, ExitCodes.Success, new
                {
                    sourceFile = file,
                    totalLines = plan.TotalLines,
                    maxLines = plan.MaxLines,
                    minSectionLines = plan.MinSectionLines,
                    candidateCount = plan.Candidates.Count,
                    candidates = plan.Candidates.Select(c => new
                    {
                        heading = c.Heading,
                        level = c.Level,
                        selector = c.Selector,
                        lineCount = c.LineCount,
                        range = new { start = c.Range.Start, end = c.Range.End },
                        suggestedTargetPath = c.SuggestedTargetPath,
                        warnings = c.Warnings.Count > 0 ? c.Warnings : null
                    }).ToArray()
                });
            }
            else
            {
                formatter.WriteMessage($"Source: {file} ({plan.TotalLines} lines)");
                formatter.WriteMessage($"Thresholds: max-lines={plan.MaxLines}, min-section-lines={plan.MinSectionLines}");
                formatter.WriteMessage("");

                if (plan.Candidates.Count == 0)
                {
                    formatter.WriteMessage("No sections meet the extraction threshold.");
                }
                else
                {
                    formatter.WriteMessage($"{plan.Candidates.Count} candidate section(s) to extract:");
                    formatter.WriteMessage("");
                    foreach (var c in plan.Candidates)
                    {
                        var level = new string('#', c.Level);
                        formatter.WriteMessage($"  {level} {c.Heading}  ({c.LineCount} lines, lines {c.Range.Start}-{c.Range.End})");
                        formatter.WriteMessage($"    selector:  {c.Selector}");
                        formatter.WriteMessage($"    target:    {c.SuggestedTargetPath}");
                        foreach (var w in c.Warnings)
                            formatter.WriteMessage($"    warning:   {w}");
                        formatter.WriteMessage("");
                    }
                }
            }

            return ExitCodes.Success;
        });

        return command;
    }
}
