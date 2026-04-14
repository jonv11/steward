using System.CommandLine;
using Steward.Core;
using Steward.Core.Formatting;
using Steward.Core.Markdown;

namespace Steward.Cli.Commands;

public static class MdEditCommand
{
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
            var content = parseResult.GetValue(contentOpt);
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
            var content = parseResult.GetValue(contentOpt)!;
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
        command.Add(headingOpt);
        command.Add(underOpt);
        command.Add(contentOpt);

        command.SetAction(parseResult =>
        {
            var file = parseResult.GetValue(fileArg)!;
            var heading = parseResult.GetValue(headingOpt)!;
            var under = parseResult.GetValue(underOpt);
            var content = parseResult.GetValue(contentOpt);
            var apply = parseResult.GetValue(applyOption);

            return ExecuteEdit(parseResult, file, apply,
                doc => StructuralEditor.InsertSection(doc, heading, under, content));
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
            var content = parseResult.GetValue(contentOpt)!;
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
            var content = parseResult.GetValue(contentOpt)!;
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
