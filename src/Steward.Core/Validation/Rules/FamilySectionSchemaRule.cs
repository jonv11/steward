using Steward.Core.Configuration;
using Steward.Core.Markdown;

namespace Steward.Core.Validation.Rules;

/// <summary>
/// STWD-021: Enforces <c>section_schema</c> declared on artifact families.
/// Checks that required H2 sections are present, optionally that no unlisted H2s exist
/// (<c>allow_extra: false</c>), and optionally that sections appear in the declared order
/// (<c>enforce_order: true</c>).
/// Heading matching defaults to case-insensitive substring ("contains") to handle numbered
/// sections like "1. Context" matching a schema entry of "Context".
/// Not auto-fixable; section structure requires human judgment.
/// </summary>
public sealed class FamilySectionSchemaRule : IValidationRule
{
    public string RuleId => "STWD-021";
    public string Category => "structure";
    public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Warning;
    public string Description => "Files matched by an artifact family must satisfy the family's section_schema: required H2 sections present, no unexpected sections (if allow_extra: false), and correct order (if enforce_order: true).";

    public Task<IReadOnlyList<Diagnostic>> EvaluateAsync(ValidationContext context)
    {
        var diagnostics = new List<Diagnostic>();

        var families = context.Policy?.ArtifactFamilies;
        if (families == null || families.Count == 0)
            return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);

        var applicable = families
            .Where(f => f.SectionSchema?.Sections is { Count: > 0 } && f.Match != null && !string.IsNullOrWhiteSpace(f.Family))
            .ToList();

        if (applicable.Count == 0)
            return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);

        var classifier = new ArtifactFamilyClassifier(applicable);

        foreach (var file in context.TargetFiles)
        {
            if (file.IsDirectory) continue;
            if (!file.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)) continue;

            var matched = classifier.ClassifyFile(file.RelativePath, context.FileSystem, context.RepositoryRoot);
            if (matched == null) continue;

            var schema = matched.SectionSchema!;
            var entries = schema.Sections!;
            var useContains = !string.Equals(schema.HeadingMatch, "exact", StringComparison.OrdinalIgnoreCase);

            StructuredDocument? doc = null;
            try
            {
                doc = context.DocumentCache?.GetOrParse(file.RelativePath)
                    ?? MarkdownParser.Parse(file.RelativePath,
                        context.FileSystem.ReadAllText(
                            Path.Combine(context.RepositoryRoot, file.RelativePath)));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            var h2Sections = MarkdownHeadings.Flatten(doc.Sections)
                .Where(s => s.Level == 2)
                .ToList();

            // Check required sections are present (skip if no H2s — document skeleton not yet written)
            if (h2Sections.Count > 0)
            {
                foreach (var entry in entries.Where(e => e.Required && !string.IsNullOrWhiteSpace(e.Heading)))
                {
                    var found = h2Sections.Any(h => MatchesEntry(h.Heading, entry.Heading!, useContains));
                    if (!found)
                    {
                        diagnostics.Add(new Diagnostic(
                            RuleId,
                            DefaultSeverity,
                            Category,
                            file.RelativePath,
                            null,
                            $"Required section '{entry.Heading}' is missing from '{file.RelativePath}' [family: {matched.Family}].",
                            $"Add a '## {entry.Heading}' heading to the document.",
                            "policy.yaml"));
                    }
                }
            }

            // Check for unexpected sections (allow_extra: false)
            if (!schema.AllowExtra)
            {
                foreach (var h2 in h2Sections)
                {
                    var inSchema = entries.Any(e =>
                        !string.IsNullOrWhiteSpace(e.Heading) &&
                        MatchesEntry(h2.Heading, e.Heading!, useContains));
                    if (!inSchema)
                    {
                        diagnostics.Add(new Diagnostic(
                            RuleId,
                            DefaultSeverity,
                            Category,
                            file.RelativePath,
                            h2.Range.Start,
                            $"Section '{h2.Heading}' (line {h2.Range.Start}) in '{file.RelativePath}' is not defined in the section_schema for family '{matched.Family}'.",
                            $"Remove the section or add it to section_schema in policy.yaml.",
                            "policy.yaml"));
                    }
                }
            }

            // Check section ordering (enforce_order: true)
            if (schema.EnforceOrder && h2Sections.Count > 0)
            {
                // Build a map from each document H2 to its first matching schema index
                var schemaMatches = h2Sections
                    .Select(h2 =>
                    {
                        var idx = entries.FindIndex(e =>
                            !string.IsNullOrWhiteSpace(e.Heading) &&
                            MatchesEntry(h2.Heading, e.Heading!, useContains));
                        return (H2: h2, SchemaIndex: idx);
                    })
                    .Where(x => x.SchemaIndex >= 0)
                    .ToList();

                // Verify the schema indices are non-decreasing across document order
                var lastSchemaIndex = -1;
                var lastMatchedEntry = (string?)null;
                foreach (var (h2, schemaIndex) in schemaMatches)
                {
                    if (schemaIndex < lastSchemaIndex)
                    {
                        diagnostics.Add(new Diagnostic(
                            RuleId,
                            DefaultSeverity,
                            Category,
                            file.RelativePath,
                            h2.Range.Start,
                            $"Section '{h2.Heading}' (line {h2.Range.Start}) appears out of order in '{file.RelativePath}'; schema requires it after '{lastMatchedEntry}' [family: {matched.Family}].",
                            $"Reorder the sections to match the sequence declared in section_schema in policy.yaml.",
                            "policy.yaml"));
                    }
                    else
                    {
                        lastSchemaIndex = schemaIndex;
                        lastMatchedEntry = entries[schemaIndex].Heading;
                    }
                }
            }
        }

        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }

    private static bool MatchesEntry(string actualHeading, string schemaHeading, bool useContains)
    {
        return useContains
            ? actualHeading.Contains(schemaHeading, StringComparison.OrdinalIgnoreCase)
            : string.Equals(actualHeading, schemaHeading, StringComparison.OrdinalIgnoreCase);
    }
}
