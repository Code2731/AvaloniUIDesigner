[CmdletBinding()]
param(
    [switch]$NoRestore
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot

function Invoke-DotNetStep {
    param([string]$Label, [string[]]$Arguments)

    Write-Host "`n=== $Label ==="
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Label failed (exit code $LASTEXITCODE)."
    }
}

Push-Location $repositoryRoot
try {
    $common = @('-c', 'Release', '-p:UsedAvaloniaProducts=')
    if ($NoRestore) {
        $common += '--no-restore'
    }

    Invoke-DotNetStep -Label 'Release build' -Arguments (@('build', 'AvaloniaUIDesigner.slnx') + $common)
    Invoke-DotNetStep -Label 'Layout regression checks' -Arguments (@('run', '--project', 'tests/LayoutValidation') + $common)
    Invoke-DotNetStep -Label 'File persistence regression checks' -Arguments (@('run', '--project', 'tests/FilePersistence') + $common)
    Invoke-DotNetStep -Label 'Preview regression checks' -Arguments (@('run', '--project', 'tests/PreviewBehavior') + $common)
    Write-Host "`nAll verification steps passed."
}
finally {
    Pop-Location
}
