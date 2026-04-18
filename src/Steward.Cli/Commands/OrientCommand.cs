using System.CommandLine;
using Steward.Core;
using Steward.Core.Formatting;
using Steward.Core.Orientation;
using Steward.Cli.Formatting;

namespace Steward.Cli.Commands;

public static class OrientCommand
{
    public static Command Create()
    {
        var command = new Command("orient", "Show repository structure classified by artifact role (readme, governance, planning, etc.)");

        var depthOption = new Option<int>("--depth", "-d")
        {
            Description = "Maximum depth to display",
            DefaultValueFactory = _ => 3
        };
        command.Add(depthOption);

        var signalsOption = new Option<bool>("--signals")
        {
            Description = "Include cheap required/stale signals without running full validation"
        };
        command.Add(signalsOption);

        var compactOption = new Option<bool>("--compact")
        {
            Description = "Show a curated repo-start view (default in text output)"
        };
        command.Add(compactOption);

        var fullOption = new Option<bool>("--full")
        {
            Description = "Show the full classified inventory instead of the compact text default"
        };
        command.Add(fullOption);

        var treeOption = new Option<bool>("--tree")
        {
            Description = "Render an actual tree with basenames instead of a flat path list"
        };
        command.Add(treeOption);

        command.SetAction((parseResult) =>
        {
            if (!CommandSetup.TryBuild(parseResult, out var ctx))
                return ExitCodes.UsageError;

            var depth = parseResult.GetValue(depthOption);
            var includeSignals = parseResult.GetValue(signalsOption);
            var compactRequested = parseResult.GetValue(compactOption);
            var fullRequested = parseResult.GetValue(fullOption);
            var treeRequested = parseResult.GetValue(treeOption);

            if (compactRequested && fullRequested)
            {
                if (ctx!.OutputFormat == OutputFormat.Json)
                {
                    JsonEnvelopeWriter.WriteError(ctx.Formatter, ctx.JsonEnvelope, "orient", ExitCodes.UsageError,
                        "conflicting-options", "Choose either --compact or --full, not both.");
                    return ExitCodes.UsageError;
                }
                ctx!.Formatter.WriteError("Choose either --compact or --full, not both.");
                return ExitCodes.UsageError;
            }

            var engine = new OrientationEngine();
            var status = includeSignals
                ? StatusCommand.ComputeStatus(ctx!.Policy, ctx.Config?.Profile, ctx.FileSystem, ctx.RootPath, ctx.Files!)
                : null;
            var result = engine.Orient(
                ctx!.RootPath,
                ctx.Files!,
                ctx.Policy,
                ctx.Config?.Profile,
                depth,
                status != null ? ToSignalInput(status) : null);
            var fullEntries = result.Entries.ToList();

            var useCompact = compactRequested || (!fullRequested && ctx.OutputFormat == OutputFormat.Text);
            var entriesToRender = useCompact
                ? SelectCompactEntries(fullEntries)
                : fullEntries;

            if (useCompact)
            {
                result.Entries.Clear();
                result.Entries.AddRange(entriesToRender);
            }

            if (ctx.OutputFormat == OutputFormat.Json)
            {
                JsonEnvelopeWriter.Write(ctx.Formatter, ctx.JsonEnvelope, "orient", true, ExitCodes.Success, result);
            }
            else
            {
                WriteTextOutput(ctx.Formatter, result, entriesToRender, fullEntries, includeSignals, treeRequested);
            }

            return ExitCodes.Success;
        });

        return command;
    }

    private static OrientationSignalInput ToSignalInput(StatusCommand.RepositoryStatus status)
    {
        return new OrientationSignalInput
        {
            MissingRequiredArtifacts =
            [
                .. status.RequiredArtifacts
                    .Where(static artifact => !artifact.Present)
                    .Select(static artifact => new OrientationSignalInputItem
                    {
                        Path = artifact.Path,
                        Message = $"Required artifact '{artifact.Path}' is missing."
                    })
            ],
            StaleArtifacts =
            [
                .. status.MaintenanceArtifacts
                    .Where(static artifact => artifact.Stale)
                    .Select(static artifact => new OrientationSignalInputItem
                    {
                        Path = artifact.Path,
                        Message = $"Maintained artifact '{artifact.Id}' is stale."
                    })
            ]
        };
    }

