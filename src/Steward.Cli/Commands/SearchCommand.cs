using System.CommandLine;
using Steward.Core;
using Steward.Core.Abstractions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Formatting;
using Steward.Core.Search;
using Steward.Cli.Formatting;

namespace Steward.Cli.Commands;

public static class SearchCommand
{
    public static Command Create()
    {
        var command = new Command("search", "Search repository content and headings");

        var queryArg = new Argument<string>("query") { Description = "Search query string" };
        command.Add(queryArg);

        var modeOption = new Option<string>("--mode", "-m")
        {
            Description = "Search mode: content, headings, or all",
            DefaultValueFactory = _ => "all"
        };
        command.Add(modeOption);

        var scopeOption = new Option<string?>("--scope", "-s")
        {
            Description = "Filter to artifacts matching a policy-defined role"
        };
        command.Add(scopeOption);

        var maxOption = new Option<int>("--max")
        {
            Description = "Maximum number of results",
            DefaultValueFactory = _ => 100
        };
        command.Add(maxOption);

        command.SetAction((parseResult) =>
        {
            var output = parseResult.GetValue(GlobalOptionsSetup.OutputOption);
            var noColor = parseResult.GetValue(GlobalOptionsSetup.NoColorOption);
            var configPath = parseResult.GetValue(GlobalOptionsSetup.ConfigOption);
            var query = parseResult.GetValue(queryArg)!;
            var modeStr = parseResult.GetValue(modeOption)!;
            var scope = parseResult.GetValue(scopeOption);
            var max = parseResult.GetValue(maxOption);

            var mode = modeStr.ToLowerInvariant() switch
            {
                "content" => SearchMode.Content,
                "headings" => SearchMode.Headings,
                _ => SearchMode.All
            };

            var formatter = CreateFormatter(output, noColor);
            var fileSystem = new PhysicalFileSystem();
            var rootPath = Directory.GetCurrentDirectory();

            // Load config/policy
            var configLoader = new ConfigLoader(fileSystem);
            var configDir = configLoader.FindConfigDirectory(rootPath, configPath);
            RepositoryPolicy? policy = null;
            if (configDir != null)
            {
                policy = configLoader.LoadPolicy(configDir);
            }

            // Discover files
            var ignoreFilter = GitIgnoreFilter.Load(rootPath, fileSystem);
            var discoveryService = new FileDiscoveryService(fileSystem, ignoreFilter);
            var files = discoveryService.Discover(rootPath);

            // Search
            var engine = new SearchEngine(fileSystem, rootPath);
            var result = engine.Search(query, files, mode, scope, policy, max);

            // Output
            if (output == OutputFormat.Json)
            {
                formatter.WriteObject(new
                {
                    query = result.Query,
                    mode = result.Mode.ToString().ToLowerInvariant(),
                    totalMatches = result.TotalMatches,
                    truncated = result.Truncated,
                    matches = result.Matches.Select(m => new
                    {
                        path = m.Path,
                        line = m.Line,
                        column = m.Column,
                        snippet = m.Snippet,
                        kind = m.Kind.ToString().ToLowerInvariant(),
                        headingContext = m.HeadingContext
                    }).ToArray()
                });
            }
            else
            {
                foreach (var match in result.Matches)
                {
                    var context = match.HeadingContext != null ? $" [{match.HeadingContext}]" : "";
                    var kind = match.Kind == SearchMatchKind.Heading ? " (heading)" : "";
                    formatter.WriteMessage($"{match.Path}:{match.Line}:{match.Column}{kind}{context}");
                    formatter.WriteMessage($"  {match.Snippet}");
                }

                formatter.WriteMessage("");
                var truncMsg = result.Truncated ? $" (showing {result.Matches.Count})" : "";
                formatter.WriteMessage($"{result.TotalMatches} match(es) found{truncMsg}");
            }

            return ExitCodes.Success;
        });

        return command;
    }

    private static IOutputFormatter CreateFormatter(OutputFormat format, bool noColor)
    {
        return format switch
        {
            OutputFormat.Json => new JsonOutputFormatter(Console.Out),
            _ => new TextOutputFormatter(Console.Out, !noColor && !Console.IsOutputRedirected)
        };
    }
}
