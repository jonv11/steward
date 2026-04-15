using System.CommandLine;
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
            if (!CommandSetup.TryBuild(parseResult, out var ctx))
                return ExitCodes.UsageError;

            var oldPath = parseResult.GetValue(oldArg) ?? "";
            var newPath = parseResult.GetValue(newArg) ?? "";
            var preview = parseResult.GetValue(previewOption);
            var apply = parseResult.GetValue(applyOption);

            if (!preview && !apply)
            {
                ctx!.Formatter.WriteError("Specify --preview to see changes, or --apply to execute.");
                return ExitCodes.UsageError;
            }

            var plan = MoveEngine.ComputeMove(oldPath, newPath, ctx!.Files!, ctx.FileSystem, ctx.RootPath);

            if (ctx.OutputFormat == OutputFormat.Json)
            {
                ctx.Formatter.WriteObject(new
                {
                    oldPath = plan.OldPath,
                    newPath = plan.NewPath,
                    edits = plan.Edits.Select(e => new { file = e.FilePath }).ToArray()
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

                if (apply)
                {
                    // Move the file
                    var srcFull = Path.Combine(ctx.RootPath, oldPath.Replace('/', Path.DirectorySeparatorChar));
                    var dstFull = Path.Combine(ctx.RootPath, newPath.Replace('/', Path.DirectorySeparatorChar));

                    var dstDir = Path.GetDirectoryName(dstFull);
                    if (dstDir != null && !Directory.Exists(dstDir))
                        Directory.CreateDirectory(dstDir);

                    if (File.Exists(srcFull))
                        File.Move(srcFull, dstFull);

                    // Update references
                    foreach (var edit in plan.Edits)
                    {
                        var editFull = Path.Combine(ctx.RootPath, edit.FilePath.Replace('/', Path.DirectorySeparatorChar));
                        File.WriteAllText(editFull, edit.NewContent);
                    }

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
}
