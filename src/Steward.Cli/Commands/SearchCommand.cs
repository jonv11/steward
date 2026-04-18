using System.CommandLine;
using Steward.Cli.Formatting;
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

        var scopeOption = new Option<string?>("--role", "-r")
        {
            Description = "Restrict search to artifacts with this policy-defined role (e.g. requirements, planning)"
        };
        command.Add(scopeOption);

        var maxOption = new Option<int>("--max")
        {
            Description = "Maximum number of results",
            DefaultValueFactory = _ => 100
        };
        command.Add(maxOption);

        var regexOption = new Option<bool>("--regex")
        {
            Description = "Treat query as a regular expression"
        };
        command.Add(regexOption);

        command.SetAction((parseResult) =>
        {
            if (!CommandSetup.TryBuild(parseResult, out var ctx))
                return ExitCodes.UsageError;

            var query = parseResult.GetValue(queryArg)!;
            var modeStr = parseResult.GetValue(modeOption)!;
            var scope = parseResult.GetValue(scopeOption);
            var max = parseResult.GetValue(maxOption);
            var useRegex = parseResult.GetValue(regexOption);

            var mode = modeStr.ToLowerInvariant() switch
            {
                "content" => SearchMode.Content,
                "headings" => SearchMode.Headings,
                _ => SearchMode.All
            };

            // Search
            var engine = new SearchEngine(ctx!.FileSystem, ctx.RootPath);
            var result = engine.Search(query, ctx.Files!, mode, scope, ctx.Policy, max, useRegex);

            if (result.Error != null)
            {
                if (ctx.OutputFormat == OutputFormat.Json)
                {
                    JsonEnvelopeWriter.WriteError(ctx.Formatter, ctx.JsonEnvelope, "search", ExitCodes.UsageError,
                        "invalid-query", result.Error,
                        details: new Dictionary<string, object> { ["query"] = query });
                    return ExitCodes.UsageError;
                }
                ctx.Formatter.WriteError(result.Error);
                return ExitCodes.UsageError;
            }

            // Output
            if (ctx.OutputFormat == OutputFormat.Json)
            {
                JsonEnvelopeWriter.Write(ctx.Formatter, ctx.JsonEnvelope, "search", true, ExitCodes.Success, new
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
