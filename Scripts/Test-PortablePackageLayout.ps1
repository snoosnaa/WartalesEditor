[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $StagingDirectory,

    [string] $SourcePublishDirectory,

    [string] $SourceLauncherExecutable
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-Sha256FileHash {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    $stream = [System.IO.File]::OpenRead($Path)
    $algorithm = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = $algorithm.ComputeHash($stream)
        return [System.BitConverter]::ToString($bytes).Replace('-', '')
    }
    finally {
        $algorithm.Dispose()
        $stream.Dispose()
    }
}

$staging = [System.IO.Path]::GetFullPath($StagingDirectory)
if (-not (Test-Path -LiteralPath $staging -PathType Container)) {
    throw "Package staging directory was not found: $staging"
}

$expectedRootFiles = @(
    'CHANGELOG.md',
    'LICENSE',
    'README.pdf',
    'THIRD-PARTY-NOTICES.txt',
    'USER-GUIDE.pdf',
    'WartalesEditor.exe'
) | Sort-Object

$actualRootFiles = @(
    Get-ChildItem -LiteralPath $staging -File |
        Select-Object -ExpandProperty Name
) | Sort-Object

$rootFileDifference = @(
    Compare-Object $expectedRootFiles $actualRootFiles
)
if ($rootFileDifference.Count -ne 0) {
    $details = $rootFileDifference | Out-String
    throw "Package root files do not match the approved layout.`n$details"
}

$actualRootDirectories = @(
    Get-ChildItem -LiteralPath $staging -Directory |
        Select-Object -ExpandProperty Name
)
if ($actualRootDirectories.Count -ne 1 -or
    $actualRootDirectories[0] -cne 'App') {
    throw 'The package root must contain exactly one directory named App.'
}

$appDirectory = Join-Path $staging 'App'
$requiredApplicationFiles = @(
    'WartalesEditor.exe',
    'WartalesEditor.dll',
    'WartalesEditor.deps.json',
    'WartalesEditor.runtimeconfig.json'
)

foreach ($requiredFile in $requiredApplicationFiles) {
    $requiredPath = Join-Path $appDirectory $requiredFile
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "Required application payload file is missing: $requiredPath"
    }
}

$pdbFiles = @(
    Get-ChildItem -LiteralPath $staging -Filter *.pdb -File -Recurse
)
if ($pdbFiles.Count -ne 0) {
    throw 'Public package staging must not contain PDB files.'
}

$rootExecutables = @(
    Get-ChildItem -LiteralPath $staging -Filter *.exe -File
)
if ($rootExecutables.Count -ne 1 -or
    $rootExecutables[0].Name -cne 'WartalesEditor.exe') {
    throw 'The package root must contain only the WartalesEditor.exe launcher.'
}

$rootRuntimeFiles = @(
    Get-ChildItem -LiteralPath $staging -File |
        Where-Object {
            $_.Extension -in @('.dll', '.json')
        }
)
if ($rootRuntimeFiles.Count -ne 0) {
    throw 'Application runtime DLL or JSON files were found at package root.'
}

if (-not [string]::IsNullOrWhiteSpace($SourcePublishDirectory)) {
    $source = [System.IO.Path]::GetFullPath($SourcePublishDirectory)
    if (-not (Test-Path -LiteralPath $source -PathType Container)) {
        throw "Source publish directory was not found: $source"
    }

    $sourceFiles = @(
        Get-ChildItem -LiteralPath $source -File -Recurse |
            Where-Object { $_.Extension -ine '.pdb' }
    )

    $applicationFiles = @(
        Get-ChildItem -LiteralPath $appDirectory -File -Recurse
    )

    $sourceMap = [System.Collections.Generic.Dictionary[string, string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)
    foreach ($sourceFile in $sourceFiles) {
        $relativePath = $sourceFile.FullName.Substring(
            $source.TrimEnd(
                [System.IO.Path]::DirectorySeparatorChar,
                [System.IO.Path]::AltDirectorySeparatorChar).Length + 1)
        if ($sourceMap.ContainsKey($relativePath)) {
            throw "The source publish contains an ambiguous relative path: $relativePath"
        }
        $sourceMap.Add($relativePath, $sourceFile.FullName)
    }

    $applicationMap = [System.Collections.Generic.Dictionary[string, string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)
    foreach ($applicationFile in $applicationFiles) {
        $relativePath = $applicationFile.FullName.Substring(
            $appDirectory.TrimEnd(
                [System.IO.Path]::DirectorySeparatorChar,
                [System.IO.Path]::AltDirectorySeparatorChar).Length + 1)
        if ($applicationMap.ContainsKey($relativePath)) {
            throw "The staged App payload contains an ambiguous relative path: $relativePath"
        }
        $applicationMap.Add(
            $relativePath,
            $applicationFile.FullName)
    }

    $payloadDifference = @(
        Compare-Object `
            @($sourceMap.Keys | Sort-Object) `
            @($applicationMap.Keys | Sort-Object)
    )
    if ($payloadDifference.Count -ne 0) {
        $details = $payloadDifference | Out-String
        throw "The App payload does not exactly match the publish output after the approved PDB exclusion.`n$details"
    }

    foreach ($relativePath in $sourceMap.Keys) {
        $sourceHash = Get-Sha256FileHash `
            -Path $sourceMap[$relativePath]
        $applicationHash = Get-Sha256FileHash `
            -Path $applicationMap[$relativePath]
        if (-not [string]::Equals(
                $sourceHash,
                $applicationHash,
                [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "The App payload file content does not match the publish output: $relativePath"
        }
    }
}

if (-not [string]::IsNullOrWhiteSpace($SourceLauncherExecutable)) {
    $sourceLauncher = [System.IO.Path]::GetFullPath(
        $SourceLauncherExecutable)
    if (-not (Test-Path -LiteralPath $sourceLauncher -PathType Leaf)) {
        throw "Source launcher executable was not found: $sourceLauncher"
    }

    $stagedLauncher = Join-Path $staging 'WartalesEditor.exe'
    $sourceLauncherHash = Get-Sha256FileHash `
        -Path $sourceLauncher
    $stagedLauncherHash = Get-Sha256FileHash `
        -Path $stagedLauncher
    if (-not [string]::Equals(
            $sourceLauncherHash,
            $stagedLauncherHash,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'The staged root launcher content does not match the launcher publish output.'
    }
}

[pscustomobject]@{
    StagingDirectory = $staging
    RootFileCount = $actualRootFiles.Count
    ApplicationFileCount = @(
        Get-ChildItem -LiteralPath $appDirectory -File -Recurse
    ).Count
    PdbFileCount = $pdbFiles.Count
    IsValid = $true
}
