param([string]$Unity = 'C:/Program Files/Unity/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$project = Join-Path $PSScriptRoot 'UnityProject'
$canonical = Join-Path $repo 'SteriaBuild/SlazeyaStormVisualController.cs'
$mirror = Join-Path $project 'Assets/Scripts/SlazeyaStormVisualController.cs'
$preview = Join-Path $repo 'preview_exports/slazeya_storm_mass/round4'
New-Item -ItemType Directory -Force -Path (Split-Path $mirror),$preview | Out-Null
Copy-Item -LiteralPath $canonical -Destination $mirror -Force
$sourceHash = (Get-FileHash -LiteralPath $canonical -Algorithm SHA256).Hash
if ((Get-FileHash -LiteralPath $mirror -Algorithm SHA256).Hash -ne $sourceHash) { throw 'Canonical/mirror SHA256 mismatch' }
& (Join-Path $PSScriptRoot 'verify_slazeya_storm_source.ps1')
if (!(Test-Path -LiteralPath $Unity)) { throw "Unity unavailable: $Unity" }
$launchTime = [DateTime]::UtcNow
$runId = $launchTime.ToString('yyyyMMdd-HHmmss-fff') + '-' + [Guid]::NewGuid().ToString('N').Substring(0,8)
$log = Join-Path $preview ("unity-build-" + $runId + '.log')
$arguments = @('-batchmode', '-quit', '-projectPath', ('"' + $project + '"'),
    '-executeMethod', 'SlazeyaStormMassBundleBuilder.BuildBundle', '-logFile', ('"' + $log + '"'))
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
if (!(Select-String -LiteralPath $log -SimpleMatch 'SLAZEYA_ROUND4_BUILD_PREVIEW_PASS' -Quiet)) { throw "This run's R4 success marker absent: $log" }
if ((Get-FileHash -LiteralPath $canonical -Algorithm SHA256).Hash -ne $sourceHash -or
    (Get-FileHash -LiteralPath $mirror -Algorithm SHA256).Hash -ne $sourceHash) { throw 'Controller changed during build; preview is stale' }
$bundle = Join-Path $project 'AssetBundles/steria_slazeya_storm_mass'
if (!(Test-Path -LiteralPath $bundle) -or (Get-Item -LiteralPath $bundle).LastWriteTimeUtc -lt $launchTime.AddSeconds(-1)) { throw 'Bundle was not produced by this run' }
$manifest = Get-Content -LiteralPath (Join-Path $preview 'manifest.json') -Raw | ConvertFrom-Json
if ($manifest.bundleSha256 -ne (Get-FileHash -LiteralPath $bundle -Algorithm SHA256).Hash -or $manifest.controllerSha256 -ne $sourceHash) { throw 'Readback manifest bundle/controller hash mismatch' }
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
$inputs += Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'source_assets/round2') -Filter '*.png' -File | ForEach-Object { $_.FullName }
$hashes = $inputs | ForEach-Object { Get-FileHash -LiteralPath $_ -Algorithm SHA256 | Select-Object Path,Hash }
$hashes | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $preview 'candidate-sha256.json') -Encoding utf8
Get-Item -LiteralPath $bundle | Select-Object FullName,Length,LastWriteTime
Write-Host "PASS build, readback and native previews: $preview"
if (Get-Command python -ErrorAction SilentlyContinue) {
    & python (Join-Path $PSScriptRoot 'export_review_media.py') --output $preview
    if ($LASTEXITCODE -ne 0) { throw 'Native frames succeeded but contact-sheet/media assembly failed' }
}
