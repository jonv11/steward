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
            _ => new TextOutputFormatter(Console.Out, !noColor && !Console.IsOutputRedirected)
        };
    }

    /// <summary>
    /// Builds a CommandContext with config, policy, and discovered files.
    /// </summary>
    public static CommandContext Build(ParseResult parseResult, bool discoverFiles = true)
    {
        var output = parseResult.GetValue(GlobalOptionsSetup.OutputOption);
        var noColor = parseResult.GetValue(GlobalOptionsSetup.NoColorOption);
        var configPath = parseResult.GetValue(GlobalOptionsSetup.ConfigOption);

        var formatter = CreateFormatter(output, noColor);
        var fileSystem = new PhysicalFileSystem();
        var rootPath = Directory.GetCurrentDirectory();

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

        IReadOnlyList<DiscoveredFile>? files = null;
        if (discoverFiles)
        {
            var ignoreFilter = GitIgnoreFilter.Load(rootPath, fileSystem);
            var discoveryService = new FileDiscoveryService(fileSystem, ignoreFilter);
            files = discoveryService.Discover(rootPath);
        }

        return new CommandContext
        {
            RootPath = rootPath,
            FileSystem = fileSystem,
            Formatter = formatter,
            OutputFormat = output,
            ConfigDirectory = configDir,
            Config = config,
            Policy = policy,
            PathPolicy = pathPolicy,
            Files = files
        };
    }
}
