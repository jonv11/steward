namespace Steward.Core.Maintenance;

/// <summary>
/// Shared helper for finding and replacing content between managed region markers.
/// </summary>
public static class ManagedRegionRewriter
{
    /// <summary>
    /// Replaces the content between steward managed region markers.
    /// Returns null if markers are not found.
    /// </summary>
    public static string? Replace(string content, string sectionId, string newInner)
    {
        var lines = content.Split('\n').ToList();
        var beginMarker = $"<!-- steward:begin id=\"{sectionId}\"";
        var endMarker = "<!-- steward:end -->";

        var beginIdx = lines.FindIndex(l =>
            l.TrimEnd('\r').Contains(beginMarker, StringComparison.OrdinalIgnoreCase));

        if (beginIdx < 0) return null;

        var endIdx = lines.FindIndex(beginIdx + 1, l =>
            l.TrimEnd('\r').Contains(endMarker, StringComparison.OrdinalIgnoreCase));

        if (endIdx < 0) return null;

        var newLines = new List<string>(lines.Take(beginIdx + 1));
        newLines.AddRange(newInner.Split('\n'));
        newLines.AddRange(lines.Skip(endIdx));

        return string.Join('\n', newLines);
    }

    /// <summary>
    /// Checks whether managed region markers exist in the content.
    /// </summary>
    public static bool HasMarkers(string content, string sectionId)
    {
        var beginMarker = $"<!-- steward:begin id=\"{sectionId}\"";
        return content.Contains(beginMarker, StringComparison.OrdinalIgnoreCase);
    }
}
