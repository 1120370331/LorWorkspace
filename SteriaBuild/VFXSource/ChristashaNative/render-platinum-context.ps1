param([string]$Project = 'C:/Users/rog/WorkSpace/projects/games/lor/output/christasha-native-redesign/reference-editor/UnityProject')
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$contextOutput = Join-Path $PSScriptRoot 'evidence/slash-candidate-11'
foreach ($name in @('ChristashaPlatinumContext.cs','ChristashaPlatinumCleanContext.cs','ChristashaPlatinumContextBatch.cs')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot ('Editor/' + $name)) -Destination (Join-Path $Project ('Assets/Editor/' + $name))
}
Copy-Item -LiteralPath (Join-Path $repo 'SteriaBuild/ChristashaNativeParticleLibrary.cs') -Destination (Join-Path $Project 'Assets/NativeRuntime/ChristashaNativeParticleLibrary.cs')
$savedOpenSSL = $env:OPENSSL_ia32cap
try {
    $env:OPENSSL_ia32cap = ':~0x20000000'
    $arguments = @('-batchmode','-quit','-force-d3d11','-job-worker-count','4','-projectPath',('"'+$Project+'"'),'-executeMethod','ChristashaPlatinumContextBatch.Render','-logFile',('"'+$contextOutput+'/context-batch.log"'))
    $process = Start-Process -FilePath 'C:/Program Files/Unity/Editor/Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $process.PriorityClass = 'BelowNormal'
    Write-Output ('UnityPID=' + $process.Id + '; workers=4; priority=' + $process.PriorityClass)
} finally { $env:OPENSSL_ia32cap = $savedOpenSSL }
