param([switch]$StageCurrentDll,[switch]$SkipRender)
$ErrorActionPreference = 'Stop'
$borderRepo = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
Push-Location $borderRepo
try {
    $borderOutput = 'output/ocean-current-vfx/ui'
    New-Item -ItemType Directory -Force "$borderOutput/candidate", "$borderOutput/managed" | Out-Null
    if ($StageCurrentDll) { Copy-Item 'SteriaBuild/bin/Release/net472/Steria.dll' "$borderOutput/candidate/Steria.dll" }
    if (!(Test-Path "$borderOutput/candidate/Steria.dll")) { throw 'Build the production candidate and stage its DLL first.' }
    & dotnet build 'SteriaBuild/VFXSource/OceanCurrentBorder/Mechanics.csproj' -v quiet -clp:ErrorsOnly
    if ($LASTEXITCODE -ne 0) { throw 'Actual game mechanics fixture compile failed' }
    & 'SteriaBuild/VFXSource/OceanCurrentBorder/bin/Debug/net472/FlowBorderMechanics.exe' | Tee-Object -FilePath "$borderOutput/managed/real-mechanics-checks.txt"
    if ($LASTEXITCODE -ne 0) { throw 'Actual game mechanics checks failed' }
    & python 'SteriaBuild/VFXSource/OceanCurrentBorder/prepare_managed.py'
    if ($LASTEXITCODE -ne 0) { throw 'Production-hook extraction failed' }
    & dotnet run --project 'SteriaBuild/VFXSource/OceanCurrentBorder/ManagedFixture/Managed.csproj'
    if ($LASTEXITCODE -ne 0) { throw 'Production UI mesh checks failed' }
    if (!$SkipRender) {
        & python 'SteriaBuild/VFXSource/OceanCurrentBorder/render_managed.py'
        if ($LASTEXITCODE -ne 0) { throw 'CPU preview compositing failed' }
        $borderEncoder = & python -c "import imageio_ffmpeg; print(imageio_ffmpeg.get_ffmpeg_exe())"
        if ($LASTEXITCODE -ne 0) { throw 'imageio-ffmpeg encoder is required' }
        & $borderEncoder -hide_banner -loglevel error -y -framerate 30 -i "$borderOutput/managed/frames/current-%03d.png" -c:v libx264 -pix_fmt yuv420p "$borderOutput/managed/normal-speed.mp4"
        if ($LASTEXITCODE -ne 0) { throw 'Preview encoding failed' }
    }
} finally { Pop-Location }
