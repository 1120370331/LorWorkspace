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

    Assert-Contains $glowShader '_SoftEnvelopeStrength' "edge glow shader must expose soft-envelope strength."
    Assert-Contains $glowShader 'brushTex\.a' "edge glow shader must be driven primarily by the broad envelope channel."
    Assert-Contains $glowShader 'brushTex\.g' "edge glow shader may retain restrained gold-fiber energy."
}

if ($failures.Count -gt 0) {
    Write-Host "Christasha brushwork source verification failed:"
    foreach ($failure in $failures) {
        Write-Host " - $failure"
    }
    exit 1
}

Write-Host "Christasha brushwork source verification passed."
