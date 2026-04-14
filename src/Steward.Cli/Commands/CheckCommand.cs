using System.CommandLine;
using Steward.Core;
using Steward.Core.Formatting;
using Steward.Core.Markdown;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;

namespace Steward.Cli.Commands;

public static class CheckCommand
{
    public static Command Create()
    {
        var command = new Command("check", "Validate repository against policy");

        var scopeOption = new Option<string>("--scope", "-s")
        {
            Description = "Validation scope: full, changed, staged, or paths",
            DefaultValueFactory = _ => "full"
        };
        command.Add(scopeOption);

        command.SetAction((parseResult) =>
        {
            var ctx = CommandSetup.Build(parseResult);

            // Create validation context
            var docCache = new DocumentCache(ctx.FileSystem, ctx.RootPath);
            var context = new ValidationContext
            {
                Policy = ctx.Policy,
                PathPolicy = ctx.PathPolicy,
                TargetFiles = ctx.Files!,
                FileSystem = ctx.FileSystem,
                RepositoryRoot = ctx.RootPath,
                DocumentCache = docCache
            };

            // Run validation
            var rules = new IValidationRule[]
            {
                new RequiredArtifactRule(),
                new ForbiddenPathRule(),
                new RequiredFrontmatterFieldRule(),
                new SectionSizeRule(),
                new ManagedRegionIntegrityRule(),
                new ManagedScopeViolationRule(),
                new StaleArtifactRule(),
                new BrokenInternalLinkRule()
            };

            var engine = new ValidationEngine(rules);
            var result = engine.ValidateAsync(context).GetAwaiter().GetResult();

            // Output
            if (ctx.OutputFormat == OutputFormat.Json)
            {
                ctx.Formatter.WriteObject(result);
            }
            else
            {
                foreach (var diag in result.Diagnostics)
                {
                    var severity = diag.Severity switch
                    {
                        DiagnosticSeverity.Error => "ERROR",
                        DiagnosticSeverity.Warning => "WARN ",
                        DiagnosticSeverity.Info => "INFO ",
                        _ => "     "
                    };

                    var location = diag.Path != null
                        ? diag.Line.HasValue ? $"{diag.Path}:{diag.Line}" : diag.Path
                        : "";

                    var message = SecretFilter.Redact(diag.Message);
                    ctx.Formatter.WriteMessage($"[{severity}] {diag.RuleId} {location}: {message}");

                    if (diag.Remediation != null)
                        ctx.Formatter.WriteMessage($"         Fix: {diag.Remediation}");
                }

                ctx.Formatter.WriteMessage("");
                ctx.Formatter.WriteMessage($"Files checked: {result.Summary.FilesChecked}");
                ctx.Formatter.WriteMessage($"Errors: {result.Summary.Errors}, Warnings: {result.Summary.Warnings}, Info: {result.Summary.Infos}");
                ctx.Formatter.WriteMessage(result.Summary.Pass ? "PASS" : "FAIL");
            }

            return result.Summary.Pass ? ExitCodes.Success : ExitCodes.ValidationFailure;
        });

        return command;
    }
}
