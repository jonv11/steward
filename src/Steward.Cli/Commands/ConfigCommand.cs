using System.CommandLine;
using DotNet.Globbing;
using Steward.Cli.Formatting;
using Steward.Core;
using Steward.Core.Abstractions;
using Steward.Core.Configuration;
using Steward.Core.Formatting;
using Steward.Core.Maintenance;

namespace Steward.Cli.Commands;

public static class ConfigCommand
{
    public static Command Create()
    {
        var command = new Command("config", "Inspect and validate .steward configuration");

        command.Add(CreateValidateCommand());
        command.Add(CreateShowCommand());
        command.Add(CreateDoctorCommand());
        command.Add(CreateSuggestCommand());

        return command;
    }

    private static Command CreateValidateCommand()
    {
        var command = new Command("validate", "Check .steward/ YAML files for syntax and field errors");

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
                if (output == OutputFormat.Json)
                {
                    JsonEnvelopeWriter.WriteError(formatter, "config validate", ExitCodes.UsageError,
                        "config-not-found", "No .steward/ directory found. Run 'steward init' first.");
                    return ExitCodes.UsageError;
                }
                formatter.WriteError("No .steward/ directory found. Run 'steward init' first.");
                return ExitCodes.UsageError;
            }

            var errors = new List<object>();
            var errorStrings = new List<string>();

            try { loader.LoadConfig(configDir); }
            catch (Exception ex)
            {
                errors.Add(new { file = "config.yaml", message = ex.Message });
                errorStrings.Add($"config.yaml: {ex.Message}");
            }

            try { loader.LoadPolicy(configDir); }
            catch (Exception ex)
            {
                errors.Add(new { file = "policy.yaml", message = ex.Message });
                errorStrings.Add($"policy.yaml: {ex.Message}");
            }

            try { loader.LoadPathPolicy(configDir); }
            catch (Exception ex)
            {
                errors.Add(new { file = "path-policy.yaml", message = ex.Message });
                errorStrings.Add($"path-policy.yaml: {ex.Message}");
            }

            if (errors.Count > 0)
            {
                if (output == OutputFormat.Json)
                {
                    // CC-08: Structured errors with file + message instead of plain strings
                    JsonEnvelopeWriter.Write(formatter, "config validate", false, ExitCodes.UsageError,
                        new { valid = false, errors });
                }
                else
                {
                    formatter.WriteError("Configuration is invalid:");
                    foreach (var error in errorStrings)
                        formatter.WriteError($"  - {error}");
                }

                return ExitCodes.UsageError;
            }

