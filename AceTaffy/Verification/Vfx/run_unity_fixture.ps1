param([string]$Unity = 'C:/Program Files/Unity/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
$taskHere = $PSScriptRoot
& python (Join-Path $taskHere 'prepare_unity_fixture.py')
if ($LASTEXITCODE -ne 0) { throw 'Fixture preparation failed' }
$taskProject = Join-Path $taskHere 'UnityFixture'
$taskOutput = Join-Path $taskHere 'unity-preview'
New-Item -ItemType Directory -Path $taskOutput -Force | Out-Null
$taskLog = Join-Path $taskOutput 'unity.log'
$taskProcess = Start-Process -FilePath $Unity -ArgumentList '-batchmode','-projectPath',('"'+$taskProject+'"'),'-executeMethod','TaffyDispatchPreview.Run','-logFile',('"'+$taskLog+'"') -WindowStyle Hidden -PassThru
$taskProcess.WaitForExit()
if ($taskProcess.ExitCode -ne 0) { throw "Unity fixture exited $($taskProcess.ExitCode): $taskLog" }
if (-not (Select-String -LiteralPath $taskLog -Pattern 'TAFFY_UNITY_FACTORY_PASS' -Quiet)) { throw "Unity fixture did not report PASS: $taskLog" }
Get-Content (Join-Path $taskOutput 'dispatch-evidence.txt')
