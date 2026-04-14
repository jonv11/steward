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

            if (!Directory.Exists(stewardDir))
                Directory.CreateDirectory(stewardDir);

            var configPath = Path.Combine(stewardDir, "config.yaml");
            if (File.Exists(configPath))
            {
                formatter.WriteError($"Config already exists: {configPath}");
                return ExitCodes.UsageError;
            }

            var policyPath = Path.Combine(stewardDir, "policy.yaml");
            if (File.Exists(policyPath))
            {
                formatter.WriteError($"Policy already exists: {policyPath}");
                return ExitCodes.UsageError;
            }

            var config = ProfileDefaults.CreateDefaultConfig(profile);
            var policy = ProfileDefaults.GetProfilePolicy(profile);

            File.WriteAllText(configPath, ConfigLoader.SerializeConfig(config));
            if (policy != null)
                File.WriteAllText(policyPath, ConfigLoader.SerializePolicy(policy));

            formatter.WriteMessage($"Initialized .steward/ with profile '{profile}'");
            formatter.WriteMessage($"  Created: {Path.GetRelativePath(rootPath, configPath)}");
            if (policy != null)
                formatter.WriteMessage($"  Created: {Path.GetRelativePath(rootPath, policyPath)}");

            return ExitCodes.Success;
        });

        return command;
    }

}
