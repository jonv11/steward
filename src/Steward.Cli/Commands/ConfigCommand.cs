using System.CommandLine;
using Steward.Core;
using Steward.Core.Abstractions;
using Steward.Core.Configuration;
using Steward.Core.Formatting;
using Steward.Cli.Formatting;

namespace Steward.Cli.Commands;

public static class ConfigCommand
{
    public static Command Create()
    {
        var command = new Command("config", "Configuration management");

        command.Add(CreateValidateCommand());
        command.Add(CreateShowCommand());

        return command;
    }

    private static Command CreateValidateCommand()
    {
        var command = new Command("validate", "Validate configuration files");

        command.SetAction((parseResult) =>
        {
            var output = parseResult.GetValue(GlobalOptionsSetup.OutputOption);
            var noColor = parseResult.GetValue(GlobalOptionsSetup.NoColorOption);
            var configPath = parseResult.GetValue(GlobalOptionsSetup.ConfigOption);

            var formatter = CreateFormatter(output, noColor);
            var fileSystem = new PhysicalFileSystem();
            var loader = new ConfigLoader(fileSystem);

            var configDir = loader.FindConfigDirectory(Directory.GetCurrentDirectory(), configPath);
            if (configDir == null)
            {
                formatter.WriteError("No .steward/ directory found. Run 'steward init' first.");
                return ExitCodes.UsageError;
            }

            var errors = new List<string>();

            try { loader.LoadConfig(configDir); }
            catch (Exception ex) { errors.Add($"config.yaml: {ex.Message}"); }

            try { loader.LoadPolicy(configDir); }
            catch (Exception ex) { errors.Add($"policy.yaml: {ex.Message}"); }

            try { loader.LoadPathPolicy(configDir); }
            catch (Exception ex) { errors.Add($"path-policy.yaml: {ex.Message}"); }

            if (errors.Count > 0)
            {
                foreach (var error in errors)
                    formatter.WriteError(error);
                return ExitCodes.ValidationFailure;
            }

            formatter.WriteMessage("Configuration is valid.");
            return ExitCodes.Success;
        });

        return command;
    }

    private static Command CreateShowCommand()
    {
        var command = new Command("show", "Show effective configuration");

        var effectiveOption = new Option<bool>("--effective")
        {
            Description = "Show merged effective configuration"
        };
        command.Add(effectiveOption);

        command.SetAction((parseResult) =>
        {
            var output = parseResult.GetValue(GlobalOptionsSetup.OutputOption);
            var noColor = parseResult.GetValue(GlobalOptionsSetup.NoColorOption);
            var configPath = parseResult.GetValue(GlobalOptionsSetup.ConfigOption);

            var formatter = CreateFormatter(output, noColor);
            var fileSystem = new PhysicalFileSystem();
            var loader = new ConfigLoader(fileSystem);

            var configDir = loader.FindConfigDirectory(Directory.GetCurrentDirectory(), configPath);
            if (configDir == null)
            {
                formatter.WriteError("No .steward/ directory found. Run 'steward init' first.");
                return ExitCodes.UsageError;
            }

            var config = loader.LoadConfig(configDir);
            var policy = loader.LoadPolicy(configDir);

            if (output == OutputFormat.Json)
            {
                formatter.WriteObject(new { config, policy });
            }
            else
            {
                if (config != null)
                {
                    formatter.WriteMessage("--- Config ---");
                    formatter.WriteMessage(ConfigLoader.SerializeConfig(config));
                }
                if (policy != null)
                {
                    formatter.WriteMessage("--- Policy ---");
                    formatter.WriteMessage(ConfigLoader.SerializePolicy(policy));
                }
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
