param([ValidatePattern('^[A-Za-z0-9_-]+$')][string]$Revision='candidate-06-downward')
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$sourceProject=Join-Path $PSScriptRoot 'UnityProject'
$project=Join-Path $repo ('output/slazeya-water-thunder/'+$Revision+'/UnityProject')
$evidence=Join-Path $PSScriptRoot ('evidence/'+$Revision)
if((Test-Path -LiteralPath $project) -or (Test-Path -LiteralPath $evidence)){throw 'Choose a fresh snapshot revision'}
& python -X utf8 (Join-Path $PSScriptRoot 'verify_inputs.py')
if($LASTEXITCODE -ne 0){throw 'Inputs failed validation'}
New-Item -ItemType Directory -Path $project,$evidence -Force | Out-Null
foreach($folder in @('Assets','Packages','ProjectSettings')){Copy-Item -LiteralPath (Join-Path $sourceProject $folder) -Destination (Join-Path $project $folder) -Recurse}
$log=Join-Path $evidence 'acceptance.log'
$parent=[Diagnostics.Process]::GetCurrentProcess();$prior=$parent.ProcessorAffinity;$ssl=$env:OPENSSL_ia32cap
try {
 $parent.ProcessorAffinity=[IntPtr]4294901760;$env:OPENSSL_ia32cap=':~0x20000000'
 $p=Start-Process -FilePath 'C:/Program Files/Unity/Editor/Unity.exe' -ArgumentList @('-batchmode','-force-d3d11','-job-worker-count','4','-projectPath',('"'+$project+'"'),'-executeMethod','WaterThunderPreview.BuildAndPreview','-waterOutput',('"'+$evidence+'"'),'-logFile',('"'+$log+'"')) -WindowStyle Hidden -PassThru
 $p.PriorityClass='BelowNormal'
} finally {$parent.ProcessorAffinity=$prior;$env:OPENSSL_ia32cap=$ssl}
@{pid=$p.Id;project=$project;startedUtc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $evidence 'run.json')
$timer=[Diagnostics.Stopwatch]::StartNew()
while(!$p.WaitForExit(1000)){if($timer.Elapsed.TotalMinutes -gt 12){Stop-Process -Id $p.Id -Force;throw 'Own snapshot timed out'}}
if($p.ExitCode -ne 0){Select-String -LiteralPath $log -Pattern 'error CS|Exception:|CHECK|error:' | Select-Object -Last 20;throw ('Snapshot Unity failed '+$p.ExitCode)}
if(!(Test-Path -LiteralPath (Join-Path $evidence 'complete.json'))){throw 'Completion marker missing'}
& python -X utf8 (Join-Path $PSScriptRoot 'package_preview.py') $evidence $project
if($LASTEXITCODE -ne 0){throw 'Media packaging failed'}
Write-Output ('WATER_THUNDER_SNAPSHOT_PASS '+$evidence)
