using DotNet.Globbing;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Steward.Core.Configuration;

namespace Steward.Core.Validation.Rules;

/// <summary>STWD-022: deterministic source-word budgets and bounded index link counts.</summary>
public sealed class DocumentBudgetRule : IValidationRule
{
    public string RuleId => "STWD-022";
    public string Category => "document-budget";
    public DiagnosticSeverity DefaultSeverity => DiagnosticSeverity.Error;
    public string Description => "Documents, sections, table cells and index pages must fit declared content budgets.";
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables().UsePreciseSourceLocation().Build();

    public Task<IReadOnlyList<Diagnostic>> EvaluateAsync(ValidationContext context)
    {
        var diagnostics = new List<Diagnostic>();
        var budgets = (context.Policy?.DocumentContracts?.Budgets ?? [])
            .Select(b => (Glob.Parse(b.Pattern!), Budget: b)).ToArray();
        if (budgets.Length == 0) return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
        foreach (var file in context.TargetFiles.Where(f => !f.IsDirectory &&
                     f.RelativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)))
        {
            var limits = new DocumentBudget();
            foreach (var (glob, budget) in budgets.Where(b => b.Item1.IsMatch(file.RelativePath)))
            {
                limits.MaxWords = budget.MaxWords ?? limits.MaxWords;
                limits.MaxSectionWords = budget.MaxSectionWords ?? limits.MaxSectionWords;
                limits.MaxTableCellWords = budget.MaxTableCellWords ?? limits.MaxTableCellWords;
                limits.MaxIndexEntries = budget.MaxIndexEntries ?? limits.MaxIndexEntries;
            }
            if (limits.MaxWords == null && limits.MaxSectionWords == null &&
                limits.MaxTableCellWords == null && limits.MaxIndexEntries == null) continue;
            try
            {
                var doc = DocumentContractSupport.Read(context, file.RelativePath);
                var body = DocumentContractSupport.Body(file.RelativePath, doc.RawContent);
                Check("document", DocumentContractSupport.Words(body), limits.MaxWords);
                var lines = doc.RawContent.Split('\n');
                var preambleStart = doc.Frontmatter?.Range.End ?? 0;
                var preambleEnd = doc.Sections.Count > 0 ? doc.Sections[0].Range.Start - 1 : lines.Length;
                Check("preamble", DocumentContractSupport.Words(string.Join('\n',
                    lines.Skip(preambleStart).Take(preambleEnd - preambleStart))), limits.MaxSectionWords,
                    preambleStart + 1);
                void Sections(IReadOnlyList<Steward.Core.Markdown.Section> sections)
                {
                    foreach (var section in sections)
                    {
                        var text = string.Join('\n', lines.Skip(section.Range.Start - 1).Take(section.LineCount));
                        Check($"section:{section.Heading}", DocumentContractSupport.Words(text),
                            limits.MaxSectionWords, section.Range.Start);
                        Sections(section.Children);
                    }
                }
                Sections(doc.Sections);
                var parsed = Markdig.Markdown.Parse(body, Pipeline);
                foreach (var cell in parsed.Descendants<TableCell>())
                {
                    // Precise spans correctly preserve escaped pipes and code spans in cells.
                    var text = cell.Span.Start >= 0 && cell.Span.End >= cell.Span.Start
                        ? body.Substring(cell.Span.Start, cell.Span.Length) : "";
                    Check($"table-cell:{cell.Line + 1}:{cell.Column + 1}", DocumentContractSupport.Words(text),
                        limits.MaxTableCellWords, cell.Line + 1 + (doc.Frontmatter?.Range.End ?? 0));
                }
                Check("index-entries", parsed.Descendants<LinkInline>().Count(l => !l.IsImage), limits.MaxIndexEntries);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                diagnostics.Add(DocumentContractSupport.Error(RuleId, Category, file.RelativePath,
                    $"Cannot measure document: {ex.Message}", "Restore read access and rerun check.", "read"));
            }

            void Check(string item, int actual, int? maximum, int? line = null)
            {
                if (maximum != null && actual > maximum)
                    diagnostics.Add(DocumentContractSupport.Error(RuleId, Category, file.RelativePath,
                        $"{item} has {actual} {(item == "index-entries" ? "links" : "words")}; maximum {maximum}.",
                        "Remove duplication or extract a focused document and link to it.", item, line));
            }
        }
        return Task.FromResult<IReadOnlyList<Diagnostic>>(diagnostics);
    }
}
