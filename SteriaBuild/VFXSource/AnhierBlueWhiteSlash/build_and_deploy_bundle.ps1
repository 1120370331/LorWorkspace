$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$unity = "C:\Program Files\Unity\Editor\Unity.exe"
$project = Join-Path $scriptRoot "UnityProject"
$log = Join-Path $scriptRoot "unity-build.log"
$bundleName = "steria_anhier_bluewhite_slash"
$output = Join-Path $project "AssetBundles"
$projectModAb = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot "..\..\SteriaModFolder\Assemblies\AB"))
$gameModAbs = @(
    "C:\Program Files (x86)\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Mods\SteriaModFolder\Assemblies\AB",
    "D:\Game Center\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Mods\SteriaModFolder\Assemblies\AB"
)

if (!(Test-Path -LiteralPath $unity)) {
    throw "Unity 2019.3.15f1 not found: $unity"
}

if (Test-Path -LiteralPath $log) {
    try { Remove-Item -LiteralPath $log -Force } catch { }
}

$args = @(
    "-batchmode",
    "-quit",
    "-nographics",
    "-projectPath", $project,
    "-executeMethod", "AnhierBlueWhiteSlashBundleBuilder.BuildBundle",
    "-logFile", $log
)

$p = Start-Process -FilePath $unity -ArgumentList $args -Wait -PassThru -WindowStyle Hidden
if ($p.ExitCode -ne 0) {
    if (Test-Path -LiteralPath $log) {
        Get-Content -LiteralPath $log -Tail 120
    }
    throw "Unity AssetBundle build failed with exit code $($p.ExitCode). Check Unity activation/license first."
}

$bundle = Join-Path $output $bundleName
if (!(Test-Path -LiteralPath $bundle)) {
    $bundle = Join-Path $output "$bundleName.ab"
}
if (!(Test-Path -LiteralPath $bundle)) {
    throw "Bundle was not generated in $output"
}

New-Item -ItemType Directory -Force -Path $projectModAb | Out-Null
Copy-Item -LiteralPath $bundle -Destination (Join-Path $projectModAb $bundleName) -Force
Copy-Item -LiteralPath $bundle -Destination (Join-Path $projectModAb "$bundleName.ab") -Force

foreach ($gameModAb in $gameModAbs) {
    New-Item -ItemType Directory -Force -Path $gameModAb | Out-Null
    Copy-Item -LiteralPath $bundle -Destination (Join-Path $gameModAb $bundleName) -Force
    Copy-Item -LiteralPath $bundle -Destination (Join-Path $gameModAb "$bundleName.ab") -Force
}

$gameModAbs | ForEach-Object {
    Get-Item -LiteralPath (Join-Path $_ $bundleName), (Join-Path $_ "$bundleName.ab") -ErrorAction SilentlyContinue
} | Select-Object FullName, Length, LastWriteTime
