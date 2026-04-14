using System.CommandLine;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
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

        var scopeOption = new Option<string?>("--scope", "-s")
        {
            Description = "Maintain only a specific artifact by id"
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

            if (ctx.OutputFormat == OutputFormat.Json)
            {
                ctx.Formatter.WriteObject(new
                {
                    hasChanges = plan.HasChanges,
                    actions = plan.Actions.Select(a => new
                    {
                        artifactId = a.ArtifactId,
                        artifactPath = a.ArtifactPath,
                        type = a.Type,
                        description = a.Description,
                        hasChanges = a.HasChanges
                    }).ToArray()
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
                    var status = action.HasChanges ? "MAINTAIN" : "OK      ";
                    ctx.Formatter.WriteMessage($"{status}  {action.ArtifactId}  {action.ArtifactPath}");
                    ctx.Formatter.WriteMessage($"  {action.Description}");

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
                }

                ctx.Formatter.WriteMessage("");
            }

            if (apply && plan.HasChanges)
            {
                foreach (var action in plan.Actions.Where(a => a.HasChanges && a.ExpectedContent != null))
                {
                    var fullPath = Path.Combine(ctx.RootPath, action.ArtifactPath);
                    var dir = Path.GetDirectoryName(fullPath);
                    if (dir != null && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    File.WriteAllText(fullPath, action.ExpectedContent);
                }

                if (ctx.OutputFormat != OutputFormat.Json)
                {
                    ctx.Formatter.WriteMessage("Changes applied.");
                }
            }
            else if (!apply && plan.HasChanges && ctx.OutputFormat != OutputFormat.Json)
            {
                ctx.Formatter.WriteMessage("No changes applied. Run with --apply to commit changes.");
            }

            return ExitCodes.Success;
        });

        return command;
    }
}
