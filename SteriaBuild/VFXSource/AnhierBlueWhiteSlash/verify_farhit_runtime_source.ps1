$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourcePath = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot "..\..\DiceAttackEffect_Steria_AnhierBlueWhiteCombat.cs"))
$source = Get-Content -LiteralPath $sourcePath -Raw
$failures = New-Object System.Collections.Generic.List[string]

function Assert-SourceContains {
    param(
        [Parameter(Mandatory = $true)][string]$Pattern,
        [Parameter(Mandatory = $true)][string]$Message
    )

    if ($source -notmatch $Pattern) {
        $failures.Add($Message)
    }
}

Assert-SourceContains 'AB-combat-vfx-farhit-runtime-projectile-20260706' "far-hit runtime projectile version marker is missing."
Assert-SourceContains 'protected\s+virtual\s+bool\s+UseRuntimeProjectile\s*=>\s*false' "combat base must expose a runtime projectile switch."
Assert-SourceContains 'protected\s+override\s+bool\s+UseRuntimeProjectile\s*=>\s*true' "far-hit must enable runtime projectile mode."
Assert-SourceContains '_targetRoot\s*=\s*GetPivot\(target,\s*PivotAction\)' "target root must use the target attack pivot, not raw atkEffectRoot."
Assert-SourceContains 'CreateRuntimeProjectileTrail\(' "far-hit must create a runtime projectile trail."
Assert-SourceContains 'TrailRenderer' "runtime projectile must use TrailRenderer for first-in first-out trail fading."
Assert-SourceContains 'UpdateRuntimeProjectile\(' "runtime projectile must be advanced in Update."
Assert-SourceContains 'RefreshRuntimeImpactWorld\(' "runtime target point must be refreshed from the target transform."
Assert-SourceContains 'impactRoot\.position\s*=\s*_runtimeImpactWorld' "impact root must be placed with target world coordinates."
Assert-SourceContains 'Vector3\.Lerp\(_runtimeStartWorld,\s*_runtimeImpactWorld,\s*eased\)' "projectile head must travel along the actual self-to-target path."

if ($failures.Count -gt 0) {
    Write-Host "Far-hit runtime source verification failed:"
    foreach ($failure in $failures) {
        Write-Host " - $failure"
    }
    exit 1
}

Write-Host "Far-hit runtime source verification passed."
