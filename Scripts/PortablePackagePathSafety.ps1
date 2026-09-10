Set-StrictMode -Version Latest

function Assert-SafePortablePackageOutput {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string] $RepositoryRoot,

        [Parameter(Mandatory = $true)]
        [string] $OutputDirectory,

        [string[]] $RequiredInputPaths = @()
    )

    $normalizedRepositoryRoot = [System.IO.Path]::GetFullPath(
        $RepositoryRoot).TrimEnd(
            [System.IO.Path]::DirectorySeparatorChar,
            [System.IO.Path]::AltDirectorySeparatorChar)
    $repositoryOutput = [System.IO.Path]::GetFullPath(
        (Join-Path $normalizedRepositoryRoot 'output')).TrimEnd(
            [System.IO.Path]::DirectorySeparatorChar,
            [System.IO.Path]::AltDirectorySeparatorChar)
    $expectedRepositoryOutput =
        $normalizedRepositoryRoot +
        [System.IO.Path]::DirectorySeparatorChar +
        'output'

    if (-not [string]::Equals(
            $repositoryOutput,
            $expectedRepositoryOutput,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'The repository output authority could not be resolved safely.'
    }

    $output = [System.IO.Path]::GetFullPath($OutputDirectory).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar)
    $repositoryOutputPrefix =
        $repositoryOutput + [System.IO.Path]::DirectorySeparatorChar
    if (-not $output.StartsWith(
            $repositoryOutputPrefix,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'Portable package output must be a child of the repository output directory.'
    }
    $relativeOutput = $output.Substring($repositoryOutputPrefix.Length)

    $components = @($repositoryOutput)
    $current = $repositoryOutput
    foreach ($component in $relativeOutput.Split(
            @(
                [System.IO.Path]::DirectorySeparatorChar,
                [System.IO.Path]::AltDirectorySeparatorChar),
            [System.StringSplitOptions]::RemoveEmptyEntries)) {
        $current = [System.IO.Path]::Combine($current, $component)
        $components += $current
    }

    foreach ($componentPath in $components) {
        if (-not (Test-Path -LiteralPath $componentPath)) {
            continue
        }

        $item = Get-Item -LiteralPath $componentPath -Force
        if (-not $item.PSIsContainer) {
            throw "Portable package output requires a directory, but found a file: $componentPath"
        }

        if (($item.Attributes -band
             [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Portable package output cannot use a redirected directory: $componentPath"
        }
    }

    $traversal = $output
    $terminatedAtAuthority = $false
    while ($null -ne $traversal) {
        if ([string]::Equals(
                $traversal,
                $repositoryOutput,
                [System.StringComparison]::OrdinalIgnoreCase)) {
            $terminatedAtAuthority = $true
            break
        }

        $parent = [System.IO.Directory]::GetParent($traversal)
        $traversal = if ($null -eq $parent) {
            $null
        }
        else {
            $parent.FullName.TrimEnd(
                [System.IO.Path]::DirectorySeparatorChar,
                [System.IO.Path]::AltDirectorySeparatorChar)
        }
    }

    if (-not $terminatedAtAuthority) {
        throw 'Portable package output validation did not terminate at the expected authority.'
    }

    $outputPrefix =
        $output + [System.IO.Path]::DirectorySeparatorChar
    foreach ($requiredInputPath in $RequiredInputPaths) {
        $requiredInput = [System.IO.Path]::GetFullPath(
            $requiredInputPath)
        if ([string]::Equals(
                $requiredInput,
                $output,
                [System.StringComparison]::OrdinalIgnoreCase) -or
            $requiredInput.StartsWith(
                $outputPrefix,
                [System.StringComparison]::OrdinalIgnoreCase)) {
            throw 'Package inputs cannot be stored inside the fresh output directory.'
        }
    }

    return $output
}
