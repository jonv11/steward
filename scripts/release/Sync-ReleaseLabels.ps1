[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$Repository,
    [string]$ManifestPath = ".github/release-labels.json"
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "GitHub CLI ('gh') is required to sync release labels."
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot "..\..")
$resolvedManifest = Join-Path $repoRoot $ManifestPath

if (-not (Test-Path $resolvedManifest)) {
    throw "Label manifest not found: $resolvedManifest"
}

if (-not $Repository) {
    if ($env:GITHUB_REPOSITORY) {
        $Repository = $env:GITHUB_REPOSITORY
    }
    else {
        $Repository = (gh repo view --json nameWithOwner --jq .nameWithOwner).Trim()
    }
}

$desiredLabels = Get-Content $resolvedManifest -Raw | ConvertFrom-Json
$existingLabels = gh label list --repo $Repository --limit 200 --json name,color,description | ConvertFrom-Json

foreach ($label in $desiredLabels) {
    $existing = $existingLabels | Where-Object { $_.name -eq $label.name } | Select-Object -First 1

    if ($existing) {
        if ($PSCmdlet.ShouldProcess("$Repository/$($label.name)", "Update label")) {
            gh label edit $label.name --repo $Repository --color $label.color --description $label.description | Out-Null
        }
    }
    else {
        if ($PSCmdlet.ShouldProcess("$Repository/$($label.name)", "Create label")) {
            gh label create $label.name --repo $Repository --color $label.color --description $label.description | Out-Null
        }
    }
}
