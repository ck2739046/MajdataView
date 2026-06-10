# rcedit v2.0.0 — edit EXE/DLL VERSIONINFO resources on Windows
# Download: https://github.com/electron/rcedit/releases/tag/v2.0.0
# License: MIT (electron/rcedit)
param(
    [Parameter(Mandatory=$false)]
    [string]$ExePath = ".\aaa-build\MajdataView.exe",

    [Parameter(Mandatory=$false)]
    [string]$Version = "4.3.0"
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rcedit = Join-Path $scriptDir "rcedit.exe"

if (-not (Test-Path $rcedit)) {
    Write-Error "rcedit.exe not found at: $rcedit"
    Write-Error "Download from: https://github.com/electron/rcedit/releases"
    exit 1
}

if (-not (Test-Path $ExePath)) {
    Write-Error "EXE not found at: $ExePath"
    exit 1
}

$exeFullPath = Resolve-Path $ExePath

Write-Host "=== Patching MajdataView.exe version info ==="
Write-Host "Target: $exeFullPath"
Write-Host ""

$buildDate = (Get-Date).ToString("yyyy.MM.dd_HH:mm:ss_UTCzzz")
$copyright = "$([char]0x00A9) bbben & Simon273 $buildDate"

$props = @{
    "LegalCopyright"  = $copyright
    "FileDescription" = "https://github.com/ck2739046/MajdataView/tree/431-NC-TH"
    "ProductName"     = "MajdataView"
}

foreach ($key in $props.Keys) {
    $value = $props[$key]
    Write-Host "  Setting $key = $value"
    & $rcedit $exeFullPath --set-version-string $key $value
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "  Failed to set $key (exit code: $LASTEXITCODE)"
    }
}

# Only set FileVersion (ProductVersion retained from Unity build)
Write-Host "  Setting FileVersion = $Version"
& $rcedit $exeFullPath --set-file-version $Version

Write-Host ""
Write-Host "=== Done! ==="
Write-Host ""
Write-Host "Verify with:"
Write-Host '  $file = Get-Item "' $exeFullPath '"; $file.VersionInfo | Format-List *'
