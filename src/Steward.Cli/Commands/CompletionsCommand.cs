using System.CommandLine;
using Steward.Cli.Formatting;
using Steward.Core;

namespace Steward.Cli.Commands;

/// <summary>
/// <c>steward completions &lt;bash|zsh|pwsh&gt;</c>: prints a shell completion script. Every script calls
/// back into System.CommandLine's built-in <c>[suggest:&lt;position&gt;]</c> directive, so commands,
/// options and option values complete from the parser itself and never drift from the CLI.
/// </summary>
public static class CompletionsCommand
{
    internal static readonly IReadOnlyList<string> Shells = ["bash", "zsh", "pwsh"];

    public static Command Create()
    {
        var command = new Command(
            "completions",
            "Print a shell completion script (bash, zsh or pwsh); see the README for how to load it");

        var shellArg = new Argument<string>("shell")
        {
            Description = "The shell to print a script for: bash, zsh or pwsh"
        };
        shellArg.AcceptOnlyFromAmong([.. Shells]);
        command.Add(shellArg);

        command.SetAction(parseResult =>
        {
            var output = parseResult.GetValue(GlobalOptionsSetup.OutputOption);
            var noColor = parseResult.GetValue(GlobalOptionsSetup.NoColorOption);
            var shell = parseResult.GetValue(shellArg)!;
            var script = Script(shell);

            if (output == OutputFormat.Json)
            {
                var formatter = CommandSetup.CreateFormatter(output, noColor);
                JsonEnvelopeWriter.Write(formatter, "completions", true, ExitCodes.Success, new { shell, script });
            }
            else
            {
                Console.Out.Write(script);
            }

            return ExitCodes.Success;
        });

        return command;
    }

    internal static string Script(string shell) => shell switch
    {
        "bash" => Bash,
        "zsh" => Zsh,
        "pwsh" => PowerShell,
        _ => throw new ArgumentOutOfRangeException(nameof(shell), shell, "Unsupported shell.")
    };

    private const string Bash = """
        # steward completion for bash. Load it with:
        #   eval "$(steward completions bash)"
        _steward_complete() {
            local IFS=$'\n'
            local cur="${COMP_WORDS[COMP_CWORD]}"
            local suggestions
            suggestions="$(steward "[suggest:${COMP_POINT}]" "${COMP_LINE}" 2>/dev/null)"
            COMPREPLY=($(compgen -W "${suggestions}" -- "${cur}"))
        }
        complete -o default -F _steward_complete steward

        """;

    private const string Zsh = """
        #compdef steward
        # steward completion for zsh. Load it with:
        #   eval "$(steward completions zsh)"
        _steward() {
            local line="${(j: :)words[1,CURRENT]}"
            local -a suggestions
            suggestions=("${(@f)$(steward "[suggest:${#line}]" "${line}" 2>/dev/null)}")
            compadd -a suggestions
        }
        compdef _steward steward

        """;

    private const string PowerShell = """
        # steward completion for PowerShell. Load it with:
        #   steward completions pwsh | Out-String | Invoke-Expression
        Register-ArgumentCompleter -Native -CommandName steward -ScriptBlock {
            param($wordToComplete, $commandAst, $cursorPosition)
            $line = $commandAst.ToString()
            $position = $cursorPosition - $commandAst.Extent.StartOffset
            if ($line.Length -lt $position) { $line = $line.PadRight($position) }
            & steward "[suggest:$position]" "$line" 2>$null |
                Where-Object { $_ -like "$wordToComplete*" } |
                ForEach-Object { [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_) }
        }

        """;
}
