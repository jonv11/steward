using System.CommandLine;
using DotNet.Globbing;
using Steward.Core;
using Steward.Core.Configuration;
using Steward.Core.Formatting;
using Steward.Core.Markdown;

namespace Steward.Cli.Commands;

public static class MdEditCommand
{
    /// <summary>
    /// Resolves content from the --content option. If the value is "-", reads from stdin.
    /// </summary>
    private static string? ResolveContent(string? value)
    {
        if (value == "-")
            return Console.In.ReadToEnd();
        return value;
    }

    public static Command Create()
    {
        var command = new Command("edit", "Structural Markdown editing with preview/apply workflow");

        command.Add(CreateEnsureSectionCommand());
        command.Add(CreateSetSectionCommand());
        command.Add(CreateInsertSectionCommand());
        command.Add(CreateAppendBlockCommand());
        command.Add(CreatePrependBlockCommand());
        command.Add(CreateFmSetCommand());
        command.Add(CreateFmMergeCommand());
        command.Add(CreateFmValidateCommand());

        return command;
    }

    private static (Argument<string> file, Option<bool> apply) AddCommonParams(Command command)
    {
        var fileArg = new Argument<string>("file") { Description = "Path to the Markdown file" };
        var applyOption = new Option<bool>("--apply")
        {
            Description = "Apply changes to the file instead of previewing"
        };
        command.Add(fileArg);
        command.Add(applyOption);
        return (fileArg, applyOption);
    }

    private static Command CreateEnsureSectionCommand()
    {
        var command = new Command("ensure-section", "Create a section if it does not exist");
        var (fileArg, applyOption) = AddCommonParams(command);
        var headingOpt = new Option<string>("--heading") { Description = "Heading text for the section" };
        headingOpt.Required = true;
        var underOpt = new Option<string?>("--under") { Description = "Parent section heading" };
        var contentOpt = new Option<string?>("--content") { Description = "Section body content" };
        command.Add(headingOpt);
        command.Add(underOpt);
        command.Add(contentOpt);

        command.SetAction(parseResult =>
        {
            var file = parseResult.GetValue(fileArg)!;
            var heading = parseResult.GetValue(headingOpt)!;
            var under = parseResult.GetValue(underOpt);
            var content = ResolveContent(parseResult.GetValue(contentOpt));
            var apply = parseResult.GetValue(applyOption);

            return ExecuteEdit(parseResult, file, apply,
                doc => StructuralEditor.EnsureSection(doc, heading, under, content));
        });

        return command;
    }

    private static Command CreateSetSectionCommand()
    {
        var command = new Command("set-section", "Replace the content of a section");
        var (fileArg, applyOption) = AddCommonParams(command);
        var headingOpt = new Option<string>("--heading") { Description = "Heading of the target section" };
        headingOpt.Required = true;
        var contentOpt = new Option<string>("--content") { Description = "New content for the section" };
        contentOpt.Required = true;
        command.Add(headingOpt);
        command.Add(contentOpt);

        command.SetAction(parseResult =>
        {
            var file = parseResult.GetValue(fileArg)!;
            var heading = parseResult.GetValue(headingOpt)!;
            var content = ResolveContent(parseResult.GetValue(contentOpt))!;
            var apply = parseResult.GetValue(applyOption);

            return ExecuteEdit(parseResult, file, apply,
                doc => StructuralEditor.SetSection(doc, heading, content));
        });

        return command;
    }

    private static Command CreateInsertSectionCommand()
    {
        var command = new Command("insert-section", "Insert a new section");
        var (fileArg, applyOption) = AddCommonParams(command);
        var headingOpt = new Option<string>("--heading") { Description = "Heading text for the new section" };
        headingOpt.Required = true;
        var underOpt = new Option<string?>("--under") { Description = "Parent section heading" };
        var contentOpt = new Option<string?>("--content") { Description = "Section body content" };
        var afterOpt = new Option<string?>("--after") { Description = "Insert after this sibling section" };
        var beforeOpt = new Option<string?>("--before") { Description = "Insert before this sibling section" };
        var levelOpt = new Option<int?>("--level") { Description = "Explicit heading level (1-6)" };
        command.Add(headingOpt);
        command.Add(underOpt);
        command.Add(contentOpt);
        command.Add(afterOpt);
        command.Add(beforeOpt);
        command.Add(levelOpt);

        command.SetAction(parseResult =>
        {
            var file = parseResult.GetValue(fileArg)!;
            var heading = parseResult.GetValue(headingOpt)!;
            var under = parseResult.GetValue(underOpt);
            var content = ResolveContent(parseResult.GetValue(contentOpt));
            var after = parseResult.GetValue(afterOpt);
            var before = parseResult.GetValue(beforeOpt);
            var level = parseResult.GetValue(levelOpt);
            var apply = parseResult.GetValue(applyOption);

            return ExecuteEdit(parseResult, file, apply,
                doc => StructuralEditor.InsertSection(doc, heading, under, content, after, before, level));
        });

        return command;
    }

