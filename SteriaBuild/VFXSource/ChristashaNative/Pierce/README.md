# Christasha native Pierce / Projectile

The recipe clones the installed `FX_PC_RolRang_Dagger` visual hierarchy under an inactive staging root, strips gameplay MonoBehaviours, and retains the native `Slash1`, smoke, burn, distortion, and particle carriers. `ChristashaNativePierceRecipes` owns cloned materials, runtime brush-alpha mask, support meshes, and inactive templates; game Resources remain read-only.

`Pierce` uses raw native units and a 0.68 s effective clock (native motion at 0.91 speed). `Projectile` uses raw native units at 0.92 art scale and a 0.32 s effective clock (0.91 speed) so its primary burst completes before the adapter's 0.26 s travel. Direction is inherited from the caller; the preview demonstrates the source -X direction.

Visual hierarchy: the native Dagger flash provides the sharp lead, `PierceGoldBundle` is a warm-white/platinum broken core, `PierceBackscatter` supplies unequal gold side rebound, and `PierceAirTail` is a sparse fine wake. These supports are emitted from prebuilt systems in `AfterParticles`; playback never adds children and clears every particle system on cancellation.

Preview (hidden Unity 2019.3.15f1 only):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File render-pierce.ps1
```

The output includes `Pierce/` and `Projectile/` dark/mid PNGs at 60 fps, `timeline.csv`, material/alpha metadata, and focused cancellation/replay checks. It is a reference replay, not a game battle capture.
