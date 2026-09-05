param([string]$UnityData = 'C:/Program Files/Unity/Editor/Data')
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$output = Join-Path $repo 'preview_exports/slazeya_storm_mass/round4'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$managed = Join-Path $UnityData 'Managed'
$framework = 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319'
$compilerArgs = @('/nologo','/target:library',('/out:' + (Join-Path $output 'BuilderCompileCheck.dll')),('/reference:' + $managed + '/UnityEditor.dll'))
$compilerArgs += @('mscorlib.dll','System.dll','System.Core.dll') | ForEach-Object { '/reference:' + $framework + '/' + $_ }
$compilerArgs += Get-ChildItem -LiteralPath ($managed + '/UnityEngine') -Filter 'UnityEngine*.dll' | ForEach-Object { '/reference:' + $_.FullName }
$compilerArgs += @((Join-Path $repo 'SteriaBuild/SlazeyaStormVisualController.cs'),
    (Join-Path $PSScriptRoot 'UnityProject/Assets/Editor/SlazeyaStormMassBundleBuilder.cs'))
& (Join-Path $UnityData 'Tools/Roslyn/csc.exe') @compilerArgs *> (Join-Path $output 'unity-source-compile.log')
if ($LASTEXITCODE -ne 0) { Get-Content (Join-Path $output 'unity-source-compile.log'); throw 'Unity API source compile failed' }
Write-Host 'PASS Unity 2019 C# API source compilation (does not prove shader support or native rendering)'
