$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$output = Join-Path $repo 'output/velia-tide-mist/implementation/api'
New-Item -ItemType Directory -Force $output | Out-Null
$csc = 'C:/Program Files/Unity/Editor/Data/Tools/Roslyn/csc.exe'
$framework = 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319'
$game = 'C:/Program Files (x86)/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/Managed'
if (!(Test-Path (Join-Path $game 'Assembly-CSharp.dll'))) { throw 'Real game Managed folder unavailable; supply path before API verification' }
$argsList = @('/nologo','/target:library','/langversion:7.3',('/out:' + (Join-Path $output 'VeliaTideMistApiCheck.dll')))
$argsList += @('mscorlib.dll','System.dll','System.Core.dll') | ForEach-Object { '/reference:' + $framework + '/' + $_ }
$argsList += Get-ChildItem $game -Filter 'UnityEngine*.dll' | ForEach-Object { '/reference:' + $_.FullName }
$argsList += @('/reference:' + (Join-Path $game 'Assembly-CSharp.dll'))
$argsList += @('BehaviourAction_Steria_VeliaTideMist.cs','FarAreaEffect_Steria_VeliaTideMist.cs','VeliaTideMistVisualController.cs','VeliaTideMistScreenFilter.cs') | ForEach-Object { Join-Path $repo ('SteriaBuild/' + $_) }
& $csc @argsList *> (Join-Path $output 'api-compile.log')
if ($LASTEXITCODE -ne 0) { Get-Content (Join-Path $output 'api-compile.log'); throw 'Real game API compile failed' }
Write-Host 'VELIA_REAL_GAME_API_PASS (isolated DLL; no Steria.dll build or deployment)'
