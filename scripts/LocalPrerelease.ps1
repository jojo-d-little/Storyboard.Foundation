[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$LocalNuGetFeed,

    [string]$BaseVersion,

    [switch]$KeepPackage
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Set-Location $repoRoot

if (-not $LocalNuGetFeed) {
    $LocalNuGetFeed = 'C:\work\LocalNugetDevFeed'
}
elseif (-not [IO.Path]::IsPathRooted($LocalNuGetFeed)) {
    $LocalNuGetFeed = Join-Path $repoRoot $LocalNuGetFeed
}

$LocalNuGetFeed = [IO.Path]::GetFullPath($LocalNuGetFeed)
$projectFile = Join-Path $repoRoot 'src\Storyboard.Foundation\Storyboard.Foundation.csproj'

if (-not $BaseVersion) {
    [xml]$project = Get-Content -LiteralPath $projectFile -Raw
    $BaseVersion = [string]$project.Project.PropertyGroup.Version
}

$versionMatch = [regex]::Match(
    $BaseVersion,
    '^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-(?<prerelease>[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$')
if (-not $versionMatch.Success) {
    throw "Cannot create a local prerelease from base version '$BaseVersion'."
}

$major = $versionMatch.Groups['major'].Value
$minor = $versionMatch.Groups['minor'].Value
$patch = $versionMatch.Groups['patch'].Value
if ($versionMatch.Groups['prerelease'].Success) {
    $prereleaseBaseVersion = "$major.$minor.$patch"
}
else {
    $patchNumber = [System.Numerics.BigInteger]::Parse($patch, [Globalization.CultureInfo]::InvariantCulture)
    $nextPatch = ($patchNumber + [System.Numerics.BigInteger]::One).ToString([Globalization.CultureInfo]::InvariantCulture)
    $prereleaseBaseVersion = "$major.$minor.$nextPatch"
}

$timestamp = [DateTime]::UtcNow.ToString("yyyyMMdd.'t'HHmmssfff", [Globalization.CultureInfo]::InvariantCulture)
$version = "$prereleaseBaseVersion-local.$timestamp"
$packageRoot = Join-Path $repoRoot 'artifacts\packages\local-prerelease'
$packageOutput = Join-Path $packageRoot $version
$packageFile = Join-Path $packageOutput "Storyboard.Foundation.$version.nupkg"

New-Item -ItemType Directory -Force -Path $LocalNuGetFeed, $packageOutput | Out-Null

Write-Host "Version source:   $BaseVersion"
Write-Host "Local prerelease: $version"
Write-Host "NuGet feed:       $LocalNuGetFeed"

try {
    Write-Host 'Packing Storyboard.Foundation (Release)...'
    $packArguments = @(
        $projectFile,
        '--configuration', 'Release',
        '--output', $packageOutput,
        '--source', $LocalNuGetFeed,
        "-p:Version=$version",
        "-p:PackageVersion=$version"
    )
    & dotnet pack @packArguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet pack failed with exit code $LASTEXITCODE."
    }
    if (-not (Test-Path -LiteralPath $packageFile -PathType Leaf)) {
        throw "Expected package '$packageFile' was not produced."
    }

    Write-Host 'Copying package to the local NuGet feed...'
    Copy-Item -LiteralPath $packageFile -Destination (Join-Path $LocalNuGetFeed (Split-Path $packageFile -Leaf)) -Force

    Write-Host "Local prerelease package is available as $version." -ForegroundColor Green
    Write-Host "  $(Join-Path $LocalNuGetFeed (Split-Path $packageFile -Leaf))"
}
finally {
    if (-not $KeepPackage -and (Test-Path -LiteralPath $packageOutput -PathType Container)) {
        $resolvedOutput = (Resolve-Path -LiteralPath $packageOutput).Path
        $resolvedPackageRoot = [IO.Path]::GetFullPath($packageRoot)
        $packageRootPrefix = $resolvedPackageRoot + [IO.Path]::DirectorySeparatorChar
        if (-not $resolvedOutput.StartsWith($packageRootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove package output outside '$packageRoot'."
        }

        Remove-Item -LiteralPath $resolvedOutput -Recurse -Force
    }
}
