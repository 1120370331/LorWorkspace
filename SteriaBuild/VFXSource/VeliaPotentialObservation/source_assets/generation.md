# 方形流纹与光影贴图来源

Built-in `image_gen`, transparent RGBA. No custom endpoint/API fallback. Original outputs are saved in `raw/`; `import_textures.py` performs deterministic per-tile alpha-bound crop, uniform downsampling and transparent padding. No complete fountain image is used. Final dimensions are flow atlas 1024² (four 512² tiles), mist 512², mathematical glint 256². Actual original/final SHA256 values are in `provenance.json`.

`texture-contact-sheet.png` shows each tile on black/mid-gray, alpha, luminance×alpha and a 64px view. The main thread accepted this texture checkpoint before native particle rendering. Texture acceptance does not imply full VFX acceptance.

## Flow atlas prompt

> Create a production Unity ParticleSystem sprite texture atlas, 1024x1024 square, a precise 2 by 2 grid of four equal square cells. NO visible grid/borders/labels. Each cell contains ONE different short airy upward-flowing luminous brush wisp, pointing from bottom toward top. These are LOCAL particle brush textures, NOT a complete fountain/beam or scene. A warm near-white inner broken light filament transitions into pale gold then very soft transparent gold edges. Shapes are irregular flowing tapered brushwork with uneven scalloped side edges, fine translucent filaments, broad soft alpha variation and actual transparent gaps through the streak. Four variants have different bends and split tips: gently curved left, gently curved right, two short split filaments, and a thin broken curl. Main stroke roughly 45% cell width and 65% cell height, surrounded by at least15% fully transparent padding on every cell edge. Avoid straight cylinders, uniform rods, solid flames, rounded tubes, fat comma shapes, metallic shading, hard outlines, opaque fill, entire magic effects, circles, stars, particles around the stroke, black backgrounds or checkerboards. Soft hand-painted VFX brush alpha, bright but non-bloomy, no baked surrounding glow haze outside the stroke. Four independent isolated short luminous upward wisps, real RGBA transparency.

Saved original: `raw/flow-brush-raw.png`. Four quadrants remain independent local-brush variants. The final particle atlas does not assume generated source dimensions; import splits actual dimensions equally and places each downsampled alpha-bound brush inside 74% of its 512² cell.

## Mist prompt

> One square Unity ParticleSystem RGBA sprite texture of a small airy luminous mist wisp stretching upward. NOT a scene, NOT a complete fountain. A loose asymmetrical grouping of 3 delicate ivory-white translucent brush veils drifting up and slightly outward, with 2 or3 large truly transparent holes separating parts, smaller torn gaps, and irregular feather-thin fading edges. Extremely soft subtle pale warm light texture, moderate alpha, texture carries gray-to-white brightness variation. No black or gray smoke, no fire, no solid white cloud ball, no circular disc, no thick opaque blob, no hard contour, no lens flare. Occupy only middle65% of square with transparent margins of at least15% all sides. Single isolated local mist brush, transparent RGBA background, no checkerboard, no text.

Saved original: `raw/mist-raw.png`. SoftLift lifetime alpha remains low; this supporting texture is not intended as a dominant smoke cloud.

## Glint

`import_textures.py` creates the small four-ray light mask deterministically from two anisotropic exponential rays plus a weak central halo. Ray lengths differ slightly, all four corners are transparent, and shader alpha is applied once. It is a simple light primitive, not a substitute for authored organic flow artwork.
