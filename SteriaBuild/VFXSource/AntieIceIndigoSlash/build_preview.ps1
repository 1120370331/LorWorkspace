param([ValidatePattern('^[A-Za-z0-9_-]+$')][string]$Revision='candidate-01')
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'UnityProject';$out=Join-Path $PSScriptRoot ('evidence/'+$Revision)
if(Test-Path -LiteralPath $out){throw 'Choose a fresh evidence revision'}
$normalized=[IO.Path]::GetFullPath($project).Replace('\','/')
if(Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object {$_.CommandLine -and $_.CommandLine.Replace('\','/').Contains($normalized)}){throw 'Antie project is open; use a source snapshot'}
& python -X utf8 (Join-Path $PSScriptRoot 'verify_inputs.py')
if($LASTEXITCODE -ne 0){throw 'Source image verification failed'}
New-Item -ItemType Directory -Path $out | Out-Null
$parent=[Diagnostics.Process]::GetCurrentProcess();$prior=$parent.ProcessorAffinity;$ssl=$env:OPENSSL_ia32cap
try {
 $parent.ProcessorAffinity=[IntPtr]4294901760;$env:OPENSSL_ia32cap=':~0x20000000'
 $p=Start-Process -FilePath 'C:/Program Files/Unity/Editor/Unity.exe' -ArgumentList @('-batchmode','-force-d3d11','-job-worker-count','4','-projectPath',('"'+$project+'"'),'-executeMethod','AntiePreview.Run','-antieOutput',('"'+$out+'"'),'-logFile',('"'+(Join-Path $out 'acceptance.log')+'"')) -WindowStyle Hidden -PassThru
 $p.PriorityClass='BelowNormal'
} finally {$parent.ProcessorAffinity=$prior;$env:OPENSSL_ia32cap=$ssl}
@{pid=$p.Id;project=$project;startedUtc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'run.json')
$timer=[Diagnostics.Stopwatch]::StartNew()
while(!$p.WaitForExit(1000)){if($timer.Elapsed.TotalMinutes -gt 12){Stop-Process -Id $p.Id -Force;throw 'Own Antie preview timed out'}}
if($p.ExitCode -ne 0 -or !(Test-Path -LiteralPath (Join-Path $out 'complete.json'))){Select-String -LiteralPath (Join-Path $out 'acceptance.log') -Pattern 'error CS|Shader error|Exception:|ANTIE_CHECK' | Select-Object -Last 20;throw 'Antie Unity preview failed'}
& python -X utf8 (Join-Path $PSScriptRoot 'package_preview.py') $out
if($LASTEXITCODE -ne 0){throw 'Antie media packaging failed'}
Write-Output ('ANTIE_PREVIEW_READY '+$out)
