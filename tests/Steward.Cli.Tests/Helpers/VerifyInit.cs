using System.Runtime.CompilerServices;

namespace Steward.Cli.Tests;

/// <summary>
/// Configures the Verify snapshot library before any tests run.
/// Sets the DiffEngine_Disabled environment variable so that snapshot mismatches
/// produce a test failure immediately instead of blocking the runner while waiting
/// for user input to a GUI diff program.
/// </summary>
public static class VerifyInit
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // Prevent DiffEngine (used by Verify) from opening a diff tool window
        // when a snapshot does not match. Without this, a mismatch hangs the
        // test runner on a developer machine waiting for the diff GUI to close.
        Environment.SetEnvironmentVariable("DiffEngine_Disabled", "true");
    }
}
