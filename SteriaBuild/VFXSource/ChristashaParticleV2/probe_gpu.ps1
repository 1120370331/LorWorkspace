param([string]$Method='ChristashaParticleBuilder.DiagnoseSparseRender',[string]$LogName='diagnostic/unity.log',[string]$OpenSslCaps=':~0x20000000',[long]$AffinityMask=0)
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'UnityProject'
$log=Join-Path $PSScriptRoot ('evidence/'+$LogName)
$previous=$env:OPENSSL_ia32cap
$parentProcess=[Diagnostics.Process]::GetCurrentProcess()
$previousAffinity=$parentProcess.ProcessorAffinity
$launchedUtc=[DateTime]::UtcNow
try{
    # Temporary affinity for this launcher and its newly created Unity child only.
    if($AffinityMask -ne 0){$parentProcess.ProcessorAffinity=[IntPtr]$AffinityMask}
    $env:OPENSSL_ia32cap=$OpenSslCaps
    $process=Start-Process -FilePath 'C:/Program Files/Unity/Editor/Unity.exe' -WorkingDirectory 'C:/Program Files/Unity/Editor' -ArgumentList @('-batchmode','-quit','-force-d3d11','-projectPath',('"'+$project+'"'),'-executeMethod',$Method,'-logFile',('"'+$log+'"')) -PassThru -WindowStyle Hidden
}finally{$env:OPENSSL_ia32cap=$previous; $parentProcess.ProcessorAffinity=$previousAffinity}
Write-Host "GPU diagnostic PID=$($process.Id) log=$log"
$process.WaitForExit()
if($process.ExitCode -ne 0){throw "Diagnostic Unity exit $($process.ExitCode); previous logs are not accepted"}
if(!(Test-Path $log) -or (Get-Item $log).LastWriteTimeUtc -lt $launchedUtc.AddSeconds(-1)){throw 'No fresh Unity diagnostic log'}
if(!(Select-String -LiteralPath $log -Pattern 'CHRISTASHA_V2_.*PASS' -Quiet)){throw 'Fresh diagnostic log has no success marker'}
