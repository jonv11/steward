using System.CommandLine;
using Steward.Core;
using Steward.Core.Formatting;
using Steward.Core.Markdown;
using Steward.Core.Validation;
namespace Steward.Cli.Commands;

public static class CheckCommand
{
    internal static IValidationRule[] AllRules => RuleRegistry.CreateAllRules();

    public static Command Create()
    {
        var command = new Command("check", "Validate repository against policy: required artifacts, frontmatter, links, naming, freshness");

        var scopeOption = new Option<string?>("--scope", "-s")
        {
            Description = "Validation scope: full (all files), changed (git-modified), or staged (git-staged). Default: full"
        };
        scopeOption.AcceptOnlyFromAmong("full", "changed", "staged");
        var pathsOption = new Option<string[]?>("--paths")
        {
            Description = "Validate only the specified paths"
        };
        var fixOption = new Option<bool>("--fix")
        {
            Description = "Apply deterministic fixes for fixable rules"
        };
        var dryRunOption = new Option<bool>("--dry-run")
        {
            Description = "Show what --fix would change without applying"
        };
        var quietOption = new Option<bool>("--quiet")
        {
            Description = "Suppress output and return only the exit code"
        };

        command.Add(scopeOption);
        command.Add(pathsOption);
        command.Add(fixOption);
        command.Add(dryRunOption);
        command.Add(quietOption);

        command.SetAction((parseResult) =>
        {
            if (!CommandSetup.TryBuild(parseResult, out var ctx))
                return ExitCodes.UsageError;

            var scopeValue = parseResult.GetValue(scopeOption);
            var pathsValue = parseResult.GetValue(pathsOption);
            var fixRequested = parseResult.GetValue(fixOption);
            var dryRunRequested = parseResult.GetValue(dryRunOption);
            var quiet = parseResult.GetValue(quietOption);

            // Resolve scope
            IScopeResolver scopeResolver = ResolveScope(scopeValue, pathsValue);
            var targetFiles = scopeResolver.Resolve(ctx!.Files!, ctx.RootPath);
            var scopeLabel = scopeValue ?? (pathsValue is { Length: > 0 } ? "paths" : "full");

            // Create validation context
            var docCache = new DocumentCache(ctx.FileSystem, ctx.RootPath);
            var context = new ValidationContext
            {
                Policy = ctx.Policy,
                PathPolicy = ctx.PathPolicy,
                TargetFiles = targetFiles,
                FileSystem = ctx.FileSystem,
                RepositoryRoot = ctx.RootPath,
                DocumentCache = docCache
            };

            // Run validation
            var rules = AllRules;
            var engine = new ValidationEngine(rules);
            var result = engine.ValidateAsync(context).GetAwaiter().GetResult();

            // Update the scope label in the result
            result = result with
            {
                Summary = result.Summary with { Scope = scopeLabel }
            };

            var orderedDiagnostics = OrderDiagnostics(result.Diagnostics);

            // Handle --fix / --dry-run
            if (fixRequested || dryRunRequested)
            {
                var fixes = ComputeFixes(rules, context);
                if (fixes.Count == 0)
                {
                    if (!quiet && ctx.OutputFormat != OutputFormat.Json)
                        ctx.Formatter.WriteMessage("No automatic fixes available.");
                }
                else
                {
                    foreach (var fix in fixes)
                    {
                        if (!quiet && ctx.OutputFormat != OutputFormat.Json)
                        {
                            ctx.Formatter.WriteMessage($"[fix] {fix.RuleId}: {fix.Description}");
                            ctx.Formatter.WriteMessage($"       {fix.FilePath}");
                        }

                        if (fixRequested && !dryRunRequested)
                        {
                            var dir = Path.GetDirectoryName(Path.Combine(ctx.RootPath, fix.FilePath));
                            if (dir != null && !Directory.Exists(dir))
                                Directory.CreateDirectory(dir);

                            var fullPath = Path.Combine(ctx.RootPath, fix.FilePath);
                            var oldContent = File.Exists(fullPath) ? File.ReadAllText(fullPath) : "";
                            File.WriteAllText(fullPath, fix.NewContent);

                            var (added, removed) = MaintainCommand.CountDiffLines(oldContent, fix.NewContent);
                            if (!quiet && ctx.OutputFormat != OutputFormat.Json)
                                ctx.Formatter.WriteMessage($"       (+{added} -{removed})");
                        }
                    }

                    if (!quiet && dryRunRequested && ctx.OutputFormat != OutputFormat.Json)
                        ctx.Formatter.WriteMessage($"\nDry run: {fixes.Count} fix(es) would be applied. Use --fix to apply.");
                    else if (!quiet && fixRequested && ctx.OutputFormat != OutputFormat.Json)
                        ctx.Formatter.WriteMessage($"\nApplied {fixes.Count} fix(es).");
                }
            }

            // In quiet mode, return only the exit code
            if (quiet)
                return result.Summary.Pass ? ExitCodes.Success : ExitCodes.ValidationFailure;

            // Compute completion data for output
            var completionData = ComputeCompletionData(result.Diagnostics);

            // Output
            if (ctx.OutputFormat == OutputFormat.Json)
            {
                ctx.Formatter.WriteObject(new CheckResponse
                {
                    Summary = new CheckSummaryResponse
                    {
                        Scope = result.Summary.Scope,
                        FilesChecked = result.Summary.FilesChecked,
                        Errors = result.Summary.Errors,
                        Warnings = result.Summary.Warnings,
                        Infos = result.Summary.Infos,
                        Pass = result.Summary.Pass
                    },
                    Completion = completionData,
                    Diagnostics =
                    [
                        .. orderedDiagnostics.Select(diag => new CheckDiagnosticResponse
                        {
                            RuleId = diag.RuleId,
                            Severity = FormatSeverity(diag.Severity),
                            Category = diag.Category,
                            Path = diag.Path,
                            Line = diag.Line,
                            Message = SecretFilter.Redact(diag.Message),
                            Remediation = diag.Remediation != null ? SecretFilter.Redact(diag.Remediation) : null,
                            Source = diag.Source
                        })
                    ]
                });
            }
            else
            {
                if (orderedDiagnostics.Count == 0 && !fixRequested && !dryRunRequested)
                {
                    ctx.Formatter.WriteMessage("No issues found.");
                }
                else if (orderedDiagnostics.Count > 0)
                {
                    foreach (var diag in orderedDiagnostics)
                    {
                        var location = diag.Path != null
                            ? diag.Line.HasValue ? $"{diag.Path}:{diag.Line}" : diag.Path
                            : "";

                        var message = SecretFilter.Redact(diag.Message);
                        var locationPart = location.Length > 0 ? $" {location}:" : ":";
                        ctx.Formatter.WriteMessage($"[{FormatSeverity(diag.Severity),-5}] {diag.RuleId}{locationPart} {message}");

                        if (diag.Remediation != null)
                            ctx.Formatter.WriteMessage($"         fix: {SecretFilter.Redact(diag.Remediation)}");
                    }
                }

                // Impact analysis
                var impacts = ComputeImpactSignals(ctx.Policy, orderedDiagnostics);
                if (impacts.Count > 0)
                {
                    ctx.Formatter.WriteMessage("");
                    ctx.Formatter.WriteMessage("Impact signals:");
                    foreach (var (artifactId, artifactPath, sourcePath) in impacts)
                    {
                        ctx.Formatter.WriteMessage($"  [info] Changes in '{sourcePath}' may affect maintained artifact '{artifactId}' ({artifactPath})");
                    }
                }

                // Staged-scope completeness check
                if (string.Equals(scopeLabel, "staged", StringComparison.OrdinalIgnoreCase))
                {
                    var stagedPaths = new HashSet<string>(
                        targetFiles.Select(f => f.RelativePath.Replace('\\', '/')),
                        StringComparer.OrdinalIgnoreCase);

                    var incomplete = ComputeStagedCompleteness(ctx.Policy, stagedPaths);
                    if (incomplete.Count > 0)
                    {
                        ctx.Formatter.WriteMessage("");
                        ctx.Formatter.WriteMessage("Staging completeness:");
                        foreach (var (artifactId, artifactPath) in incomplete)
                        {
                            ctx.Formatter.WriteMessage($"  [info] Maintained artifact '{artifactId}' ({artifactPath}) has staged sources but is not itself staged.");
                        }
                    }
                }

                // Completion summary
                ctx.Formatter.WriteMessage("");
                ctx.Formatter.WriteMessage($"Files checked: {result.Summary.FilesChecked}  " +
                    $"Errors: {result.Summary.Errors}  Warnings: {result.Summary.Warnings}  Info: {result.Summary.Infos}");
                if (scopeLabel != "full")
                    ctx.Formatter.WriteMessage($"Scope: {scopeLabel}");
                ctx.Formatter.WriteMessage(result.Summary.Pass ? "Result: PASS" : "Result: FAIL");

                // Completion policy summary
                WriteCompletionSummary(ctx, result.Diagnostics);
            }

            return result.Summary.Pass ? ExitCodes.Success : ExitCodes.ValidationFailure;
        });

        return command;
    }

