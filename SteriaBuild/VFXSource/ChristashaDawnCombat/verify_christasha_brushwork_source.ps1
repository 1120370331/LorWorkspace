$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot "..\..\.."))
$builderPath = Join-Path $repoRoot "SteriaBuild\VFXSource\AnhierBlueWhiteSlash\UnityProject\Assets\Editor\ChristashaDawnCombatBundleBuilder.cs"
$coreShaderPath = Join-Path $repoRoot "SteriaBuild\VFXSource\AnhierBlueWhiteSlash\UnityProject\Assets\Shaders\ChristashaDawn_CoreOnlyFlow.shader"
$glowShaderPath = Join-Path $repoRoot "SteriaBuild\VFXSource\AnhierBlueWhiteSlash\UnityProject\Assets\Shaders\ChristashaDawn_CoreOnlyEdgeGlow.shader"
$previewDir = Join-Path $repoRoot "SteriaBuild\SteriaModFolder\Resource\Preview\ChristashaDawnBrushwork\round8_implementation"
$requiredPreviewPaths = [ordered]@{
    GeometryOverlay = Join-Path $previewDir "christasha_dawn_round8_geometry_overlay.png"
    Early = Join-Path $previewDir "christasha_dawn_round8_early.png"
    Mid = Join-Path $previewDir "christasha_dawn_round8_mid.png"
    FullBlack = Join-Path $previewDir "christasha_dawn_round8_full_black.png"
    FullGray = Join-Path $previewDir "christasha_dawn_round8_full_gray.png"
    BattleBackground = Join-Path $previewDir "christasha_dawn_round8_battle_background.png"
    RetreatEarly = Join-Path $previewDir "christasha_dawn_round8_retreat_early.png"
    RetreatLate = Join-Path $previewDir "christasha_dawn_round8_retreat_late.png"
    CoreOnly = Join-Path $previewDir "christasha_dawn_round8_core_only.png"
    GoldOnly = Join-Path $previewDir "christasha_dawn_round8_gold_only.png"
    GlowOnly = Join-Path $previewDir "christasha_dawn_round8_glow_only.png"
    AfterglowBaseline = Join-Path $previewDir "christasha_dawn_round8_inner_afterglow_baseline.png"
    Afterglow2x = Join-Path $previewDir "christasha_dawn_round8_inner_afterglow_2x.png"
    AfterglowComparison = Join-Path $previewDir "christasha_dawn_round8_inner_afterglow_comparison_baseline_left_2x_right.png"
    SilhouetteFreezeOverlay = Join-Path $previewDir "christasha_dawn_round8_silhouette_freeze_overlay.png"
    AfterglowPositionOverlay = Join-Path $previewDir "christasha_dawn_round8_afterglow_position_overlay.png"
    RepresentativeBattle = Join-Path $previewDir "christasha_dawn_round8_representative_battle.png"
    EarlyRevealFrontDiagnostic = Join-Path $previewDir "christasha_dawn_round8_early_reveal_front_diagnostic.png"
    MidRevealFrontDiagnostic = Join-Path $previewDir "christasha_dawn_round8_mid_reveal_front_diagnostic.png"
}
$glowOnlyPreviewPath = $requiredPreviewPaths.GlowOnly
$earlyPreviewPath = $requiredPreviewPaths.Early
$fullPreviewPath = $requiredPreviewPaths.FullBlack
$fullGrayPreviewPath = $requiredPreviewPaths.FullGray
$battlePreviewPath = $requiredPreviewPaths.BattleBackground
$coreOnlyPreviewPath = $requiredPreviewPaths.CoreOnly
$latePreviewPath = $requiredPreviewPaths.RetreatLate
$failures = New-Object System.Collections.Generic.List[string]

foreach ($path in @($builderPath, $coreShaderPath, $glowShaderPath)) {
    if (!(Test-Path -LiteralPath $path)) {
        $failures.Add("required source file is missing: $path")
    }
}

