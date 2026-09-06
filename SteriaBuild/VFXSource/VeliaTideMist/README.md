# Velia Tide Mist — card 9004004

Reference refinement follows `docs/superpowers/specs/2026-09-07-velia-tide-mist-reference-refinement.md`. Native first-r7 passed independent visual review and main-thread frame inspection after one revision of the r6 lighting. Technical export reports remain separate from visual acceptance. Final packaging, native checks and deployment evidence are recorded in `docs/superpowers/plans/2026-09-07-velia-tide-mist-reference-delivery.md`.

Historical R5 passed independent review and 31 main-thread native lifecycle/camera checks and was delivered on 2026-09-07, bundle `521DB741...DB42F98`. Its acceptance does not apply to this refinement. See `docs/superpowers/plans/2026-09-07-velia-tide-mist-delivery.md` for the historical evidence and proxy-scene limitations.

The four canonical runtime files are under `SteriaBuild/`. Only the pure Unity controller and filter are copied to the small Unity project by the build script. The game host reuses one FarAreaEffect per owner/currentDiceAction, keeps the same filter between dice, and inspects the public remaining queue in Update after each pulse tail. Settlement remains entirely with the default manager and existing Velia card abilities.

`VeliaTideMistVisualController`: `BeginDice(int ordinal)` uses zero-based, increasing ordinals; duplicate ordinals are ignored. `TriggerPulse(Vector2[])` accepts successful, bottom-left viewport positions and ignores duplicate callbacks for the current ordinal. `SetProtectionRects(Rect[])`, `Finish()`, `Cancel()`, `Advance(float dt)`, `Apply(Material,float aspect)` are pure Unity. `DiceReady`, `PulseFinished`, `IsComplete`, `PulseCount` expose lifecycle state. `Finish` is the 0.4-second normal exit; `Cancel` clears immediately. At most 16 distinct hit positions/character bounds are shaded, with max blending to avoid additive overlap. The default central band and HUD margins remain protected if bounds are absent.

`VeliaTideMistScreenFilter.Initialize(driver, materialTemplate)` clones the material and retains no shared material writes. `Release()` is idempotent; the game host destroys its own component afterwards. Disabling the filter cancels its own driver. Rendering never advances time. New sessions dispose the prior module-owned session; identity-guarded cleanup cannot touch the replacement.

Run from repository root in PowerShell:

```powershell
& SteriaBuild/VFXSource/VeliaTideMist/verify_velia_tide_mist_source.ps1
& SteriaBuild/VFXSource/VeliaTideMist/verify_velia_tide_mist_api.ps1
& SteriaBuild/VFXSource/VeliaTideMist/build_velia_tide_mist.ps1 -Mode First -PreviewName first-r6
& SteriaBuild/VFXSource/VeliaTideMist/export_velia_tide_mist_video.ps1 -PreviewName first-r6
# Use a unique PreviewName for another export; existing evidence is never overwritten.
# Reviewed mode belongs to the main thread only after independent preview approval.
```

Native target: Unity 2019.3.15f1 Built-in/Gamma, D3D11, hidden batchmode with graphics; run only one Unity instance on this module project. Preview is the unchanged gloomy **proxy** battlefield with real repository sprites and simulated numbers/UI, not actual game/HUD evidence. All image rendering uses the same shader, controller and OnRenderImage filter as the game. The 145-frame 60fps sequence retains callbacks at frames36/90, second-dice Begin at72, and one final fade. Representative frames include reveal10, wait30, peaks39/93, between66, fade122, complete140, plus each dice's exact +.10/+.18s propagation samples. Source/texture readback receipts and a neutral-named full video accompany each preview revision. Existing R5 previews remain available for same-frame comparison. Native logs stay in `output/velia-tide-mist/implementation`.

The material-only bundle has one explicit material asset, with its shader, byte-identical mist atlas and independent cloud color texture dependencies. No prefab, behaviour script, character or preview UI asset ships. Material name `VeliaTideMistMaterial`; bundle `steria_velia_tide_mist`. Runtime lookup uses existing `Resource/AssetBundle` and `Assemblies/AB` paths, with optional `.ab`. Missing camera/material retains the lifecycle without synthetic combat events. Eight seconds without callback or next dice triggers cleanup using scaled time. Owner/card/manager cancellation and OnDisable/OnDestroy clear only this session's resources.

`source_assets/reference_refinement/dawn_cloud_frame_v1.png` is main-authored cloud color art: true straight RGBA, sRGB enabled, FromInput alpha, no mips, Clamp/Bilinear, uncompressed RGBA32 and original NPOT dimensions within 2048. `_CloudPlate` samples the disconnected source halves independently with bounded drift and preserved aspect; the sun and at most six cloud-only shadow samples drive three broad shafts. The second dice spreads warmth across existing lit planes without moving or expanding the cloud shape. The old atlas retains its hash and linear data interpretation and supplies two low foreground wisps only. Main owns image generation and prompt provenance; `references.json` pins the chosen source hash and dimensions.

The source verifier decodes alpha and checks the transparent center/lower battlefield and thick soft-edged cores. Both First and Reviewed native exports validate dimensions, RGBA32, import/filter settings and every alpha pixel against the source, recording `cloud-readback.json`. The material's dependency whitelist includes exactly its shader, atlas and cloud art; later Reviewed readback checks that cloud texture and the single material-only self-contained bundle. Completion/source restoration and the existing <=15% additional near-white limit remain technical gates. They do not certify the visual design, actual combat/HUD readability or game performance.
