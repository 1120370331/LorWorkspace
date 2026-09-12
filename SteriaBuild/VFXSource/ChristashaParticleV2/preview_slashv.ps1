param([string]$Unity='C:/Program Files/Unity/Editor/Unity.exe',[string]$Python='C:/Users/rog/AppData/Local/Programs/Python/Python312/python.exe',[long]$AffinityMask=0)
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'UnityProject'
$source=Join-Path $PSScriptRoot '../../ChristashaParticleVisual.cs'
if((Get-FileHash $source).Hash -ne (Get-FileHash (Join-Path $project 'Assets/Scripts/ChristashaParticleVisual.cs')).Hash){throw 'Production driver differs from the built bundle candidate mirror; do not use supplemental-only export'}
$output=Join-Path $PSScriptRoot 'evidence/candidate-r3-slashv'
if(Test-Path (Join-Path $output 'candidate-manifest.json')){throw 'Historical supplemental candidate already exists; use the current full preview for a new candidate'}
New-Item -ItemType Directory -Force $output | Out-Null
$log=Join-Path $output 'unity.log'
$previous=$env:OPENSSL_ia32cap
$parentProcess=[Diagnostics.Process]::GetCurrentProcess()
$previousAffinity=$parentProcess.ProcessorAffinity
$launchedUtc=[DateTime]::UtcNow
try{
    if($AffinityMask -ne 0){$parentProcess.ProcessorAffinity=[IntPtr]$AffinityMask}
    $env:OPENSSL_ia32cap=':~0x20000000'
    $process=Start-Process -FilePath $Unity -ArgumentList @('-batchmode','-quit','-force-d3d11','-projectPath',('"'+$project+'"'),'-executeMethod','ChristashaParticleBuilder.PreviewSlashVOnly','-logFile',('"'+$log+'"')) -PassThru -WindowStyle Hidden
}finally{$env:OPENSSL_ia32cap=$previous; $parentProcess.ProcessorAffinity=$previousAffinity}
Write-Host "Supplemental-only Unity PID=$($process.Id) log=$log"
$process.WaitForExit()
if($process.ExitCode -ne 0){throw 'Supplemental SlashV export failed; old logs are not accepted'}
if(!(Test-Path $log) -or (Get-Item $log).LastWriteTimeUtc -lt $launchedUtc.AddSeconds(-1)){throw 'No fresh supplemental log'}
if(!(Select-String -LiteralPath $log -SimpleMatch 'CHRISTASHA_V2_SLASHV_PASS' -Quiet)){throw 'No supplemental success marker'}
& $Python (Join-Path $PSScriptRoot 'package_evidence.py') --revision candidate-r3-slashv
if($LASTEXITCODE -ne 0){throw 'Supplemental packaging failed'}
