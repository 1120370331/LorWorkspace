# Christasha native Pierce / Projectile

## Current: platinum pyramid and flying sword (2026-09-22)

**Current delivery: `through-r12`, deployed 2026-09-22.** Earlier flat-facet and wide/bowed sword forms were rejected. The user selected the original character's slender straight sword, then specified a thrust spanning the release point through and behind the victim, accelerating curved flight, a stronger impact burst and a small shake. This delivery follows those revisions; earlier rejected candidates remain historical evidence only. Reference and shape decisions are recorded in `docs/superpowers/specs/2026-09-22-christasha-strike-research.md`.

`Pierce` contacts the victim at 0.16 s, extends beyond the victim's body over the next 0.075 s, holds briefly and erodes by 0.54 s. Its rear stays at the release point throughout; it is not a short detached packet. `ChristashaThrustEnvelope` uses the victim's body center and map scale to place the exit behind the full figure, while the contact ring stays at the actual hit pivot. Width grows with the full span and is bounded; the representative map-S context covers 23.84 world units with a 10.4896-unit peak carrier height on either facing.

`Projectile` retains the character-like diamond-section straight blade (length/width 12.75), compact straight guard, grip and small pommel. `ChristashaProjectileArc` applies a quadratic acceleration clock to the 0.38 s upward cubic arc and provides its tangent to the adapter and preview. Both endpoints remain live, the tip lands exactly on the target and the wake stays in world space. Contact has a larger white-gold flash, faster/wider shock expansion and 24 short outward fragments. The adapter calls the existing `SteriaEffectHelper.AddScreenShake(.012,.009,70,.14)` once after confirmed projectile contact; misses and cancelled flights do not shake.

`IChristashaStrikePlayback.Aim` receives separate contact and exit points in instance-local coordinates; `ConfirmImpact` is idempotent and `ImpactOccurred` exposes the actual visual contact gate. Confirmation comes from the native `RencounterManager` winning presentation's `CreateDamagedEffectByBehaviour` call, paired with the immediately preceding `CreateBehaviourEffect` and the same victim/frame. Arrival alone never confirms a hit. Unrelated effects keep native feedback; stale/wrong-target/cancelled events cannot consume it.

The driver owns mesh opacity, line history and particles on the existing sole clock. Camera feedback stays in the game adapter, outside the pure visual library. The library owns cached meshes/materials and the original local brush mask; per-instance `_Phase`, `_Opacity` and `_Dissolve` blocks animate axial flow and eroding shock arcs without altering shared materials. Cancellation disables all mesh/line renderers and clears particles. Total lifetimes remain 0.72 s (Pierce) and 0.86 s (Projectile).

Rebuild the exact current production sources and run actual Unity Play Mode:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File SteriaBuild/VFXSource/ChristashaNative/run-frame-loop.ps1 -Output output/christasha-platinum-fix/new-candidate -RequireStable
python SteriaBuild/VFXSource/ChristashaNative/package-frame-loop.py output/christasha-platinum-fix/new-candidate
```

This includes GPU readbacks, same-seed replay/cancel checks, missing-hit cases, and two actual-sprite context cases. The far context uses the character's Penetrate sprite as a documented pose proxy because this skin has no dedicated Fire image; it validates world flight and target contact, not the game's full action animation. `inputs.json` fingerprints source/assets. Wait for the launched Unity process to finish and require a fresh `complete.txt=PASS`; the launcher itself is not acceptance.

The runner builds the authored `Shaders/PlatinumStrike.shader` with Unity 2019.3.15f1 and loads that exact AssetBundle in Play Mode. A deployable candidate requires its matching `Assemblies/Steria.dll` and `Assemblies/AB/steria_christasha_light_strike`; the original local tide brush remains at its existing resource path. The bundle contains only this project's shader, not reference art or native game resources. `-ModelOnly` emits the exact production geometry as opaque neutral clay before material review. Below is historical documentation for the superseded recipe and its old snapshot-based preview runner.

## Historical Dagger recipe

The recipe clones the installed `FX_PC_RolRang_Dagger` visual hierarchy under an inactive staging root, strips gameplay MonoBehaviours, and retains the native `Slash1`, smoke, burn, distortion, and particle carriers. `ChristashaNativePierceRecipes` owns cloned materials, runtime brush-alpha mask, support meshes, and inactive templates; game Resources remain read-only.

`Pierce` uses raw native units and a 0.68 s effective clock (native motion at 0.91 speed). `Projectile` uses raw native units at 0.92 art scale and a 0.32 s effective clock (0.91 speed) so its primary burst completes before the adapter's 0.26 s travel. Direction is inherited from the caller; the preview demonstrates the source -X direction.

Visual hierarchy: the native Dagger flash provides the sharp lead, `PierceGoldBundle` is a warm-white/platinum broken core, `PierceBackscatter` supplies unequal gold side rebound, and `PierceAirTail` is a sparse fine wake. These supports are emitted from prebuilt systems in `AfterParticles`; playback never adds children and clears every particle system on cancellation.

Preview (hidden Unity 2019.3.15f1 only):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File render-pierce.ps1
```

The output includes `Pierce/` and `Projectile/` dark/mid PNGs at 60 fps, `timeline.csv`, material/alpha metadata, and focused cancellation/replay checks. It is a reference replay, not a game battle capture.