            if (output == OutputFormat.Json)
            {
                JsonEnvelopeWriter.Write(formatter, "config validate", true, ExitCodes.Success,
                    new { valid = true });
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
        var command = new Command("show", "Print raw config files and (with --effective) the resolved runtime defaults plus merged policy");

        var effectiveOption = new Option<bool>("--effective")
        {
            Description = "Include resolved effective runtime defaults"
        };
        command.Add(effectiveOption);

        command.SetAction((parseResult) =>
        {
            if (!CommandSetup.TryBuild(parseResult, out var ctx, "config show", discoverFiles: false))
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
                JsonEnvelopeWriter.Write(ctx.Formatter, "config show", true, ExitCodes.Success, new
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

                    ctx.Formatter.WriteMessage("");
                    ctx.Formatter.WriteMessage("Effective policy (profile defaults merged):");
                    if (ctx.Policy == null)
                    {
                        ctx.Formatter.WriteMessage("  (none)");
                    }
                    else
                    {
                        ctx.Formatter.WriteMessage(ConfigLoader.SerializePolicy(ctx.Policy).TrimEnd());
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
        var command = new Command("doctor", "Detect valid but ineffective config: dead start_here entries, unmatched patterns, missing sources");

        command.SetAction(parseResult =>
        {
            if (!CommandSetup.TryBuild(parseResult, out var ctx, "config doctor"))
                return ExitCodes.UsageError;

            if (ctx!.ConfigDirectory == null)
            {
                ctx.Formatter.WriteError("No .steward/ directory found. Run 'steward init' first.");
                return ExitCodes.UsageError;
            }

            var findings = RunDoctor(ctx);

            if (ctx.OutputFormat == OutputFormat.Json)
            {
                var doctorExitCode = findings.Count > 0 ? ExitCodes.ValidationFailure : ExitCodes.Success;
                // CC-03: success=true means process executed correctly.
                // findings.Count > 0 is a domain result, not a process failure.
                JsonEnvelopeWriter.Write(ctx.Formatter, "config doctor", true, doctorExitCode, new
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
            (ctx.Files ?? []).Select(f => PathHelper.NormalizeSeparators(f.RelativePath)),
            StringComparer.OrdinalIgnoreCase);

        // 1. Dead start_here entries
        if (ctx.Policy?.Governance?.StartHere != null)
        {
            foreach (var entry in ctx.Policy.Governance.StartHere)
            {
                if (!existingPaths.Contains(PathHelper.NormalizeSeparators(entry)))
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
                var artifactPath = PathHelper.NormalizeAndTrim(artifact.Path!);
                if (!existingPaths.Contains(artifactPath))
                {
                    findings.Add(new DoctorFinding(
                        "missing-artifact",
                        $"Artifact declaration '{artifactPath}' does not match any discovered file.",
                        $"Create the file at '{artifactPath}' or remove the artifact from policy.yaml."));
                }
            }
        }

        // 4. Path-policy rulesets matching no files
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
                        anyMatch = existingPaths.Contains(PathHelper.NormalizeSeparators(rule.Pattern));
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

        // 5. Maintenance sources matching nothing
        if (ctx.Policy?.Maintenance?.Artifacts != null)
        {
            foreach (var maint in ctx.Policy.Maintenance.Artifacts)
            {
                if (string.IsNullOrWhiteSpace(maint.Source)) continue;
                if (!MaintenanceSourceMatcher.MatchesAny(maint.Source, existingPaths))
                {
                    findings.Add(new DoctorFinding(
                        "unmatched-maintenance-source",
                        $"Maintenance source '{maint.Source}' for '{maint.Id ?? maint.Path}' matches no discovered files.",
                        $"Update the source pattern or remove the maintenance artifact."));
                }
            }
        }

        // 6. Dead suppressions — disabled rules that would produce no violations anyway
        if (ctx.Policy?.Validation?.DisabledRules is { Count: > 0 })
        {
            var allRuleIds = new HashSet<string>(
                CheckCommand.AllRules.Select(r => r.RuleId), StringComparer.OrdinalIgnoreCase);

            foreach (var ruleId in ctx.Policy.Validation.DisabledRules)
            {
                if (!allRuleIds.Contains(ruleId))
                {
                    findings.Add(new DoctorFinding(
                        "dead-suppression",
                        $"Disabled rule '{ruleId}' is not a recognized rule id.",
                        $"Remove '{ruleId}' from validation.disabled_rules."));
                }
            }
        }

        // 7. Unreachable path-override patterns
        if (ctx.Policy?.Validation?.PathOverrides is { Count: > 0 })
        {
            foreach (var ov in ctx.Policy.Validation.PathOverrides)
            {
                if (string.IsNullOrWhiteSpace(ov.Pattern) || ov.DisabledRules == null) continue;
                var glob = Glob.Parse(ov.Pattern);
                if (!existingPaths.Any(p => glob.IsMatch(p)))
                {
                    findings.Add(new DoctorFinding(
                        "unreachable-path-override",
                        $"Path override pattern '{ov.Pattern}' matches no discovered files.",
                        $"Update the pattern or remove the override from validation.path_overrides."));
                }
            }
        }

        // 8. Unreachable frontmatter-requirement patterns
        if (ctx.Policy?.Validation?.FrontmatterRequirements is { Count: > 0 })
        {
            foreach (var req in ctx.Policy.Validation.FrontmatterRequirements)
            {
                if (string.IsNullOrWhiteSpace(req.Pattern)) continue;
                var glob = Glob.Parse(req.Pattern);
                if (!existingPaths.Any(p => glob.IsMatch(p)))
                {
                    findings.Add(new DoctorFinding(
                        "unreachable-frontmatter-pattern",
                        $"Frontmatter requirement pattern '{req.Pattern}' matches no discovered files.",
                        $"Update the pattern or remove the requirement from validation.frontmatter_requirements."));
                }
            }
        }

        // 9. Unreachable artifact family path patterns
        if (ctx.Policy?.ArtifactFamilies is { Count: > 0 })
        {
            foreach (var family in ctx.Policy.ArtifactFamilies)
            {
                if (string.IsNullOrWhiteSpace(family.Family) ||
                    string.IsNullOrWhiteSpace(family.Match?.PathPattern))
                    continue;

                var glob = Glob.Parse(family.Match.PathPattern);
                if (!existingPaths.Any(p => glob.IsMatch(p)))
                {
                    findings.Add(new DoctorFinding(
                        "unreachable-family-pattern",
                        $"Artifact family '{family.Family}' path_pattern '{family.Match.PathPattern}' matches no discovered files.",
                        $"Update the path_pattern or remove the family from artifact_families."));
                }
            }
        }

        // 10. index_of directories that do not exist (artifacts with index_of pointing at non-existent dirs)
        if (ctx.Policy?.Artifacts != null)
        {
            foreach (var artifact in ctx.Policy.Artifacts)
            {
                if (string.IsNullOrWhiteSpace(artifact.IndexOf)) continue;
                var indexOfDir = PathHelper.NormalizeSeparators(artifact.IndexOf.TrimEnd('/')) + "/";
                var dirExists = existingPaths.Any(p => p.StartsWith(indexOfDir, StringComparison.OrdinalIgnoreCase));
                if (!dirExists)
                {
                    findings.Add(new DoctorFinding(
                        "dead-index-of-directory",
                        $"Artifact '{artifact.Path}' declares index_of: '{artifact.IndexOf}', but no files were discovered in that directory. " +
                        $"STWD-011 (IndexCompletenessRule) will not enforce completeness for this index.",
                        $"Check that the directory '{artifact.IndexOf}' exists and is not excluded by discovery.exclude patterns. " +
                        $"Update index_of or remove the declaration if the directory is intentionally empty."));
                }
            }
        }

        // 11. Artifact paths that would be excluded by discovery.exclude patterns
        if (ctx.Policy?.Artifacts != null && ctx.EffectiveDiscoveryExcludes.Count > 0)
        {
            var excludeGlobs = ctx.EffectiveDiscoveryExcludes
                .Select(p => { try { return Glob.Parse(p); } catch { return null; } })
                .Where(g => g != null)
                .ToList();

            foreach (var artifact in ctx.Policy.Artifacts)
            {
                if (string.IsNullOrWhiteSpace(artifact.Path)) continue;
                var artifactPath = PathHelper.NormalizeAndTrim(artifact.Path!);
                if (existingPaths.Contains(artifactPath)) continue; // Already discovered, not excluded

                // Check if it would be excluded by a discovery.exclude pattern
                if (excludeGlobs.Any(g => g!.IsMatch(artifactPath)))
                {
                    findings.Add(new DoctorFinding(
                        "artifact-excluded-by-discovery",
                        $"Artifact '{artifactPath}' is declared in policy.yaml but is excluded by a discovery.exclude pattern. " +
                        $"STWD-001 will report it as missing, but the real cause is the exclude pattern.",
                        $"Remove '{artifactPath}' from the discovery.exclude list in config.yaml, " +
                        $"or remove/relocate the artifact declaration in policy.yaml."));
                }
            }
        }

        // 12. Conflicting allowed_values for the same frontmatter field between scoped requirements and families
        if (ctx.Policy?.Validation?.FrontmatterRequirements is { Count: > 0 } &&
            ctx.Policy?.ArtifactFamilies is { Count: > 0 })
        {
            foreach (var family in ctx.Policy.ArtifactFamilies)
            {
                if (family.Match?.PathPattern == null || family.FrontmatterSchema?.AllowedValues == null)
                    continue;

                var familyGlob = Glob.Parse(family.Match.PathPattern);

                foreach (var req in ctx.Policy.Validation.FrontmatterRequirements)
                {
                    if (string.IsNullOrWhiteSpace(req.Pattern) || req.AllowedValues == null)
                        continue;

                    var reqGlob = Glob.Parse(req.Pattern);
                    var hasOverlap = existingPaths.Any(path => familyGlob.IsMatch(path) && reqGlob.IsMatch(path));
                    if (!hasOverlap)
                        continue;

                    // Check for conflicting allowed_values on the same field
                    foreach (var (field, familyValues) in family.FrontmatterSchema.AllowedValues)
                    {
                        if (!req.AllowedValues.TryGetValue(field, out var reqValues))
                            continue;

                        // If both declare allowed_values for the same field but with different sets, warn
                        var familySet = new HashSet<string>(familyValues, StringComparer.OrdinalIgnoreCase);
                        var reqSet = new HashSet<string>(reqValues, StringComparer.OrdinalIgnoreCase);
                        if (!familySet.SetEquals(reqSet))
                        {
                            findings.Add(new DoctorFinding(
                                "conflicting-allowed-values",
                                $"Artifact family '{family.Family}' and frontmatter requirement pattern '{req.Pattern}' " +
                                $"declare conflicting allowed_values for field '{field}'. " +
                                $"Family allows: [{string.Join(", ", familyValues)}]. " +
                                $"Requirement allows: [{string.Join(", ", reqValues)}]. " +
                                $"For files matched by both, STWD-003 will use the family's allowed_values.",
                                $"Align the allowed_values for '{field}' between the family '{family.Family}' " +
                                $"and the frontmatter requirement pattern '{req.Pattern}', or remove the overlap."));
                        }
                    }
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
        var command = new Command("suggest", "Analyze the repository and suggest artifact declarations with confidence hints plus exclude patterns for policy.yaml");

        command.SetAction(parseResult =>
        {
            if (!CommandSetup.TryBuild(parseResult, out var ctx, "config suggest"))
                return ExitCodes.UsageError;

            var suggestion = BootstrapAnalyzer.Analyze(ctx!.Files!, ctx.FileSystem, ctx.RootPath, ctx.Policy);

            if (ctx.OutputFormat == OutputFormat.Json)
            {
                JsonEnvelopeWriter.Write(ctx.Formatter, "config suggest", true, ExitCodes.Success, suggestion);
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
                    {
                        var conservativeLabel = a.Conservative ? " [conservative]" : "";
                        ctx.Formatter.WriteMessage(
                            $"  - {a.Path} (role: {a.Role}, importance: {a.Importance}, confidence: {a.Confidence}){conservativeLabel} — {a.Reason}");
                    }
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
                    ctx.Formatter.WriteMessage("These are suggestions only. Review low-confidence or [conservative] entries before editing .steward/policy.yaml.");
                }
            }

            return ExitCodes.Success;
        });

        return command;
    }

}