    internal static IScopeResolver ResolveScope(string? scopeValue, string[]? pathsValue)
    {
        if (pathsValue is { Length: > 0 })
            return new PathsScopeResolver(pathsValue);

        return scopeValue?.ToLowerInvariant() switch
        {
            "changed" => new ChangedScopeResolver(),
            "staged" => new StagedScopeResolver(),
            _ => new FullScopeResolver()
        };
    }

    private static List<Fix> ComputeFixes(IValidationRule[] rules, ValidationContext context)
    {
        var fixes = new List<Fix>();
        foreach (var rule in rules.OfType<IFixableRule>())
        {
            var ruleFixes = rule.ComputeFixesAsync(context).GetAwaiter().GetResult();
            fixes.AddRange(ruleFixes);
        }
        return fixes;
    }

    private static void WriteCompletionSummary(CommandContext ctx, IReadOnlyList<Diagnostic> diagnostics)
    {
        var data = ComputeCompletionData(diagnostics);
        if (data.RequiredArtifactsMissing == 0 && data.StaleArtifacts == 0 &&
            data.BrokenLinks == 0 && data.BrokenReferences == 0)
            return;

        ctx.Formatter.WriteMessage("");
        ctx.Formatter.WriteMessage("Completion:");
        if (data.RequiredArtifactsMissing > 0)
            ctx.Formatter.WriteMessage($"  - {data.RequiredArtifactsMissing} required artifact(s) missing");
        if (data.StaleArtifacts > 0)
            ctx.Formatter.WriteMessage($"  - {data.StaleArtifacts} maintained artifact(s) stale → run 'steward maintain --apply'");
        if (data.BrokenLinks > 0)
            ctx.Formatter.WriteMessage($"  - {data.BrokenLinks} broken internal link(s)");
        if (data.BrokenReferences > 0)
            ctx.Formatter.WriteMessage($"  - {data.BrokenReferences} broken artifact reference(s) in policy");
    }

