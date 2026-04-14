using System.CommandLine;
using Steward.Core;
using Steward.Core.Formatting;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;

namespace Steward.Cli.Commands;

public static class ExplainCommand
{
    private static readonly IValidationRule[] AllRules =
    [
        new RequiredArtifactRule(),
        new ForbiddenPathRule(),
        new RequiredFrontmatterFieldRule(),
        new SectionSizeRule(),
        new ManagedRegionIntegrityRule(),
        new ManagedScopeViolationRule(),
        new StaleArtifactRule(),
        new BrokenInternalLinkRule(),
        new BrokenArtifactReferenceRule()
    ];

    public static Command Create()
    {
        var command = new Command("explain", "Explain a validation rule. Run without arguments to list all rules.");

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

            if (output == OutputFormat.Json)
            {
                formatter.WriteObject(new
                {
                    ruleId = rule.RuleId,
                    category = rule.Category,
                    severity = rule.DefaultSeverity.ToString().ToLowerInvariant(),
                    description = rule.Description,
                    remediation = GetRemediation(rule.RuleId)
                });
            }
            else
            {
                formatter.WriteMessage($"Rule: {rule.RuleId}");
                formatter.WriteMessage($"Category: {rule.Category}");
                formatter.WriteMessage($"Default Severity: {rule.DefaultSeverity}");
                formatter.WriteMessage($"Description: {rule.Description}");
                formatter.WriteMessage("");
                formatter.WriteMessage($"Remediation: {GetRemediation(rule.RuleId)}");
            }

            return ExitCodes.Success;
        });

        return command;
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
            _ => "No specific remediation guidance available."
        };
    }

}
