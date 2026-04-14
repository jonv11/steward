using System.Text.RegularExpressions;

namespace Steward.Core.Validation;

public static partial class SecretFilter
{
    private static readonly Regex[] Patterns =
    [
        ApiKeyPattern(),
        AwsKeyPattern(),
        GenericSecretPattern(),
        ConnectionStringPasswordPattern(),
    ];

    public static string Redact(string text)
    {
        var result = text;
        foreach (var pattern in Patterns)
        {
            result = pattern.Replace(result, match =>
            {
                var prefix = match.Groups[1].Value;
                return $"{prefix}***REDACTED***";
            });
        }
        return result;
    }

    public static bool ContainsSecret(string text)
    {
        return Patterns.Any(p => p.IsMatch(text));
    }

    [GeneratedRegex(@"((?:api[_-]?key|apikey)\s*[=:]\s*)[""']?[\w\-]{20,}[""']?", RegexOptions.IgnoreCase)]
    private static partial Regex ApiKeyPattern();

    [GeneratedRegex(@"(AKIA)[A-Z0-9]{16}", RegexOptions.None)]
    private static partial Regex AwsKeyPattern();

    [GeneratedRegex(@"((?:secret|token|password|passwd|pwd)\s*[=:]\s*)[""']?[\w\-\.]{8,}[""']?", RegexOptions.IgnoreCase)]
    private static partial Regex GenericSecretPattern();

    [GeneratedRegex(@"((?:Password|pwd)\s*=\s*)[^;""'\s]{8,}", RegexOptions.IgnoreCase)]
    private static partial Regex ConnectionStringPasswordPattern();
}
