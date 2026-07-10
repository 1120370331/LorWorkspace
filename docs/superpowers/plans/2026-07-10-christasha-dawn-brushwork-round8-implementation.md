# Christasha Dawn Brushwork Round 8 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the approved asymmetric crescent endpoints, monotonic lower inner arc, embedded 2x white afterglow, and a complete Round 8 preview evidence set without deployment or commits.

**Architecture:** Keep the accepted centerline, position, start/end direction, and outer-side reach unchanged. Change only the transverse width distribution and terminal cap to produce a true upper needle, a monotonic lower inner edge, and a finite approximately +26 degree bevel; add the narrow delayed white afterglow inside the existing core shader so it cannot alter the external silhouette. Extend the existing Unity preview method with diagnostic shader modes and deterministic captures written directly to the requested implementation directory.

**Tech Stack:** Unity 2019.4.40f1 batch editor operating on the existing Unity 2019.3 project, C# editor tooling, ShaderLab/Cg, PowerShell source/image verification, .NET Release build.

## Global Constraints

- Preserve `AGENTS.md`, both design specifications, the design board, and unrelated user changes.
- Preserve the accepted centerline, world position, start/end tangent targets, end-to-end reach, and opening direction.
- Keep the body near the existing +30% volume, the hot core near +50%, 50% reveal continuity, and attached gold/glow behavior.
- The upper endpoint is a zero-width needle; the lower endpoint retains 10%-14% of peak width and closes with a short approximately +26 degree bevel.
- The embedded white afterglow covers `s=0.20-0.84`, remains centered at `s=0.52`, uses 8%-14% of local width, is 5%-12% brighter, delays retreat by 3%-6%, and never survives alone for 0.08 seconds or longer.
- Do not deploy, copy AssetBundles, or commit.

---

### Task 1: Lock the Round 8 source contract

**Files:**
- Modify: `SteriaBuild/VFXSource/ChristashaDawnCombat/verify_christasha_brushwork_source.ps1`

**Interfaces:**
- Consumes: builder and standalone shader source plus generated PNG evidence.
- Produces: deterministic RED/GREEN checks for geometry, afterglow, retreat timing, diagnostics, and preview inventory.

- [x] Add assertions for `terminalWidthRatio = 0.12f`, `terminalBevelDegrees = 26f`, upper zero-width taper, lower monotonic width samples, `_InnerAfterglowStart = 0.20`, `_InnerAfterglowEnd = 0.84`, `_InnerAfterglowCenter = 0.52`, `_InnerAfterglowWidth = 0.11`, `_InnerAfterglowBrightness = 1.08`, `_InnerAfterglowRetreatDelay = 0.04`, and `_DiagnosticMode`.
- [x] Add numeric checks that the centerline metrics remain unchanged, lower width samples strictly decrease after `s=0.66`, terminal width is 10%-14% of peak, and the bevel is 18-35 degrees with a target near 26 degrees.
- [x] Point preview checks to `SteriaBuild/SteriaModFolder/Resource/Preview/ChristashaDawnBrushwork/round8_implementation/` and require geometry overlay, early, mid, full black, full gray, battle background, retreat, core-only, gold-only, glow-only, baseline afterglow, 2x afterglow, and combined comparison PNGs.
- [x] Run `& SteriaBuild/VFXSource/ChristashaDawnCombat/verify_christasha_brushwork_source.ps1`; expect exit code 1 because the production constants, shader properties, and new preview files do not exist yet.

### Task 2: Implement the asymmetric frozen-reach mesh

**Files:**
- Modify: `SteriaBuild/VFXSource/AnhierBlueWhiteSlash/UnityProject/Assets/Editor/ChristashaDawnCombatBundleBuilder.cs`

**Interfaces:**
- Consumes: the existing accepted `GetUserSlashCenterline` and legacy outer-side width/reach.
- Produces: a single-sided ribbon with unchanged outer reach, zero-width upper tip, monotonic lower inner arc, and finite beveled lower cap.

