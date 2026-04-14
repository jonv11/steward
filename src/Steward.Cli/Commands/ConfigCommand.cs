using System.CommandLine;
using Steward.Core;
using Steward.Core.Abstractions;
using Steward.Core.Configuration;
using Steward.Core.Formatting;

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

            var formatter = CommandSetup.CreateFormatter(output, noColor);
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
                if (output == OutputFormat.Json)
                {
                    formatter.WriteObject(new
                    {
                        valid = false,
                        errors = errors
                    });
                }
                else
                {
                    formatter.WriteError("Configuration is invalid:");
                    foreach (var error in errors)
                        formatter.WriteError($"  - {error}");
                }

                return ExitCodes.UsageError;
            }

            if (output == OutputFormat.Json)
            {
                formatter.WriteObject(new { valid = true });
            }
            else
            {
                formatter.WriteMessage("Configuration is valid.");
            }

            return ExitCodes.Success;
        });

        return command;
    }

    private static Command CreateShowCommand()
    {
        var command = new Command("show", "Show the loaded configuration");

        var effectiveOption = new Option<bool>("--effective")
        {
            Description = "Include resolved effective runtime defaults"
        };
        command.Add(effectiveOption);

        command.SetAction((parseResult) =>
        {
            if (!CommandSetup.TryBuild(parseResult, out var ctx, discoverFiles: false))
                return ExitCodes.UsageError;

            if (ctx!.ConfigDirectory == null)
            {
                ctx.Formatter.WriteError("No .steward/ directory found. Run 'steward init' first.");
                return ExitCodes.UsageError;
            }

            var effective = parseResult.GetValue(effectiveOption);
            var fileSystem = new PhysicalFileSystem();
            var rawConfig = ReadRawFile(fileSystem, Path.Combine(ctx.ConfigDirectory, "config.yaml"));
            var rawPolicy = ReadRawFile(fileSystem, Path.Combine(ctx.ConfigDirectory, "policy.yaml"));
            var rawPathPolicy = ReadRawFile(fileSystem, Path.Combine(ctx.ConfigDirectory, "path-policy.yaml"));

            if (ctx.OutputFormat == OutputFormat.Json)
            {
                ctx.Formatter.WriteObject(new
                {
                    configDirectory = ctx.ConfigDirectory,
                    config = ctx.Config,
                    policy = ctx.Policy,
                    pathPolicy = ctx.PathPolicy,
                    rawFiles = new
                    {
                        configYaml = rawConfig,
                        policyYaml = rawPolicy,
                        pathPolicyYaml = rawPathPolicy
                    },
                    effectiveRuntime = effective
                        ? new
                        {
                            profile = ctx.Config?.Profile,
                            output = new
                            {
                                format = ctx.OutputFormat.ToString().ToLowerInvariant(),
                                verbosity = ctx.Verbosity.ToString().ToLowerInvariant(),
                                noColor = ctx.NoColor
                            },
                            discovery = new
                            {
                                exclude = ctx.EffectiveDiscoveryExcludes
                            }
                        }
                        : null
                });
            }
            else
            {
                ctx.Formatter.WriteMessage($"Config directory: {ctx.ConfigDirectory}");
                ctx.Formatter.WriteMessage("");
                WriteRawSection(ctx.Formatter, "config.yaml", rawConfig);
                WriteRawSection(ctx.Formatter, "policy.yaml", rawPolicy);
                WriteRawSection(ctx.Formatter, "path-policy.yaml", rawPathPolicy);

                if (effective)
                {
                    ctx.Formatter.WriteMessage("Effective runtime defaults:");
                    ctx.Formatter.WriteMessage($"  profile: {ctx.Config?.Profile ?? "(none)"}");
                    ctx.Formatter.WriteMessage($"  output.format: {ctx.OutputFormat.ToString().ToLowerInvariant()}");
                    ctx.Formatter.WriteMessage($"  output.verbosity: {ctx.Verbosity.ToString().ToLowerInvariant()}");
                    ctx.Formatter.WriteMessage($"  output.no_color: {ctx.NoColor.ToString().ToLowerInvariant()}");
                    if (ctx.EffectiveDiscoveryExcludes.Count == 0)
                    {
                        ctx.Formatter.WriteMessage("  discovery.exclude: []");
                    }
                    else
                    {
                        ctx.Formatter.WriteMessage("  discovery.exclude:");
                        foreach (var pattern in ctx.EffectiveDiscoveryExcludes)
                            ctx.Formatter.WriteMessage($"    - {pattern}");
                    }
                }
            }

            return ExitCodes.Success;
        });

        return command;
    }

    private static string? ReadRawFile(IFileSystem fileSystem, string path)
    {
        return fileSystem.FileExists(path) ? fileSystem.ReadAllText(path) : null;
    }

    private static void WriteRawSection(IOutputFormatter formatter, string fileName, string? content)
    {
        if (content == null)
        {
            formatter.WriteMessage($"--- {fileName} --- (not present)");
            formatter.WriteMessage("");
            return;
        }

        formatter.WriteMessage($"--- {fileName} ---");
        formatter.WriteMessage(content);
        if (!content.EndsWith('\n'))
            formatter.WriteMessage("");
    }

}