    private static Command CreateAppendBlockCommand()
    {
        var command = new Command("append-block", "Append content to a section");
        var (fileArg, applyOption) = AddCommonParams(command);
        var underOpt = new Option<string>("--under") { Description = "Target section heading" };
        underOpt.Required = true;
        var contentOpt = new Option<string>("--content") { Description = "Content to append" };
        contentOpt.Required = true;
        command.Add(underOpt);
        command.Add(contentOpt);

        command.SetAction(parseResult =>
        {
            var file = parseResult.GetValue(fileArg)!;
            var under = parseResult.GetValue(underOpt)!;
            var content = ResolveContent(parseResult.GetValue(contentOpt))!;
            var apply = parseResult.GetValue(applyOption);

            return ExecuteEdit(parseResult, file, apply,
                doc => StructuralEditor.AppendBlock(doc, under, content));
        });

        return command;
    }

    private static Command CreatePrependBlockCommand()
    {
        var command = new Command("prepend-block", "Prepend content to a section");
        var (fileArg, applyOption) = AddCommonParams(command);
        var underOpt = new Option<string>("--under") { Description = "Target section heading" };
        underOpt.Required = true;
        var contentOpt = new Option<string>("--content") { Description = "Content to prepend" };
        contentOpt.Required = true;
        command.Add(underOpt);
        command.Add(contentOpt);

        command.SetAction(parseResult =>
        {
            var file = parseResult.GetValue(fileArg)!;
            var under = parseResult.GetValue(underOpt)!;
            var content = ResolveContent(parseResult.GetValue(contentOpt))!;
            var apply = parseResult.GetValue(applyOption);

            return ExecuteEdit(parseResult, file, apply,
                doc => StructuralEditor.PrependBlock(doc, under, content));
        });

        return command;
    }

    private static Command CreateFmSetCommand()
    {
        var command = new Command("fm-set", "Set a frontmatter field");
        var (fileArg, applyOption) = AddCommonParams(command);
        var keyOpt = new Option<string>("--key") { Description = "Field key" };
        keyOpt.Required = true;
        var valueOpt = new Option<string>("--value") { Description = "Field value" };
        valueOpt.Required = true;
        command.Add(keyOpt);
        command.Add(valueOpt);

        command.SetAction(parseResult =>
        {
            var file = parseResult.GetValue(fileArg)!;
            var key = parseResult.GetValue(keyOpt)!;
            var value = parseResult.GetValue(valueOpt)!;
            var apply = parseResult.GetValue(applyOption);

            return ExecuteEdit(parseResult, file, apply,
                doc => FrontmatterEditor.SetField(doc, key, value));
        });

        return command;
    }

    private static Command CreateFmMergeCommand()
    {
        var command = new Command("fm-merge", "Merge YAML into frontmatter");
        var (fileArg, applyOption) = AddCommonParams(command);
        var inputOpt = new Option<string>("--input") { Description = "YAML string or file path to merge" };
        inputOpt.Required = true;
        command.Add(inputOpt);

        command.SetAction(parseResult =>
        {
            var file = parseResult.GetValue(fileArg)!;
            var input = parseResult.GetValue(inputOpt)!;
            var apply = parseResult.GetValue(applyOption);

            var yamlInput = input;
            if (File.Exists(input))
            {
                yamlInput = File.ReadAllText(input);
            }

            return ExecuteEdit(parseResult, file, apply,
                doc => FrontmatterEditor.MergeFields(doc, yamlInput));
        });

        return command;
    }

