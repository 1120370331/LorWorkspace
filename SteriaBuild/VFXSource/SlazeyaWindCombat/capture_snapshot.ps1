param([ValidatePattern('^[A-Za-z0-9_-]+$')][string]$Revision='candidate-10-swept')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$sourceProject=Join-Path $PSScriptRoot 'UnityProject'
$project=Join-Path $repo ('output/slazeya-wind-combat/'+$Revision+'-project/UnityProject')
$output=Join-Path $PSScriptRoot ('evidence/'+$Revision)
if((Test-Path -LiteralPath $project) -or (Test-Path -LiteralPath $output)){throw 'Choose a fresh snapshot revision'}
New-Item -ItemType Directory -Path $project,$output -Force | Out-Null
foreach($folder in @('Assets','Packages','ProjectSettings')){Copy-Item -LiteralPath (Join-Path $sourceProject $folder) -Destination (Join-Path $project $folder) -Recurse}
$inputs=@(Get-ChildItem -LiteralPath (Join-Path $sourceProject 'Assets') -Recurse -File | Where-Object {$_.Extension -in '.cs','.shader','.asmdef','.png','.json','.mat'})
$inputs+=Get-Item -LiteralPath (Join-Path $repo 'SteriaBuild/SlazeyaWindVisual.cs')
$hashes=@($inputs | ForEach-Object {@{path=$_.FullName;sha256=(Get-FileHash -LiteralPath $_.FullName).Hash}})
$hashes | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $output 'source-hashes.json') -Encoding utf8
$log=Join-Path $output 'acceptance.log'
$launcher=[Diagnostics.Process]::GetCurrentProcess();$oldAffinity=$launcher.ProcessorAffinity;$oldSsl=$env:OPENSSL_ia32cap
try{
 $launcher.ProcessorAffinity=[IntPtr]4294901760;$env:OPENSSL_ia32cap=':~0x20000000'
 $arguments=@('-batchmode','-force-d3d11','-job-worker-count','4','-projectPath',('"'+$project+'"'),'-executeMethod','SlazeyaWindPreview.BuildAndPreview','-windOutput',('"'+$output+'"'),'-logFile',('"'+$log+'"'))
 $process=Start-Process 'C:/Program Files/Unity/Editor/Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
 $process.PriorityClass='BelowNormal'
}finally{$launcher.ProcessorAffinity=$oldAffinity;$env:OPENSSL_ia32cap=$oldSsl}
@{pid=$process.Id;project=$project;startedUtc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'run.json')
$timer=[Diagnostics.Stopwatch]::StartNew()
while(!$process.WaitForExit(1000)){if($timer.Elapsed.TotalMinutes -gt 15){Stop-Process -Id $process.Id -Force;throw 'Own snapshot process timed out'}}
if($process.ExitCode -ne 0){Get-Content -LiteralPath $log -Tail 35;throw ('Unity snapshot exit '+$process.ExitCode)}
if(!(Select-String -LiteralPath $log -SimpleMatch 'SLAZEYA_WIND_NATIVE_PASS' -Quiet)){throw 'No native completion marker'}
foreach($entry in $hashes){if((Get-FileHash -LiteralPath $entry.path).Hash -ne $entry.sha256){throw ('Authoring inputs changed during capture: '+$entry.path)}}
& python (Join-Path $PSScriptRoot 'verify_pixels.py') $output
if($LASTEXITCODE -ne 0){throw 'Native pixel visibility gate failed'}
@{exitCode=$process.ExitCode;project=$project;completedUtc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'completion.json')
& python (Join-Path $PSScriptRoot 'package_preview.py') $output
if($LASTEXITCODE -ne 0){throw 'Packaging failed'}
Write-Output ('SLAZEYA_SNAPSHOT_PASS '+$output)