    private static void WriteTextOutput(
        IOutputFormatter formatter,
        OrientationResult result,
        IReadOnlyList<OrientationEntry> entriesToRender,
        IReadOnlyList<OrientationEntry> allEntries,
        bool includeSignals,
        bool treeRequested)
    {
        if (!string.IsNullOrWhiteSpace(result.RepositoryName))
        {
            formatter.WriteMessage(
                $"{OutputStyler.Style(formatter, "Repository:", CliTextStyle.Heading)} {result.RepositoryName}");
        }

        if (!string.IsNullOrWhiteSpace(result.RepositoryType) || !string.IsNullOrWhiteSpace(result.Profile))
        {
            var details = new List<string>();
            if (!string.IsNullOrWhiteSpace(result.RepositoryType))
                details.Add($"type={result.RepositoryType}");
            if (!string.IsNullOrWhiteSpace(result.Profile))
                details.Add($"profile={result.Profile}");

            formatter.WriteMessage(
                $"{OutputStyler.Style(formatter, "Context:", CliTextStyle.Heading)} {string.Join(", ", details)}");
        }

        if (result.StartHere.Count > 0)
        {
            formatter.WriteMessage("");
            formatter.WriteMessage(OutputStyler.Style(formatter, "Start Here", CliTextStyle.Heading));
            foreach (var path in result.StartHere)
                formatter.WriteMessage($"  - {path}");
        }

        if (entriesToRender.Count > 0)
        {
            formatter.WriteMessage("");
            formatter.WriteMessage(OutputStyler.Style(
                formatter,
                treeRequested ? "Orientation Tree" : "Orientation",
                CliTextStyle.Heading));

            if (treeRequested)
                WriteTreeEntries(formatter, entriesToRender, allEntries);
            else
                WriteListEntries(formatter, entriesToRender);
        }

        if (includeSignals)
        {
            formatter.WriteMessage("");
            formatter.WriteMessage(OutputStyler.Style(formatter, "Signals", CliTextStyle.Heading));

            if (result.Signals.Count == 0)
            {
                formatter.WriteMessage($"  {OutputStyler.Style(formatter, "none", CliTextStyle.Muted)}");
            }
            else
            {
                foreach (var signal in result.Signals)
                {
                    var location = signal.Path != null ? $" {signal.Path}" : "";
                    formatter.WriteDiagnostic(signal.Severity, $"  [{signal.Severity}] {signal.Code}{location}: {signal.Message}");
                }
            }
        }
    }

    private static void WriteListEntries(IOutputFormatter formatter, IReadOnlyList<OrientationEntry> entries)
    {
        foreach (var entry in entries)
            formatter.WriteMessage($"  {FormatListLabel(formatter, entry)}");
    }

    private static string FormatListLabel(IOutputFormatter formatter, OrientationEntry entry)
    {
        var path = entry.IsDirectory ? $"{entry.Path}/" : entry.Path;
        var classification = OutputStyler.Classification(formatter, $"[{entry.Classification}]");
        var marker = entry.IsStartHere
            ? $" {OutputStyler.Style(formatter, "[start]", CliTextStyle.Success)}"
            : string.Empty;

        return $"{path} {classification}{marker}";
    }

    private static void WriteTreeEntries(
        IOutputFormatter formatter,
        IReadOnlyList<OrientationEntry> entries,
        IReadOnlyList<OrientationEntry> allEntries)
    {
        var treeEntries = IncludeTreeAncestors(entries, allEntries);
        var roots = BuildTree(treeEntries);
        WriteTreeNodes(formatter, roots, prefix: "");
    }

