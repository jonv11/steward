using System.CommandLine;
using Steward.Cli.Formatting;
using Steward.Core;
using Steward.Core.Baseline;
using Steward.Core.Configuration;
using Steward.Core.Formatting;

namespace Steward.Cli.Commands;

/// <summary>
/// <c>steward baseline</c>: RFC-017 rule phase-in. A baseline grandfathers the violations that
/// exist when a rule is adopted, so <c>check</c> enforces the rule on new content only.
/// </summary>
public static class BaselineCommand
{
    private static readonly string ToolVersion =
        typeof(BaselineCommand).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    public static Command Create()
    {
        var command = new Command("baseline", "Grandfather existing violations so rules apply to new content only");
        command.Add(CreateGenerateCommand());
        return command;
    }

    private static Command CreateGenerateCommand()
    {
        var command = new Command("generate", "Snapshot every current check diagnostic into the baseline file");

        command.SetAction(parseResult =>
        {
            const string commandName = "baseline generate";
            if (!CommandSetup.TryBuild(parseResult, out var ctx, commandName))
                return ExitCodes.UsageError;

            if (ctx!.ConfigDirectory == null)
            {
                CommandSetup.WriteCommandError(parseResult, commandName, ExitCodes.UsageError, "config-not-found",
                    "No .steward/ directory found; a baseline belongs to a configuration.",
                    suggestedNextStep: "Run 'steward init' first, or pass --config <dir>.");
                return ExitCodes.UsageError;
            }

            var result = CheckCommand.Validate(ctx, ctx.Files!, out _);
            var settings = BaselineSettings.Resolve(ctx.Policy, ctx.RootPath, ctx.ConfigDirectory);
            var generated = BaselineDocument.FromDiagnostics(
                result.Diagnostics, DateOnly.FromDateTime(DateTime.Now), ToolVersion);

            // Leave an existing file untouched when it already records exactly these entries, so
            // regenerating an unchanged repository on a later day produces no diff.
            var unchanged = TryReadExisting(settings.FullPath) is { } existing &&
                existing.Entries.Select(e => e.Identity).SequenceEqual(generated.Entries.Select(e => e.Identity));

            if (!unchanged)
            {
                var dir = Path.GetDirectoryName(settings.FullPath);
                if (dir != null) Directory.CreateDirectory(dir);
                File.WriteAllText(settings.FullPath, generated.Serialize());
            }

            var byRule = generated.Entries
                .GroupBy(e => e.Rule, StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.Count());

            if (ctx.OutputFormat == OutputFormat.Json)
            {
                JsonEnvelopeWriter.Write(ctx.Formatter, commandName, true, ExitCodes.Success, new
                {
                    path = settings.DisplayPath,
                    entries = generated.Entries.Count,
                    unchanged,
                    disabled = settings.Enabled == false,
                    byRule
                });
                return ExitCodes.Success;
            }

            ctx.Formatter.WriteMessage(unchanged
                ? $"Baseline {settings.DisplayPath} is already current ({generated.Entries.Count} entries)."
                : $"Wrote {settings.DisplayPath} with {generated.Entries.Count} entries.");
            foreach (var (rule, count) in byRule)
                ctx.Formatter.WriteMessage($"  {rule}  {count}");
            if (settings.Enabled == false)
                ctx.Formatter.WriteMessage("Note: baseline.enabled is false in policy.yaml, so check does not apply it.");
            else if (generated.Entries.Count > 0)
                ctx.Formatter.WriteMessage("'steward check' now reports these as baseline debt instead of failures. Commit the file.");

            return ExitCodes.Success;
        });

        return command;
    }

    private static BaselineDocument? TryReadExisting(string fullPath)
    {
        if (!File.Exists(fullPath)) return null;
        try { return BaselineDocument.Parse(File.ReadAllText(fullPath), fullPath); }
        catch (StewardConfigException) { return null; }
    }
}
