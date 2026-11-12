using System.CommandLine;
using System.Reflection;
using Steward.Cli.Formatting;
using Steward.Core;

namespace Steward.Cli.Commands;

/// <summary>
/// <c>steward schema [&lt;command&gt;]</c>: lists the JSON Schemas (draft 2020-12) that describe
/// <c>--output json</c>, or prints one. The schemas are embedded in the tool, so the contract an agent
/// reads always matches the binary it runs.
/// </summary>
public static class SchemaCommand
{
    private const string ResourcePrefix = "Steward.Cli.Schemas.v1.";

    /// <summary>The published schemas, by the command they describe. <c>common</c> holds the envelope.</summary>
    internal static readonly IReadOnlyList<(string Command, string File)> Schemas =
    [
        ("check", "check.schema.json"),
        ("common", "common.schema.json"),
        ("completions", "completions.schema.json"),
        ("schema", "schema.schema.json"),
        ("version", "version.schema.json"),
    ];

    public static Command Create()
    {
        var command = new Command(
            "schema",
            "List the JSON Schemas for '--output json', or print one (e.g. 'steward schema check')");

        var nameArg = new Argument<string?>("command")
        {
            Description = "The command whose schema to print, or 'common'. Omit to list the schemas.",
            DefaultValueFactory = _ => null
        };
        command.Add(nameArg);

        command.SetAction(parseResult =>
        {
            var output = parseResult.GetValue(GlobalOptionsSetup.OutputOption);
            var noColor = parseResult.GetValue(GlobalOptionsSetup.NoColorOption);
            var name = parseResult.GetValue(nameArg);
            var formatter = CommandSetup.CreateFormatter(output, noColor);

            if (string.IsNullOrWhiteSpace(name))
            {
                if (output == OutputFormat.Json)
                {
                    JsonEnvelopeWriter.Write(formatter, "schema", true, ExitCodes.Success, new
                    {
                        schemas = Schemas.Select(s => new { command = s.Command, file = s.File }).ToArray()
                    });
                }
                else
                {
                    foreach (var (schemaCommand, file) in Schemas)
                        formatter.WriteMessage($"{schemaCommand}\t{file}");
                }

                return ExitCodes.Success;
            }

            var match = Schemas.FirstOrDefault(s => string.Equals(s.Command, name.Trim(), StringComparison.Ordinal));
            if (match.File is null)
            {
                const string next = "Run 'steward schema' to list the published schemas.";
                if (output == OutputFormat.Json)
                {
                    JsonEnvelopeWriter.WriteError(formatter, "schema", ExitCodes.UsageError, "unknown-schema",
                        $"No schema for '{name}'.",
                        details: new Dictionary<string, object> { ["command"] = name },
                        suggestedNextStep: next);
                }
                else
                {
                    formatter.WriteError($"No schema for '{name}'. {next}");
                }

                return ExitCodes.UsageError;
            }

            Console.Out.Write(Read(match.File));
            return ExitCodes.Success;
        });

        return command;
    }

    /// <summary>The text of one embedded schema file.</summary>
    internal static string Read(string file)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourcePrefix + file)
            ?? throw new InvalidOperationException($"Schema resource '{file}' is missing from the build.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
