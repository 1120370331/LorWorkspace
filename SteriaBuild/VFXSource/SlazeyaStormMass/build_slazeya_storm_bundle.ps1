param(
    [string]$Unity = 'C:/Program Files/Unity/Editor/Unity.exe',
    [string]$PreviewDirectory,
    [ValidateSet('FirstCandidate','Reviewed')][string]$Stage = 'FirstCandidate',
    [string]$ReviewedCandidateDirectory,
    [switch]$CameraApproximation,
    [switch]$StreakFramesOnly
)
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
if ($Stage -eq 'Reviewed' -and $StreakFramesOnly) { throw 'Reviewed stage requires full existing gates and preview output' }
$project = Join-Path $PSScriptRoot 'UnityProject'
$canonical = Join-Path $repo 'SteriaBuild/SlazeyaStormVisualController.cs'
$mirror = Join-Path $project 'Assets/Scripts/SlazeyaStormVisualController.cs'
$runId = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff') + '-' + [Guid]::NewGuid().ToString('N').Substring(0,8)
$preview = if ($PreviewDirectory) { [IO.Path]::GetFullPath($PreviewDirectory) } else { Join-Path $repo ('preview_exports/slazeya_storm_mass/round6/' + $Stage.ToLowerInvariant() + '-' + $runId) }
$protectedRound5 = [IO.Path]::GetFullPath((Join-Path $repo 'preview_exports/slazeya_storm_mass/round5')).TrimEnd('\','/')
if ($preview -eq $protectedRound5 -or $preview.StartsWith($protectedRound5 + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Round5 is a frozen external baseline' }
if (Test-Path -LiteralPath (Join-Path $preview 'manifest.json')) { throw 'Preview already exported; choose a new unique PreviewDirectory' }
New-Item -ItemType Directory -Force -Path (Split-Path $mirror),$preview | Out-Null
Copy-Item -LiteralPath $canonical -Destination $mirror -Force
$sourceHash = (Get-FileHash -LiteralPath $canonical -Algorithm SHA256).Hash
if ((Get-FileHash -LiteralPath $mirror -Algorithm SHA256).Hash -ne $sourceHash) { throw 'Canonical/mirror SHA256 mismatch' }
& (Join-Path $PSScriptRoot 'verify_slazeya_storm_source.ps1')
if (!(Test-Path -LiteralPath $Unity)) { throw "Unity unavailable: $Unity" }
$frozenInputs = @($canonical,$mirror,(Join-Path $project 'Assets/Editor/SlazeyaStormMassBundleBuilder.cs'),(Join-Path $project 'Assets/Editor/SlazeyaStormCloudNoiseBaker.cs'),$PSCommandPath,(Join-Path $PSScriptRoot 'verify_slazeya_storm_source.ps1'))
$frozenInputs += Get-ChildItem -LiteralPath (Join-Path $project 'Assets/Shaders') -Recurse -File | Where-Object { $_.Extension -ne '.meta' } | ForEach-Object { $_.FullName }
$frozenInputs += Get-ChildItem -LiteralPath (Join-Path $project 'Assets/Textures/CloudVolume') -File | Where-Object { $_.Extension -in '.asset','.json' } | ForEach-Object { $_.FullName }
$frozenInputs += Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'source_assets/round2') -Filter '*.png' -File | ForEach-Object { $_.FullName }
$frozenHashes = @($frozenInputs | ForEach-Object { Get-FileHash -LiteralPath $_ -Algorithm SHA256 | Select-Object Path,Hash })
if ($Stage -eq 'Reviewed') {
    if (!$ReviewedCandidateDirectory) { throw 'Reviewed stage requires the independently accepted candidate directory' }
    $reviewedHashes = Get-Content -LiteralPath (Join-Path $ReviewedCandidateDirectory 'visual-input-sha256.json') -Raw | ConvertFrom-Json
    foreach ($item in $frozenHashes) {
        $accepted = @($reviewedHashes | Where-Object { $_.Path -eq $item.Path })
        if ($accepted.Count -ne 1 -or $accepted[0].Hash -ne $item.Hash) { throw "Reviewed visual input changed: $($item.Path)" }
    }
}
$frozenHashes | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $preview 'visual-input-sha256.json') -Encoding utf8
$launchTime = [DateTime]::UtcNow
$log = Join-Path $preview ("unity-build-" + $runId + '.log')
$arguments = @('-batchmode', '-quit', '-projectPath', ('"' + $project + '"'),
    '-executeMethod', 'SlazeyaStormMassBundleBuilder.BuildBundle', '-previewDirectory', ('"' + $preview + '"'), '-previewStage', $Stage,
    '-cameraApproximation', $CameraApproximation.IsPresent.ToString().ToLowerInvariant(), '-force-d3d11', '-logFile', ('"' + $log + '"'))
if ($StreakFramesOnly) { $arguments += @('-streakFramesOnly','true') }
$process = Start-Process -FilePath $Unity -ArgumentList $arguments -PassThru -WindowStyle Hidden
Write-Host "Unity PID=$($process.Id); native graphics enabled; log=$log"
@{ RunId=$runId; ProcessId=$process.Id; StartedUtc=$launchTime.ToString('o'); Log=$log } | ConvertTo-Json |
    Set-Content -LiteralPath (Join-Path $preview 'last-build-run.json') -Encoding utf8
$watch = [Diagnostics.Stopwatch]::StartNew()
while (!$process.WaitForExit(1000)) {
    if ($watch.Elapsed.TotalMinutes -gt 15) {
        # Stop only the editor launched by this invocation, never another task's Unity.
        Stop-Process -Id $process.Id -Force
        throw "Dedicated Unity build exceeded 15 minutes; inspect $log"
    }
}
$freshLog = (Test-Path -LiteralPath $log) -and (Get-Item -LiteralPath $log).Length -gt 0 -and (Get-Item -LiteralPath $log).LastWriteTimeUtc -ge $launchTime.AddSeconds(-1)
if (!$freshLog) { throw "Unity process $($process.Id) exited $($process.ExitCode) without a new log for run $runId. No previous licensing log was used." }
Copy-Item -LiteralPath $log -Destination (Join-Path $preview 'unity-build.log') -Force
if ($process.ExitCode -ne 0) { Get-Content -LiteralPath $log -Tail 65; throw "Unity process $($process.Id) exit $($process.ExitCode); fresh log: $log" }
if (!(Select-String -LiteralPath $log -SimpleMatch 'SLAZEYA_ROUND6_BUILD_PREVIEW_PASS' -Quiet)) { throw "This run's R6 success marker absent: $log" }
foreach ($item in $frozenHashes) { if ((Get-FileHash -LiteralPath $item.Path -Algorithm SHA256).Hash -ne $item.Hash) { throw "Visual input changed during build: $($item.Path)" } }
if ((Get-FileHash -LiteralPath $canonical -Algorithm SHA256).Hash -ne $sourceHash -or
    (Get-FileHash -LiteralPath $mirror -Algorithm SHA256).Hash -ne $sourceHash) { throw 'Controller changed during build; preview is stale' }
$bundle = Join-Path $project 'AssetBundles/steria_slazeya_storm_mass'
if (!(Test-Path -LiteralPath $bundle) -or (Get-Item -LiteralPath $bundle).LastWriteTimeUtc -lt $launchTime.AddSeconds(-1)) { throw 'Bundle was not produced by this run' }
$manifest = Get-Content -LiteralPath (Join-Path $preview 'manifest.json') -Raw | ConvertFrom-Json
if ($manifest.bundleSha256 -ne (Get-FileHash -LiteralPath $bundle -Algorithm SHA256).Hash -or $manifest.controllerSha256 -ne $sourceHash) { throw 'Readback manifest bundle/controller hash mismatch' }
if ($Stage -eq 'Reviewed') {
    $reviewedManifest = Get-Content -LiteralPath (Join-Path $ReviewedCandidateDirectory 'manifest.json') -Raw | ConvertFrom-Json
    if ($manifest.bundleSha256 -ne $reviewedManifest.bundleSha256) { throw 'Reviewed rebuild differs from visually accepted bundle' }
}
$archive = Join-Path $preview 'bundle'
New-Item -ItemType Directory -Path $archive | Out-Null
Copy-Item -LiteralPath $bundle -Destination $archive
$bundle = Join-Path $archive 'steria_slazeya_storm_mass'
$manifest.bundle = $bundle
$manifest | Add-Member -NotePropertyName previewStage -NotePropertyValue $Stage
$manifest | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $preview 'manifest.json') -Encoding utf8
for ($i=0; $i -lt $manifest.textureFiles.Count; $i++) {
    $sourceTexture = Join-Path $PSScriptRoot ('source_assets/round2/' + $manifest.textureFiles[$i])
    $importedTexture = Join-Path $project ('Assets/Textures/Round2/' + $manifest.textureFiles[$i])
    if ((Get-FileHash -LiteralPath $sourceTexture -Algorithm SHA256).Hash -ne $manifest.textureSha256[$i] -or
        (Get-FileHash -LiteralPath $importedTexture -Algorithm SHA256).Hash -ne $manifest.textureSha256[$i]) { throw 'Texture source changed during build; do not accept mixed candidate' }
}
$inputs = @($canonical, $mirror, (Join-Path $repo 'SteriaBuild/FarAreaEffect_Steria_OceanWave.cs'),
    (Join-Path $project 'Assets/Editor/SlazeyaStormMassBundleBuilder.cs'),
    (Join-Path $project 'Assets/Shaders/SlazeyaStormFlow.shader'),
    (Join-Path $project 'Assets/Shaders/SlazeyaStormParticles.shader'), $bundle)
