$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$failures = [Collections.Generic.List[string]]::new()
function Check([bool]$condition, [string]$message) {
    if (!$condition) { $failures.Add($message); Write-Host "FAIL $message" } else { Write-Host "PASS $message" }
}
function Source([string]$path) {
    $full = Join-Path $repo $path
    if (!(Test-Path -LiteralPath $full)) { return '' }
    return [IO.File]::ReadAllText($full)
}
$effect = Source 'SteriaBuild/FarAreaEffect_Steria_OceanWave.cs'
$driver = Source 'SteriaBuild/SlazeyaStormVisualController.cs'
$card = Source 'SteriaBuild/CardAbilities.cs'
$ability = [regex]::Match($card, '(?s)public class DiceCardSelfAbility_SlazeyaMassAttackTeamLightGain.*?(?=// --- Slazeya Dice Abilities ---)').Value
$builder = Source 'SteriaBuild/VFXSource/SlazeyaStormMass/UnityProject/Assets/Editor/SlazeyaStormMassBundleBuilder.cs'
$shader = Source 'SteriaBuild/VFXSource/SlazeyaStormMass/UnityProject/Assets/Shaders/SlazeyaStormFlow.shader'
$particleShader = Source 'SteriaBuild/VFXSource/SlazeyaStormMass/UnityProject/Assets/Shaders/SlazeyaStormParticles.shader'
$lightningShader = Source 'SteriaBuild/VFXSource/SlazeyaStormMass/UnityProject/Assets/Shaders/SlazeyaStormLightning.shader'
$build = Source 'SteriaBuild/VFXSource/SlazeyaStormMass/build_slazeya_storm_bundle.ps1'
Check ($effect -notmatch 'override\s+bool\s+(HasIndependentAction|ActionPhase)|\.GiveDamage\(|\.TakeDamage\(|OnEndFarAreaBehaviourAtk\(') 'default manager owns damage and defense'
Check ($effect -match 'override void GiveDamageFromManager' -and $effect -match '_burstTriggered' -and $effect -notmatch 'damagedUnitList\.Count') 'callback permits empty victims and locks duplicate burst'
Check ($effect -notmatch 'if\s*\(\s*!isRunning\s*\)\s*return') 'tail continues after gathering gate opens'
Check ($effect -match 'isRunning = true' -and $driver -match 'GatherDuration = 1.35f' -and $driver -match 'TailDuration = 1.20f') 'R3 frozen gate and tail durations'
Check ($effect -match 'IsBreakLifeZero\(' -and $effect -match 'IsKnockout\(' -and $effect -match 'CallbackWatchdog' -and $effect -match 'manager.attacker != _self') 'cancellation and bounded callback wait'
Check ($effect -match 'Singleton<BattleFarAreaPlayManager>.Instance' -and $effect -match 'manager.victims' -and $effect -match 'GetAliveList_opponent\(self.faction\)' -and $effect -match 'unit.faction != self.faction') 'current opposing recipients without fixed faction'
Check ($effect -match 'SpriteRenderer' -and $effect -match 'bounds' -and $driver -match 'FitFootprint' -and $driver -match 'normalizedRadius') 'rendered body bounds and corner-covering XZ ellipse'
Check ($ability -notmatch 'CreateOceanWaveEffect|OceanWaveEffectComponent|AddScreenShake' -and $ability -match 'flowConsumedByThisCard / 8' -and $ability -match 'PrimalTidePowerScope.RunWithAllowance') 'remove only duplicate visual; preserve Flow power'
Check ($driver -match 'sealed class SlazeyaStormVisualController\s*:\s*IDisposable' -and $driver -notmatch ':\s*MonoBehaviour') 'canonical plain C# controller'
Check (([regex]::Matches($driver, '\.Simulate\(').Count -eq 1) -and $driver -match '\.Simulate\(deltaTime, false, false, false\)' -and $driver -notmatch '\.Play\(') 'single non-recursive explicit simulation path, no autoplay'
Check ($driver -match 'MaterialPropertyBlock' -and $driver -match '"_Phase"' -and $shader -notmatch '_Time|GrabPass|ZTest Always') 'explicit phase and normal scene depth'
Check ($shader -match 'Shader "Steria/SlazeyaStormFlow"' -and $shader -match 'ZWrite Off') 'fixed supported shader contract'
Check ($builder -match 'AssetBundle.LoadFromFile' -and $builder -match 'new SlazeyaStormVisualController' -and $builder -notmatch 'AddComponent<SlazeyaStormVisualController>') 'bundle readback drives actual shared controller'
Check ($build -match 'Get-FileHash' -and $build -match 'SHA256' -and $build -match 'WindowStyle Hidden' -and $build -notmatch '-nographics|git commit|Copy-Item.*SteriaModFolder') 'isolated hash-checked build without deployment'
Check ($builder -notmatch 'Length == 5|particles.Length == 3|budget <= 160|maximumAlive <= 160|MakeTexture\(|Mesh Ribbon\(') 'old count restrictions and striped ribbon generation removed'
Check ($driver -match 'EnvelopeCenter' -and $driver -match 'EnvelopeRadii' -and $driver -match 'Bounds\[\] bodyBounds' -and $driver -match '0.81f') 'shared all-body sample envelope with 0.90 margin'
Check ($builder -match 'BubbleEnvelope' -and $builder -match 'cap' -and $shader -match 'BubblePoint' -and $shader -match 'pole') 'closed bubble pole and joined bottom cap'
Check ($shader -match '_FoamGather' -and $shader -match 'transport') 'foam transported from both hemispheres into burst latitude'
Check ($driver -match 'FoamParcelPosition' -and $driver -match 'UpdateFoamParcels' -and $driver -match 'SetParticles\(_foamParcels' -and $builder -match 'VerifyFoamTransport') 'persistent native foam positions and readback convergence check'
Check ($shader -match '_Rupture' -and $shader -match 'sector' -and $driver -match 'RingPoint') 'flexible sector rupture and actual circumferential ring jets'
Check ($shader -match 'BubbleNormal' -and $shader -match '_NormalRoughness') 'finite ellipsoid / deformed smooth normal, including pole'
Check ($shader -match 'Blend One OneMinusSrcAlpha' -and $shader -match 'color \* alpha') 'single premultiplied transparent water body'
Check ($builder -match 'water_body_rgba.png' -and $builder -match 'water_normal_roughness.png' -and $builder -match 'water_flow_rg.png' -and $builder -match 'foam_spray_4x4.png' -and $builder -match 'mist_density_lighting_4x4.png' -and $builder -match 'droplet_spindrift_4x2.png') 'six frozen texture worker assets imported, no substitute textures'
Check ($driver -match '_burstPhase' -and $driver -match 'BandPoint' -and $driver -match 'RingNormal' -and $driver -match 'RingTangent') 'burst inherits foam-band phase and ellipse-normal momentum'
Check ($builder -match 'burstAge' -and $builder -match 'round3' -and $build -match 'LastWriteTimeUtc') 'R3 output, callback-relative samples, fresh log validation'
Check ($driver -match 'CrestLiftMist' -and $driver -match '_primaryReturnReleased' -and $driver -match '_secondaryReturnReleased') 'crest mist and two finite return clusters'
Check ($driver -match 't>=0.05f' -and $driver -match '0.10f' -and $lightningShader -match 'Steria/SlazeyaStormLightning' -and $lightningShader -notmatch '_Time|ZTest Always') 'two bounded local discharges with dark interval'
Check ($particleShader -match 'alpha=data.a\*_Alpha;' -and $particleShader -notmatch 'data.a\*data.r' -and $builder -match 'color.enabled=!mist') 'authored mist alpha once, no repeated density/lifetime fade'
if ($failures.Count) { throw "Slazeya storm source verification failed: $($failures.Count) contract(s)." }
Write-Host 'Slazeya storm focused source verification PASS'
