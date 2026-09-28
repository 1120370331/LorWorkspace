param(
    [switch]$Quick,
    [string]$Unity = 'C:/Program Files/Unity/Editor/Unity.exe'
)
$ErrorActionPreference = 'Stop'
$taskHere = $PSScriptRoot
$taskRunId = (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + $(if ($Quick) { '-quick' } else { '-candidate' })
$taskOutput = Join-Path $taskHere "ultimate-v1/$taskRunId"
New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null
$env:TAFFY_ULTIMATE_RUN_ID = $taskRunId
$env:TAFFY_ULTIMATE_QUICK = $(if ($Quick) { '1' } else { '0' })
& python (Join-Path $taskHere 'verify_ultimate_contract.py') --after
if ($LASTEXITCODE -ne 0) { throw 'Ultimate source contract failed' }
& python (Join-Path $taskHere 'prepare_unity_fixture.py') | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Ultimate fixture preparation failed' }
$taskProject = Join-Path $taskHere 'UnityFixture'
$taskLog = Join-Path $taskOutput 'acceptance.log'
$taskProcess = Start-Process -FilePath $Unity -ArgumentList '-batchmode','-projectPath',('"'+$taskProject+'"'),'-executeMethod','TaffyUltimatePreview.Run','-logFile',('"'+$taskLog+'"') -WindowStyle Hidden -PassThru
$taskProcess.WaitForExit()
if ($taskProcess.ExitCode -ne 0) { throw "Ultimate Unity fixture exited $($taskProcess.ExitCode): $taskLog" }
if (-not (Select-String -LiteralPath $taskLog -Pattern 'TAFFY_ULTIMATE_UNITY_PASS' -Quiet)) { throw "Ultimate Unity fixture did not report PASS: $taskLog" }
if (-not $Quick) {
    & python (Join-Path $taskHere 'assemble_ultimate_preview.py') $taskRunId
    if ($LASTEXITCODE -ne 0) { throw "Ultimate media assembly failed: $taskOutput" }
}
Write-Output "ULTIMATE_PREVIEW_PATH=$taskOutput"
Write-Output "ULTIMATE_PREVIEW_LOG=$taskLog"
