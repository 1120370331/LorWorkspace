param([ValidatePattern('^[A-Za-z0-9_-]+$')][string]$Revision='candidate-01',[long]$AffinityMask=4294901760)
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot 'UnityProject'
$output=Join-Path $PSScriptRoot ('evidence/'+$Revision)
if(Test-Path -LiteralPath $output){throw 'Evidence exists; choose a fresh revision'}
$normalizedProject=[IO.Path]::GetFullPath($project).Replace('\','/')
if(Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" | Where-Object {$_.CommandLine.Replace('\','/') -like ('*'+$normalizedProject+'*')}){throw 'This Unity project is already open'}
& python (Join-Path $PSScriptRoot 'prepare_project.py')
if($LASTEXITCODE -ne 0){throw 'Project preparation failed'}
& python (Join-Path $PSScriptRoot 'verify_source.py')
if($LASTEXITCODE -ne 0){throw 'Source contract failed'}
New-Item -ItemType Directory -Path $output | Out-Null
$inputs=@(Get-ChildItem -LiteralPath (Join-Path $project 'Assets') -Recurse -File | Where-Object {$_.Extension -in '.cs','.shader','.asmdef','.png','.json' -or ($_.Extension -eq '.mat' -and $_.Name -like 'Wind*')})
$inputs+=Get-Item -LiteralPath (Join-Path $PSScriptRoot '../../SlazeyaWindVisual.cs'),(Join-Path $PSScriptRoot 'source_assets/wind-local-brush-v1.png'),(Join-Path $project 'ProjectSettings/TagManager.asset'),(Join-Path $project 'ProjectSettings/ProjectVersion.txt'),(Join-Path $project 'Packages/manifest.json')
$hashes=@($inputs | ForEach-Object {@{path=$_.FullName;sha256=(Get-FileHash -LiteralPath $_.FullName).Hash}})
$hashes | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $output 'source-hashes.json') -Encoding utf8
$parentProcess=[Diagnostics.Process]::GetCurrentProcess();$priorAffinity=$parentProcess.ProcessorAffinity;$priorSsl=$env:OPENSSL_ia32cap
$log=Join-Path $output 'acceptance.log'
try {
 $parentProcess.ProcessorAffinity=[IntPtr]$AffinityMask;$env:OPENSSL_ia32cap=':~0x20000000'
 $arguments=@('-batchmode','-force-d3d11','-job-worker-count','4','-projectPath',('"'+$project+'"'),'-executeMethod','SlazeyaWindPreview.BuildAndPreview','-windOutput',('"'+$output+'"'),'-logFile',('"'+$log+'"'))
 $process=Start-Process -FilePath 'C:/Program Files/Unity/Editor/Unity.exe' -ArgumentList $arguments -PassThru -WindowStyle Hidden
 $process.PriorityClass='BelowNormal'
} finally {$parentProcess.ProcessorAffinity=$priorAffinity;$env:OPENSSL_ia32cap=$priorSsl}
@{pid=$process.Id;startedUtc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'run.json')
$timer=[Diagnostics.Stopwatch]::StartNew()
while(!$process.WaitForExit(1000)){if($timer.Elapsed.TotalMinutes -gt 15){Stop-Process -Id $process.Id -Force;throw 'Own preview timed out'}}
if($process.ExitCode -ne 0){Get-Content -LiteralPath $log -Tail 65;throw ('Unity failed '+$process.ExitCode)}
if(!(Select-String -LiteralPath $log -SimpleMatch 'SLAZEYA_WIND_NATIVE_PASS' -Quiet)){throw 'Native marker missing'}
@{pid=$process.Id;exitCode=$process.ExitCode;completedUtc=[DateTime]::UtcNow.ToString('o')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'completion.json')
foreach($entry in $hashes){if((Get-FileHash -LiteralPath $entry.path).Hash -ne $entry.sha256){throw ('Source drift '+$entry.path)}}
& python (Join-Path $PSScriptRoot 'verify_pixels.py') $output
if($LASTEXITCODE -ne 0){throw 'Native pixel visibility gate failed'}
& python (Join-Path $PSScriptRoot 'package_preview.py') $output
if($LASTEXITCODE -ne 0){throw 'Media packaging failed'}
Write-Host ('SLAZEYA_BUILD_PREVIEW_PASS '+$output)