$inputs += Join-Path $project 'Assets/Shaders/SlazeyaStormLightning.shader'
$inputs += Join-Path $project 'Assets/Shaders/SlazeyaStormCloud.shader'
$inputs += Join-Path $project 'Assets/Shaders/SlazeyaStormCloudVolume.shader'
$inputs += Join-Path $project 'Assets/Shaders/SlazeyaStormCloudNoise.cginc'
$inputs += Join-Path $project 'Assets/Shaders/SlazeyaStormCloudDensity.cginc'
$inputs += Join-Path $project 'Assets/Shaders/CloudDensitySlice.compute'
$inputs += Join-Path $project 'Assets/Shaders/CloudNoiseBake.compute'
$inputs += Join-Path $project 'Assets/Shaders/CloudNoise/PeriodicPerlin.hlsl'
$inputs += Join-Path $project 'Assets/Editor/SlazeyaStormCloudNoiseBaker.cs'
$inputs += Join-Path $project 'Assets/Textures/CloudVolume/CloudShape64.asset'
$inputs += Join-Path $project 'Assets/Textures/CloudVolume/CloudErosion32.asset'
$inputs += Join-Path $project 'Assets/Textures/CloudVolume/CloudNoiseCache.json'
$inputs += Join-Path $PSScriptRoot 'ThirdParty/NOTICE.txt'
$inputs += Join-Path $PSScriptRoot 'ThirdParty/cloud-source-references.json'

$inputs += Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'source_assets/round2') -Filter '*.png' -File | ForEach-Object { $_.FullName }
$hashes = $inputs | ForEach-Object { Get-FileHash -LiteralPath $_ -Algorithm SHA256 | Select-Object Path,Hash }
$hashes | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $preview 'candidate-sha256.json') -Encoding utf8
Get-Item -LiteralPath $bundle | Select-Object FullName,Length,LastWriteTime
Write-Host "PASS build, readback and native previews: $preview"
if (!$StreakFramesOnly -and (Get-Command python -ErrorAction SilentlyContinue)) {
    & python (Join-Path $PSScriptRoot 'export_review_media.py') --output $preview
    if ($LASTEXITCODE -ne 0) { throw 'Native frames succeeded but contact-sheet/media assembly failed' }
}
