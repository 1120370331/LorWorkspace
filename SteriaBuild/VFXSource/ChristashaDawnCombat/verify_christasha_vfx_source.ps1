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
    Assert-SourceContains 'CreateVerticalCrescentRevealMeshes\(' "vertical slash must still generate full crescent mesh layer assets."
    Assert-SourceContains 'CreateVerticalCrescentOuterBladeRevealMeshes\(' "vertical slash must generate an outer-mouth blade mesh layer."
    Assert-SourceContains 'CreateVerticalCrescentOuterBladeMesh\(' "vertical slash outer blade must use a dedicated outer-mouth mesh silhouette."
    Assert-SourceContains 'CreateVerticalCrescentMesh\(' "vertical slash must use a procedural crescent mesh silhouette."
    Assert-SourceContains 'CreateCrescentVerticalParticleSlash\(' "vertical slash must be assembled from the crescent mesh particle route."
    Assert-SourceContains 'CreateCrescentBladeParticle\(' "vertical slash must use mesh particles for the main blade layers."
    Assert-SourceContains 'CreateCrescentStarParticle\(' "vertical slash must include top and bottom cross-star mesh particles."
    Assert-SourceContains 'CreateCrescentEdgeDust\(' "vertical slash must include directional outer-edge dust."
    Assert-SourceContains 'CreateCrescentShadowDust\(' "vertical slash must include restrained brown-black inner shadow particles."
    Assert-SourceContains 'CreateCrescentRevealGradient\(' "crescent mesh particles must use a staged color/alpha reveal curve."
    Assert-SourceContains '_Reveal\s+\(""?Reveal""?' "crescent shader must expose a continuous reveal value."
    Assert-SourceContains '_Retreat\s+\(""?Retreat""?' "crescent shader must expose top-to-bottom retreat control."
    Assert-SourceContains '_RevealEdgeWidth\s+\(""?Reveal Edge Width""?' "crescent shader must provide a moving reveal edge band."
    Assert-SourceContains '_HdrEmission\s+\(""?HDR Emission""?' "crescent shader must expose HDR emission color for game-side bloom."
    Assert-SourceContains '_BladeInnerColor\s+\(""?Blade Inner Color""?' "outer blade shader must expose pale-yellow inner gradient color."
    Assert-SourceContains '_BladeOuterColor\s+\(""?Blade Outer Color""?' "outer blade shader must expose deep-yellow outer gradient color."
    Assert-SourceContains '_BladeGradientStrength\s+\(""?Blade Gradient Strength""?' "outer blade shader must allow gradient coloring without changing reveal control."
    Assert-SourceContains '_CenterPlateauWidth\s+\(""?Center Plateau Width""?' "crescent shader must expose a center plateau width for trapezoid brightness."
    Assert-SourceContains '_CenterFalloffPower\s+\(""?Center Falloff Power""?' "crescent shader must expose fast edge falloff for trapezoid brightness."
    Assert-SourceContains '_CoreFillStrength\s+\(""?Core Fill Strength""?' "crescent shader must expose a fill-driven white core sweep strength."
    Assert-SourceContains '_CoreFillTail\s+\(""?Core Fill Tail""?' "crescent shader must expose a tail length for the filled white core sweep."
    Assert-SourceContains '_DistortStrength\s+\(""?Distort Strength""?' "crescent shader must expose UV distortion strength for energy flow."
    Assert-SourceContains '_DistortScale\s+\(""?Distort Scale""?' "crescent shader must expose distortion scale for non-flat mesh energy."
    Assert-SourceContains '_DistortSpeed\s+\(""?Distort Speed""?' "crescent shader must expose distortion speed for animated flow."
    Assert-SourceContains 'pathT = 1\.0 - i\.uv\.y' "crescent shader reveal must run top-to-bottom through UV path progress."
    Assert-SourceContains 'float2 flowUv = i\.uv;' "crescent shader must sample the mesh texture through an animated flow UV."
    Assert-SourceContains 'flowUv\.x \+= distort \* _DistortStrength \* \(0\.18 \+ centerMask \* 0\.82\);' "crescent shader must bend the internal energy flow across the mesh width."
    Assert-SourceContains 'float4 tex = tex2D\(_MainTex, flowUv\);' "crescent shader must use distorted flow UVs instead of static UVs while preserving HDR values."
    Assert-SourceContains 'float3 trapezoidEmission = lerp\(float3\(1\.00, 0\.86, 0\.18\), _HdrEmission\.rgb, trapezoidProfile\);' "crescent shader must apply gold-white trapezoid HDR emission with a bright center and fast side falloff."
    Assert-SourceContains 'trapezoidEmission = lerp\(trapezoidEmission, bladeGradient, saturate\(_BladeGradientStrength\)\);' "outer blade shader must reuse the flow/reveal shader while switching to pale-to-deep yellow gradient emission."
    Assert-SourceContains 'c\.rgb \*= trapezoidEmission \* _EmissionBoost' "crescent shader must multiply body brightness by the trapezoid HDR emission profile."
    Assert-SourceContains 'float coreFill = visible \* coreMask \* saturate\(0\.34 \+ freshCore \* 0\.82\) \* _CoreFillStrength;' "white core sweep must be driven by filled crescent shader area, not particles."
    Assert-SourceContains 'float energyFlow = pow\(saturate\(sin\(\(pathT - _Time\.y \* _FlowSpeed \* 0\.82\)' "crescent shader must add a traveling energy band, not only static alpha reveal."
    Assert-SourceContains 'Transform crescentRoot = CreateGroup\(parent, "AB_DawnSlashV_CrescentMeshRoot", crescentPos\);' "vertical slash must use a shared crescent mesh root."
    Assert-SourceContains 'crescentRoot\.localRotation = Quaternion\.Euler\(crescentEuler\);' "vertical slash root must carry the crescent plane rotation."
    Assert-SourceContains 'Vector3 topStarLocal = new Vector3\(0\.14f, 3\.28f, -0\.08f\);' "top cross-star must be anchored to the enlarged crescent local endpoint."
    Assert-SourceContains 'Vector3 bottomStarLocal = new Vector3\(0\.30f, -3\.28f, -0\.08f\);' "bottom cross-star must be anchored to the enlarged crescent local endpoint."
    Assert-SourceContains 'CreateCrescentStarParticle\(crescentRoot, topStarName' "top cross-star must be an independent particle attached to the crescent root."
    Assert-SourceContains 'CreateCrescentStarParticle\(crescentRoot, bottomStarName' "bottom cross-star must be an independent particle attached to the crescent root."
    Assert-SourceContains 'CreateCrescentStarGlowParticle\(' "cross-stars must include a separate soft additive glow layer."
    Assert-SourceContains 'CreateCrescentStarEdgeSparkParticle\(' "cross-stars must include directional edge spark particles."
    Assert-SourceContains 'CreateCrossStarCoreMesh\(' "cross-star core must use a dedicated expanding cross mesh, not a plain quad."
    Assert-SourceContains 'CreateCrescentStarRayParticle\(' "cross-stars must emit outward fading light rays around the core."
    Assert-SourceContains 'CreateHdrAdditiveMaterial\(' "cross-star cores and glow must use an HDR additive material instead of plain non-HDR tint."
    Assert-SourceContains 'ChristashaDawn_StarHdr_Add\.mat' "cross-star HDR additive material must be generated."
    Assert-SourceContains 'AB_DawnSlashV_TopCrossStar_Rays' "top cross-star must have outward fading ray particles."
    Assert-SourceContains 'AB_DawnSlashV_BottomCrossStar_Rays' "bottom cross-star must have outward fading ray particles."
    Assert-SourceContains 'shape\.shapeType = ParticleSystemShapeType\.Circle' "cross-star rays must emit radially around the star."
    Assert-SourceContains 'shape\.arcMode = ParticleSystemShapeMultiModeValue\.Random' "cross-star rays must distribute around the core rather than one direction."
    Assert-SourceContains 'new Keyframe\(0\.00f, 0\.00f\), new Keyframe\(0\.18f, 1\.22f\), new Keyframe\(1f, 0f\)' "cross-star rays must expand and fade away."
    Assert-SourceContains 'AB_DawnSlashV_TopCrossStar_Glow' "top cross-star must have its own glow layer."
    Assert-SourceContains 'AB_DawnSlashV_TopCrossStar_EdgeSparks' "top cross-star must have edge spark particles."
    Assert-SourceContains 'AB_DawnSlashV_BottomCrossStar_Glow' "bottom cross-star must have its own glow layer."
    Assert-SourceContains 'AB_DawnSlashV_BottomCrossStar_EdgeSparks' "bottom cross-star must have edge spark particles."
    Assert-SourceContains 'new Keyframe\(0\.08f, 1\.34f\)' "cross-star core must use a sharper flicker pop."
    Assert-SourceContains 'new ParticleSystem\.Burst\(0\.025f, 5\)' "cross-star edge sparks must burst immediately after the star appears."
    Assert-SourceContains 'CreateCrescentBladeParticle\(crescentRoot, glowName,' "vertical slash must use one full glow crescent controlled by shader reveal, not staged glow slices."
    Assert-SourceContains 'CreateCrescentBladeParticle\(crescentRoot, outerName,' "vertical slash must use one full outer crescent controlled by shader reveal, not staged outer slices."
    Assert-SourceContains 'CreateCrescentBladeParticle\(crescentRoot, innerName,' "vertical slash must use one full inner crescent controlled by shader reveal, not staged inner slices."
    Assert-SourceContains 'CreateCrescentBladeParticle\(crescentRoot, outerBladeName,' "vertical slash must overlay one full outer-mouth blade mesh controlled by the same shader reveal."
    Assert-SourceContains 'CreateCrescentBladeParticle\(crescentRoot, coreName,' "vertical slash must use one full core crescent controlled by shader reveal, not staged core slices."
    Assert-SourceContains 'CreateCrescentBladeParticle\(crescentRoot, shadowName \+ "_Mesh",' "vertical slash must use one full shadow crescent controlled by shader reveal, not staged shadow slices."
    if ($source -match 'CreateCrescentCoreSweepParticles\(crescentRoot') {
        $failures.Add("vertical slash core sweep must be driven by crescent fill shader, not a particle sweep call.")
    }
    Assert-SourceContains 'ParticleSystemRenderMode\.Mesh' "crescent body and star anchors must use ParticleSystem mesh rendering."
    Assert-SourceContains 'EvaluateAcceleratingSlashTiming' "slash timing must accelerate along the top-to-bottom path."
    Assert-SourceContains 'float extrusionDepth = Mathf\.Max\(0\.045f, Mathf\.Abs\(depthBend\) \* 0\.34f\);' "vertical crescent mesh must be extruded into front/back surfaces instead of a flat ribbon."
    Assert-SourceContains 'float frontDepth = zCurve - extrusionDepth \* 0\.5f;' "vertical crescent mesh must define a front depth surface."
    Assert-SourceContains 'float backDepth = zCurve \+ extrusionDepth \* 0\.5f;' "vertical crescent mesh must define a back depth surface."
    Assert-SourceContains 'int a = i \* 4;' "vertical crescent mesh must emit four vertices per path sample for thickness."
    Assert-SourceContains 'int b = \(i \+ 1\) \* 4;' "vertical crescent mesh must cap the final extruded end without invalid indices."
    Assert-SourceContains 'triangles\.Add\(b \+ 3\);' "vertical crescent mesh must stitch and cap side faces between extruded samples."
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
        if ($v -notmatch 'AB_DawnSlashV_OuterYellowRim') {
            $failures.Add("vertical slash must include the yellow outer crescent rim layer.")
        }
        if ($v -notmatch 'AB_DawnSlashV_InnerPaleYellow') {
            $failures.Add("vertical slash must include the pale-yellow inner crescent layer.")
        }
        if ($v -notmatch 'AB_DawnSlashV_OuterBladeYellowWhite') {
            $failures.Add("vertical slash must include the yellow-to-white outer blade layer.")
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
        if ($v -match 'for\s*\(int\s+i\s*=\s*0;\s*i\s*<\s*outerReveal\.Length') {
            $failures.Add("vertical slash body must not use staged partial mesh reveal loops as the primary reveal.")
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
