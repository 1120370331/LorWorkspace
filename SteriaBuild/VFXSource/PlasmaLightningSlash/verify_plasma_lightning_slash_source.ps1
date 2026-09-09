$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot "..\..\.."))
$unityProject = Join-Path $repoRoot "SteriaBuild\VFXSource\AnhierBlueWhiteSlash\UnityProject"
$builderPath = Join-Path $unityProject "Assets\Editor\PlasmaLightningSlashBundleBuilder.cs"
$driverPath = Join-Path $unityProject "Assets\Scripts\PlasmaLightningSlashDriver.cs"
$shaderPath = Join-Path $unityProject "Assets\Shaders\PlasmaLightningSlashFlow.shader"
$prefabPath = Join-Path $unityProject "Assets\Prefabs\PlasmaLightningSlashPrefab.prefab"
$bundlePath = Join-Path $unityProject "AssetBundles\steria_plasma_lightning_slash"
$buildScriptPath = Join-Path $scriptRoot "build_plasma_lightning_slash_bundle.ps1"
$imageVerifierPath = Join-Path $scriptRoot "verify_plasma_lightning_slash_images.py"
$previewDir = Join-Path $repoRoot "preview_exports\plasma_lightning_slash"
$round1PreviewDir = Join-Path $repoRoot "preview_exports\plasma_lightning_slash_round1"
$nativeEvidencePath = Join-Path $previewDir "native_particle_evidence.txt"
$failures = New-Object System.Collections.Generic.List[string]

function Read-RequiredFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if (!(Test-Path -LiteralPath $Path)) {
        $script:failures.Add("Missing $Label`: $Path")
        return ""
    }

    return Get-Content -LiteralPath $Path -Raw
}

