param([string]$UnityData = 'C:/Program Files/Unity/Editor/Data', [switch]$BakerContractOnly, [string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$output = if ($OutputDirectory) { [IO.Path]::GetFullPath($OutputDirectory) } else { Join-Path $repo ('preview_exports/slazeya_storm_mass/round6/source-compile-' + [Guid]::NewGuid().ToString('N')) }
New-Item -ItemType Directory -Force -Path $output | Out-Null
$managed = Join-Path $UnityData 'Managed'
$framework = 'C:/Windows/Microsoft.NET/Framework64/v4.0.30319'
$compilerArgs = @('/nologo','/target:library',('/out:' + (Join-Path $output 'BuilderCompileCheck.dll')),('/reference:' + $managed + '/UnityEditor.dll'))
$compilerArgs += @('mscorlib.dll','System.dll','System.Core.dll') | ForEach-Object { '/reference:' + $framework + '/' + $_ }
$compilerArgs += Get-ChildItem -LiteralPath ($managed + '/UnityEngine') -Filter 'UnityEngine*.dll' | ForEach-Object { '/reference:' + $_.FullName }
$compilerArgs += @((Join-Path $repo 'SteriaBuild/SlazeyaStormVisualController.cs'),
    (Join-Path $PSScriptRoot 'UnityProject/Assets/Editor/SlazeyaStormMassBundleBuilder.cs'))
if ($BakerContractOnly) {
    # Compile only the downstream caller against the frozen upstream API. Never run this stub.
    $bakerContract = Join-Path $output 'NoiseBakerContractCompileOnly.cs'
    @'
using UnityEngine;
public static class SlazeyaStormCloudNoiseBaker {
    public const string ShapePath="Assets/Textures/CloudVolume/CloudShape64.asset";
    public const string ErosionPath="Assets/Textures/CloudVolume/CloudErosion32.asset";
    public static Texture3D[] Ensure(string output) { throw new System.NotSupportedException("Compile-only API contract"); }
    public static void VerifyBundle(GameObject prefab,string output) { throw new System.NotSupportedException("Compile-only API contract"); }
}
'@ | Set-Content -LiteralPath $bakerContract -Encoding utf8
    $compilerArgs += $bakerContract
} else {
    $compilerArgs += Join-Path $PSScriptRoot 'UnityProject/Assets/Editor/SlazeyaStormCloudNoiseBaker.cs'
}
& (Join-Path $UnityData 'Tools/Roslyn/csc.exe') @compilerArgs *> (Join-Path $output 'unity-source-compile.log')
if ($LASTEXITCODE -ne 0) { Get-Content (Join-Path $output 'unity-source-compile.log'); throw 'Unity API source compile failed' }
Write-Host 'PASS Unity 2019 C# API source compilation (does not prove shader support or native rendering)'
if ($BakerContractOnly) { Write-Host 'Baker was represented by its frozen API only; no baker implementation was compiled or executed.' }
