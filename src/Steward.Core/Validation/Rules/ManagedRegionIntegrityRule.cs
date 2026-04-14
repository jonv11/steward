using Steward.Core.Markdown;

namespace Steward.Core.Validation.Rules;

/// <summary>
/// STWD-005: Validates that managed region markers are properly paired (matching begin/end).
/// </summary>
public sealed class ManagedRegionIntegrityRule : IValidationRule
{
    public string RuleId => "STWD-005";
    public string Category => "structure";
    public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Error;
    public string Description => "Managed region markers must be properly paired with matching begin/end tags.";

    public Task<IReadOnlyList<Diagnostic>> EvaluateAsync(ValidationContext context)
    {
        var diagnostics = new List<Diagnostic>();

        foreach (var file in context.TargetFiles)
        {
            if (!file.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                continue;

            var fullPath = Path.Combine(context.RepositoryRoot, file.RelativePath);
            if (!context.FileSystem.FileExists(fullPath))
                continue;

            var content = context.FileSystem.ReadAllText(fullPath);
            var lines = content.Split('\n');
            CheckMarkers(lines, file.RelativePath, diagnostics);
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }

    private void CheckMarkers(string[] lines, string filePath, List<Diagnostic> diagnostics)
    {
        var openRegions = new Stack<(string Id, int Line)>();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');

            if (line.StartsWith("<!-- steward:begin ", StringComparison.OrdinalIgnoreCase))
            {
                var id = ExtractAttribute(line, "id");
                if (id == null)
                {
                    diagnostics.Add(new Diagnostic(
                        RuleId, DefaultSeverity, Category, filePath, i + 1,
                        "Managed region begin marker missing 'id' attribute.",
                        "Add id=\"...\" to the steward:begin marker.", null));
                }
                else
                {
                    openRegions.Push((id, i + 1));
                }
            }
            else if (line.StartsWith("<!-- steward:end", StringComparison.OrdinalIgnoreCase))
            {
                if (openRegions.Count == 0)
                {
                    diagnostics.Add(new Diagnostic(
                        RuleId, DefaultSeverity, Category, filePath, i + 1,
                        "Found steward:end marker without a matching steward:begin.",
                        "Remove the orphaned end marker or add the corresponding begin marker.", null));
                }
                else
                {
                    openRegions.Pop();
                }
            }
        }

        while (openRegions.Count > 0)
        {
            var (id, lineNum) = openRegions.Pop();
            diagnostics.Add(new Diagnostic(
                RuleId, DefaultSeverity, Category, filePath, lineNum,
                $"Managed region '{id}' has a begin marker but no matching end marker.",
                "Add a <!-- steward:end --> marker to close the region.", null));
        }
    }

    private static string? ExtractAttribute(string line, string name)
    {
        var search = $"{name}=\"";
        var idx = line.IndexOf(search, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        var start = idx + search.Length;
        var end = line.IndexOf('"', start);
        if (end < 0) return null;
        return line[start..end];
    }
}
