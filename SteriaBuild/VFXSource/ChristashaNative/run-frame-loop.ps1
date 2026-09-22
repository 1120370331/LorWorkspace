param(
    [Parameter(Mandatory=$true)][string]$Output,
    [string]$Project = 'C:\Users\rog\WorkSpace\projects\games\lor\output\christasha-native-redesign\reference-editor\UnityProject',
    [switch]$RequireStable,
    [switch]$ModelOnly
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$Output = [IO.Path]::GetFullPath($Output)
if (Test-Path -LiteralPath $Output) { throw "Choose a fresh evidence directory: $Output" }
New-Item -ItemType Directory -Path $Output | Out-Null
$inputs = @()
foreach ($name in @('ChristashaNativeParticleLibrary.cs','ChristashaNativeMassRecipes.cs','ChristashaNativePierceRecipes.cs','ChristashaProjectileArc.cs','ChristashaThrustEnvelope.cs')) {
    $source = Join-Path $repo ('SteriaBuild/' + $name)
    Copy-Item -LiteralPath $source -Destination (Join-Path $Project ('Assets/NativeRuntime/' + $name))
    Copy-Item -LiteralPath $source -Destination (Join-Path $Output ($name + '.txt'))
    $inputs += Get-FileHash -LiteralPath $source -Algorithm SHA256
}
foreach ($entry in @(@('Diagnostics/ChristashaFrameLoopProbe.cs','Assets/NativeRuntime/ChristashaFrameLoopProbe.cs'),@('Editor/ChristashaFrameLoopTest.cs','Assets/Editor/ChristashaFrameLoopTest.cs'),@('Assets/tide-brush-imagegen-v1.png','Assets/NativeRuntime/tide-brush-imagegen-v1.png'))) {
    $source = Join-Path $PSScriptRoot $entry[0]
    Copy-Item -LiteralPath $source -Destination (Join-Path $Project $entry[1])
    $inputs += Get-FileHash -LiteralPath $source -Algorithm SHA256
}
foreach ($entry in @(@('Shaders/PlatinumStrike.shader','Assets/NativeRuntime/PlatinumStrike.shader'),@('Editor/ChristashaStrikeShaderBuilder.cs','Assets/Editor/ChristashaStrikeShaderBuilder.cs'),@('Editor/ChristashaStrikeModelStudy.cs','Assets/Editor/ChristashaStrikeModelStudy.cs'))) {
    $source = Join-Path $PSScriptRoot $entry[0]
    Copy-Item -LiteralPath $source -Destination (Join-Path $Project $entry[1])
    Copy-Item -LiteralPath $source -Destination (Join-Path $Output ([IO.Path]::GetFileName($source)))
    $inputs += Get-FileHash -LiteralPath $source -Algorithm SHA256
}
$bundle = Join-Path $repo 'output/christasha-native-redesign/native-import/native_reference_christasha_sources'
$skin = Join-Path $repo 'SteriaBuild/SteriaModFolder/Resource/CharacterSkin/Christasha'
foreach ($file in @('ModInfo.xml','ClothCustom/Penetrate.png','ClothCustom/Damaged.png')) { $inputs += Get-FileHash -LiteralPath (Join-Path $skin $file) -Algorithm SHA256 }
$inputs += Get-FileHash -LiteralPath $bundle -Algorithm SHA256
$inputs | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Output 'inputs.json')
$oldOpenSSL = $env:OPENSSL_ia32cap
$launcher = [Diagnostics.Process]::GetCurrentProcess()
$oldAffinity = $launcher.ProcessorAffinity
try {
    $env:OPENSSL_ia32cap = ':~0x20000000'
    $launcher.ProcessorAffinity = [IntPtr]4294901760
    $arguments = @('-batchmode','-force-d3d11','-job-worker-count','4','-projectPath',('"'+$Project+'"'),'-executeMethod','ChristashaFrameLoopTest.Run','-referenceBundle',('"'+$bundle+'"'),'-candidateOutput',('"'+$Output+'"'),'-logFile',('"'+(Join-Path $Output 'unity.log')+'"'))
    if ($RequireStable) { $arguments += '-requireStable' }
    if ($ModelOnly) { $arguments += '-modelOnly' }
    $arguments += @('-characterSkin',('"'+$skin+'"'))
    $process = Start-Process -FilePath 'C:/Program Files/Unity/Editor/Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $process.PriorityClass = 'BelowNormal'
    Write-Output ('UnityPID=' + $process.Id + ' Log=' + (Join-Path $Output 'unity.log'))
} finally {
    $launcher.ProcessorAffinity = $oldAffinity
    $env:OPENSSL_ia32cap = $oldOpenSSL
}
