using System.CommandLine;
using Steward.Core;

namespace Steward.Cli;

public static class GlobalOptionsSetup
{
    public static readonly Option<OutputFormat> OutputOption = new(
        "--output", "-o")
    {
        Description = "Output format: text or json",
        DefaultValueFactory = _ => OutputFormat.Text
    };

    public static readonly Option<Verbosity> VerbosityOption = new(
        "--verbosity", "-v")
    {
        Description = "Verbosity level: quiet, normal, verbose, or debug",
        DefaultValueFactory = _ => Verbosity.Normal
    };

    public static readonly Option<bool> NoColorOption = new(
        "--no-color")
    {
        Description = "Disable colored output"
    };

    public static readonly Option<string?> ConfigOption = new(
        "--config", "-c")
    {
        Description = "Path to .steward configuration directory",
        HelpName = "path"
    };

    public static readonly Option<JsonEnvelopeMode> JsonEnvelopeOption = new(
        "--json-envelope")
    {
        Description = "JSON envelope mode when --output json is active: legacy (default) or standard",
        DefaultValueFactory = _ => JsonEnvelopeMode.Legacy
    };

    public static void AddGlobalOptions(RootCommand rootCommand)
    {
        OutputOption.Recursive = true;
        VerbosityOption.Recursive = true;
        NoColorOption.Recursive = true;
        ConfigOption.Recursive = true;
        JsonEnvelopeOption.Recursive = true;

        rootCommand.Add(OutputOption);
        rootCommand.Add(VerbosityOption);
        rootCommand.Add(NoColorOption);
        rootCommand.Add(ConfigOption);
        rootCommand.Add(JsonEnvelopeOption);
    }
}
