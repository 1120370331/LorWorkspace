$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot "..\..\.."))
$builder = Join-Path $repoRoot "SteriaBuild\VFXSource\AnhierBlueWhiteSlash\UnityProject\Assets\Editor\ChristashaDawnCombatBundleBuilder.cs"
$failures = New-Object System.Collections.Generic.List[string]

if (!(Test-Path -LiteralPath $builder)) {
    $failures.Add("Christasha dawn Unity builder is missing: $builder")
} else {
    $source = Get-Content -LiteralPath $builder -Raw

    function Assert-SourceContains {
        param(
            [Parameter(Mandatory = $true)][string]$Pattern,
            [Parameter(Mandatory = $true)][string]$Message
        )

        if ($source -notmatch $Pattern) {
            $failures.Add($Message)
        }
    }

    Assert-SourceContains 'steria_christasha_dawn_combat' "bundle name must be steria_christasha_dawn_combat."
    Assert-SourceContains 'ChristashaDawnSlashHorizontalPrefab' "horizontal slash prefab must be generated."
    Assert-SourceContains 'ChristashaDawnSlashVerticalPrefab' "vertical slash prefab must be generated."
    Assert-SourceContains 'ChristashaDawnPierceNearPrefab' "near pierce prefab must be generated."
    Assert-SourceContains 'ChristashaDawnPierceFarPrefab' "far pierce prefab must be generated."
    Assert-SourceContains 'ChristashaDawnHitPrefab' "hit prefab must be generated."
    Assert-SourceContains 'CreateVerticalCrescentRevealMeshes\(' "vertical slash must create ordered crescent mesh reveal slices."
    Assert-SourceContains 'CreateVerticalCrescentMesh\(' "vertical slash must use a procedural crescent mesh silhouette."
    Assert-SourceContains 'CreateCrescentVerticalParticleSlash\(' "vertical slash must be assembled from the crescent mesh particle route."
    Assert-SourceContains 'CreateCrescentBladeParticle\(' "vertical slash must use mesh particles for the main blade layers."
    Assert-SourceContains 'CreateCrescentStarParticle\(' "vertical slash must include top and bottom cross-star mesh particles."
    Assert-SourceContains 'CreateCrescentEdgeDust\(' "vertical slash must include directional outer-edge dust."
    Assert-SourceContains 'CreateCrescentShadowDust\(' "vertical slash must include restrained brown-black inner shadow particles."
    Assert-SourceContains 'CreateCrescentRevealGradient\(' "crescent mesh particles must use a staged color/alpha reveal curve."
    Assert-SourceContains 'ParticleSystemRenderMode\.Mesh' "crescent body and star anchors must use ParticleSystem mesh rendering."
    Assert-SourceContains 'Mathf\.Sqrt\(normalizedPosition\)' "slash timing must accelerate along the top-to-bottom path."
    Assert-SourceContains 'new Vector3\(1\.25f, 0f, 0f\)' "vertical slash travel root must be moved forward from the caster by 25 percent."
    Assert-SourceContains 'CreatePiercePathSegments\(' "pierce VFX must still use ordered path segments."
    Assert-SourceContains 'BuildAssetBundles' "builder must build the AssetBundle."
    Assert-SourceContains 'VerifyBuiltBundle' "builder must verify prefabs in the built bundle."

    $verticalBlock = [regex]::Match($source, 'private\s+static\s+void\s+CreateSlashVerticalPrefab[\s\S]*?SavePrefab')
    if (!$verticalBlock.Success) {
        $failures.Add("unable to locate CreateSlashVerticalPrefab body.")
    } else {
        $v = $verticalBlock.Value
        if ($v -notmatch 'AB_DawnSlashV_TopCrossStar') {
            $failures.Add("vertical slash must start with a top cross-star anchor.")
        }
        if ($v -notmatch 'AB_DawnSlashV_BottomCrossStar') {
            $failures.Add("vertical slash must spawn the bottom cross-star as the crescent arrives.")
        }
        if ($v -notmatch 'AB_DawnSlashV_OuterDeepGold') {
            $failures.Add("vertical slash must include the deep-gold outer crescent layer.")
        }
        if ($v -notmatch 'AB_DawnSlashV_InnerPaleGold') {
            $failures.Add("vertical slash must include the pale inner crescent layer.")
        }
        if ($v -notmatch 'AB_DawnSlashV_InnerBrownBlackStarfield') {
            $failures.Add("vertical slash must include brown-black starfield depth particles.")
        }
        if ($v -match 'CreateTexturedSlashWipe3D\(|ProvidedCrescent.*Layer|CreateMeshParticle\(') {
            $failures.Add("vertical slash must not use the old texture-strip/material-first route.")
        }
        if ($v -match 'HZStyle|Afterimage014|Afterimage016|TargetBurstArc|CreateSlashArcParticle|(?<![0-9.])26f|CrescentImpactGold') {
            $failures.Add("vertical slash must not retain previous flashy afterimage/arc/big-blast layers.")
        }
    }
}

if ($failures.Count -gt 0) {
    Write-Host "Christasha dawn VFX source verification failed:"
    foreach ($failure in $failures) {
        Write-Host " - $failure"
    }
    exit 1
}

Write-Host "Christasha dawn VFX source verification passed."
