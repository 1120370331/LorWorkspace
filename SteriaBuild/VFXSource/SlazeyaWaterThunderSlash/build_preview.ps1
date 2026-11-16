param([ValidatePattern('^[A-Za-z0-9_-]+$')][string]$Revision='candidate-01')
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'UnityProject'
$evidence=Join-Path $PSScriptRoot ('evidence/'+$Revision)
if(Test-Path -LiteralPath $evidence){throw 'Choose a fresh revision; existing evidence is immutable'}
$normalized=[IO.Path]::GetFullPath($project).Replace('\','/')
if(Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object {$_.CommandLine -and $_.CommandLine.Replace('\','/').Contains($normalized)}){throw 'New authoring project is open; save and close it before building'}
& python -X utf8 (Join-Path $PSScriptRoot 'generate_inputs.py')
if($LASTEXITCODE -ne 0){throw 'Input generation failed'}
& python -X utf8 (Join-Path $PSScriptRoot 'verify_inputs.py')
if($LASTEXITCODE -ne 0){throw 'Input verification failed'}
New-Item -ItemType Directory -Path $evidence | Out-Null
$parent=[Diagnostics.Process]::GetCurrentProcess();$prior=$parent.ProcessorAffinity;$ssl=$env:OPENSSL_ia32cap
try {
 $parent.ProcessorAffinity=[IntPtr]4294901760;$env:OPENSSL_ia32cap=':~0x20000000'
 $p=Start-Process -FilePath 'C:/Program Files/Unity/Editor/Unity.exe' -ArgumentList @('-batchmode','-force-d3d11','-job-worker-count','4','-projectPath',('"'+$project+'"'),'-executeMethod','WaterThunderPreview.BuildAndPreview','-waterOutput',('"'+$evidence+'"'),'-logFile',('"'+(Join-Path $evidence 'acceptance.log')+'"')) -WindowStyle Hidden -PassThru
 $p.PriorityClass='BelowNormal'
} finally {$parent.ProcessorAffinity=$prior;$env:OPENSSL_ia32cap=$ssl}
@{pid=$p.Id;startedUtc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $evidence 'run.json')
$timer=[Diagnostics.Stopwatch]::StartNew()
while(!$p.WaitForExit(1000)){if($timer.Elapsed.TotalMinutes -gt 12){Stop-Process -Id $p.Id -Force;throw 'Own Unity process timed out'}}
if($p.ExitCode -ne 0){Get-Content -LiteralPath (Join-Path $evidence 'acceptance.log') -Tail 70;throw ('Unity failed '+$p.ExitCode)}
if(!(Test-Path -LiteralPath (Join-Path $evidence 'complete.json'))){Get-Content -LiteralPath (Join-Path $evidence 'acceptance.log') -Tail 70;throw 'Unity completion marker missing'}
& python -X utf8 (Join-Path $PSScriptRoot 'package_preview.py') $evidence
if($LASTEXITCODE -ne 0){throw 'Preview packaging failed'}
Write-Output ('SLAZEYA_WATER_THUNDER_PREVIEW_READY '+$evidence)
