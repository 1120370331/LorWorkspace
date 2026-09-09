param([switch]$Preview, [string]$Unity = 'C:/Program Files/Unity/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
$flowRepo = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
Push-Location $flowRepo
try {
    & dotnet build 'SteriaBuild/Steria.csproj' -c Release --no-restore -v quiet -clp:ErrorsOnly
    if ($LASTEXITCODE -ne 0) { throw 'Product build failed' }
    & dotnet build 'SteriaBuild/Verification/FlowChecks/Checks.csproj' -v quiet -clp:ErrorsOnly
    if ($LASTEXITCODE -ne 0) { throw 'Check harness build failed' }
    $flowOutput = Join-Path $flowRepo 'output/flow-rework'
    New-Item -ItemType Directory -Force -Path $flowOutput | Out-Null
    & 'SteriaBuild/Verification/FlowChecks/bin/Debug/net472/Checks.exe' | Tee-Object -FilePath (Join-Path $flowOutput 'mechanics-checks.txt')
    if ($LASTEXITCODE -ne 0) { throw 'Mechanics checks failed' }
    if ($Preview) {
        & python 'SteriaBuild/Verification/FlowChecks/prepare_preview.py'
        if ($LASTEXITCODE -ne 0) { throw 'Preview preparation failed' }
        $flowProject = Join-Path $flowOutput 'UnityProject'
        $flowLog = Join-Path $flowOutput ('unity-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.log')
        $flowProcess = Start-Process $Unity -ArgumentList '-batchmode','-projectPath',('"'+$flowProject+'"'),'-executeMethod','FlowFixture.Run','-logFile',('"'+$flowLog+'"') -WindowStyle Hidden -PassThru
        Write-Output "Unity preview running: PID $($flowProcess.Id), log $flowLog"
    }
} finally { Pop-Location }
