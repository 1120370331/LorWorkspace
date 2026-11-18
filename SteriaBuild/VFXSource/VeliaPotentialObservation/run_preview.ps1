param(
    [string]$Unity='C:/Program Files/Unity/Editor/Unity.exe',
    [string]$Python='python',
    [ValidatePattern('^[A-Za-z0-9_-]+$')][string]$Revision='candidate-01',
    [long]$AffinityMask=4294901760
)
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'UnityProject'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$output=Join-Path $PSScriptRoot ('evidence/'+$Revision)
if(Test-Path -LiteralPath $output){throw 'Choose a new revision; existing evidence is immutable'}
if(Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object {$_.CommandLine -like '*VeliaPotentialObservation/UnityProject*' -or $_.CommandLine -like '*VeliaPotentialObservation\UnityProject*'}){throw 'This preview project is already in use'}
New-Item -ItemType Directory -Force -Path $output,(Join-Path $project 'Assets/Scripts') | Out-Null
$source=Join-Path $repo 'SteriaBuild/VeliaPotentialObservationVisual.cs'
Copy-Item -LiteralPath $source -Destination (Join-Path $project 'Assets/Scripts/VeliaPotentialObservationVisual.cs') -Force
$modelRoot=Join-Path $repo 'SteriaBuild/CharacterModels/Velia_Layered_v1'
$artManifest=Get-Content -Raw -LiteralPath (Join-Path $modelRoot 'build/build_manifest.json') | ConvertFrom-Json
$artVerification=Get-Content -Raw -LiteralPath (Join-Path $modelRoot 'build/verification.json') | ConvertFrom-Json
if($artVerification.result -ne 'PASS' -or $artVerification.candidate -ne $artManifest.candidate){throw 'Art export has no matching verification; rebuild and verify the model first'}
foreach($artSource in $artManifest.sources.PSObject.Properties){
    if((Get-FileHash -LiteralPath (Join-Path $modelRoot $artSource.Name) -Algorithm SHA256).Hash -ne $artSource.Value){throw "Stale art source: $($artSource.Name)"}
}
$inputs=@(
    $source,
    (Join-Path $repo 'SteriaBuild/VeliaPotentialObservationEffect.cs'),
    (Join-Path $repo 'SteriaBuild/VeliaCards.cs'),
    (Join-Path $repo 'SteriaBuild/VeliaAbilities.cs'),
    (Join-Path $repo 'SteriaBuild/CharacterModels/Velia_Layered_v1/model.json'),
    (Join-Path $modelRoot 'build/build_manifest.json'),
    (Join-Path $modelRoot 'build/verification.json'),
    (Join-Path $repo 'SteriaBuild/CharacterModels/Velia_Layered_v1/sources/observation/anchors.json'),
    (Join-Path $repo 'SteriaBuild/CharacterModels/Velia_Layered_v1/build/assembled/S2.png'),
    (Join-Path $repo 'SteriaBuild/CharacterModels/Velia_Layered_v1/build/assembled/Default.png'),
    (Join-Path $repo 'SteriaBuild/CharacterModels/Velia_Layered_v1/build/Velia_Layered_v1_Projection/ModInfo.Xml'),
    (Join-Path $repo 'SteriaBuild/CharacterModels/Sivier_Layered_v1/build/runtime/Resource/CharacterSkin/Sivier/ClothCustom/Default.png'),
    (Join-Path $repo 'SteriaBuild/CharacterModels/Sivier_Layered_v1/build/runtime/Resource/CharacterSkin/Sivier/ModInfo.Xml'),
    (Join-Path $repo 'SteriaBuild/CharacterModels/Ailierel_Layered_v1/build/runtime/Resource/CharacterSkin/Ailierel/ClothCustom/Default.png'),
    (Join-Path $repo 'SteriaBuild/CharacterModels/Ailierel_Layered_v1/build/runtime/Resource/CharacterSkin/Ailierel/ModInfo.Xml'),
    (Join-Path $project 'Assets/Shaders/VeliaObservation.shader'),
    (Join-Path $project 'Assets/Shaders/VeliaObservationParticle.shader'),
    (Join-Path $PSScriptRoot 'source_assets/light-masks/surge-mask.png'),
    (Join-Path $PSScriptRoot 'source_assets/light-masks/soft-source.png'),
    (Join-Path $PSScriptRoot 'source_assets/light-masks/small-streak.png'),
    (Join-Path $PSScriptRoot 'source_assets/light-masks/manifest.json'),
    (Join-Path $PSScriptRoot 'source_assets/light-masks/source-provenance.json'),
    (Join-Path $project 'Assets/Editor/VeliaObservationParticleBuilder.cs'),
    (Join-Path $project 'Assets/Editor/VeliaObservationPreview.cs'),
    (Join-Path $project 'Assets/Scripts/VeliaObservationFrameSmoke.cs')
)
$receipt=@($inputs | ForEach-Object { @{path=$_;sha256=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash} })
$receipt | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $output 'source-hashes.json') -Encoding utf8
$log=Join-Path $output 'acceptance.log'
$priorSsl=$env:OPENSSL_ia32cap
$parentProcess=[Diagnostics.Process]::GetCurrentProcess()
$priorAffinity=$parentProcess.ProcessorAffinity
$launchedUtc=[DateTime]::UtcNow
try {
    if($AffinityMask -ne 0){$parentProcess.ProcessorAffinity=[IntPtr]$AffinityMask}
    $env:OPENSSL_ia32cap=':~0x20000000'
    $arguments=@('-batchmode','-force-d3d11','-projectPath',('"'+$project+'"'),'-executeMethod','VeliaObservationPreview.BuildAndPreview','-observationOutput',('"'+$output+'"'),'-logFile',('"'+$log+'"'))
    $process=Start-Process -FilePath $Unity -ArgumentList $arguments -PassThru -WindowStyle Hidden
} finally {$env:OPENSSL_ia32cap=$priorSsl;$parentProcess.ProcessorAffinity=$priorAffinity}
@{pid=$process.Id;log=$log;revision=$Revision;startedUtc=$launchedUtc.ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'run.json') -Encoding utf8
Write-Host "Velia observation Unity PID=$($process.Id) log=$log"
$timer=[Diagnostics.Stopwatch]::StartNew()
while(!$process.WaitForExit(1000)){if($timer.Elapsed.TotalMinutes -gt 15){Stop-Process -Id $process.Id -Force;throw 'Only this preview invocation was stopped after 15 minutes'}}
if($process.ExitCode -ne 0){Get-Content -LiteralPath $log -Tail 50;throw "Unity exit $($process.ExitCode)"}
if(!(Test-Path -LiteralPath $log) -or (Get-Item -LiteralPath $log).LastWriteTimeUtc -lt $launchedUtc.AddSeconds(-1)){throw 'No fresh primary log'}
if(!(Select-String -LiteralPath $log -SimpleMatch 'VELIA_OBSERVATION_NATIVE_PASS' -Quiet)){Get-Content -LiteralPath $log -Tail 50;throw 'Native completion marker missing'}
if(Select-String -LiteralPath $log -SimpleMatch 'Particle Velocity curves must all be in the same mode' -Quiet){throw 'Unity rejected mixed velocity curve modes; candidate is failed even if another marker was emitted'}
foreach($inputFile in $receipt){if((Get-FileHash -LiteralPath $inputFile.path -Algorithm SHA256).Hash -ne $inputFile.sha256){throw "Candidate changed during native preview: $($inputFile.path)"}}
& $Python (Join-Path $PSScriptRoot 'package_preview.py') $output
if($LASTEXITCODE -ne 0){throw 'Preview media packaging failed'}
Write-Host "VELIA_OBSERVATION_BUILD_PREVIEW_PASS $output (no deployment)"
