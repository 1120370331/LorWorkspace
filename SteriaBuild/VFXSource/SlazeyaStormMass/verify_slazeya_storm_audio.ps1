param([switch]$WithWaves)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$output=Join-Path $repo 'preview_exports/slazeya_storm_mass/round2/audio'
New-Item -ItemType Directory -Force -Path $output | Out-Null
& (Join-Path $PSScriptRoot 'verify_slazeya_storm_audio_source.ps1') *> (Join-Path $output 'source-verification.log')
$unityData='C:/Program Files/Unity/Editor/Data'
$framework='C:/Windows/Microsoft.NET/Framework64/v4.0.30319'
$csc=Join-Path $unityData 'Tools/Roslyn/csc.exe'
$baseArgs=@('/nologo','/target:library',('/out:'+(Join-Path $output 'AudioApiCheck.dll')))
$baseArgs+=@('mscorlib.dll','System.dll','System.Core.dll') | ForEach-Object {'/reference:'+$framework+'/'+$_}
$apiArgs=$baseArgs + (Get-ChildItem -LiteralPath (Join-Path $unityData 'Managed/UnityEngine') -Filter 'UnityEngine*.dll' | ForEach-Object {'/reference:'+$_.FullName})
$apiArgs+=@((Join-Path $repo 'SteriaBuild/SlazeyaStormAudioController.cs'),(Join-Path $repo 'SteriaBuild/SlazeyaStormVisualController.cs'))
& $csc @apiArgs *> (Join-Path $output 'api-compile.log')
if($LASTEXITCODE -ne 0){Get-Content (Join-Path $output 'api-compile.log');throw 'Audio companion Unity 2019 API compile failed'}
Write-Host 'PASS audio source contracts and real Unity 2019 API compilation'
if($WithWaves){
    $harnessArgs=@('/nologo','/target:exe',('/out:'+(Join-Path $output 'AudioManagedChecks.exe')))
    $harnessArgs+=@('mscorlib.dll','System.dll','System.Core.dll') | ForEach-Object {'/reference:'+$framework+'/'+$_}
    $harnessArgs+=@((Join-Path $repo 'SteriaBuild/SlazeyaStormAudioController.cs'),(Join-Path $PSScriptRoot 'SlazeyaStormAudioManagedChecks.cs'))
    & $csc @harnessArgs *> (Join-Path $output 'managed-compile.log')
    if($LASTEXITCODE -ne 0){Get-Content (Join-Path $output 'managed-compile.log');throw 'Managed audio contract fixture compile failed'}
    & (Join-Path $output 'AudioManagedChecks.exe') (Join-Path $PSScriptRoot 'source_audio') (Join-Path $output 'managed-fixtures') *> (Join-Path $output 'managed-checks.log')
    if($LASTEXITCODE -ne 0){Get-Content (Join-Path $output 'managed-checks.log');throw 'Managed audio contract checks failed'}
    Get-Content (Join-Path $output 'managed-checks.log')
}
