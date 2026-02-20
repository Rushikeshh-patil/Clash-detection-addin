param(
    [string]$AssemblyPath,
    [string]$RevitVersion = "2025",
    [string]$ManifestName = "ClashDetector.addin",
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-DefaultAssemblyPath {
    param(
        [string]$ScriptDirectory
    )

    $candidates = @(
        (Join-Path $ScriptDirectory "..\bin\Debug\net8.0-windows\Clashdetector.dll"),
        (Join-Path $ScriptDirectory "..\bin\Release\net8.0-windows\Clashdetector.dll")
    )

    foreach ($candidate in $candidates) {
        $resolved = [System.IO.Path]::GetFullPath($candidate)
        if (Test-Path $resolved) {
            return $resolved
        }
    }

    return $null
}

function Ensure-FileExists {
    param(
        [string]$PathValue,
        [string]$Description
    )

    if (-not (Test-Path $PathValue)) {
        throw "$Description was not found: $PathValue"
    }
}

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$templatePath = Join-Path $scriptDirectory "ClashDetector.addin.template"

Ensure-FileExists -PathValue $templatePath -Description "Template file"

if ([string]::IsNullOrWhiteSpace($AssemblyPath)) {
    $AssemblyPath = Resolve-DefaultAssemblyPath -ScriptDirectory $scriptDirectory
}

if ([string]::IsNullOrWhiteSpace($AssemblyPath)) {
    throw "Assembly path was not provided and no default build output was found. Build first, or pass -AssemblyPath."
}

$AssemblyPath = [System.IO.Path]::GetFullPath($AssemblyPath)
Ensure-FileExists -PathValue $AssemblyPath -Description "Assembly"

$targetDirectory = Join-Path $env:APPDATA "Autodesk\Revit\Addins\$RevitVersion"
if (-not (Test-Path $targetDirectory)) {
    New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
}

$targetManifestPath = Join-Path $targetDirectory $ManifestName

if ((Test-Path $targetManifestPath) -and -not $Force) {
    $backupPath = "$targetManifestPath.bak.$(Get-Date -Format 'yyyyMMddHHmmss')"
    Copy-Item -Path $targetManifestPath -Destination $backupPath -Force
    Write-Host "Existing manifest backed up to: $backupPath"
}

$templateContent = Get-Content -Path $templatePath -Raw
$escapedAssemblyPath = [System.Security.SecurityElement]::Escape($AssemblyPath)
$manifestContent = $templateContent -replace [regex]::Escape("C:\Path\To\Clashdetector.dll"), $escapedAssemblyPath

Set-Content -Path $targetManifestPath -Value $manifestContent -Encoding UTF8

Write-Host "Manifest installed:"
Write-Host "  $targetManifestPath"
Write-Host ""
Write-Host "Assembly target:"
Write-Host "  $AssemblyPath"
Write-Host ""
Write-Host "Launch Revit $RevitVersion and open the 'BirdTools' tab."
