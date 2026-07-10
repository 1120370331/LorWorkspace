$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot "..\..\.."))
$builderPath = Join-Path $repoRoot "SteriaBuild\VFXSource\AnhierBlueWhiteSlash\UnityProject\Assets\Editor\ChristashaDawnCombatBundleBuilder.cs"
$coreShaderPath = Join-Path $repoRoot "SteriaBuild\VFXSource\AnhierBlueWhiteSlash\UnityProject\Assets\Shaders\ChristashaDawn_CoreOnlyFlow.shader"
$glowShaderPath = Join-Path $repoRoot "SteriaBuild\VFXSource\AnhierBlueWhiteSlash\UnityProject\Assets\Shaders\ChristashaDawn_CoreOnlyEdgeGlow.shader"
$failures = New-Object System.Collections.Generic.List[string]

foreach ($path in @($builderPath, $coreShaderPath, $glowShaderPath)) {
    if (!(Test-Path -LiteralPath $path)) {
        $failures.Add("required source file is missing: $path")
    }
}

if ($failures.Count -eq 0) {
    $builder = Get-Content -LiteralPath $builderPath -Raw
    $coreShader = Get-Content -LiteralPath $coreShaderPath -Raw
    $glowShader = Get-Content -LiteralPath $glowShaderPath -Raw

    function Assert-Contains {
        param(
            [Parameter(Mandatory = $true)][string]$Source,
            [Parameter(Mandatory = $true)][string]$Pattern,
            [Parameter(Mandatory = $true)][string]$Message
        )

        if ($Source -notmatch $Pattern) {
            $failures.Add($Message)
        }
    }

    Assert-Contains $builder 'ChristashaDawn_BrushMask_512\.png' "builder must generate and bind the dedicated brush mask."
    Assert-Contains $builder 'CreateDirectionalBrushMaskTexture\(' "builder must generate a directional brush mask."
    Assert-Contains $builder 'new Color\(coreFiber,\s*goldFiber,\s*abrasion,\s*softEnvelope\)' "brush mask channels must encode core, gold fibers, abrasion, and soft envelope."
    if ($builder -match 'CreateEnergyFlowMaskTexture\(') {
        $failures.Add("builder must remove the rounded cellular energy-flow mask generator.")
    }

    foreach ($property in @(
        '_BrushCoreStrength',
        '_BrushFiberStrength',
        '_BrushAbrasionStrength',
        '_SoftEnvelopeStrength'
    )) {
        Assert-Contains $coreShader ([regex]::Escape($property)) "core shader is missing brushwork property $property."
    }

    Assert-Contains $coreShader 'brushTex\.r' "core shader must use the red channel for the white blade core."
    Assert-Contains $coreShader 'brushTex\.g' "core shader must use the green channel for gold fibers."
    Assert-Contains $coreShader 'brushTex\.b' "core shader must use the blue channel for dark abrasion."
    Assert-Contains $coreShader 'brushTex\.a' "core shader must use the alpha channel for the soft envelope."
    Assert-Contains $coreShader 'float abrasionCut' "core shader must explicitly subtract abrasion from brightness and opacity."
    Assert-Contains $coreShader '_LuminousBodyStrength' "core shader must expose a broad luminous-body strength."
    Assert-Contains $coreShader 'float luminousBody' "core shader must build an opaque luminous blade body, not only thin fibers."
    Assert-Contains $coreShader 'luminousBody \* _LuminousBodyStrength' "core shader must apply luminous-body strength to the broad stroke."
    Assert-Contains $coreShader 'luminousBody \* 0\.52' "core shader alpha must retain the luminous body instead of leaving it transparent."
    Assert-Contains $builder 'mat\.SetFloat\("_LuminousBodyStrength",\s*0\.86f\)' "core material must keep the broad slash body visibly emissive."
    Assert-Contains $coreShader 'float maskPath = saturate\(pathT\);' "core brush mask must sample one continuous path without a mid-stroke wrap seam."
    Assert-Contains $glowShader 'float glowMaskPath = saturate\(pathT\);' "glow brush mask must sample one continuous path without a mid-stroke wrap seam."
    if ($coreShader -match 'pathT \* _FlowTexTiling') {
        $failures.Add("core brush mask must not repeat along the slash path because the wrap boundary causes a visible break.")
    }
    if ($glowShader -match 'pathT \* _FlowTexTiling') {
        $failures.Add("glow brush mask must not repeat along the slash path because the wrap boundary causes a visible break.")
    }

    Assert-Contains $glowShader '_SoftEnvelopeStrength' "edge glow shader must expose soft-envelope strength."
    Assert-Contains $glowShader 'brushTex\.a' "edge glow shader must be driven primarily by the broad envelope channel."
    Assert-Contains $glowShader 'brushTex\.g' "edge glow shader may retain restrained gold-fiber energy."

    Assert-Contains $builder 'CreateUserProvidedSlashVolumeMesh\(' "builder must use the asymmetric swept-blade mesh generator."
    Assert-Contains $builder 'ChristashaDawn_UserSlashCoreOnly\.asset' "builder must create a dedicated clean core mesh."
    Assert-Contains $builder 'ChristashaDawn_UserSlashGlowEnvelope\.asset' "builder must create a separate glow-envelope mesh."
    Assert-Contains $builder 'CreateSlashVerticalCoreOnlyPrefab\(coreMaterial,\s*edgeGlowMaterial,\s*coreMesh,\s*glowMesh,\s*revealClip\)' "core-only prefab must receive separate core and glow meshes."
    Assert-Contains $builder 'float trailingTaper = Smooth01\(' "slash width profile must explicitly grow out of the narrow trailing end."
    Assert-Contains $builder 'float leadingBody = Smooth01\(' "slash width profile must broaden toward the leading blade body."
    Assert-Contains $builder 'float cuttingTip = 1f - Smooth01\(' "slash width profile must end in a short cutting tip."
    Assert-Contains $builder 'float bladeWidth = trailingWidth \+ leadingBodyWidth \* trailingTaper \* leadingBody \* cuttingTip;' "slash width must encode a front-wide and rear-narrow pressure profile."

    if ($builder -match 'CreateUserProvidedCrescentVolumeMesh\(') {
        $failures.Add("builder must remove the old symmetric crescent mesh generator.")
    }
    if ($builder -match 'const float outerGlowExtension = 1\.5f') {
        $failures.Add("main slash mesh must not embed the old outer-glow extension.")
    }
    if ($builder -match 'const float edgeNoise = 0\.03f') {
        $failures.Add("primary slash silhouette must not contain geometric edge noise.")
    }
}

if ($failures.Count -gt 0) {
    Write-Host "Christasha brushwork source verification failed:"
    foreach ($failure in $failures) {
        Write-Host " - $failure"
    }
    exit 1
}

Write-Host "Christasha brushwork source verification passed."
