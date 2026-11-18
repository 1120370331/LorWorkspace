param([string]$Unity = 'C:/Program Files/Unity/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
& python (Join-Path $taskRoot 'prepare_unity_fixture.py')
if ($LASTEXITCODE -ne 0) { throw 'Preparation fixture setup failed' }
$project = Join-Path $taskRoot 'UnityFixture'
$output = Join-Path $taskRoot ('output/' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Force $output | Out-Null
$env:TAFFY_PREPARATION_OUTPUT_DIR = $output
$log = Join-Path $output 'unity.log'
$process = Start-Process -FilePath $Unity -ArgumentList '-batchmode','-projectPath',('"' + $project + '"'),'-executeMethod','TaffyPreparationFixture.Run','-logFile',('"' + $log + '"') -WindowStyle Hidden -PassThru
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Preparation fixture failed with exit $($process.ExitCode): $log" }
if (-not (Select-String -LiteralPath $log -Pattern 'TAFFY_PREPARATION_PASS' -Quiet)) { throw "Missing PASS marker: $log" }
Select-String -LiteralPath $log -Pattern 'TAFFY_PREPARATION_(PASS|CHECK)' | ForEach-Object { $_.Line }
