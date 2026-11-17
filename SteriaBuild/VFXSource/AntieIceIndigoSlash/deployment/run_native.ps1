param([Parameter(Mandatory=$true)][string]$Release,[Parameter(Mandatory=$true)][ValidatePattern('^[A-Za-z0-9_-]+$')][string]$Revision)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../..'))
& python -X utf8 (Join-Path $PSScriptRoot 'prepare_native.py') $Release $Revision
if($LASTEXITCODE -ne 0){throw 'Scoped native fixture preparation failed'}
$out=Join-Path $repo ('output/antie-ice-indigo/'+$Revision);$project=Join-Path $out 'UnityProject';$log=Join-Path $out 'acceptance.log'
$parent=[Diagnostics.Process]::GetCurrentProcess();$prior=$parent.ProcessorAffinity;$ssl=$env:OPENSSL_ia32cap
try {
 $parent.ProcessorAffinity=[IntPtr]4294901760;$env:OPENSSL_ia32cap=':~0x20000000'
 $p=Start-Process -FilePath 'C:/Program Files/Unity/Editor/Unity.exe' -ArgumentList @('-batchmode','-force-d3d11','-job-worker-count','4','-projectPath',('"'+$project+'"'),'-executeMethod','AntieNativeEntry.Run','-nativeOutput',('"'+$out+'"'),'-logFile',('"'+$log+'"')) -WindowStyle Hidden -PassThru
 $p.PriorityClass='BelowNormal'
} finally {$parent.ProcessorAffinity=$prior;$env:OPENSSL_ia32cap=$ssl}
$timer=[Diagnostics.Stopwatch]::StartNew()
while(!$p.WaitForExit(1000)){if($timer.Elapsed.TotalMinutes -gt 10){Stop-Process -Id $p.Id -Force;throw 'Own scoped native fixture timed out'}}
if($p.ExitCode -ne 0 -or !(Test-Path -LiteralPath (Join-Path $out 'checks.txt'))){
 if(Test-Path -LiteralPath (Join-Path $out 'failure.txt')){Get-Content -LiteralPath (Join-Path $out 'failure.txt')}
 else{Select-String -LiteralPath $log -Pattern 'error CS|Exception:|ANTIE_NATIVE_CHECK' | Select-Object -Last 12}
 throw 'Scoped native verification failed'
}
Get-Content -LiteralPath (Join-Path $out 'checks.txt')
