using System.CommandLine;
using Steward.Core;
using Steward.Core.Abstractions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Formatting;
using Steward.Cli.Formatting;

namespace Steward.Cli;

/// <summary>
/// Centralized setup logic shared across all commands.
/// </summary>
public static class CommandSetup
{
    public static IOutputFormatter CreateFormatter(OutputFormat format, bool noColor)
    {
        return format switch
        {
            OutputFormat.Json => new JsonOutputFormatter(Console.Out),
            // SARIF writes its own buffered JSON to stdout; route all other messages to stderr.
            OutputFormat.Sarif => new TextOutputFormatter(Console.Error, false),
            _ => new TextOutputFormatter(Console.Out, !noColor && !Console.IsOutputRedirected)
        };
    }

    public static OutputFormat ResolveRequestedOutputFormat(ParseResult? parseResult = null, IReadOnlyList<string>? rawArgs = null)
    {
        if (parseResult != null)
        {
            try
            {
                var optResult = parseResult.GetResult(GlobalOptionsSetup.OutputOption);
                if (optResult is { Implicit: false })
                    return parseResult.GetValue(GlobalOptionsSetup.OutputOption);
            }
            catch
            {
                // Fall back to raw-argument inspection below.
            }
        }

        if (rawArgs != null)
        {
            for (var i = 0; i < rawArgs.Count; i++)
            {
                var arg = rawArgs[i];

                if (arg.Equals("--output", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("-o", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < rawArgs.Count &&
                        Enum.TryParse<OutputFormat>(rawArgs[i + 1], ignoreCase: true, out var explicitFormat))
                    {
                        return explicitFormat;
                    }
                }
                else if (arg.StartsWith("--output=", StringComparison.OrdinalIgnoreCase))
                {
                    var value = arg["--output=".Length..];
                    if (Enum.TryParse<OutputFormat>(value, ignoreCase: true, out var inlineFormat))
                        return inlineFormat;
                }
            }
        }

        return OutputFormat.Text;
    }

    public static void WriteCommandError(
        ParseResult parseResult,
        string command,
        int exitCode,
        string kind,
        string message,
        Dictionary<string, object>? details = null,
        bool retryable = false,
        string? suggestedNextStep = null)
    {
        var output = ResolveRequestedOutputFormat(parseResult);
        var formatter = CreateFormatter(output, parseResult.GetValue(GlobalOptionsSetup.NoColorOption));

        if (output == OutputFormat.Json)
        {
            JsonEnvelopeWriter.WriteError(
                formatter,
                command,
                exitCode,
                kind,
                message,
                details,
                retryable,
                suggestedNextStep);
            return;
        }

        formatter.WriteError(message);
        if (!string.IsNullOrWhiteSpace(suggestedNextStep))
            formatter.WriteError(suggestedNextStep);
    }

    public static void WriteCommandError(
        IReadOnlyList<string> rawArgs,
        string command,
        int exitCode,
        string kind,
        string message,
        Dictionary<string, object>? details = null,
        bool retryable = false,
        string? suggestedNextStep = null)
    {
        var output = ResolveRequestedOutputFormat(rawArgs: rawArgs);
        var formatter = CreateFormatter(output, rawArgs.Contains("--no-color", StringComparer.OrdinalIgnoreCase));

        if (output == OutputFormat.Json)
        {
            JsonEnvelopeWriter.WriteError(
                formatter,
                command,
                exitCode,
                kind,
                message,
                details,
                retryable,
                suggestedNextStep);
            return;
        }

        formatter.WriteError(message);
        if (!string.IsNullOrWhiteSpace(suggestedNextStep))
            formatter.WriteError(suggestedNextStep);
    }

    /// <summary>
    /// Builds a CommandContext with config, policy, and discovered files.
    /// Config file settings (output format, no-color, discovery excludes) are applied as defaults;
    /// explicit CLI flags always take precedence.
    /// </summary>
    public static CommandContext Build(ParseResult parseResult, bool discoverFiles = true)
    {
        var fileSystem = new PhysicalFileSystem();
        var rootPath = Directory.GetCurrentDirectory();
        var configPath = parseResult.GetValue(GlobalOptionsSetup.ConfigOption);

        var configLoader = new ConfigLoader(fileSystem);
        var configDir = configLoader.FindConfigDirectory(rootPath, configPath);

        StewardConfig? config = null;
        RepositoryPolicy? policy = null;
        PathPolicyDocument? pathPolicy = null;

        if (configDir != null)
        {
            config = configLoader.LoadConfig(configDir);
            policy = configLoader.LoadPolicy(configDir);
            pathPolicy = configLoader.LoadPathPolicy(configDir);
        }

        // Merge profile defaults into policy where repo-local policy doesn't override.
        var profileName = config?.Profile ?? "minimal";
        var profilePolicy = ProfileDefaults.GetProfilePolicy(profileName);
        if (profilePolicy != null)
        {
            policy = ProfileMerger.Merge(policy, profilePolicy);
        }

        // Resolve effective output settings: CLI flag > config file > built-in default.
        var output = ResolveOutputFormat(parseResult, config);
        var verbosity = ResolveVerbosity(parseResult, config);
        var noColor = ResolveNoColor(parseResult, config);
        var effectiveExcludes = config?.Discovery?.Exclude ?? [];

        var formatter = CreateFormatter(output, noColor);

        IReadOnlyList<DiscoveredFile>? files = null;
        if (discoverFiles)
        {
            // Apply discovery excludes from config in addition to .gitignore rules.
            var ignoreFilter = GitIgnoreFilter.Load(rootPath, fileSystem, effectiveExcludes);
            var discoveryService = new FileDiscoveryService(fileSystem, ignoreFilter);
            files = discoveryService.Discover(rootPath);
        }

        return new CommandContext
        {
            RootPath = rootPath,
            FileSystem = fileSystem,
            Formatter = formatter,
            OutputFormat = output,
            Verbosity = verbosity,
            NoColor = noColor,
            ConfigDirectory = configDir,
            Config = config,
            Policy = policy,
            PathPolicy = pathPolicy,
            Files = files,
            EffectiveDiscoveryExcludes = effectiveExcludes
        };
    }

    public static bool TryBuild(ParseResult parseResult, out CommandContext? context, string commandName, bool discoverFiles = true)
    {
        try
        {
            context = Build(parseResult, discoverFiles);
            return true;
        }
        catch (StewardConfigException ex)
        {
            context = null;
            WriteCommandError(
                parseResult,
                commandName,
                ExitCodes.UsageError,
                "configuration-error",
                $"Configuration error: {ex.Message}",
                details: new Dictionary<string, object>
                {
                    ["errors"] = new[] { ex.Message }
                },
                suggestedNextStep: "Run 'steward config validate' for details.");
            return false;
        }
    }

    /// <summary>
    /// Resolves the effective output format: explicit CLI flag overrides config file default.
    /// System.CommandLine returns a non-null result even for options that were not explicitly
    /// provided (Implicit = true). We check Implicit to distinguish.
    /// </summary>
    private static OutputFormat ResolveOutputFormat(ParseResult parseResult, StewardConfig? config)
    {
        // If the user explicitly passed --output on the command line, honor it.
        var optResult = parseResult.GetResult(GlobalOptionsSetup.OutputOption);
        if (optResult is { Implicit: false })
            return parseResult.GetValue(GlobalOptionsSetup.OutputOption);

        // Otherwise fall back to config file setting, then built-in default.
        if (config?.Output?.Format != null &&
            Enum.TryParse<OutputFormat>(config.Output.Format, ignoreCase: true, out var fromConfig))
            return fromConfig;

        return OutputFormat.Text;
    }

    /// <summary>
    /// Resolves the effective verbosity: explicit CLI flag overrides config file default.
    /// </summary>
    private static Verbosity ResolveVerbosity(ParseResult parseResult, StewardConfig? config)
    {
        var optResult = parseResult.GetResult(GlobalOptionsSetup.VerbosityOption);
        if (optResult is { Implicit: false })
            return parseResult.GetValue(GlobalOptionsSetup.VerbosityOption);

        if (config?.Output?.Verbosity != null &&
            Enum.TryParse<Verbosity>(config.Output.Verbosity, ignoreCase: true, out var fromConfig))
            return fromConfig;

        return Verbosity.Normal;
    }

    /// <summary>
    /// Resolves the effective no-color setting: explicit CLI flag overrides config file default.
    /// </summary>
    private static bool ResolveNoColor(ParseResult parseResult, StewardConfig? config)
    {
        var optResult = parseResult.GetResult(GlobalOptionsSetup.NoColorOption);
        if (optResult is { Implicit: false })
            return parseResult.GetValue(GlobalOptionsSetup.NoColorOption);

        return config?.Output?.NoColor ?? false;
    }
}
