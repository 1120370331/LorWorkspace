$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$runtime = Join-Path $repo 'SteriaBuild'
function Require([bool]$condition, [string]$message) { if (!$condition) { throw $message } }
$driver = Get-Content -Raw (Join-Path $runtime 'VeliaTideMistVisualController.cs')
$filter = Get-Content -Raw (Join-Path $runtime 'VeliaTideMistScreenFilter.cs')
$effect = Get-Content -Raw (Join-Path $runtime 'FarAreaEffect_Steria_VeliaTideMist.cs')
$action = Get-Content -Raw (Join-Path $runtime 'BehaviourAction_Steria_VeliaTideMist.cs')
Require ($driver -notmatch '\b(BattleUnitModel|BattleCamManager|FarAreaEffect|Time\.deltaTime)\b') 'Controller must be pure Unity with an explicit clock'
Require ($filter -notmatch '\b(BattleUnitModel|BattleCamManager|FarAreaEffect)\b|\.Advance\(') 'Filter must render only, without game dependencies or clock advancement'
Require ($filter -match 'new Material\(materialTemplate\)' -and $filter -match 'Graphics.Blit') 'Filter must clone its template and use native Blit'
Require (($filter + $effect) -notmatch 'RemoveCameraFilterAll|RenderSettings\.|\.fieldOfView\s*=|\.targetTexture\s*=|\.rect\s*=') 'Do not mutate shared camera/global state'
Require ($effect -notmatch '\.GiveDamage\(|\.RecoverHP\(|\.RecoverBreakLife\(|\.OnEndFarAreaBehaviourAtk\(') 'Visual host must not perform settlement'
Require ($effect -match 'cardBehaviorQueue.Count' -and $effect -match '_visual.PulseFinished') 'Read the public queue at pulse tail'
Require ($effect -notmatch 'if\s*\(_isDoneEffect\s*\|\|') 'Dice completion must not stop inter-dice Update'
Require ($effect -match 'EffectCam' -and $effect -match 'ReferenceEquals\(Active, this\)') 'Use the effect camera and identity-guarded session cleanup'
Require ($action -match 'GetOrCreate') 'Factory must reuse the current card session'
[xml]$cards = Get-Content -Raw (Join-Path $runtime 'SteriaModFolder/Data/CardInfo.xml')
$card = $cards.SelectSingleNode('//Card[@ID="9004004"]')
Require ($null -ne $card -and $card.BehaviourList.Behaviour.Count -eq 2) 'Expected two dice on 9004004'
foreach ($die in $card.BehaviourList.Behaviour) {
    Require ($die.ActionScript -eq 'Steria_VeliaTideMist') '9004004 action route missing'
    Require ($die.EffectRes -eq 'Steria_WaterSlash') 'Original EffectRes must remain'
}
$original = Join-Path $repo 'SteriaBuild/VFXSource/SlazeyaStormMass/source_assets/round2/mist_density_lighting_4x4.png'
$reference = Get-Content -Raw (Join-Path $PSScriptRoot 'source_assets/references.json') | ConvertFrom-Json
Require ((Get-FileHash $original -Algorithm SHA256).Hash -eq $reference.mist.sha256) 'Read-only mist source drift'
Write-Host 'VELIA_SOURCE_PASS'
