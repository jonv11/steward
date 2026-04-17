[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [string]$ChangelogPath = "CHANGELOG.md",
    [string]$OutputPath
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot "..\..")
$resolvedChangelog = Join-Path $repoRoot $ChangelogPath

if (-not (Test-Path $resolvedChangelog)) {
    throw "Changelog file not found: $resolvedChangelog"
}

$lines = Get-Content $resolvedChangelog
$headingPattern = '^## \[(?<version>[^\]]+)\](?: - .+)?$'

$startIndex = -1
$endIndex = $lines.Count

for ($index = 0; $index -lt $lines.Count; $index++) {
    if ($lines[$index] -match $headingPattern) {
        if ($Matches["version"] -eq $Version) {
            $startIndex = $index
            continue
        }

        if ($startIndex -ge 0) {
            $endIndex = $index
            break
        }
    }
}

if ($startIndex -lt 0) {
    throw "No changelog entry found for version $Version."
}

$section = $lines[$startIndex..($endIndex - 1)] -join [Environment]::NewLine

if ($OutputPath) {
    $resolvedOutput = if ([System.IO.Path]::IsPathRooted($OutputPath)) {
        $OutputPath
    }
    else {
        Join-Path $repoRoot $OutputPath
    }

    $outputDirectory = Split-Path -Parent $resolvedOutput
    if ($outputDirectory) {
        New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
    }

    Set-Content -Path $resolvedOutput -Value $section
}
else {
    $section
}
