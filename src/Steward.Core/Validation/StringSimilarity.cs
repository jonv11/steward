namespace Steward.Core.Validation;

internal static class StringSimilarity
{
    internal static int LevenshteinDistance(string a, string b)
    {
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;

        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) d[0, j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[a.Length, b.Length];
    }

    /// <summary>
    /// Returns the closest candidate within <paramref name="maxDistance"/> edit distance, or null.
    /// When multiple candidates tie, returns the lexicographically first.
    /// </summary>
    internal static string? BestMatch(string query, IEnumerable<string> candidates, int maxDistance = 5)
    {
        string? best = null;
        var bestDist = maxDistance + 1;

        foreach (var candidate in candidates)
        {
            var dist = LevenshteinDistance(query, candidate);
            if (dist < bestDist || (dist == bestDist && string.Compare(candidate, best, StringComparison.Ordinal) < 0))
            {
                best = candidate;
                bestDist = dist;
            }
        }

        return bestDist <= maxDistance ? best : null;
    }
}