- [x] Split the legacy pressure width from the new target width so the outer-side offset and farthest terminal point remain frozen.
- [x] Use a smooth upper tip ramp to zero at `s=0`, retain the existing body maximum, place the sole pressure maximum at `s=0.66`, and smoothly decrease through the approved lower samples to `0.12 * peakWidth` at `s=1`.
- [x] Keep the existing `cuttingReach` for the outer endpoint and retract the inner endpoint along the tangent by the amount required for a +26 degree cap; blend the skew only through the final lower segment.
- [x] Add a geometry overlay helper that draws centerline, outer edge, inner edge, and terminal bevel without mutating runtime assets.

### Task 3: Add the embedded 2x white afterglow

**Files:**
- Modify: `SteriaBuild/VFXSource/AnhierBlueWhiteSlash/UnityProject/Assets/Shaders/ChristashaDawn_CoreOnlyFlow.shader`
- Modify: `SteriaBuild/VFXSource/AnhierBlueWhiteSlash/UnityProject/Assets/Shaders/ChristashaDawn_CoreOnlyEdgeGlow.shader`
- Modify: `SteriaBuild/VFXSource/AnhierBlueWhiteSlash/UnityProject/Assets/Editor/ChristashaDawnCombatBundleBuilder.cs`

**Interfaces:**
- Consumes: path coordinate `pathT`, transverse coordinate `outerToInner`, reveal/retreat controls, and existing body/gold masks.
- Produces: a narrow pure-white internal afterglow plus diagnostic isolation modes; external body and glow geometry remain unchanged.

- [x] Add core shader properties with exact defaults: start `0.20`, end `0.84`, center `0.52`, width `0.11`, brightness `1.08`, retreat delay `0.04`, and diagnostic mode `0`.
- [x] Build a feathered path mask whose start/end feathers remain inside the body, a single transverse peak at 11% local width, reveal gating from the existing path progress, and a separately delayed retreat fade that reaches zero at the lifecycle end.
- [x] Blend the afterglow toward neutral pure white without adding external alpha; add diagnostic modes for body/core only, gold only, and afterglow only.
- [x] Keep gold and edge glow attached to the shared geometry and terminal taper; update builder-embedded shader text and material defaults to match the standalone shaders exactly.

### Task 4: Generate the Round 8 implementation previews

**Files:**
- Modify: `SteriaBuild/VFXSource/AnhierBlueWhiteSlash/UnityProject/Assets/Editor/ChristashaDawnCombatBundleBuilder.cs`
- Generate: `SteriaBuild/SteriaModFolder/Resource/Preview/ChristashaDawnBrushwork/round8_implementation/*.png`

**Interfaces:**
- Consumes: diagnostic mode and afterglow range material properties.
- Produces: all requested visual evidence at 1280x720 plus a 2560x720 baseline-left/2x-right comparison.

- [x] Write captures directly to the requested directory and clear stale PNGs in that directory before rendering.
- [x] Capture geometry overlay, early, mid, full black, full gray, combat-style background, early retreat, late retreat, core-only, gold-only, glow-only, baseline afterglow `0.36-0.68`, 2x afterglow `0.20-0.84`, and a combined comparison.
- [x] Run Unity with `ChristashaDawnCombatBundleBuilder.RenderVerticalCoreOnlyPreview` and require exit code 0 with all PNGs present.
- [x] Restore `ChristashaDawn_CoreOnly_RevealPreview.anim` and `ProjectVersion.txt` to `HEAD` after Unity exits.

### Task 5: Verify and report

**Files:**
- Modify: `.superpowers/sdd/round8-execution-report.md`

**Interfaces:**
- Consumes: RED/GREEN logs, Unity log, build output, diff checks, and preview inventory.
- Produces: evidence-backed completion report without deployment or commit.

- [x] Run the focused verifier and require PASS.
- [x] Run `dotnet build SteriaBuild/Steria.csproj -c Release` and require exit code 0.
- [x] Run `git diff --check` and require no whitespace errors.
- [x] Inspect representative geometry, early, mid, full, retreat, diagnostics, and comparison PNGs; record dimensions and visual risks.
- [x] Confirm the two Unity pollution files match `HEAD`, no deployment outputs were changed, and all requested production/report/preview files exist.
