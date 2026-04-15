namespace Steward.Core.Configuration;

/// <summary>
/// Maps artifact roles to sensible governance defaults.
/// Explicit per-artifact configuration always takes precedence.
/// </summary>
public static class RoleDefaults
{
    private static readonly Dictionary<string, RoleBehavior> Defaults = new(StringComparer.OrdinalIgnoreCase)
    {
        ["authoritative"]   = new("required",    DefaultFreshnessDays: null),
        ["requirements"]    = new("required",    DefaultFreshnessDays: null),
        ["state-document"]  = new("required",    DefaultFreshnessDays: 30),
        ["generated"]       = new("recommended", DefaultFreshnessDays: null),
        ["guide"]           = new("recommended", DefaultFreshnessDays: null),
        ["changelog"]       = new("optional",    DefaultFreshnessDays: null),
        ["audit"]           = new("optional",    DefaultFreshnessDays: null),
    };

    /// <summary>
    /// Returns the default importance for an artifact based on its role.
    /// Returns null if role is unknown (caller decides fallback).
    /// </summary>
    public static string? GetDefaultImportance(string? role)
    {
        if (role is null || !Defaults.TryGetValue(role, out var behavior))
            return null;
        return behavior.DefaultImportance;
    }

    /// <summary>
    /// Returns the default freshness window (days) implied by the role.
    /// Returns null when the role doesn't imply freshness tracking.
    /// </summary>
    public static int? GetDefaultFreshnessDays(string? role)
    {
        if (role is null || !Defaults.TryGetValue(role, out var behavior))
            return null;
        return behavior.DefaultFreshnessDays;
    }

    private sealed record RoleBehavior(string DefaultImportance, int? DefaultFreshnessDays);
}
