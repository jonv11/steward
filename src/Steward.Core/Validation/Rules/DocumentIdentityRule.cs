using System.Collections;
using DotNet.Globbing;

namespace Steward.Core.Validation.Rules;

/// <summary>STWD-023: identity uniqueness and metadata relationships use the whole corpus.</summary>
public sealed class DocumentIdentityRule : IValidationRule
{
    public string RuleId => "STWD-023";
    public string Category => "document-identity";
    public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Error;
    public string Description => "Document identities must be unique and declared metadata relationships must resolve.";

    public Task<IReadOnlyList<Diagnostic>> EvaluateAsync(ValidationContext context)
    {
        var diagnostics = new List<Diagnostic>();
        var config = context.Policy?.DocumentContracts;
        if (config == null) return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
        var files = (context.AllDiscoveredFiles ?? context.TargetFiles)
            .Where(f => !f.IsDirectory).Select(f => f.RelativePath).ToHashSet(StringComparer.Ordinal);
        foreach (var identity in config.Identities ?? [])
        {
            var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in files.Where(Glob.Parse(identity.Pattern!).IsMatch).Order(StringComparer.Ordinal))
            {
                var fields = ReadFields(path);
                var value = fields?.GetValueOrDefault(identity.Field!);
                if (value is not string text || string.IsNullOrWhiteSpace(text))
                {
                    if (identity.Required || value != null)
                        Add(path, $"Identity '{identity.Field}' must be a non-empty scalar string.", identity.Field!);
                    continue;
                }
                if (!seen.TryAdd(text.Trim(), path))
                    Add(path, $"Identity '{identity.Field}: {text}' also belongs to '{seen[text.Trim()]}'.", identity.Field!);
            }
        }
        foreach (var relation in config.Relations ?? [])
        {
            var targets = Glob.Parse(relation.TargetPattern!);
            foreach (var path in files.Where(Glob.Parse(relation.Pattern!).IsMatch))
            {
                var value = ReadFields(path)?.GetValueOrDefault(relation.Field!);
                if (value == null) continue;
                var values = value is string scalar ? new object[] { scalar } :
                    value is IEnumerable sequence ? sequence.Cast<object>().ToArray() : new[] { value };
                foreach (var target in values)
                {
                    if (target is not string targetPath || !DocumentContractSupport.IsRepositoryPath(targetPath) ||
                        !files.Contains(targetPath) || !targets.IsMatch(targetPath))
                        Add(path, $"Relationship '{relation.Field}' target '{target}' must name a discovered file matching '{relation.TargetPattern}'.", relation.Field!);
                }
            }
        }
        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);

        Dictionary<string, object?>? ReadFields(string path)
        {
            try { return DocumentContractSupport.Read(context, path).Frontmatter?.Fields; }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Add(path, $"Cannot inspect document metadata: {ex.Message}", "read");
                return null;
            }
        }
        void Add(string path, string message, string field) => diagnostics.Add(DocumentContractSupport.Error(
            RuleId, Category, path, message, "Keep one authoritative identity and use repository-relative relationship paths.",
            $"field:{field}"));
    }
}
