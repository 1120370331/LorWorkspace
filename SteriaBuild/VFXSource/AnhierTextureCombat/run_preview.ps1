param([string]$Python = 'C:/Users/rog/AppData/Local/Programs/Python/Python312/python.exe', [string]$Unity = 'C:/Program Files/Unity/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
& $Python (Join-Path $PSScriptRoot 'verify_source.py')
if ($LASTEXITCODE -ne 0) { throw 'Source verifier failed' }
& $Python (Join-Path $PSScriptRoot 'prepare_preview.py')
if ($LASTEXITCODE -ne 0) { throw 'Preview fixture preparation failed' }
$taskProject = Join-Path $PSScriptRoot 'UnityProject'
$taskLog = Join-Path $PSScriptRoot 'previews/unity.log'
$taskProcess = Start-Process $Unity -ArgumentList '-batchmode','-projectPath',('"'+$taskProject+'"'),'-executeMethod','AnhierTexturePreview.Run','-logFile',('"'+$taskLog+'"') -WindowStyle Hidden -PassThru
$taskProcess.WaitForExit()
if ($taskProcess.ExitCode -ne 0) { throw "Unity fixture failed: $taskLog" }
if (-not (Select-String -LiteralPath $taskLog -Pattern 'ANHIER_TEXTURE_FIXTURE_PASS' -Quiet)) { throw 'Unity did not report completion' }
& $Python (Join-Path $PSScriptRoot 'assemble_previews.py')
if ($LASTEXITCODE -ne 0) { throw 'Preview contact/GIF assembly failed' }
Get-Content (Join-Path $PSScriptRoot 'previews/checks.txt')
