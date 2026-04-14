using System.CommandLine;
using System.Globalization;

namespace Steward.Cli.Tests.Helpers;

internal static class CliTestHelper
{
    private static readonly Lock ConsoleLock = new();

    public static RootCommand CreateRootCommand()
    {
        return Program.CreateRootCommand();
    }

    public static int Invoke(params string[] args)
    {
        var rootCommand = CreateRootCommand();
        var parseResult = rootCommand.Parse(args);
        var exitCode = parseResult.Invoke();

        if (parseResult.Errors.Count > 0 && exitCode != Steward.Core.ExitCodes.Success)
            return Steward.Core.ExitCodes.UsageError;

        return exitCode;
    }

    public static (int ExitCode, string Output, string Error) InvokeCapture(params string[] args)
    {
        lock (ConsoleLock)
        {
            var stdOut = new StringWriter();
            var stdErr = new StringWriter();

            var originalOut = Console.Out;
            var originalErr = Console.Error;
            var originalCulture = CultureInfo.CurrentCulture;
            var originalUiCulture = CultureInfo.CurrentUICulture;

            Console.SetOut(stdOut);
            Console.SetError(stdErr);
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            try
            {
                var exitCode = Invoke(args);
                return (exitCode, NormalizeOutput(stdOut.ToString()), NormalizeOutput(stdErr.ToString()));
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUiCulture;
            }
        }
    }

    private static string NormalizeOutput(string value)
    {
        return value.Replace("testhost", "steward", StringComparison.OrdinalIgnoreCase);
    }
}
