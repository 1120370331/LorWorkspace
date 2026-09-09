param([string]$Python = 'python', [switch]$BuildRelease)
$ErrorActionPreference = 'Stop'
$taskRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$taskOut = Join-Path $PSScriptRoot 'previews'
New-Item -ItemType Directory -Force -Path $taskOut | Out-Null

& $Python -X utf8 (Join-Path $PSScriptRoot 'verify_source.py') 2>&1 | Tee-Object -FilePath (Join-Path $taskOut 'source-checks.txt')
if ($LASTEXITCODE -ne 0) { throw 'Source verifier failed' }

# This only reads installed reference files. It never launches the game or Unity.
# Reuse the extracted references on normal runs; extract once if the cache is absent.
$taskReference = Join-Path $PSScriptRoot 'UnityProject/Assets/Reference'
if (-not (Test-Path -LiteralPath (Join-Path $taskReference 'native-effects.json'))) {
    & $Python -X utf8 (Join-Path $PSScriptRoot 'prepare_preview.py')
    if ($LASTEXITCODE -ne 0) { throw 'Native reference preparation failed' }
}
if (Test-Path -LiteralPath (Join-Path $taskReference 'native-skins.json')) {
    & $Python -X utf8 (Join-Path $PSScriptRoot 'prepare_managed.py') --reuse-references
} else {
    & $Python -X utf8 (Join-Path $PSScriptRoot 'prepare_managed.py')
}
if ($LASTEXITCODE -ne 0) { throw 'Managed fixture preparation failed' }
& dotnet run --project (Join-Path $PSScriptRoot 'ManagedFixture/NativeContract.csproj') -c Release -- $PSScriptRoot 2>&1 | Tee-Object -FilePath (Join-Path $taskOut 'managed-contract.log')
if ($LASTEXITCODE -ne 0) { throw 'Managed production/native contract failed' }
& $Python -X utf8 (Join-Path $PSScriptRoot 'render_independent.py')
if ($LASTEXITCODE -ne 0) { throw 'Independent mannequin render failed' }

if ($BuildRelease) {
    & dotnet build (Join-Path $taskRoot 'SteriaBuild/Steria.csproj') -c Release --nologo 2>&1 | Tee-Object -FilePath (Join-Path $taskOut 'release-build.log')
    if ($LASTEXITCODE -ne 0) { throw 'Game API Release build failed' }
}
& $Python -X utf8 (Join-Path $PSScriptRoot 'assemble_independent_previews.py')
if ($LASTEXITCODE -ne 0) { throw 'Independent media assembly or candidate verification failed' }
