param([string]$Unity='C:/Program Files/Unity/Editor/Unity.exe',[string]$Python='C:/Users/rog/AppData/Local/Programs/Python/Python312/python.exe',[ValidatePattern('^[a-zA-Z0-9_-]+$')][string]$Revision='new-candidate',[long]$AffinityMask=0)
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'UnityProject'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$output=Join-Path $PSScriptRoot ('evidence/'+$Revision)
if(Test-Path $output){throw 'Revision already exists; choose a new revision to preserve evidence'}
New-Item -ItemType Directory -Force $output | Out-Null
if(Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object {$_.CommandLine -like '*ChristashaParticleV2*'}){throw 'This exact Unity project is already in use'}
$source=Join-Path $repo 'SteriaBuild/ChristashaParticleVisual.cs'
Copy-Item -LiteralPath $source -Destination (Join-Path $project 'Assets/Scripts/ChristashaParticleVisual.cs') -Force
& $Python (Join-Path $PSScriptRoot 'verify_source.py')
if($LASTEXITCODE -ne 0){throw 'Source contract failed'}
$sha=(Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
$log=Join-Path $output 'unity.log'
$previous=$env:OPENSSL_ia32cap
$parentProcess=[Diagnostics.Process]::GetCurrentProcess()
$previousAffinity=$parentProcess.ProcessorAffinity
$launchedUtc=[DateTime]::UtcNow
try {
    if($AffinityMask -ne 0){$parentProcess.ProcessorAffinity=[IntPtr]$AffinityMask}
    $env:OPENSSL_ia32cap=':~0x20000000'
    $arguments=@('-batchmode','-quit','-force-d3d11','-projectPath',('"'+$project+'"'),'-executeMethod','ChristashaParticleBuilder.BuildAndPreview','-v2Revision',$Revision,'-logFile',('"'+$log+'"'))
    $process=Start-Process -FilePath $Unity -ArgumentList $arguments -PassThru -WindowStyle Hidden
} finally {$env:OPENSSL_ia32cap=$previous; $parentProcess.ProcessorAffinity=$previousAffinity}
@{pid=$process.Id;log=$log;revision=$Revision;sourceSha256=$sha} | ConvertTo-Json | Set-Content (Join-Path $output 'run.json') -Encoding utf8
Write-Host "V2 Unity PID=$($process.Id) log=$log"
$timer=[Diagnostics.Stopwatch]::StartNew()
while(!$process.WaitForExit(1000)){if($timer.Elapsed.TotalMinutes -gt 20){Stop-Process -Id $process.Id -Force;throw 'This invocation exceeded 20 minutes'}}
if($process.ExitCode -ne 0){throw "Unity exit $($process.ExitCode); no old success marker is accepted"}
if(!(Test-Path $log) -or (Get-Item $log).LastWriteTimeUtc -lt $launchedUtc.AddSeconds(-1)){throw 'Unity did not write a fresh log for this invocation'}
if(!(Select-String -LiteralPath $log -Pattern 'CHRISTASHA_V2_NATIVE_PASS' -SimpleMatch -Quiet)){Get-Content -LiteralPath $log -Tail 55;throw 'Missing native success marker'}
if((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne $sha){throw 'Canonical source changed during preview'}
& $Python (Join-Path $PSScriptRoot 'package_evidence.py') --revision $Revision
if($LASTEXITCODE -ne 0){throw 'Media packaging failed'}
Write-Host 'CHRISTASHA_V2_BUILD_PREVIEW_PASS (no game deployment)'
