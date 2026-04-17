[CmdletBinding()]
param(
    [string]$Version,
    [string]$Configuration = "Release",
    [string]$ArtifactsRoot = "artifacts/release",
    [string[]]$RuntimeIds = @("win-x64", "linux-x64", "osx-arm64")
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot "..\..")

if (-not $Version) {
    [xml]$props = Get-Content (Join-Path $repoRoot "Directory.Build.props")
    $Version = $props.Project.PropertyGroup.Version
}

$releaseRoot = Join-Path $repoRoot $ArtifactsRoot
$versionRoot = Join-Path $releaseRoot "v$Version"
$publishRoot = Join-Path $versionRoot "publish"

if (Test-Path $versionRoot) {
    Remove-Item $versionRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $versionRoot | Out-Null
New-Item -ItemType Directory -Path $publishRoot | Out-Null

Push-Location $repoRoot
try {
    dotnet pack src/Steward.Cli/Steward.Cli.csproj -c $Configuration -o $versionRoot

    foreach ($runtimeId in $RuntimeIds) {
        $publishDir = Join-Path $publishRoot $runtimeId
        $bundleName = "steward-$Version-$runtimeId"
        $bundleDir = Join-Path $versionRoot $bundleName
        $zipPath = Join-Path $versionRoot "$bundleName.zip"

        dotnet publish src/Steward.Cli/Steward.Cli.csproj `
            -c $Configuration `
            -r $runtimeId `
            --self-contained true `
            -p:PublishSingleFile=true `
            -p:PublishTrimmed=false `
            -o $publishDir

        New-Item -ItemType Directory -Path $bundleDir | Out-Null
        Copy-Item (Join-Path $publishDir "*") -Destination $bundleDir -Recurse -Force

        Compress-Archive -Path $bundleDir -DestinationPath $zipPath -CompressionLevel Optimal
    }

    $assetFiles = Get-ChildItem -Path $versionRoot -File | Sort-Object Name
    $hashLines = foreach ($file in $assetFiles) {
        $hash = (Get-FileHash -Path $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        "$hash  $($file.Name)"
    }

    Set-Content -Path (Join-Path $versionRoot "SHA256SUMS.txt") -Value $hashLines
}
finally {
    Pop-Location
}
