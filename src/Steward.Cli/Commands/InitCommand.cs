using System.CommandLine;
using Steward.Core;
using Steward.Core.Configuration;
using Steward.Core.Formatting;

namespace Steward.Cli.Commands;

public static class InitCommand
{
    public static Command Create()
    {
        var command = new Command("init", "Initialize .steward configuration");

        var profileOption = new Option<string>("--profile", "-p")
        {
            Description = "Starting-point profile to scaffold: software, docs, mixed, knowledge, minimal",
            DefaultValueFactory = _ => "software"
        };
        profileOption.AcceptOnlyFromAmong("software", "docs", "mixed", "knowledge", "minimal");
        command.Add(profileOption);

        command.SetAction((parseResult) =>
        {
            var output = parseResult.GetValue(GlobalOptionsSetup.OutputOption);
            var noColor = parseResult.GetValue(GlobalOptionsSetup.NoColorOption);
            var profile = parseResult.GetValue(profileOption) ?? "software";

            var formatter = CommandSetup.CreateFormatter(output, noColor);
            var rootPath = Directory.GetCurrentDirectory();
            var stewardDir = Path.Combine(rootPath, ".steward");
            var configPath = Path.Combine(stewardDir, "config.yaml");
            var policyPath = Path.Combine(stewardDir, "policy.yaml");

            if (File.Exists(configPath) || File.Exists(policyPath))
            {
                formatter.WriteError($"A .steward/ configuration already exists in: {stewardDir}");
                formatter.WriteError("Edit the existing files or remove .steward/ to re-initialize.");
                return ExitCodes.UsageError;
            }

            if (!Directory.Exists(stewardDir))
                Directory.CreateDirectory(stewardDir);

            var config = ProfileDefaults.CreateDefaultConfig(profile);
            var policy = ProfileDefaults.GetProfilePolicy(profile);

            File.WriteAllText(configPath, ConfigLoader.SerializeConfig(config));
            if (policy != null)
                File.WriteAllText(policyPath, ConfigLoader.SerializePolicy(policy));

            formatter.WriteMessage($"Initialized .steward/ with profile '{profile}'.");
            formatter.WriteMessage($"  {Path.GetRelativePath(rootPath, configPath)}");
            if (policy != null)
                formatter.WriteMessage($"  {Path.GetRelativePath(rootPath, policyPath)}");
            formatter.WriteMessage("");
            formatter.WriteMessage("Next steps:");
            formatter.WriteMessage("  1. Run 'steward config suggest' to get artifact and exclude suggestions for this repository.");
            formatter.WriteMessage("  2. Edit .steward/policy.yaml to declare your repository's artifacts, roles, and governance rules.");
            formatter.WriteMessage("     Add .steward/path-policy.yaml to enforce naming conventions and forbidden path patterns.");
            formatter.WriteMessage("  3. Run 'steward check' to validate policy compliance.");
            formatter.WriteMessage("  4. Run 'steward status' for a quick health summary.");

            return ExitCodes.Success;
        });

        return command;
    }

}
