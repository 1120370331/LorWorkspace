param(
    [string]$Project='C:\Users\rog\WorkSpace\projects\games\lor\output\christasha-native-redesign\reference-editor\UnityProject',
    [string]$Output='C:\Users\rog\WorkSpace\projects\games\lor\SteriaBuild\VFXSource\ChristashaNative\Mass\evidence\candidate-01',
    [string]$CoreSnapshot='C:\Users\rog\WorkSpace\projects\games\lor\SteriaBuild\VFXSource\ChristashaNative\evidence\shared-mass-hook-base\ChristashaNativeParticleLibrary.cs.txt'
)
$ErrorActionPreference='Stop'
$repo=(Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
New-Item -ItemType Directory -Force -Path (Join-Path $Project 'Assets/Editor'),(Join-Path $Project 'Assets/NativeRuntime'),$Output | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'ChristashaNativeMassPreview.cs') -Destination (Join-Path $Project 'Assets/Editor/ChristashaNativeMassPreview.cs')
foreach($name in @('ChristashaNativeParticleLibrary.cs','ChristashaNativeMassRecipes.cs','ChristashaNativePierceRecipes.cs')){
    $source=Join-Path $repo ('SteriaBuild/'+$name)
    if($name -eq 'ChristashaNativeParticleLibrary.cs'){$source=$CoreSnapshot}
    if(Test-Path -LiteralPath $source){
        Copy-Item -LiteralPath $source -Destination (Join-Path $Project ('Assets/NativeRuntime/'+$name))
        Copy-Item -LiteralPath $source -Destination (Join-Path $Output ($name+'.txt'))
    }
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'ChristashaNativeMassPreview.cs') -Destination (Join-Path $Output 'ChristashaNativeMassPreview.cs.txt')
Get-ChildItem -LiteralPath $Output -Filter '*.cs.txt' | Get-FileHash -Algorithm SHA256 | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $Output 'source-sha256.json')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../Assets/tide-brush-imagegen-v1.png') -Destination (Join-Path $Project 'Assets/NativeRuntime/tide-brush-imagegen-v1.png')
$bundle=Join-Path $repo 'output/christasha-native-redesign/native-import/native_reference_christasha_sources'
$log=Join-Path $Output 'unity.log'
$previousOpenSSL=$env:OPENSSL_ia32cap
try{
    $env:OPENSSL_ia32cap=':~0x20000000'
    $arguments=@('-batchmode','-quit','-force-d3d11','-job-worker-count','4','-projectPath',('"'+$Project+'"'),'-executeMethod','ChristashaNativeMassPreview.Render','-referenceBundle',('"'+$bundle+'"'),'-candidateOutput',('"'+$Output+'"'),'-logFile',('"'+$log+'"'))
    $process=Start-Process -FilePath 'C:/Program Files/Unity/Editor/Unity.exe' -ArgumentList $arguments -WindowStyle Hidden -PassThru
    $process.PriorityClass='BelowNormal'
    Write-Output ('UnityPID='+$process.Id+' Log='+$log)
}finally{$env:OPENSSL_ia32cap=$previousOpenSSL}
