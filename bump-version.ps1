<#
.SYNOPSIS
    Bumps the Rx Writer version number in all three required places atomically.

.DESCRIPTION
    Updates:
      - OPDClinic.csproj  : <Version>, <AssemblyVersion>, <FileVersion>
      - installer.iss     : #define AppVersion

.PARAMETER Version
    The new version in Major.Minor.Patch format (e.g. 1.3.0).

.EXAMPLE
    .\bump-version.ps1 1.3.0
#>

param(
    [Parameter(Mandatory, Position = 0)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version
)

$ErrorActionPreference = 'Stop'

$csprojPath = Join-Path $PSScriptRoot "OPDClinic.csproj"
$issPath    = Join-Path $PSScriptRoot "installer.iss"

# ── Validate files exist ──────────────────────────────────────────────────────
foreach ($f in $csprojPath, $issPath) {
    if (-not (Test-Path $f)) {
        Write-Error "File not found: $f  (run this script from the project root)"
    }
}

# ── OPDClinic.csproj ─────────────────────────────────────────────────────────
$csproj = [System.IO.File]::ReadAllText($csprojPath, [System.Text.Encoding]::UTF8)
$csproj = $csproj -replace '<Version>[^<]+</Version>',               "<Version>$Version</Version>"
$csproj = $csproj -replace '<AssemblyVersion>[^<]+</AssemblyVersion>', "<AssemblyVersion>$Version.0</AssemblyVersion>"
$csproj = $csproj -replace '<FileVersion>[^<]+</FileVersion>',         "<FileVersion>$Version.0</FileVersion>"
[System.IO.File]::WriteAllText($csprojPath, $csproj, [System.Text.Encoding]::UTF8)

# ── installer.iss ─────────────────────────────────────────────────────────────
$iss = [System.IO.File]::ReadAllText($issPath, [System.Text.Encoding]::UTF8)
$iss = $iss -replace '#define AppVersion\s+"[^"]*"', "#define AppVersion `"$Version`""
[System.IO.File]::WriteAllText($issPath, $iss, [System.Text.Encoding]::UTF8)

# ── Summary ───────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "  Bumped to v$Version" -ForegroundColor Green
Write-Host ""
Write-Host "  OPDClinic.csproj  ->  <Version>$Version</Version>"
Write-Host "                        <AssemblyVersion>$Version.0</AssemblyVersion>"
Write-Host "                        <FileVersion>$Version.0</FileVersion>"
Write-Host "  installer.iss     ->  #define AppVersion `"$Version`""
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. git add OPDClinic.csproj installer.iss"
Write-Host "  2. git commit -m `"chore: bump version to $Version`""
Write-Host "  3. git push"
Write-Host "  4. dotnet publish -c Release -r win-x64 --self-contained true -o publish\OPDClinic"
Write-Host "  5. & `"`$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe`" installer.iss"
Write-Host "  6. gh release create v$Version installer_output\OPDClinic_Setup_v$Version.exe ``"
Write-Host "         --repo HamedRawand/OPD-App --title `"Rx Writer v$Version`" --notes `"...`""
Write-Host ""
