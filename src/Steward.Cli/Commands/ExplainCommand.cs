using System.CommandLine;
using DotNet.Globbing;
using Steward.Core;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Formatting;
using Steward.Core.Markdown;
using Steward.Core.Orientation;
using Steward.Core.Validation;
namespace Steward.Cli.Commands;

public static class ExplainCommand
{
    private static readonly IValidationRule[] AllRules = RuleRegistry.CreateAllRules();

    public static Command Create()
    {
        var command = new Command("explain", "Explain a validation rule, or use 'explain path <file>' to show effective governance for a file. Run without arguments to list all rules.");

        var ruleArg = new Argument<string?>("rule-id")
        {
            Description = "Rule ID to explain (e.g., STWD-001). Omit to list all rules.",
            DefaultValueFactory = _ => null
        };
        command.Add(ruleArg);

        command.SetAction(parseResult =>
        {
            var output = parseResult.GetValue(GlobalOptionsSetup.OutputOption);
            var noColor = parseResult.GetValue(GlobalOptionsSetup.NoColorOption);
            var verbosity = parseResult.GetValue(GlobalOptionsSetup.VerbosityOption);
            var ruleId = parseResult.GetValue(ruleArg);

            var formatter = CommandSetup.CreateFormatter(output, noColor);

            if (string.IsNullOrWhiteSpace(ruleId))
            {
                // List all rules
                return ListAllRules(formatter, output);
            }

            var rule = AllRules.FirstOrDefault(r =>
                string.Equals(r.RuleId, ruleId, StringComparison.OrdinalIgnoreCase));

            if (rule == null)
            {
                formatter.WriteError($"Unknown rule ID: '{ruleId}'. Use 'steward explain' to list all rules.");
                return ExitCodes.UsageError;
            }

            // In verbose mode, attempt to show file evaluation count
            int? filesEvaluated = null;
            if (verbosity >= Verbosity.Verbose)
            {
                filesEvaluated = GetFilesEvaluatedCount(parseResult, rule);
            }

            if (output == OutputFormat.Json)
            {
                var jsonObj = new Dictionary<string, object?>
                {
                    ["ruleId"] = rule.RuleId,
                    ["category"] = rule.Category,
                    ["severity"] = rule.DefaultSeverity.ToString().ToLowerInvariant(),
                    ["description"] = rule.Description,
                    ["remediation"] = GetRemediation(rule.RuleId)
                };
                if (filesEvaluated.HasValue)
                    jsonObj["filesEvaluated"] = filesEvaluated.Value;

                formatter.WriteObject(jsonObj);
            }
            else
            {
                formatter.WriteMessage($"Rule: {rule.RuleId}");
                formatter.WriteMessage($"Category: {rule.Category}");
                formatter.WriteMessage($"Default Severity: {rule.DefaultSeverity}");
                formatter.WriteMessage($"Description: {rule.Description}");
                formatter.WriteMessage("");
                formatter.WriteMessage($"Remediation: {GetRemediation(rule.RuleId)}");

                if (filesEvaluated.HasValue)
                {
                    formatter.WriteMessage("");
                    formatter.WriteMessage($"Files evaluated: {filesEvaluated.Value}");
                }
            }

            return ExitCodes.Success;
        });

        // Add `explain path <path>` subcommand
        command.Add(CreatePathSubcommand());

        return command;
    }

