# Cloud function adaptations

Fixed reference hashes: `cloud-source-references.json`. Full redistribution notices: `NOTICE.txt`.

| Source | Local symbol / adaptation |
| --- | --- |
| stegu classicnoise3D.glsl pnoise, mod289, permute, taylorInvSqrt, fade (2024-11-07 file) | CloudNoise/PeriodicPerlin.hlsl::PeriodicPerlin and helpers. GLSL mod is positiveMod (`x-y*floor(x/y)`), vec/fract/mix translated to HLSL; permutation +10, quintic fade and 2.2 scaling retained. |
| SebLague NoiseGenCompute.compute worley | CloudNoiseBake.compute::WorleyF1: 27 neighboring cells, periodic feature points, F1 Euclidean distance. Wrapped-cell translation directly selects its periodic image instead of re-searching 27 duplicate translations. |
| SebLague CSWorley / CSNormalize | CloudNoiseBake.compute::CSRaw and SlazeyaStormCloudNoiseBaker::Pack: voxel centers, separate raw F1 channels, one min/max normalization per channel then inversion. No runtime neighborhood search. |
| sebh main.cpp remap and Perlin-Worley/FBM recipe | Baker::Pack: PW=lerp(.625W4+.25W8+.125W16,1,P); packed channels follow the approved local recipe, not sebh's original packed-FBM channel semantics. Its GLM/Shadertoy noise implementation is not copied. |
| SebLague Clouds.shader sampleDensity | Reference for separating occupied base shape, independent erosion and nonnegative density. The final local ring uses a signed cross-section distance and thresholded W4/W8/W16 density; the earlier coverage-remap/inverse-cubed recipe was replaced after visual rejection. |
| SebLague lightmarch / hg / phase | SlazeyaStormCloudVolume.shader::lightTransmission and SlazeyaStormCloudDensity.cginc::HG/CloudPhaseHG: full-density integration to the actual box exit, normalized 4-pi double HG. Explicit clocks and local rays replace the reference full-screen/depth/_Time wiring. |
| HDRP EvaluateCloudProperties / DensityRemap / PowderEffect | Read-only architectural comparison; no HDRP or Companion-licensed implementation copied. No PowderEffect added. |

Noise is generated only by the Editor baker. The generated linear RGBA32 Texture3D assets use Repeat, trilinear filtering and complete mip chains. The cache key includes recipe, seed, compute/helper/baker sources, Unity version and graphics backend/device. Mip and source hashes are checked on reuse and Bundle readback.

Runtime diagnostics are separate from offline generation: `CloudDensitySlice.compute::CSSlice` calls the same density include as the volume shader. One box bounds the transported cloud; its changing bounds do not scale the cloud material. The local analytic XZ sink pairs forward parcel motion with inverse density queries, preserving height outside .35H and compressing it locally inside. Shape/detail LOD uses the inverse-map gradient. Eye steps start at H/48 and refine with that gradient, with a fine .06H nucleus interval and a 1024-segment full-path budget. Light integration uses 48 outer steps plus core refinement; a 132-segment cap also permits the 96-outer-step diagnostic. The source shape remains the frozen 2H field with W4/W8/W16 at .5/.25/.125H, and source rolling continues on the explicit clock. Core accumulation is a visual approximation driven by arriving parcels, not a Navier-Stokes simulation. The existing value-noise field remains only for boundary wisps.
