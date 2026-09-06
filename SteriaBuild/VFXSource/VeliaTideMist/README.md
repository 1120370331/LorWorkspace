# Velia Tide Mist — card 9004004

The reviewed implementation passed independent visual review and 31 main-thread native lifecycle/camera checks, and was delivered on 2026-09-07. Bundle: `521DB741...DB42F98`. This module's scripts do not deploy or build Steria.dll. See `docs/superpowers/plans/2026-09-07-velia-tide-mist-delivery.md` for exact delivery evidence and the limits of the proxy-scene acceptance.

The four canonical runtime files are under `SteriaBuild/`. Only the pure Unity controller and filter are copied to the small Unity project by the build script. The game host reuses one FarAreaEffect per owner/currentDiceAction, keeps the same filter between dice, and inspects the public remaining queue in Update after each pulse tail. Settlement remains entirely with the default manager and existing Velia card abilities.

`VeliaTideMistVisualController`: `BeginDice(int ordinal)` uses zero-based, increasing ordinals; duplicate ordinals are ignored. `TriggerPulse(Vector2[])` accepts successful, bottom-left viewport positions and ignores duplicate callbacks for the current ordinal. `SetProtectionRects(Rect[])`, `Finish()`, `Cancel()`, `Advance(float dt)`, `Apply(Material,float aspect)` are pure Unity. `DiceReady`, `PulseFinished`, `IsComplete`, `PulseCount` expose lifecycle state. `Finish` is the 0.4-second normal exit; `Cancel` clears immediately. At most 16 distinct hit positions/character bounds are shaded, with max blending to avoid additive overlap. The default central band and HUD margins remain protected if bounds are absent.

`VeliaTideMistScreenFilter.Initialize(driver, materialTemplate)` clones the material and retains no shared material writes. `Release()` is idempotent; the game host destroys its own component afterwards. Disabling the filter cancels its own driver. Rendering never advances time. New sessions dispose the prior module-owned session; identity-guarded cleanup cannot touch the replacement.

Run from repository root in PowerShell:

```powershell
& SteriaBuild/VFXSource/VeliaTideMist/verify_velia_tide_mist_source.ps1
& SteriaBuild/VFXSource/VeliaTideMist/verify_velia_tide_mist_api.ps1
& SteriaBuild/VFXSource/VeliaTideMist/build_velia_tide_mist.ps1 -Mode First
# Approved r5 Bundle build, native material readback and export:
& SteriaBuild/VFXSource/VeliaTideMist/build_velia_tide_mist.ps1 -Mode Reviewed -PreviewName reviewed-r5
& SteriaBuild/VFXSource/VeliaTideMist/export_velia_tide_mist_video.ps1 -PreviewName reviewed-r5
```

Native target: Unity 2019.3.15f1 Built-in, D3D11, hidden batchmode with graphics. Preview is a consistent gloomy **proxy** battlefield with real repository sprites and simulated numbers/UI, not actual game/HUD evidence. All image rendering uses the same shader, controller and OnRenderImage filter as the game. Accepted source preview: `preview_exports/velia_tide_mist/first-r5`. Reviewed Bundle readback export: `preview_exports/velia_tide_mist/reviewed-r5`; continuous `sequence60` frames and timing/source receipts accompany representative PNGs and the full sequence video. Temporary API DLL and Unity logs stay in `output/velia-tide-mist/implementation`.

The material-only bundle has one explicit material asset, with its shader and byte-identical mist atlas dependencies. No prefab, behaviour script, character or preview UI asset ships. Material name `VeliaTideMistMaterial`; bundle `steria_velia_tide_mist`. Runtime lookup uses existing `Resource/AssetBundle` and `Assemblies/AB` paths, with optional `.ab`. Missing camera/material retains the lifecycle without synthetic combat events. Eight seconds without callback or next dice triggers cleanup using scaled time. Owner/card/manager cancellation and OnDisable/OnDestroy clear only this session's resources.

`source_assets/references.json` records the repository-authored read-only atlas reused for this implementation and its SHA. No new character art, cloud generator, global post-processing configuration or Slazeya module edits.