    private static int? GetFilesEvaluatedCount(System.CommandLine.ParseResult parseResult, IValidationRule rule)
    {
        try
        {
            if (!CommandSetup.TryBuild(parseResult, out var ctx) || ctx?.Files == null)
                return null;

            // Count files that would be evaluated by this rule
            var files = ctx.Files;

            // For markdown-specific rules, count only .md files
            var ruleCategory = rule.Category.ToLowerInvariant();
            if (ruleCategory is "frontmatter" or "governance" or "link-integrity")
            {
                return files.Count(f => f.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase));
            }

            return files.Count;
        }
        catch
        {
            return null;
        }
    }

    private static int ListAllRules(IOutputFormatter formatter, OutputFormat output)
    {
        if (output == OutputFormat.Json)
        {
            formatter.WriteObject(AllRules.Select(r => new
            {
                ruleId = r.RuleId,
                category = r.Category,
                severity = r.DefaultSeverity.ToString().ToLowerInvariant(),
                description = r.Description
            }).ToArray());
        }
        else
        {
            formatter.WriteMessage("Available validation rules:");
            formatter.WriteMessage("");
            foreach (var rule in AllRules)
            {
                formatter.WriteMessage($"  {rule.RuleId}  [{rule.DefaultSeverity}]  {rule.Description}");
            }
        }
        return ExitCodes.Success;
    }

    internal static string GetRemediation(string ruleId)
    {
        return ruleId switch
        {
            "STWD-001" => "Add the required artifact file to the repository, or mark it as optional in policy.yaml.",
            "STWD-002" => "Remove or rename the file/directory matching the forbidden path pattern.",
            "STWD-003" => "Add the missing frontmatter field to the Markdown file's YAML header.",
            "STWD-004" => "Break the large section into smaller subsections to improve readability.",
            "STWD-005" => "Ensure managed regions have matching begin/end markers with valid id and owner attributes.",
            "STWD-006" => "Do not manually edit content within managed regions. Use 'steward maintain --apply' to update.",
            "STWD-007" => "Run 'steward maintain --apply' to synchronize maintained artifacts.",
            "STWD-008" => "Fix the broken link target or remove the link. Verify the referenced file exists.",
            "STWD-009" => "Create the artifact, remove it from policy.yaml, or mark it as required if it is mandatory.",
            "STWD-010" => "Rename the file to match the naming convention declared in path-policy.yaml.",
            "STWD-011" => "Add a link to the missing file in the index artifact, or move the file outside the indexed directory.",
            "STWD-012" => "Update the document and set the 'last_updated' frontmatter field to the current date.",
            "STWD-013" => "Link to this file from an index or navigation document, or add 'standalone: true' to its frontmatter.",
            _ => "No specific remediation guidance available."
        };
    }

    private static Command CreatePathSubcommand()
    {
        var pathCmd = new Command("path", "Show effective governance for a specific file path.");

        var pathArg = new Argument<string>("file-path")
        {
            Description = "Relative path to the file to explain (e.g., docs/planning/milestone-plan.md)."
        };
        pathCmd.Add(pathArg);

        pathCmd.SetAction(parseResult =>
        {
            var output = parseResult.GetValue(GlobalOptionsSetup.OutputOption);
            var noColor = parseResult.GetValue(GlobalOptionsSetup.NoColorOption);
            var filePath = parseResult.GetValue(pathArg)!.Replace('\\', '/');

            var formatter = CommandSetup.CreateFormatter(output, noColor);

            if (!CommandSetup.TryBuild(parseResult, out var ctx) || ctx == null)
            {
                formatter.WriteError("Could not load steward configuration. Run from a steward-managed repository.");
                return ExitCodes.UsageError;
            }

            var info = ResolveEffectivePolicy(filePath, ctx);

            if (output == OutputFormat.Json)
            {
                var jsonObj = new Dictionary<string, object?>
                {
                    ["path"] = info.Path,
                    ["classification"] = info.Classification,
                    ["pathPolicyCategory"] = info.PathPolicyCategory,
                    ["matchedPattern"] = info.MatchedPattern,
                    ["matchedFamily"] = info.MatchedFamily,
                    ["familyDisplayName"] = info.FamilyDisplayName,
                    ["artifact"] = info.Artifact,
                    ["suppressedRules"] = info.SuppressedRules,
                    ["requiredFrontmatterFields"] = info.RequiredFrontmatterFields,
                    ["allowedValues"] = info.AllowedValues,
                    ["applicableRules"] = info.ApplicableRules
                };
                formatter.WriteObject(jsonObj);
            }
            else
            {
                formatter.WriteMessage($"Path: {info.Path}");
                formatter.WriteMessage($"Classification: {info.Classification}");
                formatter.WriteMessage($"Path-policy category: {info.PathPolicyCategory}");
                if (info.MatchedPattern != null)
                    formatter.WriteMessage($"Matched pattern: {info.MatchedPattern}");

                if (info.MatchedFamily != null)
                {
                    var familyLabel = info.FamilyDisplayName != null
                        ? $"{info.MatchedFamily} ({info.FamilyDisplayName})"
                        : info.MatchedFamily;
                    formatter.WriteMessage($"Family: {familyLabel}");
                }

                if (info.Artifact != null)
                {
                    formatter.WriteMessage("");
                    formatter.WriteMessage("Artifact:");
                    if (info.Artifact.Role != null)
                        formatter.WriteMessage($"  Role: {info.Artifact.Role}");
                    if (info.Artifact.Description != null)
                        formatter.WriteMessage($"  Description: {info.Artifact.Description}");
                    formatter.WriteMessage($"  Required: {info.Artifact.Required}");
                    if (info.Artifact.IndexOf != null)
                        formatter.WriteMessage($"  Index of: {info.Artifact.IndexOf}");
                }

                if (info.SuppressedRules.Count > 0)
                {
                    formatter.WriteMessage("");
                    formatter.WriteMessage($"Suppressed rules: {string.Join(", ", info.SuppressedRules)}");
                }

                if (info.RequiredFrontmatterFields.Count > 0)
                {
                    formatter.WriteMessage("");
                    formatter.WriteMessage($"Required frontmatter: {string.Join(", ", info.RequiredFrontmatterFields)}");
                }

                if (info.AllowedValues.Count > 0)
                {
                    foreach (var (field, values) in info.AllowedValues)
                    {
                        formatter.WriteMessage($"  {field} allowed: {string.Join(", ", values)}");
                    }
                }

                if (info.ApplicableRules.Count > 0)
                {
                    formatter.WriteMessage("");
                    formatter.WriteMessage("Applicable rules:");
                    foreach (var ruleId in info.ApplicableRules)
                    {
                        formatter.WriteMessage($"  {ruleId}");
                    }
                }
            }

            return ExitCodes.Success;
        });

        return pathCmd;
    }

    internal static EffectivePolicyInfo ResolveEffectivePolicy(string relativePath, CommandContext ctx)
    {
        // 1. Orientation classification
        var discoveredFile = ctx.Files?.FirstOrDefault(f =>
            string.Equals(f.RelativePath.Replace('\\', '/'), relativePath, StringComparison.OrdinalIgnoreCase));
        var classification = discoveredFile != null
            ? OrientationEngine.Classify(discoveredFile, ctx.Policy)
            : "unknown";

        // 2. Path-policy category
        var pathEngine = new PathPolicyEngine(ctx.PathPolicy);
        var pathEval = pathEngine.Evaluate(relativePath);

        // 3. Artifact match
        ArtifactSummary? artifactSummary = null;
        if (ctx.Policy?.Artifacts != null)
        {
            var matched = ctx.Policy.Artifacts.FirstOrDefault(a =>
                !string.IsNullOrWhiteSpace(a.Path) &&
                string.Equals(a.Path!.Replace('\\', '/').TrimEnd('/'), relativePath, StringComparison.OrdinalIgnoreCase));
            if (matched != null)
            {
                artifactSummary = new ArtifactSummary
                {
                    Role = matched.Role,
                    Description = matched.Description,
                    Required = matched.Required,
                    IndexOf = matched.IndexOf
                };
            }
        }

        // 4. Suppressed rules
        var suppressedRules = new List<string>();
        if (ctx.Policy?.Validation?.PathOverrides != null)
        {
            foreach (var ov in ctx.Policy.Validation.PathOverrides)
            {
                if (string.IsNullOrWhiteSpace(ov.Pattern) || ov.DisabledRules == null)
                    continue;
                var glob = Glob.Parse(ov.Pattern);
                if (glob.IsMatch(relativePath))
                    suppressedRules.AddRange(ov.DisabledRules);
            }
        }
        if (ctx.Policy?.Validation?.DisabledRules != null)
            suppressedRules.AddRange(ctx.Policy.Validation.DisabledRules);
        suppressedRules = suppressedRules.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        // 5. Effective frontmatter requirements
        var requiredFields = new List<string>();
        if (ctx.Policy?.Validation?.RequiredFrontmatterFields != null)
        {
            requiredFields.AddRange(ctx.Policy.Validation.RequiredFrontmatterFields);
        }

        if (ctx.Policy?.Governance?.Frontmatter?.RequiredFields != null)
        {
            foreach (var field in ctx.Policy.Governance.Frontmatter.RequiredFields)
            {
                if (!requiredFields.Contains(field, StringComparer.OrdinalIgnoreCase))
                    requiredFields.Add(field);
            }
        }

        var allowedValues = new Dictionary<string, List<string>>();

        if (ctx.Policy?.Validation?.FrontmatterRequirements != null)
        {
            foreach (var req in ctx.Policy.Validation.FrontmatterRequirements)
            {
                if (string.IsNullOrWhiteSpace(req.Pattern)) continue;
                var glob = Glob.Parse(req.Pattern);
                if (!glob.IsMatch(relativePath)) continue;

                if (req.RequiredFields != null)
                {
                    foreach (var f in req.RequiredFields)
                    {
                        if (!requiredFields.Contains(f, StringComparer.OrdinalIgnoreCase))
                            requiredFields.Add(f);
                    }
                }
                if (req.AllowedValues != null)
                {
                    foreach (var (field, values) in req.AllowedValues)
                        allowedValues[field] = values;
                }
            }
        }

        // 5b. Family classification (only if not an explicit artifact)
        string? matchedFamily = null;
        string? familyDisplayName = null;
        if (artifactSummary == null && ctx.Policy?.ArtifactFamilies is { Count: > 0 })
        {
            IReadOnlyDictionary<string, object?>? frontmatterFields = null;
            var fullPath = System.IO.Path.Combine(ctx.RootPath, relativePath);
            if (ctx.FileSystem?.FileExists(fullPath) == true)
            {
                try
                {
                    var content = ctx.FileSystem.ReadAllText(fullPath);
                    var doc = MarkdownParser.Parse(relativePath, content);
                    if (doc.Frontmatter?.Fields != null)
                    {
                        frontmatterFields = doc.Frontmatter.Fields
                            .ToDictionary(kv => kv.Key, kv => (object?)kv.Value, StringComparer.OrdinalIgnoreCase);
                    }
                }
                catch
                {
                    // File unreadable — classify by path pattern only
                }
            }

            var classifier = new ArtifactFamilyClassifier(ctx.Policy.ArtifactFamilies);
            var family = classifier.Classify(relativePath, frontmatterFields);
            if (family != null)
            {
                matchedFamily = family.Family;
                familyDisplayName = family.DisplayName;

                // Merge family-level frontmatter requirements
                foreach (var field in family.FrontmatterSchema?.Required ?? [])
                {
                    if (!requiredFields.Contains(field, StringComparer.OrdinalIgnoreCase))
                        requiredFields.Add(field);
                }
                if (family.FrontmatterSchema?.AllowedValues != null)
                {
                    foreach (var (field, values) in family.FrontmatterSchema.AllowedValues)
                        allowedValues[field] = values;
                }
            }
        }

        // 6. Applicable rules — filter by actual relevance to this path
        var suppressedSet = new HashSet<string>(suppressedRules, StringComparer.OrdinalIgnoreCase);
        var isMarkdown = relativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
        var isArtifact = artifactSummary != null;
        var isIndex = artifactSummary?.IndexOf != null;
        var hasFreshness = isArtifact && ctx.Policy?.Artifacts?.FirstOrDefault(a =>
            string.Equals(a.Path?.Replace('\\', '/').TrimEnd('/'), relativePath, StringComparison.OrdinalIgnoreCase))
            ?.Freshness?.MaxAgeDays > 0;
        var isMaintained = ctx.Policy?.Maintenance?.Artifacts?.Any(m =>
            string.Equals(m.Path?.Replace('\\', '/'), relativePath, StringComparison.OrdinalIgnoreCase)) == true;
        var hasFrontmatterReqs = requiredFields.Count > 0;
        var hasManagedRegions = ctx.Policy?.Governance?.ManagedRegions != null;
        var hasPathPolicy = ctx.PathPolicy?.Rulesets is { Count: > 0 };

        var applicableRules = AllRules
            .Where(r => !suppressedSet.Contains(r.RuleId))
            .Where(r => r.RuleId switch
            {
                "STWD-001" => isArtifact,                       // required-artifact: only for declared artifacts
                "STWD-002" => true,                             // forbidden-path: any file
                "STWD-003" => isMarkdown && hasFrontmatterReqs, // required-frontmatter: md files with fm requirements
                "STWD-004" => isMarkdown,                       // section-size: md files
                "STWD-005" => isMarkdown && hasManagedRegions,  // managed-region-integrity: md with managed regions
                "STWD-006" => isMarkdown && hasManagedRegions,  // managed-scope-violation: md under managed scopes
                "STWD-007" => isMaintained,                     // stale-artifact: maintained artifacts
                "STWD-008" => isMarkdown,                       // broken-internal-link: md files
                "STWD-009" => isMarkdown,                       // broken-artifact-reference: md files
                "STWD-010" => hasPathPolicy,                    // naming-convention: if path-policy rules exist
                "STWD-011" => isIndex,                          // index-completeness: index artifacts
                "STWD-012" => isArtifact && hasFreshness,       // freshness: artifacts with freshness config
                "STWD-013" => isMarkdown,                       // orphaned-document: md files
                _ => true
            })
            .Select(r => r.RuleId)
            .ToList();

        return new EffectivePolicyInfo
        {
            Path = relativePath,
            Classification = classification,
            PathPolicyCategory = pathEval.Category,
            MatchedPattern = pathEval.MatchedPattern,
            Artifact = artifactSummary,
            MatchedFamily = matchedFamily,
            FamilyDisplayName = familyDisplayName,
            SuppressedRules = suppressedRules,
            RequiredFrontmatterFields = requiredFields,
            AllowedValues = allowedValues,
            ApplicableRules = applicableRules
        };
    }

    internal sealed class EffectivePolicyInfo
    {
        public string Path { get; set; } = "";
        public string Classification { get; set; } = "unknown";
        public string PathPolicyCategory { get; set; } = "unclassified";
        public string? MatchedPattern { get; set; }
        public ArtifactSummary? Artifact { get; set; }
        public string? MatchedFamily { get; set; }
        public string? FamilyDisplayName { get; set; }
        public List<string> SuppressedRules { get; set; } = [];
        public List<string> RequiredFrontmatterFields { get; set; } = [];
        public Dictionary<string, List<string>> AllowedValues { get; set; } = [];
        public List<string> ApplicableRules { get; set; } = [];
    }

    internal sealed class ArtifactSummary
    {
        public string? Role { get; set; }
        public string? Description { get; set; }
        public bool Required { get; set; }
        public string? IndexOf { get; set; }
    }
}
