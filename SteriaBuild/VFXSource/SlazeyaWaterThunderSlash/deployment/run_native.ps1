param([Parameter(Mandatory=$true)][string]$Release,[Parameter(Mandatory=$true)][string]$Revision)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../..'))
& python -X utf8 (Join-Path $PSScriptRoot 'prepare_native.py') $Release $Revision
if($LASTEXITCODE -ne 0){throw 'Native fixture preparation failed'}
$out=Join-Path $repo ('output/slazeya-water-thunder/'+$Revision);$project=Join-Path $out 'UnityProject'
$log=Join-Path $out 'acceptance.log'
$parent=[Diagnostics.Process]::GetCurrentProcess();$prior=$parent.ProcessorAffinity;$ssl=$env:OPENSSL_ia32cap
try {
 $parent.ProcessorAffinity=[IntPtr]4294901760;$env:OPENSSL_ia32cap=':~0x20000000'
 $p=Start-Process -FilePath 'C:/Program Files/Unity/Editor/Unity.exe' -ArgumentList @('-batchmode','-force-d3d11','-job-worker-count','4','-projectPath',('"'+$project+'"'),'-executeMethod','WaterThunderNativeEntry.Run','-nativeOutput',('"'+$out+'"'),'-logFile',('"'+$log+'"')) -WindowStyle Hidden -PassThru
 $p.PriorityClass='BelowNormal'
} finally {$parent.ProcessorAffinity=$prior;$env:OPENSSL_ia32cap=$ssl}
@{pid=$p.Id;project=$project;startedUtc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'run.json')
$timer=[Diagnostics.Stopwatch]::StartNew()
while(!$p.WaitForExit(1000)){if($timer.Elapsed.TotalMinutes -gt 10){Stop-Process -Id $p.Id -Force;throw 'Own native fixture timed out'}}
if($p.ExitCode -ne 0 -or !(Test-Path -LiteralPath (Join-Path $out 'checks.txt'))){Select-String -LiteralPath $log -Pattern 'error CS|Exception|FAIL|NATIVE_PASS' | Select-Object -Last 25;throw 'Native integration failed'}
Get-Content -LiteralPath (Join-Path $out 'checks.txt')