    private static Command CreateFmValidateCommand()
    {
        var command = new Command("fm-validate", "Validate frontmatter against policy requirements");
        var fileArg = new Argument<string>("file") { Description = "Path to the Markdown file" };
        command.Add(fileArg);

        command.SetAction(parseResult =>
        {
            var output = parseResult.GetValue(GlobalOptionsSetup.OutputOption);
            var noColor = parseResult.GetValue(GlobalOptionsSetup.NoColorOption);
            var formatter = CommandSetup.CreateFormatter(output, noColor);

            var file = parseResult.GetValue(fileArg)!;
            var fullPath = Path.GetFullPath(file);
            if (!File.Exists(fullPath))
            {
                formatter.WriteError($"File not found: {file}");
                return ExitCodes.UsageError;
            }

            if (!file.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                formatter.WriteError("fm-validate only supports Markdown (.md) files.");
                return ExitCodes.UsageError;
            }

            // Load policy context for frontmatter requirements
            if (!CommandSetup.TryBuild(parseResult, out var ctx))
                return ExitCodes.UsageError;

            var policy = ctx!.Policy;

            // Compute effective requirements
            var globalRequired = (policy?.Validation?.RequiredFrontmatterFields ?? [])
                .Union(policy?.Governance?.Frontmatter?.RequiredFields ?? [], StringComparer.OrdinalIgnoreCase)
                .ToList();

            var scopedRequirements = (policy?.Validation?.FrontmatterRequirements ?? [])
                .Where(r => !string.IsNullOrWhiteSpace(r.Pattern))
                .Select(r => (Glob: Glob.Parse(r.Pattern!), r.RequiredFields, r.AllowedValues))
                .ToList();

            // Compute relative path for pattern matching
            var relativePath = ctx.RootPath != null
                ? Path.GetRelativePath(ctx.RootPath, fullPath).Replace('\\', '/')
                : file.Replace('\\', '/');

            // Merge global + scoped required fields
            var effectiveRequired = new HashSet<string>(globalRequired, StringComparer.OrdinalIgnoreCase);
            var effectiveAllowed = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var req in scopedRequirements)
            {
                if (req.Glob.IsMatch(relativePath))
                {
                    if (req.RequiredFields != null)
                        foreach (var f in req.RequiredFields) effectiveRequired.Add(f);
                    if (req.AllowedValues != null)
                        foreach (var (k, v) in req.AllowedValues) effectiveAllowed[k] = v;
                }
            }

            if (effectiveRequired.Count == 0 && effectiveAllowed.Count == 0)
            {
                if (output == OutputFormat.Json)
                    formatter.WriteObject(new { valid = true, issues = Array.Empty<object>(), message = "No frontmatter requirements apply to this file." });
                else
                    formatter.WriteMessage("No frontmatter requirements apply to this file.");
                return ExitCodes.Success;
            }

            var content = File.ReadAllText(fullPath);
            var doc = MarkdownParser.Parse(fullPath, content);
            var issues = new List<string>();

            if (doc.Frontmatter == null)
            {
                issues.Add("File is missing a frontmatter block.");
            }
            else
            {
                foreach (var field in effectiveRequired)
                {
                    if (!doc.Frontmatter.Fields.ContainsKey(field))
                        issues.Add($"Required field '{field}' is missing.");
                }

                foreach (var (field, allowed) in effectiveAllowed)
                {
                    if (doc.Frontmatter.Fields.TryGetValue(field, out var rawValue) && rawValue != null)
                    {
                        var value = rawValue.ToString();
                        if (value != null && !allowed.Any(v => string.Equals(v, value, StringComparison.OrdinalIgnoreCase)))
                            issues.Add($"Field '{field}' has value '{value}' which is not in allowed set [{string.Join(", ", allowed)}].");
                    }
                }
            }

            if (output == OutputFormat.Json)
            {
                formatter.WriteObject(new
                {
                    valid = issues.Count == 0,
                    issues,
                    requiredFields = effectiveRequired.ToList(),
                    allowedValues = effectiveAllowed
                });
            }
            else
            {
                if (issues.Count == 0)
                {
                    formatter.WriteMessage("Frontmatter is valid.");
                }
                else
                {
                    formatter.WriteMessage($"Found {issues.Count} frontmatter issue(s):");
                    foreach (var issue in issues)
                        formatter.WriteMessage($"  - {issue}");
                }
            }

            return issues.Count > 0 ? ExitCodes.ValidationFailure : ExitCodes.Success;
        });

        return command;
    }

    private static int ExecuteEdit(System.CommandLine.ParseResult parseResult,
        string file, bool apply, Func<StructuredDocument, EditResult> editFunc)
    {
        var output = parseResult.GetValue(GlobalOptionsSetup.OutputOption);
        var noColor = parseResult.GetValue(GlobalOptionsSetup.NoColorOption);
        var formatter = CommandSetup.CreateFormatter(output, noColor);

        var fullPath = Path.GetFullPath(file);
        if (!File.Exists(fullPath))
        {
            formatter.WriteError($"File not found: {file}");
            return ExitCodes.UsageError;
        }

        var content = File.ReadAllText(fullPath);
        var doc = MarkdownParser.Parse(fullPath, content);
        var result = editFunc(doc);

        if (result.IsError)
        {
            formatter.WriteError(result.Message);
            return ExitCodes.UsageError;
        }

        if (output == OutputFormat.Json)
        {
            formatter.WriteObject(new
            {
                hasChanges = result.HasChanges,
                message = result.Message,
                diff = result.HasChanges ? result.GetUnifiedDiff() : null,
                applied = apply && result.HasChanges
            });
        }
        else
        {
            formatter.WriteMessage(result.Message);
            if (result.HasChanges)
            {
                formatter.WriteMessage(result.GetUnifiedDiff());
            }
        }

        if (apply && result.HasChanges)
        {
            File.WriteAllText(fullPath, result.NewContent);
            if (output != OutputFormat.Json)
            {
                formatter.WriteMessage("Changes applied.");
            }
        }

        return ExitCodes.Success;
    }

}
