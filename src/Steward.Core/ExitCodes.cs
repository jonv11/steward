namespace Steward.Core;

/// <summary>
/// Standard exit codes for the steward CLI.
/// </summary>
public static class ExitCodes
{
    public const int Success = 0;
    public const int ValidationFailure = 1;
    public const int UsageError = 2;
    public const int InternalError = 3;
}