    private static CheckCompletionResponse ComputeCompletionData(IReadOnlyList<Diagnostic> diagnostics)
    {
        return new CheckCompletionResponse
        {
            RequiredArtifactsMissing = diagnostics.Count(d => d.RuleId == "STWD-001"),
            StaleArtifacts = diagnostics.Count(d => d.RuleId == "STWD-007"),
            BrokenLinks = diagnostics.Count(d => d.RuleId == "STWD-008"),
            BrokenReferences = diagnostics.Count(d => d.RuleId == "STWD-009")
        };
    }

    private static List<Diagnostic> OrderDiagnostics(IEnumerable<Diagnostic> diagnostics)
    {
        return diagnostics
            .OrderByDescending(static diag => diag.Severity)
            .ThenBy(static diag => diag.Path ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static diag => diag.Line ?? 0)
            .ThenBy(static diag => diag.RuleId, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static diag => diag.Message, StringComparer.Ordinal)
            .ToList();
    }

    internal static string FormatSeverity(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Error => "error",
        DiagnosticSeverity.Warning => "warn",
        DiagnosticSeverity.Info => "info",
        _ => "unknown"
    };

    internal static List<(string ArtifactId, string ArtifactPath, string SourcePath)> ComputeImpactSignals(
        Core.Configuration.RepositoryPolicy? policy, IReadOnlyList<Diagnostic> diagnostics)
    {
        var impacts = new List<(string, string, string)>();

        var maintenanceArtifacts = policy?.Maintenance?.Artifacts;
        if (maintenanceArtifacts is null || maintenanceArtifacts.Count == 0)
            return impacts;

        // Collect unique file paths from diagnostics
        var diagnosticPaths = new HashSet<string>(
            diagnostics.Where(d => d.Path != null).Select(d => d.Path!.Replace('\\', '/')),
            StringComparer.OrdinalIgnoreCase);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var artifact in maintenanceArtifacts)
        {
            if (string.IsNullOrWhiteSpace(artifact.Source) || string.IsNullOrWhiteSpace(artifact.Id))
                continue;

            var source = artifact.Source.Replace('\\', '/').TrimEnd('/');

            foreach (var path in diagnosticPaths)
            {
                if (path.StartsWith(source + "/", StringComparison.OrdinalIgnoreCase) ||
                    path.Equals(source, StringComparison.OrdinalIgnoreCase))
                {
                    var key = $"{artifact.Id}:{path}";
                    if (seen.Add(key))
                    {
                        impacts.Add((artifact.Id, artifact.Path ?? artifact.Id, path));
                    }
                }
            }
        }

