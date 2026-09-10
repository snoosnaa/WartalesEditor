[CmdletBinding()]
param(
    [string] $OutputDirectory,
    [string] $ReadmePdfPath,
    [string] $UserGuidePdfPath,
    [switch] $CreateValidationZip
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'PortablePackagePathSafety.ps1')

$repositoryRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot '..'))
$repositoryOutput = [System.IO.Path]::GetFullPath(
    (Join-Path $repositoryRoot 'output'))

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path (
        Join-Path $repositoryOutput 'implementation') (
        'portable-package')
}
if ([string]::IsNullOrWhiteSpace($ReadmePdfPath)) {
    $ReadmePdfPath = Join-Path $repositoryOutput 'pdf\README.pdf'
}
if ([string]::IsNullOrWhiteSpace($UserGuidePdfPath)) {
    $UserGuidePdfPath = Join-Path $repositoryOutput 'pdf\USER-GUIDE.pdf'
}

$readmePdf = [System.IO.Path]::GetFullPath($ReadmePdfPath)
$userGuidePdf = [System.IO.Path]::GetFullPath($UserGuidePdfPath)
$requiredInputs = @(
    $readmePdf,
    $userGuidePdf,
    (Join-Path $repositoryRoot 'LICENSE'),
    (Join-Path $repositoryRoot 'THIRD-PARTY-NOTICES.txt'),
    (Join-Path $repositoryRoot 'Docs\CHANGELOG.md')
)
foreach ($requiredInput in $requiredInputs) {
    if (-not (Test-Path -LiteralPath $requiredInput -PathType Leaf)) {
        throw "Required package input was not found: $requiredInput"
    }
}

$output = Assert-SafePortablePackageOutput `
    -RepositoryRoot $repositoryRoot `
    -OutputDirectory $OutputDirectory `
    -RequiredInputPaths $requiredInputs

if (Test-Path -LiteralPath $output) {
    Remove-Item -LiteralPath $output -Recurse -Force
}

$mainPublish = Join-Path $output 'publish-main'
$launcherPublish = Join-Path $output 'publish-launcher'
$staging = Join-Path $output 'staging'
$applicationStaging = Join-Path $staging 'App'

New-Item -ItemType Directory -Path $mainPublish | Out-Null
New-Item -ItemType Directory -Path $launcherPublish | Out-Null
New-Item -ItemType Directory -Path $applicationStaging | Out-Null

& dotnet publish (Join-Path $repositoryRoot 'WartalesEditor.csproj') `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:PublishTrimmed=false `
    -p:PublishReadyToRun=false `
    -o $mainPublish
if ($LASTEXITCODE -ne 0) {
    throw 'The Wartales Editor application publish failed.'
}

& dotnet publish (
    Join-Path $repositoryRoot 'Launcher\WartalesEditor.Launcher.csproj') `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishTrimmed=true `
    -p:PublishReadyToRun=false `
    -o $launcherPublish
if ($LASTEXITCODE -ne 0) {
    throw 'The Wartales Editor launcher publish failed.'
}

Get-ChildItem -LiteralPath $mainPublish -Force |
    Copy-Item -Destination $applicationStaging -Recurse -Force
Get-ChildItem -LiteralPath $applicationStaging -Filter *.pdb -File -Recurse |
    Remove-Item -Force

$launcherExecutable =
    Join-Path $launcherPublish 'WartalesEditor.Launcher.exe'
if (-not (Test-Path -LiteralPath $launcherExecutable -PathType Leaf)) {
    throw "Published launcher executable was not found: $launcherExecutable"
}
Copy-Item -LiteralPath $launcherExecutable `
    -Destination (Join-Path $staging 'WartalesEditor.exe')

Copy-Item -LiteralPath $readmePdf `
    -Destination (Join-Path $staging 'README.pdf')
Copy-Item -LiteralPath $userGuidePdf `
    -Destination (Join-Path $staging 'USER-GUIDE.pdf')
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'LICENSE') `
    -Destination (Join-Path $staging 'LICENSE')
Copy-Item -LiteralPath (
    Join-Path $repositoryRoot 'THIRD-PARTY-NOTICES.txt') `
    -Destination (Join-Path $staging 'THIRD-PARTY-NOTICES.txt')
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'Docs\CHANGELOG.md') `
    -Destination (Join-Path $staging 'CHANGELOG.md')

$validation = & (
    Join-Path $PSScriptRoot 'Test-PortablePackageLayout.ps1') `
    -StagingDirectory $staging `
    -SourcePublishDirectory $mainPublish `
    -SourceLauncherExecutable $launcherExecutable

$zipPath = $null
if ($CreateValidationZip) {
    $zipPath = Join-Path $output 'WartalesEditor-local-validation.zip'
    Compress-Archive -Path (Join-Path $staging '*') `
        -DestinationPath $zipPath `
        -CompressionLevel Optimal
}

$validation
[pscustomobject]@{
    MainPublishDirectory = $mainPublish
    LauncherPublishDirectory = $launcherPublish
    StagingDirectory = $staging
    ValidationZip = $zipPath
}
