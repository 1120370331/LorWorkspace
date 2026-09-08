$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$assets = Join-Path $PSScriptRoot 'UnityProject/Assets'
foreach ($name in @('SlazeyaStormWeatherController.cs','SlazeyaStormWeatherScreenFilter.cs')) {
    if ((Get-FileHash (Join-Path $repo ('SteriaBuild/' + $name))).Hash -ne (Get-FileHash (Join-Path $assets ('Scripts/' + $name))).Hash) { throw "Weather mirror mismatch: $name" }
}
$driver = Get-Content (Join-Path $repo 'SteriaBuild/SlazeyaStormWeatherController.cs') -Raw
$filter = Get-Content (Join-Path $repo 'SteriaBuild/SlazeyaStormWeatherScreenFilter.cs') -Raw
$shader = Get-Content (Join-Path $assets 'Shaders/SlazeyaStormWeather.shader') -Raw
if ($driver -match 'Time\.|_elapsed\s*\+=|\.TriggerBurst\(') { throw 'Weather must only read the visual clock' }
if ($filter -match '\.Advance\(|\.TriggerBurst\(|RemoveCameraFilterAll|sharedMaterial') { throw 'Filter must not drive visual or shared state' }
if ($shader -match '\b_Time\b|GrabPass|Camera\.main|SetGlobal') { throw 'Forbidden weather shader input' }
if ($shader -notmatch 'float4\(color,source.a\)' -or $filter -notmatch 'Graphics.Blit\(source, destination\)') { throw 'Missing alpha/pass-through contract' }
if ($shader -notmatch '#pragma target 3.0') { throw 'Shader target changed' }
Write-Host 'PASS weather source mirror, ownership, explicit clock and pass-through contracts (visual acceptance remains native)'
