using System.CommandLine;
using Steward.Core;
using Steward.Core.Abstractions;
using Steward.Core.Configuration;
using Steward.Core.Discovery;
using Steward.Core.Formatting;
using Steward.Core.Validation;
using Steward.Core.Validation.Rules;
using Steward.Cli.Formatting;

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
            var output = parseResult.GetValue(GlobalOptionsSetup.OutputOption);
            var noColor = parseResult.GetValue(GlobalOptionsSetup.NoColorOption);
            var configPath = parseResult.GetValue(GlobalOptionsSetup.ConfigOption);

            var formatter = CreateFormatter(output, noColor);
            var fileSystem = new PhysicalFileSystem();
            var rootPath = Directory.GetCurrentDirectory();

            // Load config and policy
            var configLoader = new ConfigLoader(fileSystem);
            var configDir = configLoader.FindConfigDirectory(rootPath, configPath);

            RepositoryPolicy? policy = null;
            PathPolicyDocument? pathPolicy = null;

            if (configDir != null)
            {
                policy = configLoader.LoadPolicy(configDir);
                pathPolicy = configLoader.LoadPathPolicy(configDir);
            }

            // Discover files
            var ignoreFilter = GitIgnoreFilter.Load(rootPath, fileSystem);
            var discoveryService = new FileDiscoveryService(fileSystem, ignoreFilter);
            var files = discoveryService.Discover(rootPath);

            // Create validation context
            var context = new ValidationContext
            {
                Policy = policy,
                PathPolicy = pathPolicy,
                TargetFiles = files,
                FileSystem = fileSystem,
                RepositoryRoot = rootPath
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
            if (output == OutputFormat.Json)
            {
                formatter.WriteObject(result);
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
                    formatter.WriteMessage($"[{severity}] {diag.RuleId} {location}: {message}");

                    if (diag.Remediation != null)
                        formatter.WriteMessage($"         Fix: {diag.Remediation}");
                }

                formatter.WriteMessage("");
                formatter.WriteMessage($"Files checked: {result.Summary.FilesChecked}");
                formatter.WriteMessage($"Errors: {result.Summary.Errors}, Warnings: {result.Summary.Warnings}, Info: {result.Summary.Infos}");
                formatter.WriteMessage(result.Summary.Pass ? "PASS" : "FAIL");
            }

            return result.Summary.Pass ? ExitCodes.Success : ExitCodes.ValidationFailure;
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
