param(
    [string]$Project = 'C:\Users\rog\WorkSpace\projects\games\lor\output\christasha-native-redesign\reference-editor\UnityProject',
    [string]$Output = 'C:\Users\rog\WorkSpace\projects\games\lor\SteriaBuild\VFXSource\ChristashaNative\evidence\hit-slam-candidate-01'
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
New-Item -ItemType Directory -Force -Path (Join-Path $Project 'Assets/Editor'), (Join-Path $Project 'Assets/NativeRuntime'), $Output | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Editor/ChristashaNativeHitPreview.cs') -Destination (Join-Path $Project 'Assets/Editor/ChristashaNativeHitPreview.cs')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Editor/ChristashaNativeSlashRegression.cs') -Destination (Join-Path $Project 'Assets/Editor/ChristashaNativeSlashRegression.cs')
Copy-Item -LiteralPath (Join-Path $repo 'SteriaBuild/ChristashaNativeParticleLibrary.cs') -Destination (Join-Path $Project 'Assets/NativeRuntime/ChristashaNativeParticleLibrary.cs')
Copy-Item -LiteralPath (Join-Path $repo 'SteriaBuild/ChristashaNativeMassRecipes.cs') -Destination (Join-Path $Project 'Assets/NativeRuntime/ChristashaNativeMassRecipes.cs')
Copy-Item -LiteralPath (Join-Path $repo 'SteriaBuild/ChristashaNativePierceRecipes.cs') -Destination (Join-Path $Project 'Assets/NativeRuntime/ChristashaNativePierceRecipes.cs')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Assets/tide-brush-imagegen-v1.png') -Destination (Join-Path $Project 'Assets/NativeRuntime/tide-brush-imagegen-v1.png')
$bundle = Join-Path $repo 'output/christasha-native-redesign/native-import/native_reference_christasha_sources'
$log = Join-Path $Output 'unity.log'
$previousOpenSSL = $env:OPENSSL_ia32cap
try {
    $env:OPENSSL_ia32cap = ':~0x20000000'
    $arguments = @('-batchmode', '-quit', '-force-d3d11', '-job-worker-count', '4', '-projectPath', ('"' + $Project + '"'), '-executeMethod', 'ChristashaNativeSlashRegression.RegressionThenHit', '-referenceBundle', ('"' + $bundle + '"'), '-candidateOutput', ('"' + $Output + '"'), '-logFile', ('"' + $log + '"'))
    $process = Start-Process -FilePath 'C:/Program Files/Unity/Editor/Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $process.PriorityClass = 'BelowNormal'
    Write-Output ('UnityPID=' + $process.Id + ' Log=' + $log)
} finally { $env:OPENSSL_ia32cap = $previousOpenSSL }
