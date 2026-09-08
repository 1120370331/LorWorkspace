param([string]$Unity='C:/Program Files/Unity/Editor/Unity.exe',[string]$ProjectPath,[string]$OutputDirectory)
$ErrorActionPreference='Stop'
$taskRepo=(Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
if (-not $ProjectPath) { $ProjectPath=Join-Path $taskRepo 'output/music-dice-ui/implementation/UnityProject' }
if (-not $OutputDirectory) { $OutputDirectory=Join-Path $taskRepo 'output/music-dice-ui/authoring-v1-regenerated' }
$taskProject=(Resolve-Path -LiteralPath $ProjectPath).Path
$taskEditor=Join-Path $taskProject 'Assets/Editor'
if (-not (Test-Path -LiteralPath $taskEditor)) { throw 'Use an existing isolated Unity 2019 Gamma project with Unity UI and an Assets/Editor folder.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'MusicDiceMaterialAuthorV1.cs') -Destination (Join-Path $taskEditor 'MusicGeneratedMaterialImport.cs')
$taskOutput=[IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force $taskOutput | Out-Null
$taskLog=Join-Path $taskOutput 'unity-authoring.log'
$taskProcess=Start-Process $Unity -ArgumentList '-batchmode','-projectPath',('"'+$taskProject+'"'),'-executeMethod','MusicGeneratedMaterialImport.Run','-musicAuthorRoot',('"'+$PSScriptRoot+'"'),'-musicAuthorOutput',('"'+$taskOutput+'"'),'-logFile',('"'+$taskLog+'"') -WindowStyle Hidden -PassThru
$taskProcess.WaitForExit()
if ($taskProcess.ExitCode -ne 0) { throw "Authoring failed: $taskLog" }
if (-not (Select-String -LiteralPath $taskLog -Pattern 'MUSIC_GENERATED_IMPORT_READY' -Quiet)) { throw 'Authoring did not report completion' }
$taskContract=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Authoring/v1/provenance.json') -Raw | ConvertFrom-Json
foreach ($taskName in @('Card.png','BlankFrame.png','Glyph.png')) {
    $taskActual=(Get-FileHash -LiteralPath (Join-Path $taskOutput $taskName) -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($taskActual -ne $taskContract.approved_rgba.$taskName) { throw "Reproduced artwork differs from reviewed v1: $taskName. Inspect before promoting." }
}
Get-Content -LiteralPath (Join-Path $taskOutput 'processing-report.txt')
