using System.Text.RegularExpressions;
using Steward.Core.Markdown;

namespace Steward.Core.Validation.Rules;

internal static class DocumentContractSupport
{
    // Source-based counting includes code, HTML and link destinations. Wrapping changes no count.
    public static int Words(string text) => Regex.Count(text, @"\S+", RegexOptions.CultureInvariant);

    public static string Body(string path, string text)
    {
        var fm = MarkdownParser.Parse(path, text).Frontmatter;
        if (fm == null) return text;
        var offset = 0;
        for (var line = 0; line < fm.Range.End; line++)
        {
            var next = text.IndexOf('\n', offset);
            if (next < 0) return "";
            offset = next + 1;
        }
        return text[offset..];
    }

    public static StructuredDocument Read(ValidationContext context, string path) =>
        context.DocumentCache?.GetOrParse(path) ?? MarkdownParser.Parse(path,
            context.FileSystem.ReadAllText(Path.Combine(context.RepositoryRoot, path)));

    public static bool IsRepositoryPath(string path) =>
        !string.IsNullOrWhiteSpace(path) && !Path.IsPathRooted(path) &&
        !path.Contains('\\') && !path.Split('/').Any(p => p is ".." or "." or "");

    public static Diagnostic Error(string rule, string category, string? path, string message,
        string remediation, string selector, int? line = null) =>
        new(rule, DiagnosticSeverity.Error, category, path, line, message, remediation,
            "policy.yaml", Selector: selector);
}
