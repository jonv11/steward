using System.Text.Json;
using Steward.Core.Validation;

namespace Steward.Cli.Formatting;

/// <summary>
/// Emits SARIF 2.1.0 JSON for 'steward check --output sarif'.
/// SARIF is the standard format for GitHub Advanced Security code-scanning annotations.
/// </summary>
public static class SarifWriter
{
    private static readonly string ToolVersion =
        typeof(SarifWriter).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static void Write(
        TextWriter stdout,
        IReadOnlyList<Diagnostic> diagnostics,
        IReadOnlyList<IValidationRule> allRules,
        string? toolVersion = null)
    {
        var version = toolVersion ?? ToolVersion;

        // Build the deduplicated rules list from diagnostics actually emitted.
        var ruleIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        var ruleObjects = new List<object>();

        foreach (var diag in diagnostics)
        {
            if (ruleIndex.ContainsKey(diag.RuleId))
                continue;

            var ruleDesc = allRules.FirstOrDefault(r => r.RuleId == diag.RuleId)?.Description ?? diag.RuleId;
            ruleIndex[diag.RuleId] = ruleObjects.Count;
            ruleObjects.Add(new
            {
                id = diag.RuleId,
                shortDescription = new { text = ruleDesc }
            });
        }

        var results = diagnostics.Select(diag =>
        {
            var level = diag.Severity switch
            {
                DiagnosticSeverity.Error => "error",
                DiagnosticSeverity.Warning => "warning",
                _ => "note"
            };

            object? location = null;
            if (diag.Path != null)
            {
                var uri = diag.Path.Replace('\\', '/');
                object region = diag.Line.HasValue
                    ? (object)new { startLine = diag.Line.Value }
                    : new { };

                location = new
                {
                    physicalLocation = new
                    {
                        artifactLocation = new { uri, uriBaseId = "%SRCROOT%" },
                        region
                    }
                };
            }

            var result = new Dictionary<string, object>
            {
                ["ruleId"] = diag.RuleId,
                ["ruleIndex"] = ruleIndex[diag.RuleId],
                ["level"] = level,
                ["message"] = new { text = diag.Message }
            };

            if (location != null)
                result["locations"] = new[] { location };

            return result;
        }).ToList();

        var sarif = new
        {
            version = "2.1.0",
            runs = new[]
            {
                new
                {
                    tool = new
                    {
                        driver = new
                        {
                            name = "Steward",
                            version,
                            informationUri = "https://github.com/jonv11/steward",
                            rules = ruleObjects
                        }
                    },
                    results
                }
            }
        };

        var json = JsonSerializer.Serialize(sarif, JsonOptions);
        stdout.WriteLine(json);
    }
}
