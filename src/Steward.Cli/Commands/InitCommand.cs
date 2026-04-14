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
            Description = "Profile to use: software, docs, mixed, knowledge, minimal",
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
            formatter.WriteMessage("  1. Edit .steward/policy.yaml to describe your repository's artifacts and governance rules.");
            formatter.WriteMessage("  2. Run 'steward check' to validate policy compliance.");
            formatter.WriteMessage("  3. Run 'steward status' for a quick health summary.");

            return ExitCodes.Success;
        });

        return command;
    }

}
