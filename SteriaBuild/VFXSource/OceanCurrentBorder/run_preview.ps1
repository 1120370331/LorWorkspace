param([string]$Unity = 'C:/Program Files/Unity/Editor/Unity.exe')
$ErrorActionPreference = 'Stop'
$borderRepo = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
Push-Location $borderRepo
try {
    & dotnet build 'SteriaBuild/VFXSource/OceanCurrentBorder/Checks.csproj' -v quiet -clp:ErrorsOnly
    if ($LASTEXITCODE -ne 0) { throw 'Border fixture compile failed' }
    & python 'SteriaBuild/VFXSource/OceanCurrentBorder/prepare_preview.py'
    if ($LASTEXITCODE -ne 0) { throw 'Border preview preparation failed' }
    $borderOutput = Join-Path $borderRepo 'output/ocean-current-vfx/ui'
    $borderProject = Join-Path $borderOutput 'UnityProject'
    $borderLog = Join-Path $borderOutput ('unity-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.log')
    $borderProcess = Start-Process $Unity -ArgumentList '-batchmode','-projectPath',('"'+$borderProject+'"'),'-executeMethod','FlowBorderFixture.Run','-logFile',('"'+$borderLog+'"') -WindowStyle Hidden -PassThru
    Write-Output "Unity border fixture running: PID $($borderProcess.Id), log $borderLog"
} finally { Pop-Location }
