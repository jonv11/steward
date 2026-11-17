using System.CommandLine;
using System.Reflection;
using System.Runtime.InteropServices;
using Steward.Cli.Formatting;
using Steward.Core;
using Steward.Core.Models;

namespace Steward.Cli.Commands;

public static class VersionCommand
{
    public static Command Create()
    {
        var command = new Command("version", "Print version and runtime information");
        command.SetAction((parseResult) =>
        {
            var output = parseResult.GetValue(GlobalOptionsSetup.OutputOption);
            var noColor = parseResult.GetValue(GlobalOptionsSetup.NoColorOption);

            var formatter = CommandSetup.CreateFormatter(output, noColor);

            var assembly = typeof(VersionCommand).Assembly;
            var releaseBuild = IsReleaseBuild(assembly);
            var info = new VersionInfo
            {
                Version = assembly.GetName().Version?.ToString(3) ?? "0.0.0",
                RuntimeVersion = RuntimeInformation.FrameworkDescription,
                OsPlatform = RuntimeInformation.OSDescription,
                Architecture = RuntimeInformation.OSArchitecture.ToString(),
                Commit = releaseBuild ? null : Commit(assembly),
                ReleaseBuild = releaseBuild,
            };

            if (output == OutputFormat.Json)
            {
                JsonEnvelopeWriter.Write(formatter, "version", true, ExitCodes.Success, info);
            }
            else
            {
                formatter.WriteMessage(Describe(info));
                formatter.WriteMessage($"Runtime: {info.RuntimeVersion}");
                formatter.WriteMessage($"OS: {info.OsPlatform} ({info.Architecture})");
            }

            return ExitCodes.Success;
        });

        return command;
    }

    /// <summary>
    /// "steward 0.19.0" for a release; a source build adds its commit, because it may behave
    /// differently from the release that shares its version number (adoption-trial-defects/12).
    /// </summary>
    internal static string Describe(VersionInfo info) => info.ReleaseBuild
        ? $"steward {info.Version}"
        : $"steward {info.Version}{(info.Commit is null ? "" : "+" + info.Commit)} (source build, not a release)";

    private static bool IsReleaseBuild(Assembly assembly) => assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .Any(attribute => attribute.Key == "StewardReleaseBuild" &&
            string.Equals(attribute.Value, "true", StringComparison.OrdinalIgnoreCase));

    /// <summary>The short commit the SDK appends to the informational version ("0.19.0+&lt;sha&gt;").</summary>
    private static string? Commit(Assembly assembly)
    {
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var plus = informational?.IndexOf('+') ?? -1;
        if (informational is null || plus < 0 || plus == informational.Length - 1)
        {
            return null;
        }

        var commit = informational[(plus + 1)..];
        return commit.Length > 7 ? commit[..7] : commit;
    }
}
