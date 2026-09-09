$ErrorActionPreference = "Stop"

$root = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
$unityProject = Join-Path $PSScriptRoot "UnityProject"
$builder = Join-Path $unityProject "Assets\Editor\AnhierBlueWhiteSlashRefinedBundleBuilder.cs"
$driver = Join-Path $unityProject "Assets\Scripts\AnhierBlueWhiteSlashRefinedDriver.cs"
$prefab = Join-Path $unityProject "Assets\Prefabs\AnhierBlueWhiteSlashRefinedPrefab.prefab"
$shader = Join-Path $unityProject "Assets\Shaders\AnhierBlueWhiteSlashRefinedFlow.shader"
$bundle = Join-Path $unityProject "AssetBundles\steria_anhier_bluewhite_slash_refined"
$previewDir = Join-Path $root "preview_exports\anhier_bluewhite_slash_refined"

$errors = New-Object System.Collections.Generic.List[string]

function Require-File($Path, $Label) {
    if (-not (Test-Path -LiteralPath $Path)) {
        $script:errors.Add("Missing $Label`: $Path")
    }
}

Require-File $builder "builder"
Require-File $driver "driver"
Require-File $prefab "prefab"
Require-File $shader "shader"
Require-File $bundle "bundle"

if (Test-Path -LiteralPath $builder) {
    $text = Get-Content -LiteralPath $builder -Raw
    foreach ($needle in @(
        "AnhierBlueWhiteSlashRefinedPrefab",
        "CreateRefinedSlashMesh",
        "AnhierBlueWhiteSlashRefinedFlow",
        "RenderPreview",
        "01_early_reveal.png",
        "04_fade_retreat.png")) {
        if ($text -notlike "*$needle*") {
            $errors.Add("Builder missing marker: $needle")
        }
    }
    if ($text -like "*OceanBlueWhiteSlashPrefab*") {
        $errors.Add("Builder references forbidden OceanBlueWhiteSlashPrefab")
    }
}

if (Test-Path -LiteralPath $shader) {
    $shaderText = Get-Content -LiteralPath $shader -Raw
    foreach ($needle in @("_Reveal", "_Retreat", "_Length", "_Alpha", "_Intensity", "_BrushStrength", "_NoiseStrength", "Blend SrcAlpha One")) {
        if ($shaderText -notlike "*$needle*") {
            $errors.Add("Shader missing marker: $needle")
        }
    }
}

foreach ($file in @("01_early_reveal.png", "02_contact_peak.png", "03_full_arc.png", "04_fade_retreat.png")) {
    $path = Join-Path $previewDir $file
    if (-not (Test-Path -LiteralPath $path)) {
        $errors.Add("Missing preview frame: $path")
    } elseif ((Get-Item -LiteralPath $path).Length -lt 4096) {
        $errors.Add("Preview frame is too small: $path")
    }
}

if ($errors.Count -gt 0) {
    Write-Host "FAIL refined slash verification"
    foreach ($errorItem in $errors) {
        Write-Host " - $errorItem"
    }
    exit 1
}

Write-Host "PASS refined slash verification"
Write-Host "Prefab: $prefab"
Write-Host "Bundle: $bundle"
Write-Host "Preview: $previewDir"
