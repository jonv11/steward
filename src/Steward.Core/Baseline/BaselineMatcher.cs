using Steward.Core.Validation;

namespace Steward.Core.Baseline;

/// <summary>The result of matching a diagnostic list against a baseline.</summary>
public sealed record BaselineMatch(
    IReadOnlyList<Diagnostic> New,
    IReadOnlyList<Diagnostic> Baselined,
    IReadOnlyList<BaselineEntry> Drift);

/// <summary>
/// Partitions diagnostics against a baseline with multiset semantics: each entry absorbs at most
/// one diagnostic with the same <see cref="DiagnosticIdentity"/>. Diagnostics left over are new;
/// entries left over have drifted (the violation they recorded is gone).
/// </summary>
public static class BaselineMatcher
{
    public static BaselineMatch Match(IEnumerable<Diagnostic> diagnostics, BaselineDocument baseline)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        ArgumentNullException.ThrowIfNull(baseline);

        var remaining = new Dictionary<DiagnosticIdentity, Queue<BaselineEntry>>();
        foreach (var entry in baseline.Entries)
        {
            if (!remaining.TryGetValue(entry.Identity, out var queue))
                remaining[entry.Identity] = queue = new Queue<BaselineEntry>();
            queue.Enqueue(entry);
        }

        var fresh = new List<Diagnostic>();
        var baselined = new List<Diagnostic>();
        foreach (var diagnostic in diagnostics)
        {
            if (remaining.TryGetValue(DiagnosticIdentity.Of(diagnostic), out var queue) && queue.Count > 0)
            {
                queue.Dequeue();
                baselined.Add(diagnostic);
            }
            else
            {
                fresh.Add(diagnostic);
            }
        }

        var drift = remaining.Values.SelectMany(q => q).OrderBy(e => e.Identity).ToList();
        return new BaselineMatch(fresh, baselined, drift);
    }
}
