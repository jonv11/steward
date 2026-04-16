using Steward.Core.Formatting;

namespace Steward.Cli.Formatting;

internal static class OutputStyler
{
    public static string Style(IOutputFormatter formatter, string text, CliTextStyle style)
    {
        return formatter is TextOutputFormatter textFormatter
            ? textFormatter.Style(text, style)
            : text;
    }

    public static string Classification(IOutputFormatter formatter, string classification)
    {
        var style = classification switch
        {
            "authoritative" => CliTextStyle.Success,
            "governance" => CliTextStyle.Accent,
            "workflow" => CliTextStyle.Directory,
            _ when classification.StartsWith("state:", StringComparison.OrdinalIgnoreCase) => CliTextStyle.Warning,
            _ when classification.StartsWith("family:", StringComparison.OrdinalIgnoreCase) => CliTextStyle.Accent,
            _ => CliTextStyle.Muted
        };

        return Style(formatter, classification, style);
    }

    public static string StatusToken(IOutputFormatter formatter, string status)
    {
        var style = status switch
        {
            "OK" or "OK   " => CliTextStyle.Success,
            "STALE" => CliTextStyle.Warning,
            "MISSING" => CliTextStyle.Error,
            _ => CliTextStyle.Muted
        };

        return Style(formatter, status, style);
    }
}
