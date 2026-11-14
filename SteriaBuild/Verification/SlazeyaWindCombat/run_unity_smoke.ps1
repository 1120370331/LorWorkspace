param(
    [Parameter(Mandatory=$true)][string]$Bundle,
    [ValidatePattern('^[A-Za-z0-9_-]+$')][string]$Revision='factory-01',
    [string]$ProjectPath=''
)
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$project=Join-Path $repo 'SteriaBuild/VFXSource/SlazeyaWindCombat/UnityProject'
if($ProjectPath){$project=[IO.Path]::GetFullPath($ProjectPath)}
$output=Join-Path $repo ('output/slazeya-wind-combat/'+$Revision)
if(Test-Path -LiteralPath $output){throw 'Use a fresh smoke revision'}
$normalizedProject=$project.Replace('\','/')
if(Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object {$_.CommandLine.Replace('\','/') -like ('*'+$normalizedProject+'*')}){throw 'Visual project is still open'}
& python (Join-Path $PSScriptRoot 'prepare_unity_smoke.py') $project $Bundle
if($LASTEXITCODE -ne 0){throw 'Smoke preparation failed'}
New-Item -ItemType Directory -Path $output | Out-Null
$inputs=@(Get-ChildItem -LiteralPath (Join-Path $project 'Assets') -Recurse -File | Where-Object {$_.Extension -in '.cs','.dll','.asmdef'})
$inputs+=Get-Item -LiteralPath $Bundle
$hashes=@($inputs | ForEach-Object {@{path=$_.FullName;sha256=(Get-FileHash -LiteralPath $_.FullName).Hash}})
$hashes | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $output 'inputs.json') -Encoding utf8
$log=Join-Path $output 'acceptance.log'
$launcher=[Diagnostics.Process]::GetCurrentProcess();$oldAffinity=$launcher.ProcessorAffinity;$oldSsl=$env:OPENSSL_ia32cap
try {
    $launcher.ProcessorAffinity=[IntPtr]4294901760;$env:OPENSSL_ia32cap=':~0x20000000'
    $arguments=@('-batchmode','-force-d3d11','-job-worker-count','4','-projectPath',('"'+$project+'"'),'-executeMethod','SlazeyaWindFactorySmoke.Run','-windSmokeOutput',('"'+$output+'"'),'-logFile',('"'+$log+'"'))
    $process=Start-Process 'C:/Program Files/Unity/Editor/Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $process.PriorityClass='BelowNormal'
} finally {$launcher.ProcessorAffinity=$oldAffinity;$env:OPENSSL_ia32cap=$oldSsl}
@{pid=$process.Id;startedUtc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'run.json')
$timer=[Diagnostics.Stopwatch]::StartNew()
while(!$process.WaitForExit(1000)){
    if($timer.Elapsed.TotalMinutes -gt 8){Stop-Process -Id $process.Id -Force;throw 'Only this smoke process was stopped after timeout'}
}
if($process.ExitCode -ne 0){Get-Content -LiteralPath $log -Tail 40;throw ('Unity smoke exit '+$process.ExitCode)}
if(!(Select-String -LiteralPath $log -SimpleMatch 'SLAZEYA_WIND_FACTORY_UNITY_PASS' -Quiet)){throw 'No actual factory PASS marker'}
foreach($entry in $hashes){if((Get-FileHash -LiteralPath $entry.path).Hash -ne $entry.sha256){throw ('Input changed during smoke: '+$entry.path)}}
@{exitCode=$process.ExitCode;completedUtc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'completion.json')
Get-Content -LiteralPath (Join-Path $output 'checks.txt')