function Assert-Contains {
    param(
        [AllowEmptyString()][Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Pattern,
        [Parameter(Mandatory = $true)][string]$Message
    )

    if ($Source -notmatch $Pattern) {
        $script:failures.Add($Message)
    }
}

$builder = Read-RequiredFile $builderPath "Plasma lightning slash builder"
$driver = Read-RequiredFile $driverPath "Plasma lightning slash driver"
$shader = Read-RequiredFile $shaderPath "Plasma lightning slash shader"
$buildScript = Read-RequiredFile $buildScriptPath "non-deploying build script"

Assert-Contains $builder 'steria_plasma_lightning_slash' "Builder must use the independent plasma lightning slash bundle name."
Assert-Contains $builder 'PlasmaLightningSlashPrefab' "Builder must generate the independent plasma slash prefab."
Assert-Contains $builder 'AB_BladeRoot' "Prefab must expose AB_BladeRoot."
Assert-Contains $builder 'AB_ImpactRoot' "Prefab must expose AB_ImpactRoot."
Assert-Contains $builder 'CreatePlasmaRibbonMesh\(' "Builder must create a target-specific plasma ribbon mesh."
Assert-Contains $builder 'const int pathSegments = 68;' "Main ribbon must use 68 path segments within the 64-72 contract."
Assert-Contains $builder 'CreateProceduralMaskTexture\(' "Builder must generate the procedural RGBA plasma mask."
Assert-Contains $builder 'CreateBranchArcMesh\(' "Builder must create mesh support for short branch arcs."
Assert-Contains $builder 'ParticleSystemRenderMode\.Mesh' "Branch support must use an assigned mesh renderer."
Assert-Contains $builder 'PlasmaLightningSlash_BranchArc_01\.asset' "Builder must generate the first authored branch mesh."
Assert-Contains $builder 'PlasmaLightningSlash_BranchArc_04\.asset' "Builder must generate the fourth authored branch mesh."
Assert-Contains $builder 'new ParticleSystem\.Burst\(0f, 1\)' "Each authored branch/contact system must emit one particle."
Assert-Contains $builder 'new ParticleSystem\.Burst\(0f, 16\)' "Micro-spark support must emit sixteen stretched sparks."
Assert-Contains $builder 'PrefabUtility\.SaveAsPrefabAsset' "Builder must save a Unity prefab."
Assert-Contains $builder 'BuildPipeline\.BuildAssetBundles' "Builder must build an AssetBundle."
Assert-Contains $builder 'AssetBundle\.LoadFromFile' "Builder must load the built AssetBundle for verification."
Assert-Contains $builder 'RenderPreview' "Builder must generate deterministic previews."
Assert-Contains $builder 'black_01_early_reveal\.png' "Builder must capture the black early-reveal frame."
Assert-Contains $builder 'stage_04_fade_retreat\.png' "Builder must capture the stage fade/retreat frame."
Assert-Contains $builder 'useAutoRandomSeed = false' "Preview support particles must use deterministic seeds."
Assert-Contains $builder 'particleRenderer\.enabled = true' "Preview capture must keep native particle renderers enabled."
Assert-Contains $builder 'native_particle_evidence\.txt' "Preview capture must record native particle evidence."
Assert-Contains $builder 'ImportAsset\(ShaderPath, ImportAssetOptions\.ForceUpdate\)' "Checked-in shader must be imported as the source of truth."

if ($builder -match 'particleRenderer\.enabled\s*=\s*false|CreatePreviewSupportProxies\s*\(') {
    $failures.Add("Preview capture must not disable or hand-reconstruct native ParticleSystem rendering.")
}
if ($builder -match 'u\s*\*\s*61(?:\.0)?f?') {
    $failures.Add("Procedural abrasion must not contain the rejected periodic u*61 scratch.")
}
Assert-Contains $builder 'v \* 8\.5f \+ u \* 1\.45f \+ \(broad - 0\.5f\) \* 1\.75f' "Directional texture fibers must run primarily along blade thickness with broad-noise distortion."
Assert-Contains $builder 'material\.SetFloat\("_AbrasionStrength", 0\.15f\)' "Abrasion strength must be reduced to the approved 0.12-0.18 range."
Assert-Contains $builder 'new Vector3\(0\.64f, 0\.009f, 0f\)' "Contact needle must use the shortened 0.64-unit asymmetric tip."
Assert-Contains $builder 'new Vector3\(-0\.12f, -0\.019f, 0f\)' "Contact needle must overlap the ribbon tip without a detached gap."
Assert-Contains $builder 'new Vector3\(-0\.06f, 0\.006f, 0f\)' "Native contact particle must sit inside the ribbon tip overlap zone."
Assert-Contains $builder 'contactShape\.enabled = false' "Native contact particle must not inherit the default random cone position."
foreach ($branchLength in @('0\.58f', '0\.71f', '0\.52f', '0\.84f')) {
    Assert-Contains $builder $branchLength "Branch mesh lengths must vary within the approved 0.50-0.85 range."
}
foreach ($branchOrigin in @('-0\.10f', '-0\.17f', '-0\.24f', '-0\.30f')) {
    Assert-Contains $builder $branchOrigin "Branch origins must stagger backward across the blade tip."
}

if ($builder -match 'File\.WriteAllText\s*\(\s*ShaderPath') {
    $failures.Add("Builder must not overwrite the checked-in plasma shader with an embedded string.")
}
if ($builder -match 'AddComponent\s*<\s*TrailRenderer\s*>|new\s+TrailRenderer') {
    $failures.Add("Instant plasma slash must not use TrailRenderer.")
}
if ($builder -match 'OceanBladeSlashUsable|AnhierBlueWhiteSlashRefinedPrefab') {
    $failures.Add("Independent builder must not reuse the Ocean or Refined prefab identity.")
}

Assert-Contains $driver 'public void ApplyAt\(float elapsed\)' "Driver must expose deterministic time sampling for previews."
Assert-Contains $driver 'Mathf\.InverseLerp\(0\.03f, 0\.15f, elapsed\)' "Driver reveal timing must follow the approved 0.03-0.15 second window."
Assert-Contains $driver 'Mathf\.InverseLerp\(0\.30f, 0\.50f, elapsed\)' "Driver retreat timing must follow the approved 0.30-0.50 second window."
Assert-Contains $driver 'block\.SetFloat\(RevealId' "Driver must animate shader reveal through a property block."
Assert-Contains $driver 'block\.SetFloat\(RetreatId' "Driver must animate FIFO retreat through a property block."
Assert-Contains $driver 'float intensity = \(1\.14f \+ contactPeak \* 0\.60f\)' "Main ribbon contact intensity must peak at 1.74."

foreach ($marker in @(
    'Shader "Steria/PlasmaLightningSlashFlow"',
    '_Reveal',
    '_Retreat',
    '_Alpha',
    '_Intensity',
    '_CoreColor',
    '_BodyColor',
    '_EdgeColor',
    '_FFF8FF',
    '_C677FF',
    '_7B16C9',
    'Blend SrcAlpha One',
    'revealMask',
    'retreatMask',
    'leadingHead',
    'connectedCore',
    'fiberMask',
    'edgeInstability',
    'abrasion',
    'envelope')) {
    Assert-Contains $shader ([regex]::Escape($marker)) "Shader missing contract marker: $marker"
}

Assert-Contains $shader 'u \* 1\.55' "Shader longitudinal flow tiling must stay within 1.2-1.8."
if ($shader -match 'u\s*\*\s*11\.5') {
    $failures.Add("Shader still contains the rejected transverse 11.5x path tiling.")
}

Assert-Contains $buildScript 'PlasmaLightningSlashBundleBuilder\.BuildBundle' "Build script must invoke the plasma bundle builder."
if ($buildScript -match 'Copy-Item|SteriaModFolder|Library Of Ruina') {
    $failures.Add("Build script must not deploy or copy into mod/game directories.")
}

if (!(Test-Path -LiteralPath $prefabPath)) {
    $failures.Add("Missing generated prefab: $prefabPath")
}
if (!(Test-Path -LiteralPath $bundlePath)) {
    $failures.Add("Missing built bundle: $bundlePath")
}

$previewFiles = @(
    "black_01_early_reveal.png",
    "black_02_contact_peak.png",
    "black_03_full_body.png",
    "black_04_fade_retreat.png",
    "stage_01_early_reveal.png",
    "stage_02_contact_peak.png",
    "stage_03_full_body.png",
    "stage_04_fade_retreat.png"
)
foreach ($fileName in $previewFiles) {
    $path = Join-Path $previewDir $fileName
    if (!(Test-Path -LiteralPath $path)) {
        $failures.Add("Missing deterministic preview: $path")
    } elseif ((Get-Item -LiteralPath $path).Length -lt 12000) {
        $failures.Add("Preview is unexpectedly small: $path")
    }
}

if (!(Test-Path -LiteralPath $imageVerifierPath)) {
    $failures.Add("Missing image contract verifier: $imageVerifierPath")
} else {
    & python $imageVerifierPath $previewDir $round1PreviewDir $nativeEvidencePath
    if ($LASTEXITCODE -ne 0) {
        $failures.Add("Preview image contract failed. See diagnostics above.")
    }
}

if ($failures.Count -gt 0) {
    Write-Host "FAIL plasma lightning slash source verification"
    foreach ($failure in $failures) {
        Write-Host " - $failure"
    }
    exit 1
}

Write-Host "PASS plasma lightning slash source verification"
Write-Host "Prefab: $prefabPath"
Write-Host "Bundle: $bundlePath"
Write-Host "Preview: $previewDir"
