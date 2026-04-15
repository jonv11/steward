using System.CommandLine;
using DotNet.Globbing;
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
        command.Add(CreateDoctorCommand());
        command.Add(CreateSuggestCommand());

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

    private static Command CreateDoctorCommand()
    {
        var command = new Command("doctor", "Detect valid but ineffective configuration");

        command.SetAction(parseResult =>
        {
            if (!CommandSetup.TryBuild(parseResult, out var ctx))
                return ExitCodes.UsageError;

            if (ctx!.ConfigDirectory == null)
            {
                ctx.Formatter.WriteError("No .steward/ directory found. Run 'steward init' first.");
                return ExitCodes.UsageError;
            }

            var findings = RunDoctor(ctx);

            if (ctx.OutputFormat == OutputFormat.Json)
            {
                ctx.Formatter.WriteObject(new
                {
                    findings = findings.Select(f => new
                    {
                        category = f.Category,
                        message = f.Message,
                        remediation = f.Remediation
                    }).ToArray()
                });
            }
            else
            {
                if (findings.Count == 0)
                {
                    ctx.Formatter.WriteMessage("No configuration issues found.");
                }
                else
                {
                    ctx.Formatter.WriteMessage($"Found {findings.Count} configuration issue(s):");
                    ctx.Formatter.WriteMessage("");
                    foreach (var f in findings)
                    {
                        ctx.Formatter.WriteMessage($"  [{f.Category}] {f.Message}");
                        ctx.Formatter.WriteMessage($"    Remediation: {f.Remediation}");
                    }
                }
            }

            return findings.Count > 0 ? ExitCodes.ValidationFailure : ExitCodes.Success;
        });

        return command;
    }

    internal static List<DoctorFinding> RunDoctor(CommandContext ctx)
    {
        var findings = new List<DoctorFinding>();
        var existingPaths = new HashSet<string>(
            (ctx.Files ?? []).Select(f => f.RelativePath.Replace('\\', '/')),
            StringComparer.OrdinalIgnoreCase);

        // 1. Dead start_here entries
        if (ctx.Policy?.Governance?.StartHere != null)
        {
            foreach (var entry in ctx.Policy.Governance.StartHere)
            {
                if (!existingPaths.Contains(entry.Replace('\\', '/')))
                {
                    findings.Add(new DoctorFinding(
                        "dead-start-here",
                        $"start_here entry '{entry}' does not match any discovered file.",
                        $"Remove '{entry}' from governance.start_here or create the file."));
                }
            }
        }

        // 2. Artifact declarations matching no existing file
        if (ctx.Policy?.Artifacts != null)
        {
            foreach (var artifact in ctx.Policy.Artifacts)
            {
                if (string.IsNullOrWhiteSpace(artifact.Path)) continue;
                var artifactPath = artifact.Path!.Replace('\\', '/').TrimEnd('/');
                if (!existingPaths.Contains(artifactPath))
                {
                    findings.Add(new DoctorFinding(
                        "missing-artifact",
                        $"Artifact declaration '{artifactPath}' does not match any discovered file.",
                        $"Create the file at '{artifactPath}' or remove the artifact from policy.yaml."));
                }
            }
        }

        // 3. Path-policy rulesets matching no files
        if (ctx.PathPolicy?.Rulesets != null)
        {
            foreach (var ruleset in ctx.PathPolicy.Rulesets)
            {
                if (ruleset.Rules == null) continue;
                foreach (var rule in ruleset.Rules)
                {
                    if (string.IsNullOrWhiteSpace(rule.Pattern)) continue;

                    bool anyMatch;
                    if (rule.Exact)
                    {
                        anyMatch = existingPaths.Contains(rule.Pattern.Replace('\\', '/'));
                    }
                    else
                    {
                        var glob = Glob.Parse(rule.Pattern);
                        anyMatch = existingPaths.Any(p => glob.IsMatch(p));
                    }

                    if (!anyMatch)
                    {
                        findings.Add(new DoctorFinding(
                            "unmatched-path-rule",
                            $"Path-policy rule '{rule.Pattern}' matches no discovered files.",
                            $"Update the pattern or remove the rule from path-policy.yaml."));
                    }
                }
            }
        }

        // 4. Maintenance sources matching nothing
        if (ctx.Policy?.Maintenance?.Artifacts != null)
        {
            foreach (var maint in ctx.Policy.Maintenance.Artifacts)
            {
                if (string.IsNullOrWhiteSpace(maint.Source)) continue;
                var glob = Glob.Parse(maint.Source);
                if (!existingPaths.Any(p => glob.IsMatch(p)))
                {
                    findings.Add(new DoctorFinding(
                        "unmatched-maintenance-source",
                        $"Maintenance source '{maint.Source}' for '{maint.Id ?? maint.Path}' matches no discovered files.",
                        $"Update the source pattern or remove the maintenance artifact."));
                }
            }
        }

        return findings;
    }

    internal sealed record DoctorFinding(string Category, string Message, string Remediation);

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

    private static Command CreateSuggestCommand()
    {
        var command = new Command("suggest", "Analyze repository and suggest initial governance configuration");

        command.SetAction(parseResult =>
        {
            if (!CommandSetup.TryBuild(parseResult, out var ctx))
                return ExitCodes.UsageError;

            var suggestion = BootstrapAnalyzer.Analyze(ctx!.Files!, ctx.FileSystem, ctx.RootPath);

            if (ctx.OutputFormat == OutputFormat.Json)
            {
                ctx.Formatter.WriteObject(suggestion);
            }
            else
            {
                if (suggestion.StartHere.Count > 0)
                {
                    ctx.Formatter.WriteMessage("Suggested start_here:");
                    foreach (var s in suggestion.StartHere)
                        ctx.Formatter.WriteMessage($"  - {s}");
                    ctx.Formatter.WriteMessage("");
                }

                if (suggestion.Artifacts.Count > 0)
                {
                    ctx.Formatter.WriteMessage("Suggested artifacts:");
                    foreach (var a in suggestion.Artifacts)
                        ctx.Formatter.WriteMessage($"  - {a.Path} (role: {a.Role}, importance: {a.Importance}) — {a.Reason}");
                    ctx.Formatter.WriteMessage("");
                }

                if (suggestion.ExcludePatterns.Count > 0)
                {
                    ctx.Formatter.WriteMessage("Suggested exclude patterns:");
                    foreach (var e in suggestion.ExcludePatterns)
                        ctx.Formatter.WriteMessage($"  - {e}");
                    ctx.Formatter.WriteMessage("");
                }

                if (suggestion.StartHere.Count == 0 && suggestion.Artifacts.Count == 0)
                {
                    ctx.Formatter.WriteMessage("No governance suggestions — repository structure does not match known patterns.");
                }
                else
                {
                    ctx.Formatter.WriteMessage("These are suggestions only. Apply them by editing .steward/policy.yaml.");
                }
            }

            return ExitCodes.Success;
        });

        return command;
    }

}