if ($failures.Count -eq 0) {
    $builder = Get-Content -LiteralPath $builderPath -Raw
    $coreShader = Get-Content -LiteralPath $coreShaderPath -Raw
    $glowShader = Get-Content -LiteralPath $glowShaderPath -Raw

    function Assert-Contains {
        param(
            [Parameter(Mandatory = $true)][string]$Source,
            [Parameter(Mandatory = $true)][string]$Pattern,
            [Parameter(Mandatory = $true)][string]$Message
        )

        if ($Source -notmatch $Pattern) {
            $failures.Add($Message)
        }
    }

    function Get-SourceFloat {
        param(
            [Parameter(Mandatory = $true)][string]$Source,
            [Parameter(Mandatory = $true)][string]$Pattern,
            [Parameter(Mandatory = $true)][string]$Message
        )

        $match = [regex]::Match($Source, $Pattern)
        if (!$match.Success) {
            $failures.Add($Message)
            return [double]::NaN
        }

        return [double]::Parse($match.Groups[1].Value, [System.Globalization.CultureInfo]::InvariantCulture)
    }

    function Get-Smooth01 {
        param([double]$A, [double]$B, [double]$Value)

        $t = [Math]::Max(0.0, [Math]::Min(1.0, ($Value - $A) / ($B - $A)))
        return $t * $t * (3.0 - 2.0 * $t)
    }

    function Get-Smoother01 {
        param([double]$A, [double]$B, [double]$Value)

        $t = [Math]::Max(0.0, [Math]::Min(1.0, ($Value - $A) / ($B - $A)))
        return $t * $t * $t * ($t * ($t * 6.0 - 15.0) + 10.0)
    }

    function Get-Round8WidthRatio {
        param([double]$T)

        if ($T -le 0.03) { return 0.065 * (Get-Smoother01 0.00 0.03 $T) }
        if ($T -le 0.08) { return 0.065 + (0.14 - 0.065) * (Get-Smoother01 0.03 0.08 $T) }
        if ($T -le 0.22) { return 0.14 + (0.38 - 0.14) * (Get-Smoother01 0.08 0.22 $T) }
        if ($T -le 0.45) { return 0.38 + (0.72 - 0.38) * (Get-Smoother01 0.22 0.45 $T) }
        if ($T -le 0.66) { return 0.72 + (1.00 - 0.72) * (Get-Smoother01 0.45 0.66 $T) }
        $lowerFade = 1.0 - (Get-Smoother01 0.66 1.00 $T)
        return 0.12 + (1.0 - 0.12) * [Math]::Pow($lowerFade, 0.65)
    }

    function Get-BaseCenterline {
        param([double]$T)

        $angleT = $T + [Math]::Sin([Math]::PI * $T) * (0.018 + 0.030 * $T)
        $angle = (65.0 + (-135.0 - 65.0) * $angleT) * [Math]::PI / 180.0
        $pressureRadius = 2.0 * (1.0 + (Get-Smooth01 0.34 0.82 $T) * 0.035)
        $x = -[Math]::Cos($angle) * $pressureRadius * 1.35
        $y = [Math]::Sin($angle) * $pressureRadius * 1.15

        $trailingLift = 1.0 - (Get-Smooth01 0.00 0.18 $T)
        $x += -0.10 * $trailingLift
        $y += 0.08 * $trailingLift

        $leadingDrive = Get-Smooth01 0.60 1.00 $T
        $x += 0.38 * $leadingDrive
        $y += -0.12 * $leadingDrive
        return [double[]]@($x, $y)
    }

    function Get-PointDistance {
        param([double[]]$A, [double[]]$B)
        $dx = $B[0] - $A[0]
        $dy = $B[1] - $A[1]
        return [Math]::Sqrt($dx * $dx + $dy * $dy)
    }

    function Get-NormalizedVector {
        param([double[]]$Vector)
        $length = [Math]::Sqrt($Vector[0] * $Vector[0] + $Vector[1] * $Vector[1])
        $x = $Vector[0] / $length
        $y = $Vector[1] / $length
        return [double[]]@($x, $y)
    }

    function Get-RotatedVector {
        param([double[]]$Vector, [double]$Degrees)
        $radians = $Degrees * [Math]::PI / 180.0
        $sin = [Math]::Sin($radians)
        $cos = [Math]::Cos($radians)
        $x = $Vector[0] * $cos - $Vector[1] * $sin
        $y = $Vector[0] * $sin + $Vector[1] * $cos
        return [double[]]@($x, $y)
    }

    function Get-CubicBezierPoint {
        param(
            [double[]]$P0,
            [double[]]$P1,
            [double[]]$P2,
            [double[]]$P3,
            [double]$T
        )

        $inverseT = 1.0 - $T
        $x =
            $inverseT * $inverseT * $inverseT * $P0[0] +
            3.0 * $inverseT * $inverseT * $T * $P1[0] +
            3.0 * $inverseT * $T * $T * $P2[0] +
            $T * $T * $T * $P3[0]
        $y =
            $inverseT * $inverseT * $inverseT * $P0[1] +
            3.0 * $inverseT * $inverseT * $T * $P1[1] +
            3.0 * $inverseT * $T * $T * $P2[1] +
            $T * $T * $T * $P3[1]
        return [double[]]@($x, $y)
    }

    function Get-PreviewImageMetrics {
        param([Parameter(Mandatory = $true)][string]$Path)

        Add-Type -AssemblyName System.Drawing
        $bitmap = [System.Drawing.Bitmap]::FromFile($Path)
        try {
            $background = $bitmap.GetPixel(0, 0)
            $nonBackgroundPixels = 0
            $pixelsAboveOne = 0
            $pixelsAboveThree = 0
            $pixelsAboveEight = 0
            $peakDeltaByte = 0
            $warmVisiblePixels = 0
            $lowChromaDimPixels = 0
            $pixelsBelowBackground = 0
            $positiveLumaDeltas = New-Object System.Collections.Generic.List[double]
            $positiveRedBlueDeltas = New-Object System.Collections.Generic.List[double]
            $positiveGreenBlueDeltas = New-Object System.Collections.Generic.List[double]
            $positiveLumaSum = 0.0
            $deltaCounts = New-Object int[] 256
            $warmDeltaCounts = New-Object int[] 256
            $warmDeltaMaxRedBlue = New-Object int[] 256
            $rowWidthsAboveOne = New-Object System.Collections.Generic.List[int]
            $columnCountsAboveOne = New-Object int[] $bitmap.Width

            for ($y = 0; $y -lt $bitmap.Height; $y++) {
                $minXAboveOne = $bitmap.Width
                $maxXAboveOne = -1
                for ($x = 0; $x -lt $bitmap.Width; $x++) {
                    $pixel = $bitmap.GetPixel($x, $y)
                    $signedR = [int]$pixel.R - [int]$background.R
                    $signedG = [int]$pixel.G - [int]$background.G
                    $signedB = [int]$pixel.B - [int]$background.B
                    $deltaR = [Math]::Abs([int]$pixel.R - [int]$background.R)
                    $deltaG = [Math]::Abs([int]$pixel.G - [int]$background.G)
                    $deltaB = [Math]::Abs([int]$pixel.B - [int]$background.B)
                    $delta = [Math]::Max($deltaR, [Math]::Max($deltaG, $deltaB))
                    $positiveDelta = [Math]::Max($signedR, [Math]::Max($signedG, $signedB))
                    $signedChroma = [Math]::Max($signedR, [Math]::Max($signedG, $signedB)) - [Math]::Min($signedR, [Math]::Min($signedG, $signedB))
                    $lumaDelta = 0.2126 * $signedR + 0.7152 * $signedG + 0.0722 * $signedB
                    if ($delta -gt 0) {
                        $nonBackgroundPixels++
                    }
                    if ($delta -gt 1) {
                        $pixelsAboveOne++
                        $columnCountsAboveOne[$x]++
                        if ($x -lt $minXAboveOne) {
                            $minXAboveOne = $x
                        }
                        if ($x -gt $maxXAboveOne) {
                            $maxXAboveOne = $x
                        }
                    }
                    if ($delta -gt 3) {
                        $pixelsAboveThree++
                    }
                    if ($delta -gt 8) {
                        $pixelsAboveEight++
                    }
                    if ($positiveDelta -gt 3 -and $signedR -gt $signedG -and $signedG -gt $signedB -and ($signedR - $signedB) -ge 4) {
                        $warmVisiblePixels++
                    }
                    if ($positiveDelta -gt 3 -and $lumaDelta -gt 0.0) {
                        $positiveLumaDeltas.Add($lumaDelta)
                        $positiveRedBlueDeltas.Add($signedR - $signedB)
                        $positiveGreenBlueDeltas.Add($signedG - $signedB)
                        $positiveLumaSum += $lumaDelta
                    }
                    if ($positiveDelta -ge 2 -and $positiveDelta -le 20 -and $signedChroma -le 4) {
                        $lowChromaDimPixels++
                    }
                    if ($lumaDelta -le -2.0) {
                        $pixelsBelowBackground++
                    }
                    $deltaCounts[$delta]++
                    $redBlueDelta = [int]$pixel.R - [int]$pixel.B
                    if ($pixel.R -gt $pixel.G -and $pixel.G -gt $pixel.B -and $redBlueDelta -ge 2) {
                        $warmDeltaCounts[$delta]++
                        if ($redBlueDelta -gt $warmDeltaMaxRedBlue[$delta]) {
                            $warmDeltaMaxRedBlue[$delta] = $redBlueDelta
                        }
                    }
                    if ($delta -gt $peakDeltaByte) {
                        $peakDeltaByte = $delta
                    }
                }
                if ($maxXAboveOne -ge 0) {
                    $rowWidthsAboveOne.Add($maxXAboveOne - $minXAboveOne + 1)
                }
            }

            $narrowRows = ($rowWidthsAboveOne | Where-Object { $_ -le 10 }).Count
            $narrowRowRatio = if ($rowWidthsAboveOne.Count -gt 0) {
                $narrowRows / $rowWidthsAboveOne.Count
            }
            else {
                0.0
            }

            $visibleColumns = New-Object System.Collections.Generic.List[int]
            for ($x = 0; $x -lt $bitmap.Width; $x++) {
                if ($columnCountsAboveOne[$x] -gt 0) {
                    $visibleColumns.Add($x)
                }
            }

            $terminalThicknesses = New-Object System.Collections.Generic.List[int]
            if ($visibleColumns.Count -gt 0) {
                $terminalEndX = $visibleColumns[$visibleColumns.Count - 1]
                $terminalStartX = [Math]::Max($visibleColumns[0], $terminalEndX - 79)
                for ($x = $terminalStartX; $x -le $terminalEndX; $x++) {
                    $terminalThicknesses.Add($columnCountsAboveOne[$x])
                }
            }

            $terminalReverseThickenings = 0
            $terminalMonotonicWindow = @($terminalThicknesses | Select-Object -Last 32)
            for ($i = 1; $i -lt $terminalMonotonicWindow.Count; $i++) {
                if ($terminalMonotonicWindow[$i] -gt $terminalMonotonicWindow[$i - 1]) {
                    $terminalReverseThickenings++
                }
            }

            $peakNeighborhoodPixels = 0
            $peakWarmPixels = 0
            $peakWarmMaxRedBlue = 0
            $peakNeighborhoodStart = [Math]::Max(0, $peakDeltaByte - 1)
            for ($delta = $peakNeighborhoodStart; $delta -le $peakDeltaByte; $delta++) {
                $peakNeighborhoodPixels += $deltaCounts[$delta]
                $peakWarmPixels += $warmDeltaCounts[$delta]
                $peakWarmMaxRedBlue = [Math]::Max($peakWarmMaxRedBlue, $warmDeltaMaxRedBlue[$delta])
            }

            return [pscustomobject]@{
                Width = $bitmap.Width
                Height = $bitmap.Height
                NonBackgroundPixels = $nonBackgroundPixels
                PixelsAboveOne = $pixelsAboveOne
                PixelsAboveThree = $pixelsAboveThree
                PixelsAboveEight = $pixelsAboveEight
                PeakDelta = $peakDeltaByte / 255.0
                PeakNeighborhoodPixels = $peakNeighborhoodPixels
                PeakWarmPixels = $peakWarmPixels
                PeakWarmMaxRedBlue = $peakWarmMaxRedBlue
                WarmVisiblePixels = $warmVisiblePixels
                WarmVisibleRatio = if ($pixelsAboveThree -gt 0) { $warmVisiblePixels / $pixelsAboveThree } else { 0.0 }
                LowChromaDimPixels = $lowChromaDimPixels
                PixelsBelowBackground = $pixelsBelowBackground
                MedianPositiveLumaDelta = if ($positiveLumaDeltas.Count -gt 0) {
                    $sortedLumaDeltas = @($positiveLumaDeltas | Sort-Object)
                    $sortedLumaDeltas[[int][Math]::Floor($sortedLumaDeltas.Count / 2)]
                }
                else {
                    0.0
                }
                MeanPositiveLumaDelta = if ($positiveLumaDeltas.Count -gt 0) {
                    $positiveLumaSum / $positiveLumaDeltas.Count
                }
                else {
                    0.0
                }
                MedianPositiveRedBlueDelta = if ($positiveRedBlueDeltas.Count -gt 0) {
                    $sortedRedBlueDeltas = @($positiveRedBlueDeltas | Sort-Object)
                    $sortedRedBlueDeltas[[int][Math]::Floor($sortedRedBlueDeltas.Count / 2)]
                }
                else {
                    0.0
                }
                MedianPositiveGreenBlueDelta = if ($positiveGreenBlueDeltas.Count -gt 0) {
                    $sortedGreenBlueDeltas = @($positiveGreenBlueDeltas | Sort-Object)
                    $sortedGreenBlueDeltas[[int][Math]::Floor($sortedGreenBlueDeltas.Count / 2)]
                }
                else {
                    0.0
                }
                Background = "($($background.R),$($background.G),$($background.B))"
                VisibleRowsAboveOne = $rowWidthsAboveOne.Count
                NarrowRowsAtMostTen = $narrowRows
                NarrowRowRatio = $narrowRowRatio
                TerminalMaxThickness = if ($terminalThicknesses.Count -gt 0) {
                    ($terminalThicknesses | Measure-Object -Maximum).Maximum
                }
                else {
                    0
                }
                TerminalAverageThickness = if ($terminalThicknesses.Count -gt 0) {
                    ($terminalThicknesses | Measure-Object -Average).Average
                }
                else {
                    0.0
                }
                TerminalLastTenAverageThickness = if ($terminalThicknesses.Count -gt 0) {
                    (($terminalThicknesses | Select-Object -Last 10) | Measure-Object -Average).Average
                }
                else {
                    0.0
                }
                TerminalReverseThickenings = $terminalReverseThickenings
            }
        }
        finally {
            $bitmap.Dispose()
        }
    }

    function Get-InnerArcDarkRunMetrics {
        param([Parameter(Mandatory = $true)][string]$Path)

        Add-Type -AssemblyName System.Drawing
        $bitmap = [System.Drawing.Bitmap]::FromFile($Path)
        try {
            $eligibleRows = 0
            $darkRows = 0
            $longestDarkRun = 0
            $currentDarkRun = 0
            $previousEligibleY = -2

            for ($y = 130; $y -le [Math]::Min(609, $bitmap.Height - 1); $y++) {
                $backgroundR = New-Object System.Collections.Generic.List[int]
                $backgroundG = New-Object System.Collections.Generic.List[int]
                $backgroundB = New-Object System.Collections.Generic.List[int]
                foreach ($x in @(0..159 + 1120..1279)) {
                    if (($x % 4) -ne 0 -or $x -ge $bitmap.Width) {
                        continue
                    }
                    $pixel = $bitmap.GetPixel($x, $y)
                    $backgroundR.Add([int]$pixel.R)
                    $backgroundG.Add([int]$pixel.G)
                    $backgroundB.Add([int]$pixel.B)
                }
                $sortedBackgroundR = @($backgroundR | Sort-Object)
                $sortedBackgroundG = @($backgroundG | Sort-Object)
                $sortedBackgroundB = @($backgroundB | Sort-Object)
                $backgroundIndex = [int][Math]::Floor($sortedBackgroundR.Count / 2)
                $baseR = $sortedBackgroundR[$backgroundIndex]
                $baseG = $sortedBackgroundG[$backgroundIndex]
                $baseB = $sortedBackgroundB[$backgroundIndex]

                $minX = $bitmap.Width
                $maxX = -1
                for ($x = 360; $x -le [Math]::Min(820, $bitmap.Width - 1); $x++) {
                    $pixel = $bitmap.GetPixel($x, $y)
                    $deltaR = [int]$pixel.R - $baseR
                    $deltaG = [int]$pixel.G - $baseG
                    $deltaB = [int]$pixel.B - $baseB
                    $positiveDelta = [Math]::Max($deltaR, [Math]::Max($deltaG, $deltaB))
                    $lumaDelta = 0.2126 * $deltaR + 0.7152 * $deltaG + 0.0722 * $deltaB
                    if ($positiveDelta -gt 16 -and $lumaDelta -gt 12.0) {
                        $minX = [Math]::Min($minX, $x)
                        $maxX = [Math]::Max($maxX, $x)
                    }
                }

                if ($maxX -lt 0 -or ($maxX - $minX + 1) -lt 24) {
                    $currentDarkRun = 0
                    $previousEligibleY = -2
                    continue
                }

                $rowLumaDeltas = New-Object System.Collections.Generic.List[double]
                for ($x = $minX; $x -le $maxX; $x++) {
                    $pixel = $bitmap.GetPixel($x, $y)
                    $rowLumaDeltas.Add(
                        0.2126 * ([int]$pixel.R - $baseR) +
                        0.7152 * ([int]$pixel.G - $baseG) +
                        0.0722 * ([int]$pixel.B - $baseB))
                }
                $sortedRowLuma = @($rowLumaDeltas | Sort-Object)
                $peakIndex = [int][Math]::Floor(($sortedRowLuma.Count - 1) * 0.90)
                $rowPeakLuma = [Math]::Max($sortedRowLuma[$peakIndex], 0.001)

                $bandLumaDeltas = New-Object System.Collections.Generic.List[double]
                $bandStart = [Math]::Max($minX, $maxX - 12)
                $bandEnd = [Math]::Max($minX, $maxX - 3)
                for ($x = $bandStart; $x -le $bandEnd; $x++) {
                    $pixel = $bitmap.GetPixel($x, $y)
                    $bandLumaDeltas.Add(
                        0.2126 * ([int]$pixel.R - $baseR) +
                        0.7152 * ([int]$pixel.G - $baseG) +
                        0.0722 * ([int]$pixel.B - $baseB))
                }
                $sortedBandLuma = @($bandLumaDeltas | Sort-Object)
                $bandMedianLuma = $sortedBandLuma[[int][Math]::Floor($sortedBandLuma.Count / 2)]
                $bandToPeakRatio = $bandMedianLuma / $rowPeakLuma
                $isDarkBand = $bandToPeakRatio -lt 0.70

                $eligibleRows++
                if ($isDarkBand) {
                    $darkRows++
                    $currentDarkRun = if ($previousEligibleY -eq $y - 1) { $currentDarkRun + 1 } else { 1 }
                    $longestDarkRun = [Math]::Max($longestDarkRun, $currentDarkRun)
                }
                else {
                    $currentDarkRun = 0
                }
                $previousEligibleY = $y
            }

            return [pscustomobject]@{
                EligibleRows = $eligibleRows
                DarkRows = $darkRows
                DarkRowRatio = if ($eligibleRows -gt 0) { $darkRows / $eligibleRows } else { 1.0 }
                LongestDarkRun = $longestDarkRun
            }
        }
        finally {
            $bitmap.Dispose()
        }
    }

    function Get-RevealFrontDiagnosticMetrics {
        param([Parameter(Mandatory = $true)][string]$Path)

        Add-Type -AssemblyName System.Drawing
        $bitmap = [System.Drawing.Bitmap]::FromFile($Path)
        try {
            $markerRows = @(48, 88, 128)
            $markerChannels = @('R', 'G', 'B')
            $markerPositions = New-Object System.Collections.Generic.List[int]
            for ($index = 0; $index -lt $markerRows.Count; $index++) {
                $row = $bitmap.Height - 1 - $markerRows[$index]
                $channel = $markerChannels[$index]
                $positions = New-Object System.Collections.Generic.List[int]
                for ($x = 0; $x -lt $bitmap.Width; $x++) {
                    $pixel = $bitmap.GetPixel($x, $row)
                    $value = if ($channel -eq 'R') { [int]$pixel.R } elseif ($channel -eq 'G') { [int]$pixel.G } else { [int]$pixel.B }
                    $otherA = if ($channel -eq 'R') { [int]$pixel.G } else { [int]$pixel.R }
                    $otherB = if ($channel -eq 'B') { [int]$pixel.G } else { [int]$pixel.B }
                    if ($value -ge 220 -and $value -ge $otherA + 80 -and $value -ge $otherB + 80) {
                        $positions.Add($x)
                    }
                }
                if ($positions.Count -eq 0) {
                    $markerPositions.Add(-1)
                }
                else {
                    $markerPositions.Add([int][Math]::Round(($positions | Measure-Object -Average).Average))
                }
            }

            $validPositions = @($markerPositions | Where-Object { $_ -ge 0 })
            $spread = if ($validPositions.Count -eq 3) {
                ($validPositions | Measure-Object -Maximum).Maximum - ($validPositions | Measure-Object -Minimum).Minimum
            }
            else {
                [int]::MaxValue
            }
            return [pscustomobject]@{
                BodyFront = $markerPositions[0]
                GoldFront = $markerPositions[1]
                GlowFront = $markerPositions[2]
                SpreadPixels = $spread
            }
        }
        finally {
            $bitmap.Dispose()
        }
    }

    Assert-Contains $builder 'ChristashaDawn_BrushMask_512\.png' "builder must generate and bind the dedicated brush mask."
    Assert-Contains $builder 'CreateDirectionalBrushMaskTexture\(' "builder must generate a directional brush mask."
    Assert-Contains $builder 'new Color\(coreFiber,\s*goldFiber,\s*abrasion,\s*softEnvelope\)' "brush mask channels must encode core, gold fibers, abrasion, and soft envelope."
    if ($builder -match 'CreateEnergyFlowMaskTexture\(') {
        $failures.Add("builder must remove the rounded cellular energy-flow mask generator.")
    }

    foreach ($property in @(
        '_BrushCoreStrength',
        '_BrushFiberStrength',
        '_BrushAbrasionStrength',
        '_SoftEnvelopeStrength'
    )) {
        Assert-Contains $coreShader ([regex]::Escape($property)) "core shader is missing brushwork property $property."
    }

    Assert-Contains $coreShader 'brushTex\.r' "core shader must use the red channel for the white blade core."
    Assert-Contains $coreShader 'brushTex\.g' "core shader must use the green channel for gold fibers."
    Assert-Contains $coreShader 'brushTex\.b' "core shader must use the blue channel for dark abrasion."
    Assert-Contains $coreShader 'brushTex\.a' "core shader must use the alpha channel for the soft envelope."
    Assert-Contains $coreShader 'float abrasionCut' "core shader must explicitly subtract abrasion from brightness and opacity."
    Assert-Contains $coreShader '_LuminousBodyStrength' "core shader must expose a broad luminous-body strength."
    Assert-Contains $coreShader 'float luminousBody' "core shader must build an opaque luminous blade body, not only thin fibers."
    Assert-Contains $coreShader 'luminousBody \* _LuminousBodyStrength' "core shader must apply luminous-body strength to the broad stroke."
    Assert-Contains $coreShader 'luminousBody \* 0\.60' "core shader alpha must raise luminous-body energy density to 0.60."
    Assert-Contains $builder 'mat\.SetFloat\("_LuminousBodyStrength",\s*0\.68f\)' "core material must keep the narrower slash hotspot visibly emissive."
    Assert-Contains $coreShader 'float maskPath = saturate\(pathT\);' "core brush mask must sample one continuous path without a mid-stroke wrap seam."
    Assert-Contains $glowShader 'float glowMaskPath = saturate\(pathT\);' "glow brush mask must sample one continuous path without a mid-stroke wrap seam."
    if ($coreShader -match 'pathT \* _FlowTexTiling') {
        $failures.Add("core brush mask must not repeat along the slash path because the wrap boundary causes a visible break.")
    }
    if ($glowShader -match 'pathT \* _FlowTexTiling') {
        $failures.Add("glow brush mask must not repeat along the slash path because the wrap boundary causes a visible break.")
    }

    Assert-Contains $glowShader '_SoftEnvelopeStrength' "edge glow shader must expose soft-envelope strength."
    Assert-Contains $glowShader 'brushTex\.a' "edge glow shader must be driven primarily by the broad envelope channel."
    if ($glowShader -match 'brushTex\.g|goldFiber') {
        $failures.Add("edge glow shader must not sample or expose readable gold-fiber detail.")
    }

    Assert-Contains $builder 'ChristashaDawn_UserSlashCoreOnly\.asset' "builder must create a dedicated clean core mesh."
    Assert-Contains $builder 'ChristashaDawn_UserSlashGlowEnvelope\.asset' "builder must create a separate glow-envelope mesh."
    Assert-Contains $builder 'CreateSlashVerticalCoreOnlyPrefab\(coreMaterial,\s*edgeGlowMaterial,\s*coreMesh,\s*glowMesh,\s*revealClip\)' "core-only prefab must receive separate core and glow meshes."
    Assert-Contains $builder 'const float peakBladeWidth = 1\.4755f;' "slash body must retain the approved approximately +30 percent peak width."
    Assert-Contains $builder 'const float terminalWidthRatio = 0\.12f;' "slash terminal width must remain at 12 percent of peak instead of collapsing to a needle."
    Assert-Contains $builder 'const float terminalBevelDegrees = 26f;' "slash terminal bevel must target +26 degrees."
    Assert-Contains $builder 'GetUserSlashRound8WidthRatio\(t\)' "slash width must use the approved Round 8 single-peak profile."
    Assert-Contains $builder 'float outerOffset = GetUserSlashLegacyWidth\(t\) \* 0\.38f \* startTip;' "slash back-side outer arc must retain the accepted legacy offset after the upper needle ramp."
    Assert-Contains $builder 'float innerOffset = Mathf\.Max\(0f,\s*bladeWidth - outerOffset\);' "slash lower-inner correction must be isolated to the inner offset."
    Assert-Contains $builder 'float terminalBlend = Smoother01\(0\.97f,\s*1\.00f,\s*t\);' "terminal bevel skew must blend only through the final three percent of the lower segment."
    Assert-Contains $builder 'float terminalRetraction = bladeWidth / Mathf\.Tan\(terminalRelativeAngle \* Mathf\.Deg2Rad\);' "terminal bevel tangent retraction must derive from the target cap angle."
    Assert-Contains $builder 'p -= tangent \* terminalRetraction \* \(1f - easedU\) \* terminalBlend;' "terminal bevel must retract the opening-side inner endpoint to form the approved screen-space +26 degree cut."
    Assert-Contains $builder 'const float startTangentTurnDegrees = -25f;' "slash start tangent must turn clockwise by 25 degrees toward the character."
    Assert-Contains $builder 'const float localTurnEndT = 0\.32f;' "slash local tangent correction must rejoin the original centerline at t=0.32."
    Assert-Contains $builder 'GetUserSlashBaseArcLengthFraction\(' "slash local tangent correction must be parameterized by original-curve cumulative arc length."
    Assert-Contains $builder 'EvaluateCubicBezier\(' "slash local tangent correction must use a smooth cubic centerline segment."
    Assert-Contains $builder 'if \(t >= localTurnEndT\)\s*\{\s*return baseCenter;\s*\}' "slash centerline must return exactly to the original path from t=0.32 onward."
    Assert-Contains $builder 'if \(t <= 0\.03f\)' "slash upper endpoint must begin from a true zero-width needle."
    Assert-Contains $builder 'return terminalWidthRatio \+ \(1f - terminalWidthRatio\) \* Mathf\.Pow\(lowerFade,\s*0\.65f\);' "slash lower width must use one smooth monotonic falloff to the finite 12 percent terminal ratio."
    Assert-Contains $builder 'CreateUserProvidedSlashRibbonMesh\("ChristashaDawn_UserSlashCoreOnly",\s*0f\)' "core geometry must use a single-sided ribbon."
    Assert-Contains $builder 'CreateUserProvidedSlashRibbonMesh\("ChristashaDawn_UserSlashGlowEnvelope",\s*0f\)' "glow geometry must reuse the exact core silhouette so every layer shares the same terminal cap."
    Assert-Contains $builder 'private const float CoreOnlyVisualRotationDegrees = -42\.25f;' "core-only prefab must expose the approved near-vertical visual rotation."
    Assert-Contains $builder 'travelRoot\.localRotation = Quaternion\.Euler\(0f,\s*0f,\s*CoreOnlyVisualRotationDegrees\);' "core-only prefab must apply the approved rotation at the shared travel root."
    if ($builder -match 'CreateUserProvidedSlashVolumeMesh\("ChristashaDawn_UserSlashCoreOnly"') {
        $failures.Add("core geometry must not reuse the front-and-back volume mesh because transparent faces would contribute twice.")
    }
    if ($builder -match 'CreateUserProvidedSlashVolumeMesh\("ChristashaDawn_UserSlashGlowEnvelope"') {
        $failures.Add("glow geometry must not reuse the front-and-back volume mesh because transparent faces would contribute twice.")
    }

    $widthSamples = @(0.66, 0.80, 0.92, 0.97, 1.00) | ForEach-Object { Get-Round8WidthRatio $_ }
    for ($i = 1; $i -lt $widthSamples.Count; $i++) {
        if ($widthSamples[$i] -ge $widthSamples[$i - 1]) {
            $failures.Add("Round 8 lower width profile must strictly decrease after s=0.66; measured $($widthSamples -join ', ').")
            break
        }
    }
    $terminalWidthRatio = $widthSamples[$widthSamples.Count - 1]
    if ($terminalWidthRatio -lt 0.10 -or $terminalWidthRatio -gt 0.14) {
        $failures.Add("Round 8 terminal width ratio must stay within 10%-14%; measured $terminalWidthRatio.")
    }
    $terminalTangentDegrees = -31.0
    $terminalBevelDegrees = 26.0
    $terminalRelativeDegrees = $terminalBevelDegrees - $terminalTangentDegrees
    $terminalRetractionRatio = 1.0 / [Math]::Tan($terminalRelativeDegrees * [Math]::PI / 180.0)
    $reconstructedBevel = $terminalTangentDegrees + [Math]::Atan2(1.0, $terminalRetractionRatio) * 180.0 / [Math]::PI
    if ($reconstructedBevel -lt 18.0 -or $reconstructedBevel -gt 35.0 -or [Math]::Abs($reconstructedBevel - 26.0) -gt 0.5) {
        $failures.Add("Round 8 terminal bevel must reconstruct to +26 degrees inside the allowed +18 to +35 range; measured $reconstructedBevel degrees.")
    }
    Write-Host ("Round 8 width metrics: s66={0:F3} s80={1:F3} s92={2:F3} s97={3:F3} s100={4:F3} bevel={5:F3}deg" -f `
        $widthSamples[0], $widthSamples[1], $widthSamples[2], $widthSamples[3], $widthSamples[4], $reconstructedBevel)

    $startTurnDegrees = Get-SourceFloat $builder 'const float startTangentTurnDegrees = (-?[0-9.]+)f;' "slash centerline must expose its start tangent turn for numeric verification."
    $localTurnEndT = Get-SourceFloat $builder 'const float localTurnEndT = ([0-9.]+)f;' "slash centerline must expose its local rejoin parameter for numeric verification."
    $startHandleLength = Get-SourceFloat $builder 'const float startBezierHandleLength = ([0-9.]+)f;' "slash centerline must expose its start Bezier handle length for numeric verification."
    $endHandleLength = Get-SourceFloat $builder 'const float endBezierHandleLength = ([0-9.]+)f;' "slash centerline must expose its end Bezier handle length for numeric verification."

    $hasNumericCenterlineContract =
        (-not [double]::IsNaN($startTurnDegrees)) -and
        (-not [double]::IsNaN($localTurnEndT)) -and
        (-not [double]::IsNaN($startHandleLength)) -and
        (-not [double]::IsNaN($endHandleLength))
    if ($hasNumericCenterlineContract) {
        $arcSamples = 4096
        $basePoints = New-Object object[] ($arcSamples + 1)
        $baseCumulative = New-Object double[] ($arcSamples + 1)
        $baseCumulative[0] = 0.0
        for ($i = 0; $i -le $arcSamples; $i++) {
            $sampleT = $localTurnEndT * $i / $arcSamples
            $point = Get-BaseCenterline $sampleT
            $basePoints[$i] = $point
            if ($i -gt 0) {
                $segmentLength = Get-PointDistance $basePoints[$i - 1] $point
                $baseCumulative[$i] = $baseCumulative[$i - 1] + $segmentLength
            }
        }

        $baseLocalLength = $baseCumulative[$arcSamples]
        $startPoint = Get-BaseCenterline 0.0
        $joinPoint = Get-BaseCenterline $localTurnEndT
        $startDerivativePoint = Get-BaseCenterline 0.00001
        $joinDerivativePoint = Get-BaseCenterline ($localTurnEndT - 0.00001)
        $baseStartTangent = Get-NormalizedVector ([double[]]@(
            ($startDerivativePoint[0] - $startPoint[0]),
            ($startDerivativePoint[1] - $startPoint[1])))
        $turnedStartTangent = Get-NormalizedVector (Get-RotatedVector $baseStartTangent $startTurnDegrees)
        $joinTangent = Get-NormalizedVector ([double[]]@(
            ($joinPoint[0] - $joinDerivativePoint[0]),
            ($joinPoint[1] - $joinDerivativePoint[1])))
        $control1 = [double[]]@(
            ($startPoint[0] + $turnedStartTangent[0] * $startHandleLength),
            ($startPoint[1] + $turnedStartTangent[1] * $startHandleLength))
        $control2 = [double[]]@(
            ($joinPoint[0] - $joinTangent[0] * $endHandleLength),
            ($joinPoint[1] - $joinTangent[1] * $endHandleLength))

        $adjustedPoints = New-Object object[] ($arcSamples + 1)
        for ($i = 0; $i -le $arcSamples; $i++) {
            $baseFraction = $baseCumulative[$i] / $baseLocalLength
            $adjustedPoints[$i] = Get-CubicBezierPoint $startPoint $control1 $control2 $joinPoint $baseFraction
        }

        $fullSamples = 8192
        $adjustedLocalLength = 0.0
        for ($i = 1; $i -le $arcSamples; $i++) {
            $adjustedLocalLength += Get-PointDistance $adjustedPoints[$i - 1] $adjustedPoints[$i]
        }

        $baseRestLength = 0.0
        $previousBase = Get-BaseCenterline $localTurnEndT
        for ($i = 1; $i -le $fullSamples; $i++) {
            $sampleT = $localTurnEndT + (1.0 - $localTurnEndT) * $i / $fullSamples
            $currentBase = Get-BaseCenterline $sampleT
            $baseRestLength += Get-PointDistance $previousBase $currentBase
            $previousBase = $currentBase
        }

        $baseLength = $baseLocalLength + $baseRestLength
        $adjustedLength = $adjustedLocalLength + $baseRestLength
        $lengthDeltaPercent = ($adjustedLength / $baseLength - 1.0) * 100.0
        $startError = Get-PointDistance $adjustedPoints[0] $startPoint
        $joinError = Get-PointDistance $adjustedPoints[$arcSamples] $joinPoint
        $endError = Get-PointDistance $previousBase (Get-BaseCenterline 1.0)
        $adjustedStartVector = [double[]]@(
            ($adjustedPoints[1][0] - $adjustedPoints[0][0]),
            ($adjustedPoints[1][1] - $adjustedPoints[0][1]))
        $baseStartAngle = [Math]::Atan2($baseStartTangent[1], $baseStartTangent[0]) * 180.0 / [Math]::PI
        $adjustedStartAngle = [Math]::Atan2($adjustedStartVector[1], $adjustedStartVector[0]) * 180.0 / [Math]::PI
        $startAngleDelta = $adjustedStartAngle - $baseStartAngle
        while ($startAngleDelta -gt 180.0) { $startAngleDelta -= 360.0 }
        while ($startAngleDelta -lt -180.0) { $startAngleDelta += 360.0 }

        $reverseTurns = 0
        for ($i = 1; $i -lt $arcSamples; $i++) {
            $a = $adjustedPoints[$i - 1]
            $b = $adjustedPoints[$i]
            $c = $adjustedPoints[$i + 1]
            $abX = $b[0] - $a[0]
            $abY = $b[1] - $a[1]
            $bcX = $c[0] - $b[0]
            $bcY = $c[1] - $b[1]
            if (($abX * $bcY - $abY * $bcX) -lt -0.00000001) {
                $reverseTurns++
            }
        }

        if ([Math]::Abs($baseLength - 9.2148363317) -gt 0.0005) {
            $failures.Add("numeric base-centerline length changed unexpectedly: $baseLength")
        }
        if ([Math]::Abs($lengthDeltaPercent) -ge 0.5) {
            $failures.Add("numeric adjusted-centerline length delta must stay below 0.5 percent; measured $lengthDeltaPercent percent.")
        }
        if ([Math]::Abs([Math]::Abs($startAngleDelta) - 25.0) -gt 1.0) {
            $failures.Add("numeric start tangent turn must be 25 degrees +/- 1; measured $startAngleDelta degrees.")
        }
        if ($startError -ge 0.001 -or $joinError -ge 0.001 -or $endError -ge 0.001) {
            $failures.Add("numeric centerline endpoint errors must stay below 0.001; measured start=$startError join=$joinError end=$endError.")
        }
        if ($reverseTurns -gt 0) {
            $failures.Add("numeric local centerline curvature must not reverse; measured $reverseTurns reverse turns.")
        }

        Write-Host ("Centerline metrics: base={0:F9} adjusted={1:F9} delta={2:F6}% startTurn={3:F3}deg startErr={4:E3} joinErr={5:E3} endErr={6:E3} reverseTurns={7}" -f `
            $baseLength, $adjustedLength, $lengthDeltaPercent, $startAngleDelta, $startError, $joinError, $endError, $reverseTurns)

        $visualRotationDegrees = Get-SourceFloat $builder 'private const float CoreOnlyVisualRotationDegrees = (-?[0-9.]+)f;' "core-only visual rotation must be available for screen-space chord verification."
        if (-not [double]::IsNaN($visualRotationDegrees)) {
            $screenStart = Get-BaseCenterline 0.0
            $screenEnd = Get-BaseCenterline 1.0
            $localChordDegrees = [Math]::Atan2($screenEnd[1] - $screenStart[1], $screenEnd[0] - $screenStart[0]) * 180.0 / [Math]::PI
            $screenChordDegrees = $localChordDegrees + $visualRotationDegrees
            while ($screenChordDegrees -gt 180.0) { $screenChordDegrees -= 360.0 }
            while ($screenChordDegrees -le -180.0) { $screenChordDegrees += 360.0 }
            Write-Host ("Screen-space orientation metrics: localChord={0:F3}deg rotation={1:F3}deg screenChord={2:F3}deg" -f $localChordDegrees, $visualRotationDegrees, $screenChordDegrees)
            if ($screenChordDegrees -lt -94.0 -or $screenChordDegrees -gt -86.0) {
                $failures.Add("approved screen-space endpoint chord must remain near vertical at -94 to -86 degrees; measured $screenChordDegrees degrees.")
            }
        }
    }

    foreach ($coreContract in @(
        @('_CenterPlateauWidth \("Center Plateau Width", Float\) = 0\.46', "core shader center plateau width must be 0.46."),
        @('_CenterFalloffPower \("Center Falloff Power", Float\) = 1\.70', "core shader center falloff power must be 1.70."),
        @('_LuminousBodyStrength \("Luminous Body Strength", Float\) = 0\.68', "core shader luminous-body strength must be 0.68."),
        @('_CoreFillStrength \("Core Fill Strength", Float\) = 0\.80', "core shader fill strength must be 0.80."),
        @('_CoreWidthScale \("Core Width Scale", Float\) = 1\.50', "core shader must expose a 1.50 physical hotspot width scale."),
        @('_RevealEdgeWidth \("Reveal Edge Width", Float\) = 0\.030', "core shader reveal edge width must keep one compact shared transition at 0.030."),
        @('_BrushAbrasionStrength \("Brush Abrasion Strength", Float\) = 0\.16', "core shader abrasion strength must be reduced to 0.16."),
        @('_BrushFiberStrength \("Brush Fiber Strength", Float\) = 0\.44', "core shader fiber strength must be 0.44."),
        @('_SoftEnvelopeStrength \("Soft Envelope Strength", Float\) = 0\.30', "core shader soft-envelope strength must be 0.30."),
        @('_GoldBandCenter \("Gold Band Center", Float\) = 0\.24', "core shader must use one embedded gold shoulder centered at 24 percent local thickness."),
        @('_GoldBandWidth \("Gold Band Width", Float\) = 0\.22', "core shader embedded gold shoulder width must be 0.22."),
        @('_GoldBandStrength \("Gold Band Strength", Float\) = 0\.34', "core shader embedded gold shoulder strength must be 0.34."),
        @('_GoldBandAlpha \("Gold Band Alpha", Float\) = 0\.22', "core shader embedded gold shoulder alpha must be 0.22."),
        @('_InnerAfterglowStart \("Inner Afterglow Start", Float\) = 0\.198', "inner afterglow must begin at s=0.198 for measured 2.00x coverage."),
        @('_InnerAfterglowEnd \("Inner Afterglow End", Float\) = 0\.842', "inner afterglow must end at s=0.842 for measured 2.00x coverage."),
        @('_InnerAfterglowCenter \("Inner Afterglow Center", Float\) = 0\.52', "inner afterglow must remain centered at s=0.52."),
        @('_InnerAfterglowCrossCenter \("Inner Afterglow Cross Center", Float\) = 0\.50', "inner afterglow transverse center must remain at 50 percent local thickness."),
        @('_InnerAfterglowWidth \("Inner Afterglow Width", Float\) = 0\.11', "inner afterglow must remain 11 percent of local body width."),
        @('_InnerAfterglowBrightness \("Inner Afterglow Brightness", Float\) = 1\.08', "inner afterglow must be eight percent brighter than the surrounding core."),
        @('_InnerAfterglowRetreatDelay \("Inner Afterglow Retreat Delay", Float\) = 0\.04', "inner afterglow retreat delay must be four percent of lifecycle."),
        @('_DiagnosticMode \("Diagnostic Mode", Float\) = 0\.0', "core shader must expose deterministic diagnostic isolation modes.")
    )) {
        Assert-Contains $coreShader $coreContract[0] $coreContract[1]
    }

    if ($coreShader -match 'float\s+(outerGoldEdge|innerGoldEdge|goldEdgeMask)') {
        $failures.Add("core shader must remove the two complete separated gold contour lines.")
    }
    Assert-Contains $coreShader 'float goldBand = 1\.0 - smoothstep\(_GoldBandWidth \* 0\.62,\s*_GoldBandWidth,\s*abs\(outerToInner - _GoldBandCenter\)\);' "core shader must generate one overlapping gold band rather than two edge contours."
    Assert-Contains $coreShader 'float goldFiberPresence = goldFiber \* goldBand;' "gold fibers must remain inside the single overlapping gold shoulder."
    Assert-Contains $coreShader 'float sharedVisibility = saturate\(revealMask \* pathRetreatFade\);' "core shader must expose one shared reveal and retreat visibility mask."
    Assert-Contains $coreShader 'float finalPathVisibility = sharedVisibility \* alphaFade;' "core shader must apply one final path visibility after all local body, core, gold, and afterglow shading is complete."
    if ($coreShader -match 'float visible = sharedVisibility|visible \*=|tailWidthMask|alphaProfile \*= lerp\(0\.86,\s*1\.0,\s*tailFade\)') {
        $failures.Add("core shader must not retain layer-specific reveal-head width, alpha, or brightness shaping.")
    }
    Assert-Contains $coreShader 'float coreSampleU = saturate\(0\.5 \+ \(saturate\(outerToInner \+ brushDrift\) - 0\.5\) / max\(_CoreWidthScale,\s*0\.001\)\);' "core sampling must expand only the brush-core channel by the dedicated width scale."
    Assert-Contains $coreShader 'float coreHotspotWidth = saturate\(0\.30 \* _CoreWidthScale\);' "core hotspot width must target roughly 45 percent at the approved 1.50 scale."
    Assert-Contains $coreShader 'float coreHotspot = 1\.0 - smoothstep\(coreHotspotWidth,\s*min\(coreHotspotWidth \+ 0\.10,\s*1\.0\),\s*centerDistance\);' "core hotspot must be a localized center mask rather than the broad plateau."
    Assert-Contains $coreShader 'coreHotspot \* 0\.52 \+ brushCore \* 0\.48' "core fill must derive from the localized hotspot without a separate reveal-head alpha band."
    if ($coreShader -match 'float revealEdge|freshCore \* 0\.06|revealEdge \* 0\.04') {
        $failures.Add("core shader must remove reveal-head and fresh-core bands that create layered transparent ghosts.")
    }
    if ($coreShader -match 'corePlateau \* 0\.72') {
        $failures.Add("core shader must not use the broad center plateau as the primary fill.")
    }
    Assert-Contains $coreShader 'float3 whiteCoreColor = float3\(1\.10, 1\.08, 0\.96\);' "core hotspot must keep the approved restrained warm-white value."
    Assert-Contains $coreShader 'float3 neutralBodyColor = float3\(0\.94, 0\.91, 0\.82\);' "main body must use the warm ivory base sampled from Christasha battle-page artwork."
    Assert-Contains $coreShader 'float3 luminousBodyColor = lerp\(float3\(1\.00, 0\.98, 0\.86\),\s*whiteCoreColor,\s*saturate\(0\.58 \+ luminousBody \* 0\.42\)\);' "luminous body must remain clean white-gold instead of gray chrome."
    Assert-Contains $builder 'float3 whiteCoreColor = float3\(1\.10, 1\.08, 0\.96\);' "builder embedded core shader must match the restrained warm-white hotspot."
    Assert-Contains $builder 'float3 neutralBodyColor = float3\(0\.94, 0\.91, 0\.82\);' "builder embedded core shader must match the battle-page ivory body."
    Assert-Contains $coreShader 'float tipCoreFade = 1\.0 - smoothstep\(0\.94,\s*0\.995,\s*pathT\);' "core shader must fade only the terminal hotspot before the geometric endpoint."
    Assert-Contains $coreShader 'coreHotspot \*= tipCoreFade;' "core hotspot must use the terminal path fade."
    Assert-Contains $coreShader 'coreFill \*= tipCoreFade;' "core fill must use the terminal path fade."
    if ($coreShader -match 'bodyContinuity \* 0\.02 \+ goldFiberPresence \* 0\.14') {
        $failures.Add("core shader must not tint the broad body warm gold.")
    }
    Assert-Contains $builder 'CreateCoreOnlyWhiteMaterial\("Assets/Textures/christasha_dawn_solid_white\.png",\s*"Assets/Materials/ChristashaDawn_CrescentCoreOnly_WhiteHdr\.mat",\s*new Color\(1\.0f,\s*1\.0f,\s*1\.0f,\s*1f\),\s*3160\)' "core material tint and HDR multiplier must remain neutral white so the platinum body is not warmed twice."
    $neutralCoreMaterialCalls = [regex]::Matches($builder, 'new Color\(1\.0f,\s*1\.0f,\s*1\.0f,\s*1f\),\s*3160').Count
    if ($neutralCoreMaterialCalls -lt 2) {
        $failures.Add("both BuildBundle and preview-only core material creation paths must use the neutral white multiplier.")
    }
    if ($builder -match 'new Color\(1\.14f,\s*1\.12f,\s*0\.98f,\s*1f\),\s*3160') {
        $failures.Add("core material creation must not retain the previous warm multiplier.")
    }
    if ($coreShader -match 'lerp\(shadowGold,\s*warmGold') {
        $failures.Add("core shader must not build the broad body from a shadow-to-gold sheet.")
    }
    Assert-Contains $coreShader 'bodyContinuity \* 0\.19' "core alpha body-continuity weight must be 0.19."
    Assert-Contains $coreShader 'softEnvelope \* 0\.17' "core alpha must add a uniform low-frequency soft-envelope body contribution of 0.17."
    Assert-Contains $coreShader 'goldFiberPresence \* 0\.10' "core alpha gold-fiber weight must be 0.10."
    Assert-Contains $coreShader 'float bodyAlphaProfile = saturate\(' "core shader must establish body coverage before gold material modulation."
    Assert-Contains $coreShader 'float attachedGoldAlphaScale = 1\.0 \+ goldFiberPresence \* 0\.10 \+ goldBand \* _GoldBandAlpha;' "gold shoulder alpha must scale existing body coverage instead of creating an independent shell."
    Assert-Contains $coreShader 'float alphaProfile = saturate\(bodyAlphaProfile \* attachedGoldAlphaScale\);' "gold shoulder must remain attached to the luminous body alpha."
    Assert-Contains $coreShader 'luminousBody \* 0\.60' "core alpha luminous-body weight must be 0.60."
    Assert-Contains $coreShader 'coreFill \* 0\.66' "core alpha fill weight must be 0.66."
    Assert-Contains $coreShader 'goldBand \* _GoldBandAlpha' "single embedded gold shoulder must contribute alpha without creating complete contour copies."
    if ($coreShader -match 'tipAlphaFade') {
        $failures.Add("core shader must not fade the complete lower terminal geometry back into a needle.")
    }
    if ($builder -match 'float tipAlphaFade = pow\(1\.0 - smoothstep\(0\.86,\s*0\.96,\s*pathT\)') {
        $failures.Add("builder embedded shaders must not retain the old complete-terminal needle fade.")
    }
    Assert-Contains $coreShader 'float afterglowPathMask = smoothstep\(afterglowStart,\s*afterglowStart \+ afterglowFeather,\s*pathT\)\s*\* \(1\.0 - smoothstep\(afterglowEnd - afterglowFeather,\s*afterglowEnd,\s*pathT\)\);' "inner afterglow must feather symmetrically around s=0.52 inside the configured endpoints."
    Assert-Contains $coreShader 'float afterglowCrossDistance = abs\(outerToInner - _InnerAfterglowCrossCenter\);' "inner afterglow must use the locked transverse center."
    Assert-Contains $coreShader 'float afterglowCrossMask = 1\.0 - smoothstep\(afterglowHalfWidth \* 0\.72,\s*afterglowHalfWidth,\s*afterglowCrossDistance\);' "inner afterglow must remain a narrow single transverse peak."
    Assert-Contains $coreShader 'float afterglowRetreatProgress = saturate\(\(retreatProgress - _InnerAfterglowRetreatDelay\) / max\(1\.0 - _InnerAfterglowRetreatDelay,\s*0\.001\)\);' "inner afterglow must use the independent four-percent delayed retreat."
    Assert-Contains $coreShader 'float afterglowRetreatEnergy = lerp\(1\.0,\s*0\.46,\s*smoothstep\(0\.0,\s*0\.45,\s*retreatProgress\)\);' "early retreat must reduce afterglow energy before it reads as a second crescent."
    Assert-Contains $coreShader 'float afterglowMask = afterglowPathMask \* afterglowCrossMask \* afterglowAlphaFade \* afterglowRetreatEnergy;' "inner afterglow must finish local shaping before the shared final path visibility is applied."
    Assert-Contains $coreShader 'c\.a = saturate\(\(c\.a \* alphaProfile \+ afterglowMask \* 0\.18\) \* finalPathVisibility\);' "core final alpha must multiply every local layer by the same final path visibility exactly once."
    Assert-Contains $coreShader 'float3 afterglowColor = float3\(_InnerAfterglowBrightness,\s*_InnerAfterglowBrightness,\s*_InnerAfterglowBrightness\);' "inner afterglow must remain neutral pure white."
    Assert-Contains $coreShader 'if \(_DiagnosticMode > 2\.5\)' "core shader must expose an afterglow-only diagnostic mode."
    Assert-Contains $coreShader 'if \(_DiagnosticMode > 1\.5\)' "core shader must expose a gold-only diagnostic mode."
    Assert-Contains $coreShader 'if \(_DiagnosticMode > 0\.5\)' "core shader must expose a body/core-only diagnostic mode."

    foreach ($builderContract in @(
        @('mat\.SetFloat\("_CenterPlateauWidth",\s*0\.46f\)', "core material center plateau width must be 0.46."),
        @('mat\.SetFloat\("_CenterFalloffPower",\s*1\.70f\)', "core material center falloff power must be 1.70."),
        @('mat\.SetFloat\("_LuminousBodyStrength",\s*0\.68f\)', "core material luminous-body strength must be 0.68."),
        @('mat\.SetFloat\("_CoreFillStrength",\s*0\.80f\)', "core material fill strength must be 0.80."),
        @('mat\.SetFloat\("_CoreWidthScale",\s*1\.50f\)', "core material width scale must be 1.50."),
        @('mat\.SetFloat\("_RevealEdgeWidth",\s*0\.030f\)', "core and glow material reveal edge width must be 0.030."),
        @('mat\.SetFloat\("_BrushAbrasionStrength",\s*0\.16f\)', "core material abrasion strength must be reduced to 0.16."),
        @('mat\.SetFloat\("_BrushFiberStrength",\s*0\.44f\)', "core material fiber strength must be 0.44."),
        @('mat\.SetFloat\("_SoftEnvelopeStrength",\s*0\.30f\)', "core material soft-envelope strength must be 0.30."),
        @('mat\.SetFloat\("_GoldBandCenter",\s*0\.24f\)', "core material gold band center must be 0.24."),
        @('mat\.SetFloat\("_GoldBandWidth",\s*0\.22f\)', "core material gold band width must be 0.22."),
        @('mat\.SetFloat\("_GoldBandStrength",\s*0\.34f\)', "core material gold band strength must be 0.34."),
        @('mat\.SetFloat\("_GoldBandAlpha",\s*0\.22f\)', "core material gold band alpha must be 0.22."),
        @('mat\.SetFloat\("_InnerAfterglowStart",\s*0\.198f\)', "core material afterglow start must be 0.198."),
        @('mat\.SetFloat\("_InnerAfterglowEnd",\s*0\.842f\)', "core material afterglow end must be 0.842."),
        @('mat\.SetFloat\("_InnerAfterglowCenter",\s*0\.52f\)', "core material afterglow center must be 0.52."),
        @('mat\.SetFloat\("_InnerAfterglowCrossCenter",\s*0\.50f\)', "core material afterglow transverse center must be 0.50."),
        @('mat\.SetFloat\("_InnerAfterglowWidth",\s*0\.11f\)', "core material afterglow width must be 0.11."),
        @('mat\.SetFloat\("_InnerAfterglowBrightness",\s*1\.08f\)', "core material afterglow brightness must be 1.08."),
        @('mat\.SetFloat\("_InnerAfterglowRetreatDelay",\s*0\.04f\)', "core material afterglow retreat delay must be 0.04."),
        @('mat\.SetFloat\("_DiagnosticMode",\s*0\.0f\)', "core material diagnostic mode must default to normal rendering.")
    )) {
        Assert-Contains $builder $builderContract[0] $builderContract[1]
    }

    if ($glowShader -match 'float bladeContact|outerRim|innerRim|rimMask|max\(outerRim') {
        $failures.Add("edge glow shader must remove blade-contact and dual-rim silhouette logic.")
    }
    foreach ($glowContract in @(
        @('_TintColor \("Tint Color", Color\) = \(1\.00,0\.82,0\.50,0\.42\)', "edge glow tint must carry enough warm-gold energy to read as emitted light."),
        @('_RevealEdgeWidth \("Reveal Edge Width", Float\) = 0\.030', "edge glow reveal edge width must match the compact core path fade."),
        @('_SoftEnvelopeStrength \("Soft Envelope Strength", Float\) = 0\.60', "edge glow soft-envelope strength must be 0.60."),
        @('Blend One One', "edge glow must use explicitly premultiplied additive blending so it cannot darken the background."),
        @('float softEnvelope = pow\(saturate\(brushTex\.a \* _SoftEnvelopeStrength\),\s*0\.75\);', "edge glow must use a low-frequency soft envelope."),
        @('float sharedVisibility = saturate\(revealMask \* pathRetreatFade\);', "edge glow must use the exact shared reveal and retreat visibility mask."),
        @('float finalPathVisibility = sharedVisibility \* alphaFade;', "edge glow must share the exact final path visibility used by the core shader."),
        @('float attachedCrossMask = 1\.0 - smoothstep\(0\.03,\s*0\.11,\s*abs\(outerToInner - 0\.28\)\);', "edge glow must be one narrow attached shoulder rather than a complete body fill."),
        @('float glowEnvelope = smoothstep\(0\.16,\s*0\.58,\s*softEnvelope\);', "edge glow must remove the low-energy gray fringe before premultiplication."),
        @('float localGlowAlpha = _TintColor\.a \* attachedCrossMask \* glowEnvelope \* 0\.70;', "edge glow must use a bright local soft-light profile without a full-body opacity floor."),
        @('float3 premultipliedGlow = rgb \* localGlowAlpha \* finalPathVisibility;', "edge glow RGB must be premultiplied by the same final path visibility and local alpha."),
        @('return float4\(premultipliedGlow,\s*0\.0\);', "edge glow must output additive warm light without destination-darkening alpha.")
    )) {
        Assert-Contains $glowShader $glowContract[0] $glowContract[1]
    }
    Assert-Contains $builder 'const string checkedInCoreShader = "Assets/Shaders/ChristashaDawn_CoreOnlyFlow\.shader";' "builder must preserve the checked-in core shader as the production source of truth."
    Assert-Contains $builder 'const string checkedInGlowShader = "Assets/Shaders/ChristashaDawn_CoreOnlyEdgeGlow\.shader";' "builder must preserve the checked-in glow shader as the production source of truth."
    Assert-Contains $builder 'AssetDatabase\.ImportAsset\(checkedInCoreShader,\s*ImportAssetOptions\.ForceUpdate\);' "builder must import the checked-in core shader instead of overwriting it with stale embedded source."
    Assert-Contains $builder 'AssetDatabase\.ImportAsset\(checkedInGlowShader,\s*ImportAssetOptions\.ForceUpdate\);' "builder must import the checked-in glow shader instead of overwriting it with stale embedded source."
    if ($glowShader -match '_TintColor\.a \* saturate\(0\.') {
        $failures.Add("edge glow alpha must not contain a constant opacity floor.")
    }
    Assert-Contains $builder 'new Color\(1\.00f,\s*0\.82f,\s*0\.50f,\s*0\.42f\)' "edge glow material tint must use the localized warm-gold light sample."
    Assert-Contains $builder 'mat\.SetColor\("_HdrEmission",\s*new Color\(1\.18f,\s*1\.08f,\s*0\.72f,\s*1f\)\)' "edge glow HDR color must stay visibly warm-gold after quantization."
    Assert-Contains $builder 'mat\.SetFloat\("_SoftEnvelopeStrength",\s*0\.60f\)' "edge glow material soft-envelope strength must be 0.60."
    Assert-Contains $coreShader 'float pathRetreatFade = smoothstep\(retreatThreshold,\s*retreatThreshold \+ edge,\s*pathT\);' "core shader must use the shared path-based retreat fade."
    Assert-Contains $glowShader 'float pathRetreatFade = smoothstep\(retreatThreshold,\s*retreatThreshold \+ edge,\s*pathT\);' "glow shader must use the shared path-based retreat fade."
    Assert-Contains $coreShader 'float extinctionRate = lerp\(1\.12,\s*1\.06,\s*brightnessWeight\);' "core retreat extinction rates must stay tightly grouped around 1.08."
    Assert-Contains $coreShader 'float alphaFade = pow\(saturate\(1\.0 - extinction\),\s*1\.8\);' "core retreat alpha must use the steeper 1.8 extinction curve."
    Assert-Contains $coreShader 'c\.rgb \*= lerp\(0\.01,\s*1\.0,\s*alphaFade\);' "core RGB retreat floor must be no higher than 0.01."
    Assert-Contains $glowShader 'float alphaFade = pow\(saturate\(1\.0 - extinction\),\s*1\.8\);' "glow retreat must share the steeper global extinction curve."
    Assert-Contains $glowShader 'rgb \*= lerp\(0\.01,\s*1\.0,\s*alphaFade\);' "glow RGB retreat floor must be no higher than 0.01."
    Assert-Contains $builder 'float extinctionRate = lerp\(1\.12,\s*1\.06,\s*brightnessWeight\);' "builder embedded core shader must match the unified retreat extinction rates."
    Assert-Contains $builder 'c\.rgb \*= lerp\(0\.01,\s*1\.0,\s*alphaFade\);' "builder embedded core shader must match the 0.01 RGB retreat floor."
    if ($coreShader -match '1\.85 \+ tailExtinction|0\.78 \+ tailExtinction') {
        $failures.Add("core shader must remove the previous large bright/dark retreat-rate split.")
    }
    if ($glowShader -match 'outerToInner - retreatThreshold|retreatMask') {
        $failures.Add("glow retreat must not use an across-blade mask.")
    }

    Assert-Contains $builder 'float microScratch = Mathf\.Pow\(' "brush mask must retain only fine path-aligned micro scratches."
    Assert-Contains $builder 'microScratch\s*\* sparseScratch\s*\* softEnvelope\s*\* 0\.08f' "brush mask abrasion must stay sparse and low amplitude."
    if ($builder -match 'float chippedGap|float scratchPhase|chippedGap \* 0\.34f') {
        $failures.Add("brush mask must remove broad chipped gaps and transverse dirty bands.")
    }
    Assert-Contains $coreShader 'luminousBody \*= 1\.0 - abrasionCut \* 0\.04;' "abrasion must only minimally modulate luminous-body brightness."
    Assert-Contains $coreShader 'brushColor \*= 1\.0 - abrasionCut \* 0\.04;' "abrasion must not create a near-black outer bevel."
    Assert-Contains $coreShader 'alphaProfile \*= 1\.0 - abrasionCut \* 0\.04;' "abrasion must not punch chrome-like dark channels in body alpha."

    Assert-Contains $builder 'round8_implementation' "preview renderer must write to the requested Round 8 implementation directory."
    Assert-Contains $builder 'christasha_dawn_round8_geometry_overlay\.png' "preview renderer must generate a geometry overlay."
    Assert-Contains $builder 'christasha_dawn_round8_early\.png' "preview renderer must generate the early reveal frame."
    Assert-Contains $builder 'christasha_dawn_round8_mid\.png' "preview renderer must generate the 50 percent continuity frame."
    Assert-Contains $builder 'christasha_dawn_round8_full_black\.png' "preview renderer must generate the full black-background frame."
    Assert-Contains $builder 'christasha_dawn_round8_full_gray\.png' "preview renderer must generate the full gray-background frame."
    Assert-Contains $builder 'christasha_dawn_round8_battle_background\.png' "preview renderer must generate the combat-context frame."
    Assert-Contains $builder 'christasha_dawn_round8_retreat_early\.png' "preview renderer must generate the early retreat frame."
    Assert-Contains $builder 'christasha_dawn_round8_retreat_late\.png' "preview renderer must generate the late retreat frame."
    Assert-Contains $builder 'christasha_dawn_round8_core_only\.png' "preview renderer must generate the body/core-only diagnostic."
    Assert-Contains $builder 'christasha_dawn_round8_gold_only\.png' "preview renderer must generate the gold-only diagnostic."
    Assert-Contains $builder 'christasha_dawn_round8_glow_only\.png' "preview renderer must generate the glow-only diagnostic."
    Assert-Contains $builder 'christasha_dawn_round8_inner_afterglow_baseline\.png' "preview renderer must generate the baseline afterglow diagnostic."
    Assert-Contains $builder 'christasha_dawn_round8_inner_afterglow_2x\.png' "preview renderer must generate the 2x afterglow diagnostic."
    Assert-Contains $builder 'christasha_dawn_round8_inner_afterglow_comparison_baseline_left_2x_right\.png' "preview renderer must generate the baseline-left/2x-right comparison."
    Assert-Contains $builder 'christasha_dawn_round8_silhouette_freeze_overlay\.png' "preview renderer must generate approved-versus-current silhouette freeze evidence."
    Assert-Contains $builder 'christasha_dawn_round8_afterglow_position_overlay\.png' "preview renderer must generate body-outline plus isolated-afterglow positioning evidence."
    Assert-Contains $builder 'christasha_dawn_round8_representative_battle\.png' "preview renderer must generate a representative Christasha-versus-target battle frame."
    Assert-Contains $builder 'CreateRound8BattleBackground\(' "preview renderer must create a deterministic battle-style context without changing runtime assets."
    Assert-Contains $builder 'CreateRound8GeometryOverlay\(' "preview renderer must create a deterministic geometry overlay without changing runtime assets."
    Assert-Contains $builder 'CreateRound8SilhouetteFreezeOverlay\(' "preview renderer must overlay approved and current silhouettes in the same transformed canvas."
    Assert-Contains $builder 'CreateRound8AfterglowPositionOverlay\(' "preview renderer must show isolated afterglow inside the current body outline."
    Assert-Contains $builder 'CreateRound8RepresentativeBattleScene\(' "preview renderer must place Christasha, the target, and the slash in one representative battle composition."
    Assert-Contains $builder 'Templates/christasha_rmbg\.png' "representative battle evidence must use the real Christasha sprite sheet."
    Assert-Contains $builder 'Templates/Ailirial_rmbg\.png' "representative battle evidence must include a real target sprite sheet."
    Assert-Contains $builder 'new Rect\(690f,\s*731f,\s*346f,\s*366f\)' "representative battle must crop the complete top-right Christasha pose from the actual 1036x1097 sheet."
    Assert-Contains $builder 'new Rect\(0f,\s*1365f,\s*683f,\s*683f\)' "representative battle must crop the complete top-left target pose from the actual 2048x2048 sheet."
    Assert-Contains $builder 'root\.transform\.localScale = Vector3\.one \* 0\.58f;' "representative battle must scale the slash to a readable character-relative size."
    Assert-Contains $builder 'CombinePreviewTextures\(' "preview renderer must combine the baseline-left and 2x-right afterglow evidence."
    Assert-Contains $builder 'renderer\.gameObject\.name == "AB_DawnSlashV_CoreOnlyGoldEdgeGlow"' "diagnostic preview must identify the glow renderer explicitly."
    Assert-Contains $builder 'propertyBlock\.SetFloat\(diagnosticModeId,\s*diagnosticMode\);' "preview diagnostics must drive the core shader isolation mode through a property block."
    Assert-Contains $builder 'CreateRound8RevealFrontDiagnostic\(' "preview renderer must generate high-contrast cross-layer reveal-front diagnostics."
    Assert-Contains $builder 'christasha_dawn_round8_early_reveal_front_diagnostic\.png' "preview renderer must write the Early reveal-front diagnostic."
    Assert-Contains $builder 'christasha_dawn_round8_mid_reveal_front_diagnostic\.png' "preview renderer must write the Mid reveal-front diagnostic."
    Assert-Contains $builder 'bool\[\] originalRendererEnabled = new bool\[renderers\.Length\];' "diagnostic preview must snapshot each renderer enabled state."
    Assert-Contains $builder 'renderer\.enabled = originalRendererEnabled\[i\];' "diagnostic preview must restore each renderer enabled state."

    if ($builder -match 'CreateUserProvidedCrescentVolumeMesh\(') {
        $failures.Add("builder must remove the old symmetric crescent mesh generator.")
    }
    if ($builder -match 'const float outerGlowExtension = 1\.5f') {
        $failures.Add("main slash mesh must not embed the old outer-glow extension.")
    }
    if ($builder -match 'const float edgeNoise = 0\.03f') {
        $failures.Add("primary slash silhouette must not contain geometric edge noise.")
    }

    foreach ($previewEntry in $requiredPreviewPaths.GetEnumerator()) {
        if (!(Test-Path -LiteralPath $previewEntry.Value)) {
            $failures.Add("required Round 8 preview is missing: $($previewEntry.Value)")
        }
        else {
            $previewImage = [System.Drawing.Image]::FromFile($previewEntry.Value)
            try {
                $expectedWidth = if ($previewEntry.Key -eq 'AfterglowComparison') { 2560 } else { 1280 }
                if ($previewImage.Width -ne $expectedWidth -or $previewImage.Height -ne 720) {
                    $failures.Add("Round 8 preview $($previewEntry.Key) must be ${expectedWidth}x720; measured $($previewImage.Width)x$($previewImage.Height).")
                }
            }
            finally {
                $previewImage.Dispose()
            }
        }
    }

    if (!(Test-Path -LiteralPath $glowOnlyPreviewPath)) {
        $failures.Add("glow-only diagnostic preview is missing: $glowOnlyPreviewPath")
    }
    else {
        $glowMetrics = Get-PreviewImageMetrics $glowOnlyPreviewPath
        $coreCoverageMetrics = if (Test-Path -LiteralPath $coreOnlyPreviewPath) { Get-PreviewImageMetrics $coreOnlyPreviewPath } else { $null }
        $glowCoverageRatio = if ($null -ne $coreCoverageMetrics -and $coreCoverageMetrics.PixelsAboveThree -gt 0) {
            $glowMetrics.PixelsAboveThree / $coreCoverageMetrics.PixelsAboveThree
        }
        else {
            1.0
        }
        $glowPeakRounded = [Math]::Round($glowMetrics.PeakDelta, 6)
        Write-Host ("Glow-only image metrics: size={0}x{1} background={2} above3={3} coverageRatio={4:P2} peakDelta={5:F6} meanLuma={6:F3} medianLuma={7:F3} medianRB={8:F3} medianGB={9:F3} warmRatio={10:P2} lowChromaDim={11} belowBackground={12}" -f `
            $glowMetrics.Width, $glowMetrics.Height, $glowMetrics.Background, $glowMetrics.PixelsAboveThree, $glowCoverageRatio, `
            $glowMetrics.PeakDelta, $glowMetrics.MeanPositiveLumaDelta, $glowMetrics.MedianPositiveLumaDelta, `
            $glowMetrics.MedianPositiveRedBlueDelta, $glowMetrics.MedianPositiveGreenBlueDelta, $glowMetrics.WarmVisibleRatio, `
            $glowMetrics.LowChromaDimPixels, $glowMetrics.PixelsBelowBackground)
        if ($glowCoverageRatio -lt 0.12 -or $glowCoverageRatio -gt 0.46) {
            $failures.Add("glow-only visible area must remain a narrow attached shoulder covering 12-46 percent of the core body, not a full-body fill; measured $glowCoverageRatio.")
        }
        if ($glowPeakRounded -lt 0.160784) {
            $failures.Add("glow-only diagnostic peak must reach at least background + 41/255 as visible warm-gold light; measured $($glowMetrics.PeakDelta).")
        }
        if ($glowPeakRounded -gt 0.380392) {
            $failures.Add("glow-only diagnostic peak must stay at or below background + 97/255 to avoid a detached second contour; measured $($glowMetrics.PeakDelta).")
        }
        if ($glowMetrics.PixelsAboveThree -lt 3000) {
            $failures.Add("glow-only diagnostic must contain at least 3000 pixels above background + 3/255; measured $($glowMetrics.PixelsAboveThree).")
        }
        if ($glowMetrics.WarmVisibleRatio -lt 0.90) {
            $failures.Add("glow-only visible pixels must be at least 90 percent warm R>G>B light; measured $($glowMetrics.WarmVisibleRatio).")
        }
        if ($glowMetrics.MeanPositiveLumaDelta -lt 32.0) {
            $failures.Add("glow-only mean visible luminance must reach at least background + 32/255 so the pass reads as emitted light; measured $($glowMetrics.MeanPositiveLumaDelta).")
        }
        if ($glowMetrics.MedianPositiveLumaDelta -lt 38.0) {
            $failures.Add("glow-only median visible luminance must reach at least background + 38/255 so the pass does not read as a dim brown silhouette; measured $($glowMetrics.MedianPositiveLumaDelta).")
        }
        if ($glowMetrics.MedianPositiveRedBlueDelta -lt 22.0 -or $glowMetrics.MedianPositiveGreenBlueDelta -lt 14.0) {
            $failures.Add("glow-only median visible color must preserve a clear warm-gold separation of at least R-B 22 and G-B 14; measured R-B $($glowMetrics.MedianPositiveRedBlueDelta), G-B $($glowMetrics.MedianPositiveGreenBlueDelta).")
        }
        if ($glowMetrics.LowChromaDimPixels -gt 5000) {
            $failures.Add("glow-only diagnostic must not contain a broad dim gray/brown body shell; measured $($glowMetrics.LowChromaDimPixels) low-chroma dim pixels.")
        }
        if ($glowMetrics.PixelsBelowBackground -gt 0) {
            $failures.Add("glow-only diagnostic must never darken the background; measured $($glowMetrics.PixelsBelowBackground) pixels below background luminance.")
        }
    }

    foreach ($shellCheck in @(
        @($coreOnlyPreviewPath, 'CoreOnly', 2000, 0),
        @($fullPreviewPath, 'FullBlack', 1500, 0),
        @($fullGrayPreviewPath, 'FullGray', 1500, 50)
    )) {
        if (Test-Path -LiteralPath $shellCheck[0]) {
            $shellMetrics = Get-PreviewImageMetrics $shellCheck[0]
            Write-Host ("{0} shell metrics: lowChromaDim={1} belowBackground={2}" -f $shellCheck[1], $shellMetrics.LowChromaDimPixels, $shellMetrics.PixelsBelowBackground)
            if ($shellMetrics.LowChromaDimPixels -gt [int]$shellCheck[2]) {
                $failures.Add("$($shellCheck[1]) must not retain a full-length deep gray shell; measured $($shellMetrics.LowChromaDimPixels) low-chroma dim pixels.")
            }
            if ($shellMetrics.PixelsBelowBackground -gt [int]$shellCheck[3]) {
                $failures.Add("$($shellCheck[1]) must not darken its background; measured $($shellMetrics.PixelsBelowBackground) pixels below background luminance.")
            }
        }
    }

    foreach ($innerArcCheck in @(
        @($fullPreviewPath, 'FullBlack'),
        @($fullGrayPreviewPath, 'FullGray'),
        @($battlePreviewPath, 'BattleBackground')
    )) {
        if (Test-Path -LiteralPath $innerArcCheck[0]) {
            $innerArcMetrics = Get-InnerArcDarkRunMetrics $innerArcCheck[0]
            Write-Host ("{0} inner-arc metrics: eligibleRows={1} darkRows={2} darkRatio={3:P2} longestRun={4}" -f `
                $innerArcCheck[1], $innerArcMetrics.EligibleRows, $innerArcMetrics.DarkRows, $innerArcMetrics.DarkRowRatio, $innerArcMetrics.LongestDarkRun)
            if ($innerArcMetrics.DarkRowRatio -gt 0.35) {
                $failures.Add("$($innerArcCheck[1]) inner arc must not carry low-luminance gray/brown material across more than 35 percent of the major arc; measured $($innerArcMetrics.DarkRowRatio).")
            }
            if ($innerArcMetrics.LongestDarkRun -gt 90) {
                $failures.Add("$($innerArcCheck[1]) inner arc must not contain a continuous low-luminance gray/brown run longer than 90 rows; measured $($innerArcMetrics.LongestDarkRun).")
            }
        }
    }

    if (!(Test-Path -LiteralPath $earlyPreviewPath)) {
        $failures.Add("early preview is missing: $earlyPreviewPath")
    }
    else {
        $earlyMetrics = Get-PreviewImageMetrics $earlyPreviewPath
        Write-Host ("Early image metrics: visibleRows={0} narrowRows<=10={1} narrowRatio={2:P2}" -f `
            $earlyMetrics.VisibleRowsAboveOne, $earlyMetrics.NarrowRowsAtMostTen, $earlyMetrics.NarrowRowRatio)
        if ($earlyMetrics.NarrowRowRatio -ge 0.50) {
            $failures.Add("early preview must keep rows at or below ten pixels under half of visible rows; measured $($earlyMetrics.NarrowRowRatio).")
        }
    }

    foreach ($diagnosticEntry in @(
        @('Early', $requiredPreviewPaths.EarlyRevealFrontDiagnostic),
        @('Mid', $requiredPreviewPaths.MidRevealFrontDiagnostic)
    )) {
        if (Test-Path -LiteralPath $diagnosticEntry[1]) {
            $frontMetrics = Get-RevealFrontDiagnosticMetrics $diagnosticEntry[1]
            Write-Host ("{0} reveal-front metrics: body={1} gold={2} glow={3} spread={4}px" -f `
                $diagnosticEntry[0], $frontMetrics.BodyFront, $frontMetrics.GoldFront, $frontMetrics.GlowFront, $frontMetrics.SpreadPixels)
            if ($frontMetrics.SpreadPixels -gt 2) {
                $failures.Add("$($diagnosticEntry[0]) body/core, gold shoulder, and Glow reveal fronts must coincide within 2 pixels; measured $($frontMetrics.SpreadPixels) pixels.")
            }
        }
    }

    if ((Test-Path -LiteralPath $requiredPreviewPaths.AfterglowBaseline) -and (Test-Path -LiteralPath $requiredPreviewPaths.Afterglow2x)) {
        $baselineAfterglowMetrics = Get-PreviewImageMetrics $requiredPreviewPaths.AfterglowBaseline
        $afterglow2xMetrics = Get-PreviewImageMetrics $requiredPreviewPaths.Afterglow2x
        $afterglowPixelRatio = if ($baselineAfterglowMetrics.PixelsAboveThree -gt 0) {
            $afterglow2xMetrics.PixelsAboveThree / $baselineAfterglowMetrics.PixelsAboveThree
        }
        else {
            0.0
        }
        Write-Host ("Inner-afterglow metrics: baselineAbove3={0} twoXAbove3={1} ratio={2:F3}" -f `
            $baselineAfterglowMetrics.PixelsAboveThree, $afterglow2xMetrics.PixelsAboveThree, $afterglowPixelRatio)
        if ($afterglowPixelRatio -lt 1.90 -or $afterglowPixelRatio -gt 2.10) {
            $failures.Add("2x inner afterglow visible pixel coverage must be 1.90-2.10 times baseline; measured $afterglowPixelRatio.")
        }
    }

    if (!(Test-Path -LiteralPath $latePreviewPath)) {
        $failures.Add("late-retreat preview is missing: $latePreviewPath")
    }
    else {
        $lateMetrics = Get-PreviewImageMetrics $latePreviewPath
        Write-Host ("Late-retreat image metrics: size={0}x{1} background={2} above8={3} peakDelta={4:F6}" -f `
            $lateMetrics.Width, $lateMetrics.Height, $lateMetrics.Background, $lateMetrics.PixelsAboveEight, $lateMetrics.PeakDelta)
        if ($lateMetrics.PixelsAboveEight -ge 500) {
            $failures.Add("late-retreat preview must keep pixels above background + 8/255 below 500; measured $($lateMetrics.PixelsAboveEight).")
        }
        if ($lateMetrics.PeakDelta -gt 0.02) {
            $failures.Add("late-retreat preview peak must stay at or below background + 0.02; measured $($lateMetrics.PeakDelta).")
        }
    }
}

if ($failures.Count -gt 0) {
    Write-Host "Christasha brushwork source verification failed:"
    foreach ($failure in $failures) {
        Write-Host " - $failure"
    }
    exit 1
}

Write-Host "Christasha brushwork source verification passed."
