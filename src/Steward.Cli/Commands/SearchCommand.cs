using System.CommandLine;
using Steward.Core;
using Steward.Core.Formatting;
using Steward.Core.Search;

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
        modeOption.AcceptOnlyFromAmong("all", "content", "headings");
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
            var query = parseResult.GetValue(queryArg)!;
            var modeStr = parseResult.GetValue(modeOption)!;
            var scope = parseResult.GetValue(scopeOption);
            var max = parseResult.GetValue(maxOption);
            var ctx = CommandSetup.Build(parseResult);

            var mode = modeStr.ToLowerInvariant() switch
            {
                "content" => SearchMode.Content,
                "headings" => SearchMode.Headings,
                _ => SearchMode.All
            };

            // Search
            var engine = new SearchEngine(ctx.FileSystem, ctx.RootPath);
            var result = engine.Search(query, ctx.Files!, mode, scope, ctx.Policy, max);

            // Output
            if (ctx.OutputFormat == OutputFormat.Json)
            {
                ctx.Formatter.WriteObject(new
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
                    ctx.Formatter.WriteMessage($"{match.Path}:{match.Line}:{match.Column}{kind}{context}");
                    ctx.Formatter.WriteMessage($"  {match.Snippet}");
                }

                ctx.Formatter.WriteMessage("");
                var truncMsg = result.Truncated ? $" (showing {result.Matches.Count})" : "";
                ctx.Formatter.WriteMessage($"{result.TotalMatches} match(es) found{truncMsg}");
            }

            return ExitCodes.Success;
        });

        return command;
    }
}
