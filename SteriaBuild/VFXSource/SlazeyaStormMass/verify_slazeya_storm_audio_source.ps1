$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$audioPath=Join-Path $repo 'SteriaBuild/SlazeyaStormAudioController.cs'
$audio=if(Test-Path -LiteralPath $audioPath){[IO.File]::ReadAllText($audioPath)}else{''}
$bridge=[IO.File]::ReadAllText((Join-Path $repo 'SteriaBuild/FarAreaEffect_Steria_OceanWave.cs'))
$failed=[Collections.Generic.List[string]]::new()
function Check([bool]$ok,[string]$name){if($ok){Write-Host "PASS $name"}else{$failed.Add($name);Write-Host "FAIL $name"}}
Check ($audio -match 'sealed class SlazeyaStormAudioController : IDisposable' -and $audio -notmatch 'UnityWebRequest|:\s*MonoBehaviour') 'plain companion, no web dependency'
Check ($audio -match 'Dictionary<string, AudioClip>' -and $audio -match 'GatherGain = 0.30f' -and $audio -match 'BurstGain = 0.78f') 'cached clips and frozen gain interface'
Check ($audio -match 'ReadPcm16Wave' -and $audio -match 'MaxWaveBytes' -and $audio -match '44100' -and $audio -match 'blockAlign != 4' -and $audio -match 'bits != 16') 'bounded strict PCM16 stereo loader'
Check ($audio -match 'spatialBlend = 0f' -and $audio -match 'dopplerLevel = 0f' -and $audio -match 'playOnAwake = false' -and $audio -match 'ignoreListenerPause = false') 'instance 2D sources, no Doppler/autoplay/pause bypass'
Check ($audio -match 'Time.timeScale' -and $audio -match '\.Pause\(' -and $audio -match '\.UnPause\(' -and $audio -match 'timeSamples') 'pause, pitch and visual-clock resynchronization'
Check ($audio -match 'BurstTriggered\) return false' -and $audio -match 'GatherRelease = 0.04f' -and $audio -match 'ReleaseSources') 'one burst, short gather release, immediate cleanup'
Check ($bridge -match 'audioPosition.HasValue' -and $bridge -match 'recipients.Count > 0\) audioPosition' -and $bridge -match '_audio.Advance\(Time.deltaTime\)' -and $bridge -match '_audio.TriggerBurst\(\)' -and $bridge -match '_audio.Dispose\(\)') 'recipient-only bridge with Advance/callback/dispose'
Check (([regex]::Matches($bridge,'GetVolumeEffect\(').Count -eq 1) -and $bridge -notmatch 'soundVolume_all|soundVolume_effect') 'option master-times-effects read once, not squared'
Check ($bridge -notmatch 'damagedUnitList.Count|override bool ActionPhase|\.GiveDamage\(|\.TakeDamage\(') 'empty damage list allowed; default gameplay unchanged'
$visual=Join-Path $repo 'SteriaBuild/SlazeyaStormVisualController.cs'
$bundle=Join-Path $PSScriptRoot 'UnityProject/AssetBundles/steria_slazeya_storm_mass'
Check ((Get-FileHash -LiteralPath $visual -Algorithm SHA256).Hash -eq '5D6532FAF0C86382E0BE86CA0662A765A797151924F3EFEFE95ED57EDD515E8A') 'accepted visual controller frozen'
Check ((Get-FileHash -LiteralPath $bundle -Algorithm SHA256).Hash -eq '03E113FCBD45DE93E79620824B59C8E7D9E8D2EDBD3B92FC1FC43415DDAC4E3A') 'accepted visual Bundle frozen'
if($failed.Count){throw "Audio source contract failed: $($failed.Count)"}
