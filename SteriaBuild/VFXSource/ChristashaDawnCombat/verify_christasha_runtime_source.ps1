$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot "..\..\.."))
$runtimePath = Join-Path $repoRoot "SteriaBuild\DiceAttackEffect_Steria_ChristashaDawnCombat.cs"
$harmonyPath = Join-Path $repoRoot "SteriaBuild\HarmonyPatches.cs"
$cardInfoPath = Join-Path $repoRoot "SteriaBuild\SteriaModFolder\Data\CardInfo.xml"
$failures = New-Object System.Collections.Generic.List[string]

function Read-RequiredFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Name
    )

    if (!(Test-Path -LiteralPath $Path)) {
        $failures.Add("$Name is missing: $Path")
        return ""
    }

    return Get-Content -LiteralPath $Path -Raw
}

$runtime = Read-RequiredFile $runtimePath "Christasha dawn runtime source"
$harmony = Read-RequiredFile $harmonyPath "HarmonyPatches.cs"
$cardInfo = Read-RequiredFile $cardInfoPath "CardInfo.xml"

function Assert-Contains {
    param(
        [AllowEmptyString()][Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Pattern,
        [Parameter(Mandatory = $true)][string]$Message
    )

    if ($Source -notmatch $Pattern) {
        $failures.Add($Message)
    }
}

Assert-Contains $runtime 'AB-christasha-dawn-combat-20260706' "runtime version marker is missing."
Assert-Contains $runtime 'steria_christasha_dawn_combat' "runtime must load the Christasha dawn bundle."
Assert-Contains $runtime 'DiceAttackEffect_Steria_ChristashaDawnSlashH' "horizontal slash class is missing."
Assert-Contains $runtime 'DiceAttackEffect_Steria_ChristashaDawnSlashV' "vertical slash class is missing."
Assert-Contains $runtime 'DiceAttackEffect_Steria_ChristashaDawnPierceNear' "near pierce class is missing."
Assert-Contains $runtime 'DiceAttackEffect_Steria_ChristashaDawnPierceFar' "far pierce class is missing."
Assert-Contains $runtime 'DiceAttackEffect_Steria_ChristashaDawnHit' "hit class is missing."
Assert-Contains $runtime 'TrailRenderer' "runtime pierce must use TrailRenderer for first-in first-out trail fading."
Assert-Contains $runtime 'UpdateRuntimeProjectile\(' "runtime projectile must advance every frame."
Assert-Contains $runtime 'RefreshRuntimeImpactWorld\(' "impact point must refresh from the target transform."
Assert-Contains $runtime 'SlashTravelDistanceDivisor' "runtime must expose slash-specific travel scaling for larger readable melee ranges."
Assert-Contains $runtime 'SlashDistanceScaleMax' "runtime must cap slash scale higher than generic melee effects."
Assert-Contains $runtime 'CenterSlashTravelOnCaster' "runtime must allow slash travel roots to stay anchored on the caster center."
Assert-Contains $runtime 'protected override bool CenterSlashTravelOnCaster => true;' "horizontal slash must stay centered on the caster while facing the enemy."
Assert-Contains $runtime 'CenterSlashYOffset' "runtime must allow lowering caster-centered slash anchors."
Assert-Contains $runtime 'protected override float CenterSlashYOffset => -0\.06f;' "horizontal slash is temporarily redirected to the vertical slash anchor."
Assert-Contains $runtime 'CenterSlashTravelUsesTargetSide' "runtime must orient caster-centered slashes by target side instead of rotating the whole prefab backward."
Assert-Contains $runtime 'protected override bool CenterSlashTravelUsesTargetSide => true;' "horizontal slash must use target-side travel scaling to avoid firing behind the caster."
Assert-Contains $runtime 'main\.transform\.localRotation = Quaternion\.identity;' "asset bundle root must not be globally 180-degree flipped for caster-centered slash VFX."
Assert-Contains $runtime 'AnimateCrescentRevealMaterials\(' "runtime must animate crescent shader reveal continuously instead of relying on staged mesh slices."
Assert-Contains $runtime 'SetFloat\("_Reveal"' "runtime must drive shader reveal progress."
Assert-Contains $runtime 'SetFloat\("_Retreat"' "runtime must drive shader retreat progress."
Assert-Contains $runtime 'Mathf\.Pow\(revealRaw, 1\.65f\)' "runtime reveal must use an accelerating motion curve."
Assert-Contains $runtime 'float fifoRetreat = reveal - 0\.30f;' "runtime retreat must follow the reveal path so earlier regions exit first."
Assert-Contains $runtime 'Mathf\.Max\(fifoRetreat, flushRetreat\)' "runtime retreat must preserve FIFO exit while still flushing the full slash."
if ($runtime -match 'UnityEngine\.Rendering\.PostProcessing|PostProcessLayer|PostProcessVolume|RuntimeBloom') {
    $failures.Add("runtime must not override Library Of Ruina camera Bloom; slash glow should come from HDR/emissive materials and the game's own post-processing.")
}
Assert-Contains $runtime 'DiceAttackEffect_Steria_ChristashaDawnSlashH[\s\S]*?PrefabName\s*=>\s*"ChristashaDawnSlashVerticalPrefab"' "horizontal slash runtime must temporarily use the vertical cleave prefab."
Assert-Contains $runtime 'protected override float Duration => 2\.37f;' "slash duration must be extended by 50 percent for visual inspection."
Assert-Contains $runtime 'SlashShakeStrength' "runtime must expose slash-specific hit-stop/screen-shake strength."
Assert-Contains $runtime 'SlashShakeDuration' "runtime must expose slash-specific hit-stop/screen-shake duration."
Assert-Contains $runtime 'AddScreenShake\(SlashShakeStrength' "runtime must use slash-specific shake values for the stronger impact pause feel."
Assert-Contains $runtime 'ColorDawnWhite' "runtime fallback must use the dawn white palette."
Assert-Contains $runtime 'ColorDawnGold' "runtime fallback must use the dawn gold palette."
Assert-Contains $runtime 'DiceAttackEffect_Steria_ChristashaDawnSlashH[\s\S]*?Duration\s*=>\s*2\.37f' "horizontal slash redirect must use the extended vertical timing."
Assert-Contains $runtime 'DiceAttackEffect_Steria_ChristashaDawnSlashV[\s\S]*?Duration\s*=>\s*2\.37f' "vertical crescent slash duration must be extended by 50 percent."

Assert-Contains $harmony 'Steria_ChristashaDawnSlashH' "horizontal slash EffectRes must be registered."
Assert-Contains $harmony 'Steria_ChristashaDawnSlashV' "vertical slash EffectRes must be registered."
Assert-Contains $harmony 'Steria_ChristashaDawnPierceNear' "near pierce EffectRes must be registered."
Assert-Contains $harmony 'Steria_ChristashaDawnPierceFar' "far pierce EffectRes must be registered."
Assert-Contains $harmony 'Steria_ChristashaDawnHit' "hit EffectRes must be registered."

Assert-Contains $cardInfo 'EffectRes="Steria_ChristashaDawnSlashH"' "Christasha card data must use the horizontal dawn slash."
Assert-Contains $cardInfo 'EffectRes="Steria_ChristashaDawnSlashV"' "Christasha card data must use the vertical dawn slash."
Assert-Contains $cardInfo 'EffectRes="Steria_ChristashaDawnPierceNear"' "Christasha card data must use the dawn near pierce."
Assert-Contains $cardInfo 'EffectRes="Steria_ChristashaDawnPierceFar"' "Christasha card data must use the dawn far pierce."
Assert-Contains $cardInfo 'EffectRes="Steria_ChristashaDawnHit"' "Christasha card data must use the dawn hit."

$christashaBlock = [regex]::Match($cardInfo, '<!-- Christasha Cards Start -->[\s\S]*?<!-- Christasha Xiyuan Cards End -->')
if ($christashaBlock.Success) {
    if ($christashaBlock.Value -match 'EffectRes="Steria_Water(Slash|Pierce|Hit)"') {
        $failures.Add("Christasha card sections still contain old Steria_Water slash/pierce/hit effects.")
    }
    if ($christashaBlock.Value -match 'Motion="Z"[^>]*EffectRes="Steria_ChristashaDawnPierceFar"') {
        $failures.Add("near Motion=Z pierce must not use the far projectile pierce effect.")
    }
    if ($christashaBlock.Value -match 'Motion="F"[^>]*EffectRes="Steria_ChristashaDawnPierceNear"') {
        $failures.Add("far Motion=F pierce must not use the near melee pierce effect.")
    }
    if ($christashaBlock.Value -notmatch 'Motion="J"[^>]*EffectRes="Steria_ChristashaDawnSlashV"') {
        $failures.Add("at least one regular J slash must use the vertical dawn slash so the vertical slash is visible outside mass attacks.")
    }
} else {
    $failures.Add("Unable to locate Christasha card section in CardInfo.xml.")
}

if ($failures.Count -gt 0) {
    Write-Host "Christasha dawn runtime source verification failed:"
    foreach ($failure in $failures) {
        Write-Host " - $failure"
    }
    exit 1
}

Write-Host "Christasha dawn runtime source verification passed."
