using System.CommandLine;
using Steward.Core;
using Steward.Core.Abstractions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Formatting;
using Steward.Core.Maintenance;
using Steward.Cli.Formatting;

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

        command.Add(scopeOption);
        command.Add(applyOption);

        command.SetAction(parseResult =>
        {
            var output = parseResult.GetValue(GlobalOptionsSetup.OutputOption);
            var noColor = parseResult.GetValue(GlobalOptionsSetup.NoColorOption);
            var configPath = parseResult.GetValue(GlobalOptionsSetup.ConfigOption);
            var scope = parseResult.GetValue(scopeOption);
            var apply = parseResult.GetValue(applyOption);

            var formatter = CreateFormatter(output, noColor);
            var fileSystem = new PhysicalFileSystem();
            var rootPath = Directory.GetCurrentDirectory();

            // Load config and policy
            var configLoader = new ConfigLoader(fileSystem);
            var configDir = configLoader.FindConfigDirectory(rootPath, configPath);

            if (configDir == null)
            {
                formatter.WriteError("No .steward configuration directory found. Run 'steward init' first.");
                return ExitCodes.UsageError;
            }

            var policy = configLoader.LoadPolicy(configDir);

            // Discover files
            var ignoreFilter = GitIgnoreFilter.Load(rootPath, fileSystem);
            var discoveryService = new FileDiscoveryService(fileSystem, ignoreFilter);
            var files = discoveryService.Discover(rootPath);

            // Create maintenance context
            var context = new MaintenanceContext
            {
                RepositoryRoot = rootPath,
                FileSystem = fileSystem,
                Files = files
            };

            // Evaluate
            var engine = new MaintenanceEngine();
            var plan = engine.Evaluate(policy, context, scope);

            if (output == OutputFormat.Json)
            {
                formatter.WriteObject(new
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
                    formatter.WriteMessage("No maintenance artifacts configured.");
                    return ExitCodes.Success;
                }

                foreach (var action in plan.Actions)
                {
                    var status = action.HasChanges ? "MAINTAIN" : "OK      ";
                    formatter.WriteMessage($"{status}  {action.ArtifactId}  {action.ArtifactPath}");
                    formatter.WriteMessage($"  {action.Description}");
                }

                formatter.WriteMessage("");
            }

            if (apply && plan.HasChanges)
            {
                foreach (var action in plan.Actions.Where(a => a.HasChanges && a.ExpectedContent != null))
                {
                    var fullPath = Path.Combine(rootPath, action.ArtifactPath);
                    var dir = Path.GetDirectoryName(fullPath);
                    if (dir != null && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    File.WriteAllText(fullPath, action.ExpectedContent);
                }

                if (output != OutputFormat.Json)
                {
                    formatter.WriteMessage("Changes applied.");
                }
            }
            else if (!apply && plan.HasChanges && output != OutputFormat.Json)
            {
                formatter.WriteMessage("No changes applied. Run with --apply to commit changes.");
            }

            return ExitCodes.Success;
        });

        return command;
    }

    private static IOutputFormatter CreateFormatter(OutputFormat format, bool noColor)
    {
        return format switch
        {
            OutputFormat.Json => new JsonOutputFormatter(Console.Out),
            _ => new TextOutputFormatter(Console.Out, !noColor && !Console.IsOutputRedirected)
        };
    }
}
