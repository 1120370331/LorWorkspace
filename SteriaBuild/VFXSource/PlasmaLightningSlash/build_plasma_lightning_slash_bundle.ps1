$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot "..\AnhierBlueWhiteSlash\UnityProject"))
$log = Join-Path $scriptRoot "unity-build-plasma-lightning-slash.log"
$output = Join-Path $project "AssetBundles"
$bundleName = "steria_plasma_lightning_slash"
$unityCandidates = @(
    "C:\Program Files\Unity\Editor\Unity.exe",
    "C:\Program Files\Unity\Hub\Editor\2019.3.15f1\Editor\Unity.exe"
)
$unity = $unityCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1

if ([string]::IsNullOrWhiteSpace($unity)) {
    throw "A compatible Unity editor was not found."
}

if (Test-Path -LiteralPath $log) {
    Remove-Item -LiteralPath $log -Force
}

$arguments = @(
    "-batchmode",
    "-quit",
    "-projectPath", $project,
    "-executeMethod", "PlasmaLightningSlashBundleBuilder.BuildBundle",
    "-logFile", $log
)

$process = Start-Process -FilePath $unity -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden
if ($process.ExitCode -ne 0) {
    if (Test-Path -LiteralPath $log) {
        Get-Content -LiteralPath $log -Tail 160
    }
    throw "Unity plasma slash build failed with exit code $($process.ExitCode)."
}

$bundle = Join-Path $output $bundleName
if (!(Test-Path -LiteralPath $bundle)) {
    $bundle = Join-Path $output "$bundleName.ab"
}
if (!(Test-Path -LiteralPath $bundle)) {
    throw "Plasma slash bundle was not generated in $output"
}

Get-Item -LiteralPath $bundle | Select-Object FullName, Length, LastWriteTime
Write-Host "Unity log: $log"
