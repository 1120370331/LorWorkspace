param(
    [switch]$Force,
    [switch]$DeployOnly
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot "..\..\.."))
$unity = "C:\Program Files\Unity\Editor\Unity.exe"
$project = Join-Path $repoRoot "SteriaBuild\VFXSource\AnhierBlueWhiteSlash\UnityProject"
$log = Join-Path $scriptRoot "unity-build-christasha-dawn.log"
$bundleName = "steria_christasha_dawn_combat"
$output = Join-Path $project "AssetBundles"
$bundle = Join-Path $output $bundleName
$bundleAb = Join-Path $output "$bundleName.ab"
$stamp = Join-Path $scriptRoot "unity-build-christasha-dawn.inputs.sha256"
$projectModAb = Join-Path $repoRoot "SteriaBuild\SteriaModFolder\Assemblies\AB"
$gameModAbs = @(
    "C:\Program Files (x86)\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Mods\SteriaModFolder\Assemblies\AB",
    "D:\Game Center\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Mods\SteriaModFolder\Assemblies\AB"
)

function Get-InputFiles {
    $inputs = @(
        (Join-Path $project "Assets\Editor\ChristashaDawnCombatBundleBuilder.cs"),
        (Join-Path $repoRoot "SteriaBuild\DiceAttackEffect_Steria_ChristashaDawnCombat.cs")
    )

    $inputs += Get-ChildItem -LiteralPath (Join-Path $project "Assets\Textures\ChristashaProvidedVerticalCrescent\Cropped") -File -Filter "*.png" -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName }
    $inputs += Get-ChildItem -LiteralPath (Join-Path $project "Assets\Textures\ChristashaProvidedVerticalCrescent\Cropped") -File -Filter "*.meta" -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName }

    $inputs | Where-Object { Test-Path -LiteralPath $_ } | Sort-Object -Unique
}

function Get-InputFingerprint {
    $inputFiles = Get-InputFiles

    $hashText = foreach ($inputPath in $inputFiles) {
        $file = Get-Item -LiteralPath $inputPath
        $hash = (Get-FileHash -LiteralPath $inputPath -Algorithm SHA256).Hash
        "$hash|$($file.Length)|$([System.IO.Path]::GetFullPath($inputPath))"
    }

    $bytes = [System.Text.Encoding]::UTF8.GetBytes(($hashText -join "`n"))
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        -join ($sha.ComputeHash($bytes) | ForEach-Object { $_.ToString("x2") })
    }
    finally {
        $sha.Dispose()
    }
}

function Get-LatestInputWriteTimeUtc {
    $files = Get-InputFiles | ForEach-Object { Get-Item -LiteralPath $_ }
    if (!$files) {
        return [datetime]::MinValue
    }

    return ($files | Measure-Object -Property LastWriteTimeUtc -Maximum).Maximum
}

function Resolve-Bundle {
    if (Test-Path -LiteralPath $bundle) {
        return $bundle
    }
    if (Test-Path -LiteralPath $bundleAb) {
        return $bundleAb
    }
    return $null
}

function Copy-BundleToTargets([string]$sourceBundle) {
    New-Item -ItemType Directory -Force -Path $projectModAb | Out-Null
    Copy-Item -LiteralPath $sourceBundle -Destination (Join-Path $projectModAb $bundleName) -Force
    Copy-Item -LiteralPath $sourceBundle -Destination (Join-Path $projectModAb "$bundleName.ab") -Force

    foreach ($gameModAb in $gameModAbs) {
        New-Item -ItemType Directory -Force -Path $gameModAb | Out-Null
        Copy-Item -LiteralPath $sourceBundle -Destination (Join-Path $gameModAb $bundleName) -Force
        Copy-Item -LiteralPath $sourceBundle -Destination (Join-Path $gameModAb "$bundleName.ab") -Force
    }

    (@($projectModAb) + $gameModAbs) | ForEach-Object {
        Get-Item -LiteralPath (Join-Path $_ $bundleName), (Join-Path $_ "$bundleName.ab") -ErrorAction SilentlyContinue
    } | Select-Object FullName, Length, LastWriteTime
}

$currentFingerprint = Get-InputFingerprint
$previousFingerprint = if (Test-Path -LiteralPath $stamp) { Get-Content -LiteralPath $stamp -Raw } else { "" }
$existingBundle = Resolve-Bundle
$existingBundleItem = if ($existingBundle) { Get-Item -LiteralPath $existingBundle } else { $null }
$latestInputWriteTimeUtc = Get-LatestInputWriteTimeUtc

if ($DeployOnly) {
    if (!$existingBundle) {
        throw "DeployOnly requested, but no existing Christasha dawn bundle was found in $output"
    }
    Write-Host "DeployOnly: copying existing bundle without launching Unity."
    Copy-BundleToTargets $existingBundle
    return
}

if (!$Force -and $existingBundleItem -and [string]::IsNullOrWhiteSpace($previousFingerprint) -and $existingBundleItem.LastWriteTimeUtc -ge $latestInputWriteTimeUtc) {
    Set-Content -LiteralPath $stamp -Value $currentFingerprint -Encoding ASCII
    Write-Host "Existing bundle is newer than inputs: initializing fingerprint and skipping Unity build."
    Copy-BundleToTargets $existingBundle
    return
}

if (!$Force -and $existingBundle -and ($previousFingerprint.Trim() -eq $currentFingerprint.Trim())) {
    Write-Host "Inputs unchanged: skipping Unity build and copying existing bundle."
    Copy-BundleToTargets $existingBundle
    return
}

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
    "-executeMethod", "ChristashaDawnCombatBundleBuilder.BuildBundle",
    "-logFile", $log
)

$p = Start-Process -FilePath $unity -ArgumentList $args -Wait -PassThru -WindowStyle Hidden
if ($p.ExitCode -ne 0) {
    if (Test-Path -LiteralPath $log) {
        Get-Content -LiteralPath $log -Tail 180
    }
    throw "Christasha dawn AssetBundle build failed with exit code $($p.ExitCode)."
}

$builtBundle = Resolve-Bundle
if (!$builtBundle) {
    throw "Christasha dawn bundle was not generated in $output"
}

Set-Content -LiteralPath $stamp -Value $currentFingerprint -Encoding ASCII
Copy-BundleToTargets $builtBundle
