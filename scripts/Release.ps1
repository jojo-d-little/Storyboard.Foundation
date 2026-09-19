[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+$')]
    [string]$Version
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Set-Location $repositoryRoot

function Invoke-Step {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Command,
        [Parameter(Mandatory = $true)]
        [scriptblock]$Action
    )

    Write-Host "`n==> $Command" -ForegroundColor Cyan
    & $Action
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $Command"
    }
}

function Get-GitOutput {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    $output = & git @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Git command failed: git $($Arguments -join ' ')"
    }

    return ($output -join "`n").Trim()
}

$branch = Get-GitOutput @('branch', '--show-current')
if ($branch -ne 'main') {
    throw "Release must start from the main branch. Current branch: '$branch'."
}

$status = Get-GitOutput @('status', '--porcelain')
if ($status) {
    throw "Working tree is not clean. Commit or stash these changes before releasing:`n$status"
}

$remoteTag = & git ls-remote --exit-code --tags origin "refs/tags/$Version" 2>$null
$remoteExitCode = $LASTEXITCODE
if ($remoteExitCode -ne 0 -and $remoteExitCode -ne 2) {
    throw "Could not verify whether release tag '$Version' exists on origin."
}
if ($remoteExitCode -eq 0 -or (git tag --list $Version)) {
    throw "Release tag '$Version' already exists locally or on origin. Use a new version."
}

$projectFile = Join-Path $repositoryRoot 'src/Storyboard.Foundation/Storyboard.Foundation.csproj'
$projectText = [System.IO.File]::ReadAllText($projectFile)
$versionMatches = [regex]::Matches($projectText, '<Version>[^<]+</Version>')
if ($versionMatches.Count -ne 1) {
    throw "Expected exactly one <Version> element in $projectFile."
}

$currentVersion = $versionMatches[0].Value -replace '^<Version>|</Version>$', ''
if (-not $Version) {
    if ($currentVersion -notmatch '^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)$') {
        throw "Cannot suggest a patch bump for current version '$currentVersion'."
    }
    $Version = "$($matches.major).$($matches.minor).$([int]$matches.patch + 1)"
    $confirmation = Read-Host "Current version is $currentVersion. Use suggested version ${Version}? [Y/n]"
    if ($confirmation -and $confirmation -notmatch '^(?i:y|yes)$') { throw 'Release cancelled.' }
}
if ($currentVersion -ne $Version) {
    $updatedProjectText = $projectText -replace '<Version>[^<]+</Version>', "<Version>$Version</Version>"
    [System.IO.File]::WriteAllText($projectFile, $updatedProjectText)
}
else {
    Write-Host "Project version is already $Version; no version commit needed." -ForegroundColor Yellow
}

Invoke-Step "restore" {
    dotnet restore tests/Storyboard.Foundation.Tests/Storyboard.Foundation.Tests.csproj --configfile NuGet.Config
}
Invoke-Step "build library" {
    dotnet build src/Storyboard.Foundation/Storyboard.Foundation.csproj --configuration Release --no-restore
}
Invoke-Step "build tests" {
    dotnet build tests/Storyboard.Foundation.Tests/Storyboard.Foundation.Tests.csproj --configuration Release --no-restore
}
Invoke-Step "run tests" {
    dotnet run --project tests/Storyboard.Foundation.Tests/Storyboard.Foundation.Tests.csproj --configuration Release --no-build
}
if ($currentVersion -ne $Version) {
    Invoke-Step "git commit version $Version" {
        git add -- $projectFile
        git commit -m "Prepare $Version release"
    }
}
Invoke-Step "create annotated tag $Version" {
    git tag -a $Version -m "Release $Version"
}
Invoke-Step "push main and annotated tags" {
    git push --atomic --follow-tags origin main
}

Write-Host "`nRelease $Version pushed successfully. GitHub Actions will now publish the package and create the GitHub Release." -ForegroundColor Green
