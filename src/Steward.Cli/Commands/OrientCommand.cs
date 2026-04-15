using System.CommandLine;
using Steward.Core;
using Steward.Core.Formatting;
using Steward.Core.Orientation;

namespace Steward.Cli.Commands;

public static class OrientCommand
{
    public static Command Create()
    {
        var command = new Command("orient", "Show repository structure classified by artifact role (readme, governance, planning, etc.)");

        var depthOption = new Option<int>("--depth", "-d")
        {
            Description = "Maximum depth to display",
            DefaultValueFactory = _ => 3
        };
        command.Add(depthOption);

        var signalsOption = new Option<bool>("--signals")
        {
            Description = "Include cheap required/stale signals without running full validation"
        };
        command.Add(signalsOption);

        var compactOption = new Option<bool>("--compact")
        {
            Description = "Limit output to the ~15 most important entries"
        };
        command.Add(compactOption);

        command.SetAction((parseResult) =>
        {
            if (!CommandSetup.TryBuild(parseResult, out var ctx))
                return ExitCodes.UsageError;

            var depth = parseResult.GetValue(depthOption);
            var includeSignals = parseResult.GetValue(signalsOption);
            var compact = parseResult.GetValue(compactOption);

            var engine = new OrientationEngine();
            var status = includeSignals
                ? StatusCommand.ComputeStatus(ctx!.Policy, ctx.Config?.Profile, ctx.FileSystem, ctx.RootPath, ctx.Files!)
                : null;
            var result = engine.Orient(
                ctx!.RootPath,
                ctx.Files!,
                ctx.Policy,
                ctx.Config?.Profile,
                depth,
                status != null ? ToSignalInput(status) : null);

            // In compact mode, keep only top-priority entries (~15)
            if (compact)
            {
                var prioritized = result.Entries
                    .OrderByDescending(e => e.IsStartHere)
                    .ThenByDescending(e => e.Classification is "authoritative" or "governance"
                        || e.Classification.StartsWith("state:"))
                    .ThenBy(e => e.Depth)
                    .Take(15)
                    .OrderBy(e => e.Path)
                    .ToList();

                result.Entries.Clear();
                result.Entries.AddRange(prioritized);
            }

            if (ctx.OutputFormat == OutputFormat.Json)
            {
                ctx.Formatter.WriteObject(result);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(result.RepositoryName))
                    ctx.Formatter.WriteMessage($"Repository: {result.RepositoryName}");

                if (!string.IsNullOrWhiteSpace(result.RepositoryType) || !string.IsNullOrWhiteSpace(result.Profile))
                {
                    var details = new List<string>();
                    if (!string.IsNullOrWhiteSpace(result.RepositoryType))
                        details.Add($"type={result.RepositoryType}");
                    if (!string.IsNullOrWhiteSpace(result.Profile))
                        details.Add($"profile={result.Profile}");
                    ctx.Formatter.WriteMessage($"Context: {string.Join(", ", details)}");
                }

                if (result.StartHere.Count > 0)
                {
                    ctx.Formatter.WriteMessage("Start here:");
                    foreach (var path in result.StartHere)
                        ctx.Formatter.WriteMessage($"  - {path}");
                    ctx.Formatter.WriteMessage("");
                }

                foreach (var entry in result.Entries)
                {
                    var indent = new string(' ', entry.Depth * 2);
                    var kind = entry.IsDirectory ? "dir " : "file";
                    var marker = entry.IsStartHere ? " [start]" : "";
                    ctx.Formatter.WriteMessage($"{indent}[{kind}] [{entry.Classification}] {entry.Path}{marker}");
                }

                if (result.Signals.Count > 0)
                {
                    ctx.Formatter.WriteMessage("");
                    ctx.Formatter.WriteMessage("Signals:");
                    foreach (var signal in result.Signals)
                    {
                        var location = signal.Path != null ? $" {signal.Path}" : "";
                        ctx.Formatter.WriteMessage($"  [{signal.Severity}] {signal.Code}{location}: {signal.Message}");
                    }
                }
            }

            return ExitCodes.Success;
        });

        return command;
    }

    private static OrientationSignalInput ToSignalInput(StatusCommand.RepositoryStatus status)
    {
        return new OrientationSignalInput
        {
            MissingRequiredArtifacts =
            [
                .. status.RequiredArtifacts
                    .Where(static artifact => !artifact.Present)
                    .Select(static artifact => new OrientationSignalInputItem
                    {
                        Path = artifact.Path,
                        Message = $"Required artifact '{artifact.Path}' is missing."
                    })
            ],
            StaleArtifacts =
            [
                .. status.MaintenanceArtifacts
                    .Where(static artifact => artifact.Stale)
                    .Select(static artifact => new OrientationSignalInputItem
                    {
                        Path = artifact.Path,
                        Message = $"Maintained artifact '{artifact.Id}' is stale."
                    })
            ]
        };
    }
}
