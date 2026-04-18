using System.CommandLine;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Steward.Cli.Formatting;
using Steward.Core;
using Steward.Core.Formatting;
using Steward.Core.Maintenance;
using Steward.Core.Markdown;

namespace Steward.Cli.Commands;

public static class MaintainCommand
{
    public static Command Create()
    {
        var command = new Command("maintain", "Deterministic maintenance of governed artifacts");

        var scopeOption = new Option<string?>("--artifact", "-a")
        {
            Description = "Maintain only the artifact with this id (from policy.yaml maintenance.artifacts)"
        };

        var applyOption = new Option<bool>("--apply")
        {
            Description = "Apply maintenance changes (default is preview)"
        };

        var diffOption = new Option<bool>("--diff")
        {
            Description = "Show unified diff for each changed artifact in preview mode"
        };

        command.Add(scopeOption);
        command.Add(applyOption);
        command.Add(diffOption);

        command.SetAction(parseResult =>
        {
            if (!CommandSetup.TryBuild(parseResult, out var ctx))
                return ExitCodes.UsageError;

            var scope = parseResult.GetValue(scopeOption);
            var apply = parseResult.GetValue(applyOption);
            var showDiff = parseResult.GetValue(diffOption);

            if (ctx!.ConfigDirectory == null)
            {
                if (ctx.OutputFormat == OutputFormat.Json)
                {
                    JsonEnvelopeWriter.WriteError(ctx.Formatter, ctx.JsonEnvelope, "maintain", ExitCodes.UsageError,
                        "config-not-found", "No .steward configuration directory found. Run 'steward init' first.");
                    return ExitCodes.UsageError;
                }
                ctx.Formatter.WriteError("No .steward configuration directory found. Run 'steward init' first.");
                return ExitCodes.UsageError;
            }

            // Create maintenance context
            var docCache = new DocumentCache(ctx.FileSystem, ctx.RootPath);
            var context = new MaintenanceContext
            {
                RepositoryRoot = ctx.RootPath,
                FileSystem = ctx.FileSystem,
                Files = ctx.Files!,
                DocumentCache = docCache
            };

            // Evaluate
            var engine = new MaintenanceEngine();
            var plan = engine.Evaluate(ctx.Policy, context, scope);

            // Apply changes if requested
            var appliedChanges = new List<(string path, int added, int removed)>();
            if (apply && plan.HasChanges)
            {
                foreach (var action in plan.Actions.Where(a => a.HasChanges && a.ExpectedContent != null))
                {
                    var fullPath = Path.Combine(ctx.RootPath, action.ArtifactPath);
                    var dir = Path.GetDirectoryName(fullPath);
                    if (dir != null && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    var oldContent = File.Exists(fullPath) ? File.ReadAllText(fullPath) : "";
                    File.WriteAllText(fullPath, action.ExpectedContent);

                    var (added, removed) = CountDiffLines(oldContent, action.ExpectedContent!);
                    appliedChanges.Add((action.ArtifactPath, added, removed));
                }

                foreach (var action in plan.Actions.Where(a => a.HasChanges && a.FileEdits.Count > 0))
                {
                    foreach (var edit in action.FileEdits)
                    {
                        var fullPath = Path.Combine(ctx.RootPath, edit.FilePath);
                        var dir = Path.GetDirectoryName(fullPath);
                        if (dir != null && !Directory.Exists(dir))
                            Directory.CreateDirectory(dir);

                        var oldContent = File.Exists(fullPath) ? File.ReadAllText(fullPath) : "";
                        File.WriteAllText(fullPath, edit.ExpectedContent);

                        var (added, removed) = CountDiffLines(oldContent, edit.ExpectedContent);
                        appliedChanges.Add((edit.FilePath, added, removed));
                    }
                }
            }

            // Output — single pass for both text and JSON
            if (ctx.OutputFormat == OutputFormat.Json)
            {
                JsonEnvelopeWriter.Write(ctx.Formatter, ctx.JsonEnvelope, "maintain", true, ExitCodes.Success, new
                {
                    hasChanges = plan.HasChanges,
                    applied = apply,
                    actions = plan.Actions.Select(a => new
                    {
                        artifactId = a.ArtifactId,
                        artifactPath = a.ArtifactPath,
                        type = a.Type,
                        description = a.Description,
                        hasChanges = a.HasChanges,
                        blocked = a.IsBlocked,
                        blockedReason = a.BlockedReason,
                        fileEdits = a.FileEdits.Select(edit => new
                        {
                            path = edit.FilePath,
                            description = edit.Description
                        }).ToArray()
                    }).ToArray(),
                    changes = apply && appliedChanges.Count > 0
                        ? appliedChanges.Select(c => new
                        {
                            path = c.path,
                            linesAdded = c.added,
                            linesRemoved = c.removed
                        }).ToArray()
                        : null
                });
            }
            else
            {
                if (plan.Actions.Count == 0)
                {
                    ctx.Formatter.WriteMessage("No maintenance artifacts configured.");
                    return ExitCodes.Success;
                }

                foreach (var action in plan.Actions)
                {
                    var status = action.IsBlocked
                        ? "BLOCKED "
                        : action.HasChanges ? "MAINTAIN" : "OK      ";
                    ctx.Formatter.WriteMessage($"{status}  {action.ArtifactId}  {action.ArtifactPath}");
                    ctx.Formatter.WriteMessage($"  {action.Description}");
                    if (action.IsBlocked && action.BlockedReason != null)
                        ctx.Formatter.WriteMessage($"  {action.BlockedReason}");

                    // --diff works in both preview and apply modes
                    if (showDiff && action.HasChanges && action.CurrentContent != null && action.ExpectedContent != null)
                    {
                        var diffBuilder = new InlineDiffBuilder(new Differ());
                        var diff = diffBuilder.BuildDiffModel(action.CurrentContent, action.ExpectedContent);
                        foreach (var line in diff.Lines)
                        {
                            var prefix = line.Type switch
                            {
                                ChangeType.Inserted => "+",
                                ChangeType.Deleted => "-",
                                _ => " "
                            };
                            if (line.Type != ChangeType.Unchanged)
                                ctx.Formatter.WriteMessage($"  {prefix}{line.Text}");
                        }
                    }

                    if (showDiff && action.FileEdits.Count > 0)
                    {
                        foreach (var edit in action.FileEdits)
                        {
                            ctx.Formatter.WriteMessage($"  diff: {edit.FilePath}");
                            var diffBuilder = new InlineDiffBuilder(new Differ());
                            var diff = diffBuilder.BuildDiffModel(edit.CurrentContent ?? string.Empty, edit.ExpectedContent);
                            foreach (var line in diff.Lines)
                            {
                                var prefix = line.Type switch
                                {
                                    ChangeType.Inserted => "+",
                                    ChangeType.Deleted => "-",
                                    _ => " "
                                };

                                if (line.Type != ChangeType.Unchanged)
                                    ctx.Formatter.WriteMessage($"  {prefix}{line.Text}");
                            }
                        }
                    }
                }

                ctx.Formatter.WriteMessage("");

                if (apply && appliedChanges.Count > 0)
                {
                    foreach (var (path, added, removed) in appliedChanges)
                        ctx.Formatter.WriteMessage($"  {path}  (+{added} -{removed})");
                    ctx.Formatter.WriteMessage($"\nChanges applied: {appliedChanges.Count} file(s) updated.");
                }
                else if (!apply && plan.HasChanges)
                {
                    ctx.Formatter.WriteMessage("No changes applied. Run with --apply to commit changes.");
                }
            }

            return ExitCodes.Success;
        });

        return command;
    }

    internal static (int added, int removed) CountDiffLines(string oldContent, string newContent)
    {
        var diffBuilder = new InlineDiffBuilder(new Differ());
        var diff = diffBuilder.BuildDiffModel(oldContent, newContent);
        var added = diff.Lines.Count(l => l.Type == ChangeType.Inserted);
        var removed = diff.Lines.Count(l => l.Type == ChangeType.Deleted);
        return (added, removed);
    }
}
