namespace Steward.Core.Configuration;

/// <summary>
/// Well-known artifact roles with special handling in orient and status.
/// State-document roles represent memory-oriented artifacts (REQ-STATE-001).
/// </summary>
public static class WellKnownRoles
{
    // General roles
    public const string Authoritative = "authoritative";
    public const string Governance = "governance";
    public const string Documentation = "documentation";
    public const string Changelog = "changelog";
    public const string Workflow = "workflow";

    // State-document roles (REQ-STATE-001 through REQ-STATE-003)
    public const string Vision = "vision";
    public const string Roadmap = "roadmap";
    public const string CurrentState = "current-state";
    public const string Milestones = "milestones";
    public const string DecisionLog = "decision-log";

    /// <summary>
    /// Returns true if the role is a recognized state-document role.
    /// State documents receive special treatment in orient and status output.
    /// </summary>
    public static bool IsStateDocumentRole(string? role) =>
        role != null && StateDocumentRoles.Contains(role);

    public static readonly IReadOnlySet<string> StateDocumentRoles =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Vision, Roadmap, CurrentState, Milestones, DecisionLog
        };

    public static readonly IReadOnlySet<string> AllRoles =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Authoritative, Governance, Documentation, Changelog, Workflow,
            Vision, Roadmap, CurrentState, Milestones, DecisionLog
        };
}
