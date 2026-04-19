using System.CommandLine;
using Steward.Cli.Formatting;
using Steward.Core;
using Steward.Core.Maintenance;

namespace Steward.Cli.Commands;

public static class RefactorCommand
{
    public static Command Create()
    {
        var command = new Command("refactor", "Refactoring operations for governed files");

        command.Add(CreateMoveSubcommand());

        return command;
    }

    private static Command CreateMoveSubcommand()
    {
        var moveCmd = new Command("move", "Move/rename a file and update all Markdown references");

        var oldArg = new Argument<string>("old-path") { Description = "Current file path (relative to repo root)" };
        var newArg = new Argument<string>("new-path") { Description = "New file path (relative to repo root)" };
        var previewOption = new Option<bool>("--preview") { Description = "Show proposed changes without applying" };
        var applyOption = new Option<bool>("--apply") { Description = "Apply the move and update references" };

        moveCmd.Add(oldArg);
        moveCmd.Add(newArg);
        moveCmd.Add(previewOption);
        moveCmd.Add(applyOption);

        moveCmd.SetAction(parseResult =>
        {
            if (!CommandSetup.TryBuild(parseResult, out var ctx, "refactor move"))
                return ExitCodes.UsageError;

            var oldPath = parseResult.GetValue(oldArg) ?? "";
            var newPath = parseResult.GetValue(newArg) ?? "";
            var preview = parseResult.GetValue(previewOption);
            var apply = parseResult.GetValue(applyOption);

            if (!preview && !apply)
            {
                if (ctx!.OutputFormat == OutputFormat.Json)
                {
                    JsonEnvelopeWriter.WriteError(ctx.Formatter, "refactor move", ExitCodes.UsageError,
                        "missing-mode", "Specify --preview to see changes, or --apply to execute.",
                        suggestedNextStep: "Add --preview or --apply to the command.");
                    return ExitCodes.UsageError;
                }
                ctx!.Formatter.WriteError("Specify --preview to see changes, or --apply to execute.");
                return ExitCodes.UsageError;
            }

            var srcFull = Path.Combine(ctx!.RootPath, oldPath.Replace('/', Path.DirectorySeparatorChar));
            var dstFull = Path.Combine(ctx.RootPath, newPath.Replace('/', Path.DirectorySeparatorChar));
            var sourceExists = File.Exists(srcFull);
            var destinationExists = File.Exists(dstFull);

            var plan = MoveEngine.ComputeMove(oldPath, newPath, ctx.Files!, ctx.FileSystem, ctx.RootPath);

            // Execute apply logic regardless of output format (CC-04 fix)
            bool applied = false;
            if (apply)
            {
                if (!sourceExists)
                {
                    if (ctx.OutputFormat == OutputFormat.Json)
                    {
                        JsonEnvelopeWriter.WriteError(ctx.Formatter, "refactor move", ExitCodes.UsageError,
                            "file-not-found", $"Source file not found: {oldPath}",
                            details: new Dictionary<string, object> { ["path"] = oldPath });
                        return ExitCodes.UsageError;
                    }
                    ctx.Formatter.WriteError($"Source file not found: {oldPath}");
                    return ExitCodes.UsageError;
                }

                if (destinationExists)
                {
                    if (ctx.OutputFormat == OutputFormat.Json)
                    {
                        JsonEnvelopeWriter.WriteError(ctx.Formatter, "refactor move", ExitCodes.UsageError,
                            "destination-exists", $"Destination file already exists: {newPath}",
                            details: new Dictionary<string, object> { ["path"] = newPath });
                        return ExitCodes.UsageError;
                    }
                    ctx.Formatter.WriteError($"Destination file already exists: {newPath}");
                    return ExitCodes.UsageError;
                }

                // Move the file
                var dstDir = Path.GetDirectoryName(dstFull);
                if (dstDir != null && !Directory.Exists(dstDir))
                    Directory.CreateDirectory(dstDir);

                File.Move(srcFull, dstFull);

                // Update references
                foreach (var edit in plan.Edits)
                {
                    var editFull = Path.Combine(ctx.RootPath, edit.FilePath.Replace('/', Path.DirectorySeparatorChar));
                    File.WriteAllText(editFull, edit.NewContent);
                }

                applied = true;
            }

            if (ctx.OutputFormat == OutputFormat.Json)
            {
                var edits = plan.Edits.Select(e =>
                {
                    // Compute per-edit link rewrite details
                    var rewrites = ComputeRewrites(e);
                    return new
                    {
                        file = e.FilePath,
                        linkCount = rewrites.Count,
                        rewrites = rewrites.Select(r => new
                        {
                            line = r.Line,
                            oldLink = r.OldLink,
                            newLink = r.NewLink
                        }).ToArray()
                    };
                }).ToArray();

                JsonEnvelopeWriter.Write(ctx.Formatter, "refactor move", true,
                    ExitCodes.Success, new
                    {
                        oldPath = plan.OldPath,
                        newPath = plan.NewPath,
                        sourceExists,
                        destinationExists = apply ? false : destinationExists, // after apply, dest was the source
                        collision = !apply && destinationExists,
                        applied,
                        affectedFileCount = plan.Edits.Count,
                        edits
                    });
            }
            else
            {
                ctx.Formatter.WriteMessage($"Move: {plan.OldPath} → {plan.NewPath}");

                if (plan.Edits.Count == 0)
                {
                    ctx.Formatter.WriteMessage("No Markdown references to update.");
                }
                else
                {
                    ctx.Formatter.WriteMessage($"{plan.Edits.Count} file(s) with reference updates:");
                    foreach (var edit in plan.Edits)
                    {
                        ctx.Formatter.WriteMessage($"  {edit.FilePath}");
                    }
                }

                if (applied)
                {
                    ctx.Formatter.WriteMessage("\nMove applied successfully.");
                }
                else
                {
                    ctx.Formatter.WriteMessage("\nPreview only. Use --apply to execute.");
                }
            }

            return ExitCodes.Success;
        });

        return moveCmd;
    }

    private static List<(int Line, string OldLink, string NewLink)> ComputeRewrites(MoveEngine.MoveEdit edit)
    {
        var rewrites = new List<(int, string, string)>();
        var oldLines = edit.OldContent.Split('\n');
        var newLines = edit.NewContent.Split('\n');

        for (int i = 0; i < Math.Min(oldLines.Length, newLines.Length); i++)
        {
            if (oldLines[i] != newLines[i])
            {
                // Extract the changed link references from this line
                var oldLinks = ExtractMarkdownLinks(oldLines[i]);
                var newLinksList = ExtractMarkdownLinks(newLines[i]);

                for (int j = 0; j < Math.Min(oldLinks.Count, newLinksList.Count); j++)
                {
                    if (oldLinks[j] != newLinksList[j])
                    {
                        rewrites.Add((i + 1, oldLinks[j], newLinksList[j]));
                    }
                }
            }
        }

        return rewrites;
    }

    private static List<string> ExtractMarkdownLinks(string line)
    {
        var links = new List<string>();
        var idx = 0;
        while (idx < line.Length)
        {
            var start = line.IndexOf("](", idx, StringComparison.Ordinal);
            if (start < 0) break;
            start += 2;
            var end = line.IndexOf(')', start);
            if (end < 0) break;
            links.Add(line[start..end]);
            idx = end + 1;
        }
        return links;
    }
}
