param(
    [switch]$SkipBuild,
    [switch]$AllowIncompleteAssets,
    [string]$DeployTo
)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$package = Join-Path $root 'AceTaffyMod'
$managedCandidates = @(
    'D:\Game Center\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Managed',
    'C:\Program Files (x86)\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Managed'
)
$managed = $managedCandidates | Where-Object { Test-Path (Join-Path $_ 'UnityEngine.CoreModule.dll') } | Select-Object -First 1
if (-not $managed) { throw 'Library of Ruina Managed assemblies not found. Check the installed game paths in build.ps1.' }

if (-not $SkipBuild) {
    & dotnet build (Join-Path $root 'AceTaffy.csproj') -c Release "/p:AceTaffyManagedDir=$managed" --nologo
    if ($LASTEXITCODE -ne 0) { throw "AceTaffy compilation failed ($LASTEXITCODE)." }
}
$dll = Join-Path $root 'bin\Release\net472\AceTaffy.dll'
if (-not (Test-Path $dll)) { throw "Missing build output: $dll" }
$assemblies = Join-Path $package 'Assemblies'
New-Item -ItemType Directory -Force $assemblies | Out-Null
Copy-Item -LiteralPath $dll -Destination (Join-Path $assemblies 'AceTaffy.dll') -Force

# The game discovers skins from package-root Char, while Resource/CharacterSkin
# remains the authoring location. BaseMod discovers general icons from root ArtWork.
$skins = Join-Path $package 'Resource\CharacterSkin'
$skinNames = @('AceTaffy', 'AceTaffy_Projection', 'AceTaffy_Workshop')
foreach ($qualifier in 0..4) { foreach ($kind in 0..4) { $skinNames += "AceTaffy_Invention_${qualifier}_${kind}" } }
foreach ($name in $skinNames) {
    $source = Join-Path $skins $name
    if (-not (Test-Path $source)) { throw "Missing skin source: $source" }
    $target = Join-Path $package "Char\$name"
    New-Item -ItemType Directory -Force (Join-Path $target 'ClothCustom') | Out-Null
    Copy-Item -LiteralPath (Join-Path $source 'ModInfo.xml') -Destination (Join-Path $target 'ModInfo.xml') -Force
    Get-ChildItem -LiteralPath (Join-Path $source 'ClothCustom') -Filter '*.png' -File -ErrorAction SilentlyContinue | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination (Join-Path (Join-Path $target 'ClothCustom') $_.Name) -Force
    }
}
$artwork = Join-Path $package 'Resource\ArtWork'
if (Test-Path $artwork) {
    New-Item -ItemType Directory -Force (Join-Path $package 'ArtWork') | Out-Null
    Get-ChildItem -LiteralPath $artwork -Filter '*.png' -File | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination (Join-Path (Join-Path $package 'ArtWork') $_.Name) -Force
    }
}

# StorySerializer's Bg and BaseMod's CG both resolve from StoryBgSprite.
$storySource = Join-Path $package 'Resource\Story'
$storyTarget = Join-Path $package 'Resource\StoryBgSprite'
New-Item -ItemType Directory -Force $storyTarget | Out-Null
$source = Join-Path $storySource 'WorkshopInvitation.png'
$target = Join-Path $storyTarget 'AceTaffy_WorkshopInvitation.png'
if (Test-Path $source) { Copy-Item -LiteralPath $source -Destination $target -Force }
$portraitSource = Join-Path $storySource 'Characters'
$portraitTarget = Join-Path $package 'Resource\StoryStanding'
New-Item -ItemType Directory -Force $portraitTarget | Out-Null
foreach ($name in @('Taffy_Default', 'Taffy_Happy', 'Taffy_Confused')) {
    $source = Join-Path $portraitSource "$name.png"
    $target = Join-Path $portraitTarget "$name.png"
    if (Test-Path $source) { Copy-Item -LiteralPath $source -Destination $target -Force }
}

$verifyArgs = @((Join-Path $root 'Verification\verify_package.py'), '--package', $package)
if ($AllowIncompleteAssets) { $verifyArgs += '--allow-incomplete-assets' }
& python @verifyArgs
if ($LASTEXITCODE -ne 0) { throw 'Package verification failed.' }

if ($DeployTo) {
    $resolved = [System.IO.Path]::GetFullPath($DeployTo)
    $gameData = Split-Path -Parent (Split-Path -Parent $resolved)
    if ((Split-Path -Leaf (Split-Path -Parent $resolved)) -ne 'Mods' -or
        (Split-Path -Leaf $gameData) -ne 'LibraryOfRuina_Data') {
        throw 'DeployTo must be an AceTaffyMod folder inside LibraryOfRuina_Data\Mods.'
    }
    if ((Split-Path -Leaf $resolved) -ne 'AceTaffyMod') { throw 'DeployTo folder must be named AceTaffyMod.' }
    New-Item -ItemType Directory -Force $resolved | Out-Null
    foreach ($entry in Get-ChildItem -LiteralPath $package -Force) {
        Copy-Item -LiteralPath $entry.FullName -Destination $resolved -Recurse -Force
    }
    Write-Host "Deployed AceTaffyMod to $resolved"
}
Write-Host "Built package: $package"
