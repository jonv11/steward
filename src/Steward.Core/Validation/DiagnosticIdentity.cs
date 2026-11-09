namespace Steward.Core.Validation;

/// <summary>
/// The line-independent identity of a diagnostic: rule, repository-relative path and selector.
/// Two diagnostics with the same identity describe the same violation, even when the content
/// around it has moved. This is the only place identity is computed; baseline matching and any
/// other consumer that needs to recognise "the same violation" across runs must go through it.
/// </summary>
/// <remarks>
/// The selector is the one the rule supplies via <see cref="Diagnostic.Selector"/>. When the rule
/// supplies none, the message is used. Rules whose message embeds a volatile value — a line
/// number, an age, a count — must supply an explicit selector so that value never enters the
/// identity.
/// </remarks>
public readonly record struct DiagnosticIdentity(string RuleId, string Path, string Selector)
    : IComparable<DiagnosticIdentity>
{
    public static DiagnosticIdentity Of(Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        var path = diagnostic.Path == null ? "" : PathHelper.NormalizeSeparators(diagnostic.Path);
        var selector = string.IsNullOrWhiteSpace(diagnostic.Selector)
            ? diagnostic.Message
            : diagnostic.Selector;

        return new DiagnosticIdentity(diagnostic.RuleId.ToUpperInvariant(), path, selector);
    }

    /// <summary>Orders by path, then rule, then selector, using ordinal comparison.</summary>
    public int CompareTo(DiagnosticIdentity other)
    {
        var byPath = string.CompareOrdinal(Path, other.Path);
        if (byPath != 0) return byPath;
        var byRule = string.CompareOrdinal(RuleId, other.RuleId);
        return byRule != 0 ? byRule : string.CompareOrdinal(Selector, other.Selector);
    }
}
