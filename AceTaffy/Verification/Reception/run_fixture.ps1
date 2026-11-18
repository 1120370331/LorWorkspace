param([string]$Unity = 'C:/Program Files/Unity/Editor/Unity.exe', [switch]$IconsOnly, [switch]$PreviewOnly)
$ErrorActionPreference = 'Stop'
python (Join-Path $PSScriptRoot 'prepare_fixture.py')
if ($LASTEXITCODE -ne 0) { throw 'Fixture setup failed' }
$tag = if ($IconsOnly) { 'icons-' } elseif ($PreviewOnly) { 'preview-' } else { '' }
$output = Join-Path $PSScriptRoot ('output/' + $tag + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Force $output | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'candidate.json') -Destination (Join-Path $output 'candidate.json')
$env:TAFFY_RECEPTION_OUTPUT = $output
$project = Join-Path $PSScriptRoot 'UnityFixture'
$log = Join-Path $output 'unity.log'
$method = if ($IconsOnly) { 'InventionIconFixture.Run' } elseif ($PreviewOnly) { 'ReceptionFixture.Preview' } else { 'ReceptionFixture.Run' }
$process = Start-Process $Unity -ArgumentList '-batchmode','-projectPath',('"'+$project+'"'),'-executeMethod',$method,'-logFile',('"'+$log+'"') -WindowStyle Hidden -PassThru
$process.WaitForExit()
if ($process.ExitCode -ne 0 -or -not (Select-String -LiteralPath $log -Pattern 'RECEPTION_PASS' -Quiet)) { throw "Reception fixture failed: $log" }
Select-String -LiteralPath $log -Pattern 'RECEPTION_(PASS|CHECK|COVERAGE)' | ForEach-Object { $_.Line }
