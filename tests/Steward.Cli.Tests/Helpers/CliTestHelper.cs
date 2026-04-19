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
        return Program.InvokeAsync(args).GetAwaiter().GetResult();
    }

    public static int InvokeWithHandler(Func<string[], Task<int>> invoker, params string[] args)
    {
        return Program.InvokeWithTopLevelHandlingAsync(args, invoker).GetAwaiter().GetResult();
    }

    public static (int ExitCode, string Output, string Error) InvokeCapture(params string[] args)
    {
        return InvokeCaptureCore(static invocationArgs => Program.InvokeAsync(invocationArgs), args);
    }

    public static (int ExitCode, string Output, string Error) InvokeCapture(
        Func<string[], Task<int>> invoker,
        params string[] args)
    {
        return InvokeCaptureCore(invocationArgs => Program.InvokeWithTopLevelHandlingAsync(invocationArgs, invoker), args);
    }

    private static (int ExitCode, string Output, string Error) InvokeCaptureCore(
        Func<string[], Task<int>> invoker,
        string[] args)
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
                var exitCode = invoker(args).GetAwaiter().GetResult();
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