        return impacts;
    }

    internal static List<(string ArtifactId, string ArtifactPath)> ComputeStagedCompleteness(
        Core.Configuration.RepositoryPolicy? policy, HashSet<string> stagedPaths)
    {
        var incomplete = new List<(string, string)>();

        var maintenanceArtifacts = policy?.Maintenance?.Artifacts;
        if (maintenanceArtifacts is null || maintenanceArtifacts.Count == 0)
            return incomplete;

        foreach (var artifact in maintenanceArtifacts)
        {
            if (string.IsNullOrWhiteSpace(artifact.Source) || string.IsNullOrWhiteSpace(artifact.Id))
                continue;

            var source = artifact.Source.Replace('\\', '/').TrimEnd('/');
            var artifactPath = (artifact.Path ?? "").Replace('\\', '/');

            // Check if any staged path is under this source
            var hasSourceStaged = stagedPaths.Any(p =>
                p.StartsWith(source + "/", StringComparison.OrdinalIgnoreCase) ||
                p.Equals(source, StringComparison.OrdinalIgnoreCase));

            if (hasSourceStaged && !string.IsNullOrWhiteSpace(artifactPath) && !stagedPaths.Contains(artifactPath))
            {
                incomplete.Add((artifact.Id, artifactPath));
            }
        }

        return incomplete;
    }
}

internal sealed class CheckResponse
{
    public required CheckSummaryResponse Summary { get; init; }
    public CheckCompletionResponse? Completion { get; init; }
    public required List<CheckDiagnosticResponse> Diagnostics { get; init; }
}

internal sealed class CheckSummaryResponse
{
    public required string Scope { get; init; }
    public required int FilesChecked { get; init; }
    public required int Errors { get; init; }
    public required int Warnings { get; init; }
    public required int Infos { get; init; }
    public required bool Pass { get; init; }
}

internal sealed class CheckCompletionResponse
{
    public int RequiredArtifactsMissing { get; init; }
    public int StaleArtifacts { get; init; }
    public int BrokenLinks { get; init; }
    public int BrokenReferences { get; init; }
}

internal sealed class CheckDiagnosticResponse
{
    public required string RuleId { get; init; }
    public required string Severity { get; init; }
    public required string Category { get; init; }
    public string? Path { get; init; }
    public int? Line { get; init; }
    public required string Message { get; init; }
    public string? Remediation { get; init; }
    public string? Source { get; init; }
}
