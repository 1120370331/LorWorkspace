$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$unity = "C:\Program Files\Unity\Editor\Unity.exe"
$project = Join-Path $scriptRoot "UnityProject"
$log = Join-Path $scriptRoot "unity-build-sivier-wish-guard.log"
$bundleName = "steria_sivier_wish_guard"
$output = Join-Path $project "AssetBundles"
$projectModAb = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot "..\..\SteriaModFolder\Assemblies\AB"))
$gameModAbs = @(
    "C:\Program Files (x86)\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Mods\SteriaModFolder\Assemblies\AB",
    "D:\Game Center\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Mods\SteriaModFolder\Assemblies\AB"
)

if (!(Test-Path -LiteralPath $unity)) {
    throw "Unity not found: $unity"
}

if (Test-Path -LiteralPath $log) {
    try { Remove-Item -LiteralPath $log -Force } catch { }
}

$args = @(
    "-batchmode",
    "-quit",
    "-nographics",
    "-projectPath", $project,
    "-executeMethod", "SivierWishGuardBundleBuilder.BuildBundle",
    "-logFile", $log
)

$p = Start-Process -FilePath $unity -ArgumentList $args -Wait -PassThru -WindowStyle Hidden
if ($p.ExitCode -ne 0) {
    if (Test-Path -LiteralPath $log) {
        Get-Content -LiteralPath $log -Tail 180
    }
    throw "Sivier wish guard AssetBundle build failed with exit code $($p.ExitCode)."
}

$bundle = Join-Path $output $bundleName
if (!(Test-Path -LiteralPath $bundle)) {
    $bundle = Join-Path $output "$bundleName.ab"
}
if (!(Test-Path -LiteralPath $bundle)) {
    throw "Sivier wish guard bundle was not generated in $output"
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
