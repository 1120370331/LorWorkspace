# Christasha Dawn Slash Brushwork Design

## Goal

Preserve Christasha's white-gold crescent silhouette while replacing the current smooth, liquid-plastic surface with a directional, layered slash texture inspired by Hanzhou's visual construction.

## Diagnosed Cause

The current generated flow mask is dominated by broad, rounded contour bands. The core shader then fills most of the crescent with a stable bright plateau and overlays a uniformly scaled gold glow mesh. Together these choices read as illuminated resin rather than a fast weapon stroke.

## Visual Direction

- Keep the existing white-gold palette and crescent geometry.
- Make the white core narrow, sharp, and directionally stretched along the slash path.
- Break the gold edge into irregular fibers, dry-brush gaps, and sparse hot flecks.
- Introduce restrained dark abrasion inside the body to shape the stroke without muddying it.
- Keep soft bloom on a separate low-alpha layer so it supports the stroke instead of flattening it.
- Animate texture flow along the arc and let older portions lose fibers before the main body disappears.

## Layer Structure

1. **White blade core:** stable narrow highlight with uneven edge pressure.
2. **Gold brush body:** directional strands with controlled gaps and variable density.
3. **Dark abrasion:** sparse low-alpha cuts that provide depth and remove the plastic sheet look.
4. **Outer glow:** broad, soft, low-opacity silhouette independent from brush detail.

## Constraints

- Unity version remains 2019.3.15f1.
- Existing prefab names, runtime effect registration, crescent geometry, travel positioning, and animation duration remain compatible.
- Generated assets stay original; Hanzhou assets are reference-only.
- No new third-party runtime dependencies.
- The effect must remain readable against Library of Ruina combat backgrounds.

## Acceptance Criteria

- The generated mask contains predominantly directional fibers rather than rounded cellular contours.
- The main body has visible white-core, gold-fiber, and dark-abrasion separation.
- The outer glow is softer and less detailed than the body.
- At reduced scale the crescent remains continuous and bright.
- Source verification, Unity AssetBundle build, and C# build complete without errors attributable to this change.