    private static void WriteTreeNodes(IOutputFormatter formatter, IReadOnlyList<OrientationTreeNode> nodes, string prefix)
    {
        for (var i = 0; i < nodes.Count; i++)
        {
            var isLast = i == nodes.Count - 1;
            var connector = isLast ? "└── " : "├── ";
            var childPrefix = prefix + (isLast ? "    " : "│   ");

            formatter.WriteMessage(
                $"{prefix}{OutputStyler.Style(formatter, connector, CliTextStyle.Muted)}{FormatTreeLabel(formatter, nodes[i].Entry)}");

            if (nodes[i].Children.Count > 0)
                WriteTreeNodes(formatter, nodes[i].Children, childPrefix);
        }
    }

    private static string FormatTreeLabel(IOutputFormatter formatter, OrientationEntry entry)
    {
        var name = Path.GetFileName(entry.Path);
        if (string.IsNullOrWhiteSpace(name))
            name = entry.Path;

        if (entry.IsDirectory)
            name += "/";

        var styledName = entry.IsDirectory
            ? OutputStyler.Style(formatter, name, CliTextStyle.Directory)
            : name;

        var classification = OutputStyler.Classification(formatter, $"[{entry.Classification}]");
        var marker = entry.IsStartHere
            ? $" {OutputStyler.Style(formatter, "[start]", CliTextStyle.Success)}"
            : string.Empty;

        return $"{styledName} {classification}{marker}";
    }

    private static List<OrientationEntry> SelectCompactEntries(IReadOnlyList<OrientationEntry> entries)
    {
        return entries
            .OrderBy(GetCompactPriority)
            .ThenBy(static entry => entry.IsDirectory ? 0 : 1)
            .ThenBy(static entry => entry.Depth)
            .ThenBy(static entry => entry.Path, StringComparer.OrdinalIgnoreCase)
            .Take(15)
            .ToList();
    }

    private static int GetCompactPriority(OrientationEntry entry)
    {
        if (entry.IsStartHere)
            return 0;

        return entry.Classification switch
        {
            "authoritative" => 1,
            "guide" => 2,
            var classification when classification.StartsWith("state:", StringComparison.OrdinalIgnoreCase) => 3,
            "governance" => 4,
            "workflow" => 5,
            "requirements" => 6,
            "configuration" => 7,
            "source" => 8,
            "testing" => 9,
            _ => 10
        };
    }

    private static List<OrientationEntry> IncludeTreeAncestors(
        IReadOnlyList<OrientationEntry> entries,
        IReadOnlyList<OrientationEntry> allEntries)
    {
        var entryLookup = allEntries.ToDictionary(static entry => entry.Path, StringComparer.OrdinalIgnoreCase);
        var selectedPaths = new HashSet<string>(entries.Select(static entry => entry.Path), StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            var parentPath = PathHelper.NormalizeSeparators(Path.GetDirectoryName(entry.Path) ?? "");
            while (!string.IsNullOrWhiteSpace(parentPath))
            {
                if (!entryLookup.ContainsKey(parentPath))
                    break;

                selectedPaths.Add(parentPath);
                parentPath = PathHelper.NormalizeSeparators(Path.GetDirectoryName(parentPath) ?? "");
            }
        }

        return entryLookup.Values
            .Where(entry => selectedPaths.Contains(entry.Path))
            .OrderBy(static entry => entry.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<OrientationTreeNode> BuildTree(IReadOnlyList<OrientationEntry> entries)
    {
        var roots = new List<OrientationTreeNode>();
        var lookup = new Dictionary<string, OrientationTreeNode>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries.OrderBy(static entry => entry.Path, StringComparer.OrdinalIgnoreCase))
        {
            var node = new OrientationTreeNode(entry);
            lookup[entry.Path] = node;

            var parentPath = PathHelper.NormalizeSeparators(Path.GetDirectoryName(entry.Path) ?? "");
            if (string.IsNullOrWhiteSpace(parentPath) || !lookup.TryGetValue(parentPath, out var parent))
            {
                roots.Add(node);
            }
            else
            {
                parent.Children.Add(node);
            }
        }

        return roots;
    }

    private sealed class OrientationTreeNode(OrientationEntry entry)
    {
        public OrientationEntry Entry { get; } = entry;
        public List<OrientationTreeNode> Children { get; } = [];
    }
}
