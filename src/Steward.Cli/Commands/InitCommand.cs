using System.CommandLine;
using Steward.Cli.Formatting;
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
            Description = "Starting-point profile to scaffold: software, docs, minimal",
            DefaultValueFactory = _ => "software"
        };
        profileOption.AcceptOnlyFromAmong("software", "docs", "minimal");
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
            var filesWritten = new List<string>();

            if (File.Exists(configPath) || File.Exists(policyPath))
            {
                CommandSetup.WriteCommandError(
                    parseResult,
                    "init",
                    ExitCodes.UsageError,
                    "already-initialized",
                    $"A .steward/ configuration already exists in: {stewardDir}",
                    details: new Dictionary<string, object>
                    {
                        ["configDirectory"] = Path.GetRelativePath(rootPath, stewardDir)
                    },
                    suggestedNextStep: "Edit the existing files or remove .steward/ to re-initialize.");
                return ExitCodes.UsageError;
            }

            if (!Directory.Exists(stewardDir))
                Directory.CreateDirectory(stewardDir);

            var config = ProfileDefaults.CreateDefaultConfig(profile);
            var policy = ProfileDefaults.GetProfilePolicy(profile);

            var configYaml = ConfigLoader.SerializeConfig(config)
                + "  # - \"tests/**\"      # uncomment to exclude test fixture Markdown files from discovery\n";
            File.WriteAllText(configPath, configYaml);
            filesWritten.Add(Path.GetRelativePath(rootPath, configPath));
            if (policy != null)
            {
                File.WriteAllText(policyPath, ConfigLoader.SerializePolicy(policy));
                filesWritten.Add(Path.GetRelativePath(rootPath, policyPath));
            }

            // Scaffold placeholder files for all declared file artifacts so that
            // 'steward check' does not show STWD-009 warnings immediately after init.
            var scaffolded = new List<string>();
            if (policy?.Artifacts != null)
            {
                foreach (var artifact in policy.Artifacts)
                {
                    if (string.IsNullOrWhiteSpace(artifact.Path) || artifact.Path.EndsWith('/'))
                        continue;

                    var artifactFullPath = Path.Combine(rootPath, artifact.Path);
                    if (File.Exists(artifactFullPath))
                        continue;

                    var artifactDir = Path.GetDirectoryName(artifactFullPath);
                    if (artifactDir != null && !Directory.Exists(artifactDir))
                        Directory.CreateDirectory(artifactDir);

                    var placeholder = GeneratePlaceholder(artifact.Path, artifact.Role);
                    File.WriteAllText(artifactFullPath, placeholder);
                    scaffolded.Add(artifact.Path);
                    filesWritten.Add(artifact.Path);
                }
            }

            if (output == OutputFormat.Json)
            {
                JsonEnvelopeWriter.Write(formatter, "init", true, ExitCodes.Success, new
                {
                    profile,
                    configDirectory = Path.GetRelativePath(rootPath, stewardDir),
                    filesWritten,
                    scaffoldedArtifacts = scaffolded
                });
                return ExitCodes.Success;
            }

            formatter.WriteMessage($"Initialized .steward/ with profile '{profile}'.");
            formatter.WriteMessage($"  {Path.GetRelativePath(rootPath, configPath)}");
            if (policy != null)
                formatter.WriteMessage($"  {Path.GetRelativePath(rootPath, policyPath)}");
            if (scaffolded.Count > 0)
            {
                formatter.WriteMessage("");
                formatter.WriteMessage("Scaffolded artifacts:");
                foreach (var path in scaffolded)
                    formatter.WriteMessage($"  {path}");
            }
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

    internal static string GeneratePlaceholder(string path, string? role)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        if (path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            return $"# {name}\n\n> TODO: Add content.\n";

        // Non-markdown files (e.g. LICENSE) get a minimal placeholder.
        return $"TODO: Add {name} content.\n";
    }
}
