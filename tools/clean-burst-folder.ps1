param(
    [Parameter(Mandatory=$false)]
    [string]$ExePath = ".\aaa-build\MajdataView.exe",

    [Parameter(Mandatory=$false)]
    [string]$FolderName = "MajdataView_BurstDebugInformation_DoNotShip"
)

if (-not (Test-Path $ExePath)) {
    Write-Error "EXE not found at: $ExePath"
    exit 1
}

$exeFullPath = Resolve-Path $ExePath
$buildDir = Split-Path -Parent $exeFullPath
$burstDir = Join-Path $buildDir $FolderName

if (Test-Path $burstDir) {
    Write-Host "Removing Burst debug folder: $burstDir"
    Remove-Item -Path $burstDir -Recurse -Force
    if (Test-Path $burstDir) {
        Write-Warning "Failed to remove: $burstDir"
        exit 1
    }
    Write-Host "  Done."
} else {
    Write-Host "Burst debug folder not found, skipping: $burstDir"
}
