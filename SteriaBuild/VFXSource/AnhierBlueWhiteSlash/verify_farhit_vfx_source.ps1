$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$builder = Join-Path $scriptRoot "UnityProject\Assets\Editor\AnhierBlueWhiteCombatBundleBuilder.cs"
$source = Get-Content -LiteralPath $builder -Raw
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

Assert-SourceContains 'FarHitCasterCircleScale\s*=\s*1\.5f' "far-hit caster circle must be scaled to 150%."
Assert-SourceContains 'FarHitCasterCirclePitchDegrees\s*=\s*25f' "far-hit caster circle must have a vertical-plane pitch angle."
Assert-SourceContains 'Quaternion\.Euler\(0f,\s*FarHitCasterCirclePitchDegrees,\s*0f\)' "caster circle particles must render on a slanted vertical plane."
Assert-SourceContains 'CreateFarHitSegmentedTrail\(' "far-hit projectile must use segmented path trails."
Assert-SourceContains 'for\s*\(\s*int\s+i\s*=\s*0;\s*i\s*<\s*segments;\s*i\+\+\s*\)' "segmented path trail must instantiate ordered sections."
Assert-SourceContains 'Mathf\.Lerp\(startX,\s*endX,\s*t\)' "segmented path trail must distribute sections along the X path."
Assert-SourceContains 'delay\s*\+\s*stepDelay\s*\*\s*i' "segmented path trail must stagger sections over time."
Assert-SourceContains 'AB_FarHit_PathTrail_Core' "far-hit needs a visible core path trail layer."
Assert-SourceContains 'AB_FarHit_PathTrail_Ghost' "far-hit needs a delayed ghost path trail layer."
Assert-SourceContains 'AB_FarHit_PathTrail_Sparks' "far-hit needs spark path trail sections."

if ($failures.Count -gt 0) {
    Write-Host "Far-hit VFX source verification failed:"
    foreach ($failure in $failures) {
        Write-Host " - $failure"
    }
    exit 1
}

Write-Host "Far-hit VFX source verification passed."
