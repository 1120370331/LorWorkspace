param([string]$AudioDirectory='',[string]$OutputDirectory='')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$output=if($OutputDirectory){[IO.Path]::GetFullPath($OutputDirectory)}else{Join-Path $repo 'output/storm-dawn-audio/runtime-checks/velia'}
$waves=if($AudioDirectory){[IO.Path]::GetFullPath($AudioDirectory)}else{Join-Path $PSScriptRoot 'source_audio'}
New-Item -ItemType Directory -Path $output -Force | Out-Null
& (Join-Path $PSScriptRoot 'verify_velia_tide_mist_source.ps1') *> (Join-Path $output 'source-verification.log')
& (Join-Path $PSScriptRoot 'verify_velia_tide_mist_api.ps1') -OutputDirectory (Join-Path $output 'api')
$compiler='C:/Program Files/Unity/Editor/Data/Tools/Roslyn/csc.exe'
$framework='C:/Windows/Microsoft.NET/Framework64/v4.0.30319'
$compile=@('/nologo','/target:exe','/main:VeliaTideMistAudioManagedChecks',('/out:'+(Join-Path $output 'VeliaAudioManagedChecks.exe')))
$compile+=@('mscorlib.dll','System.dll','System.Core.dll') | ForEach-Object {'/reference:'+$framework+'/'+$_}
$compile+=@('VeliaTideMistAudioController.cs','SlazeyaStormAudioController.cs','SkillPcmWave.cs') | ForEach-Object {Join-Path $repo ('SteriaBuild/'+$_)}
# Reuse the existing observable Unity substitutes and regression class; select only this entry.
$compile+=@((Join-Path $PSScriptRoot '../SlazeyaStormMass/SlazeyaStormAudioManagedChecks.cs'),(Join-Path $PSScriptRoot 'VeliaTideMistAudioManagedChecks.cs'))
& $compiler @compile *> (Join-Path $output 'managed-compile.log')
if($LASTEXITCODE -ne 0){Get-Content -LiteralPath (Join-Path $output 'managed-compile.log');throw 'Velia managed audio compile failed'}
& (Join-Path $output 'VeliaAudioManagedChecks.exe') $waves (Join-Path $output 'fixtures') (Join-Path $PSScriptRoot '../SlazeyaStormMass/source_audio') *> (Join-Path $output 'managed-checks.log')
if($LASTEXITCODE -ne 0){Get-Content -LiteralPath (Join-Path $output 'managed-checks.log');throw 'Velia managed audio checks failed'}
Get-Content -LiteralPath (Join-Path $output 'managed-checks.log')
@('cast.wav','hit.wav') | ForEach-Object {Get-FileHash -LiteralPath (Join-Path $waves $_) -Algorithm SHA256} | Select-Object Path,Hash | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'wave-input-hashes.json') -Encoding utf8
