# Anhier independent texture combat effects

Six original, reusable effects for the existing 18 card dice. Sea attacks use blue
water and white foam; memory attacks retain black-purple pigment and local orange
heat. No Bada skin, game character image or native attack texture ships with them.
Native effects provide timing, attachment and scale references only.

The production adapter uses native H/J/Z/G action pivots, inherits the complete
parent transform once, and preserves native `2 * (1 + roll)` scaling (Guard has
half the ordinary base scale). Sprites use the native `Particle` sorting layer.
Slash is a new vertical falling arc; Hit reuses the original horizontal slash
master as an attacker-owned sweep. Pierce retains a long leading edge and dark
or blue body. Guard stays at the defender's own action pivot.

Melee shows its full shape at the attack event, holds through 250ms, then fades to
zero at approximately 667ms with the native curve. Default object lifetime is 1s.
The initialized visual clock and later `SetDestroyTime` changes are separate, as
in the original base class. SeaFarHit has a 500ms window: simultaneous full muzzle
and target water contact, strong through 333ms, then fading. Its neutral scene
root avoids multiplying the actor's map scale into the ranged effect. The game
continues to own damage confirmation, sound and camera shake.

## Reproduce the approved simulation path

From the repository root, with Python (Pillow, NumPy, imageio-ffmpeg) and .NET 8:

```powershell
powershell -ExecutionPolicy Bypass -File SteriaBuild/VFXSource/AnhierTextureCombat/run_managed_preview.ps1 -BuildRelease
```

The command verifies source and shipping assets, synchronizes exact production
and decompiled native base methods into the ignored managed fixture, runs its
contracts, renders the independent mannequin scenes, builds the mod against the
game API, and packages real-speed media and candidate hashes. Omit `-BuildRelease`
for subsequent simulation-only runs when a matching Release DLL already exists.
Normal runs reuse the extracted native reference cache. A missing cache is
prepared by the existing read-only reference scripts from the locally installed
game files. The command does not launch the game or Unity.

The primary presentation uses opaque geometric mannequins with authored hand
pivots, two actor scales (3 and 2.25), opposite facings and one shared camera.
Near effects use a shared engagement distance; the ranged demonstration keeps
wider spacing. No native character pixels or skin-baked Effect(9) layer appear
in these scenes. All effect matrices come from the compiled production adapter's
`Initialize -> SetScale -> SetLayer -> Update` path, not independent placement math.

Primary outputs under `previews/`:

- `independent-all-six-60fps.mp4`: both sizes, 60fps, 9 seconds across three repeats.
- `independent-all-six-30fps.gif`: both sizes, a 3-second loop using 30/40/30ms delays.
- `independent-<Profile>-two-sizes-60fps.mp4`: full-resolution, side-by-side sizes.
- `independent-phase-contact.png`: 0/17/50/150/250/400/667ms.
- `independent-two-size-contact.png`: all six effects at 150ms in both sizes/facings.
- `independent/<Profile>_<large|small>/00..60.png`: actual 60fps source frames.
- `candidate-hashes.json`: exact core, adapter, profiles, PNGs, scripts, simulation,
  checks, media and Release DLL fingerprints; visual acceptance is a separate gate.

`managed-frames.json` and optional `render_managed.py` A/B images are native
reference evidence only. Older top-level `all-six-*`, `phase-contact.png` and
standing-character previews are superseded and are not current delivery media.
`run_preview.ps1` is the legacy Unity route, not this task's acceptance entry.
Both installed Unity 2019 editors failed their existing license checks; these
outputs are explicitly managed CPU simulation, not Unity GPU or game capture.
Per the user's instruction, simulation is sufficient; opening the game is not a
completion requirement. Deployment remains a separate main-thread action.

## Asset provenance and maintenance

`imagegen_masters/inputs.json` selects the original masters with relative paths.
The new Slash master lives in `native_vertical/`; the previous horizontal Slash
master is now Hit's input. Color keying preserves dark pigment, and primary/fleck
layers remain spatially disjoint. Only original `source_assets/*.png` and the six
shipping JSON profiles are runtime inputs. All six use about 54.1 MiB decoded RGBA,
loaded once per profile. One shared `Sprites/Default` material uses RGBA alpha;
no custom shader, AssetBundle, sound or shake is introduced.

Only when changing art inputs, run `python -X utf8 import_masters.py` from this
directory before the managed command. Unchanged assets need no regeneration.
`author_assets.py` writes only the superseded `experiments/procedural_v0/` area.
Generated production copies, decompiled base copies, extracted game references
and simulation runtime files remain ignored and do not enter the mod package.
