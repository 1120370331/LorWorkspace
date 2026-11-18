# 薇莉亚「潜力观测」动作与光涌设计

Research/design handoff, 2026-10-26. Main thread has approved the S2, anchor, target and post-roll timing contract. This document is design, not evidence of implemented or accepted visuals.

## Intent

释放潜力观测时，薇莉亚站立并将解剖学右手举过头顶，张嘴微笑；掌心发出一道白金光芒。实际获得效果的友方身上出现内白外金、自下向上泉涌喷发的光涌，向外扩散、逐渐消散。敌方每幕自动施加时，在速度骰投掷动画结束后播放；友方 EGO Instance 成功使用时立即播放。效果计算与潮消耗时机不变。

## Current diagnosis and evidence

- `VeliaCards.cs:164` uses `OnUseInstance`, not `OnUseCard`. Cooldown and same-faction checks precede mechanics. Its current visual is only `Special` followed by an unconditional one-second reset. No hand or recipient VFX exists.
- `VeliaAbilities.cs:105,157,193`: enemy `OnRoundStart` selects one live, non-broken ally, including self, then applies the buff. Highest valid buff selection and Tide consumption already work. The selected recipient must be retained; visual code must never perform another random draw or reapply the buff.
- Inspected `CharacterModels/Velia_Layered_v1/build/poses.png`: Special is closed-eye kneeling prayer. README/model confirms Special/S1 share that pose, which is used by other skills. None of the current nine poses has the requested overhead hand. Recoloring or rotating a current pose cannot satisfy the request.
- `model.json` preserves a 512 square export canvas, 50 PPU, root `(256,390)`, registered 512x610 head with neck `(248,395)` and uniform head scale `.23`. Existing `joints` names appear screen-oriented; they are not reliable anatomical right/left evidence.
- `StageController.SortUnitPhase` (`StageController.cs:1924` onward) calls every unit's `RollSpeedDice`, then every unit's `AfterRollSpeedDice`. `BattleUnitModel.cs:446` invokes passive `OnAfterRollSpeedDice`. `SpeedDiceSetter.cs:73` applies each result through `SpeedDiceUI.ChangeSprite` (`:90`), which disables the roulette immediately. `SpeedDiceUI.Update` (`:255`) then starts `RouletteDisableRoutine` (`:285`): it advances `elapsed += Time.deltaTime * 5f` to 1 and resets the jitter offset, a 0.2 scaled-second visual tail. `OnRoundStartAfter` is earlier than rolling and is unsuitable.
- Recent commits inspected: `3096acd1` steam VFX, `f50f2acc` Xiyin score sheets, `08244343` VFX prewarm. Existing native modules separate render clock, game settlement, source hashes and preview receipts. The dirty working tree is not a release baseline.

## Pose and asset contract

Add a real **S2** pose and `observation_smile` expression. Preserve all existing actions and aliases, particularly Special/S1. Use the existing build/verify/package tools; do not rewrite the shared renderer for this pose.

Canonical source is left-facing, front/three-quarter body. Her anatomical right shoulder is on the viewer's left in this view. Raise that arm beside the head with a naturally raised shoulder and outward-bent elbow, with palm above the crown; keep the opposite hand low around waist/chest. The hand, forearm, sleeve and shoulder must form one believable connected structure, matching the arm lengths and palm scale of the existing Default/Guard/Hit artwork. A modest clearance above the crown is sufficient; do not stretch the arm to achieve a fixed clearance. Do not produce a symmetric two-handed prayer, crossed arms, pointing attack, or left-hand salute. Preserve standing foot baseline, original head size, pale hands, white robe, pendant and hair silhouette.

User correction after first S2 preview: “这个手的比例太失调了”. That candidate is rejected for an overlong, thin raised arm and undersized hand. This overrides the initial 0.12H clearance target and the main thread's tentative pose acceptance. The revised pose must be reviewed at the same scale beside existing poses before the final native effect export.

Use a dedicated open smiling mouth on the registered head canvas, preserving facial alignment. A small dark mouth opening with gently lifted corners is sufficient; normal-size preview must read as pleased, not a shout. Open relaxed eyes or softly smiling eyes are acceptable. Existing closed prayer eyes and neutral mouth are not the requested expression. New body art should be based on the saved character reference via built-in imagegen, transparent output, followed by normal source registration/assembly. No VFX baked into character sprites.

Record an explicit `rightPalm` coordinate in final 512 output pixels, anatomical ownership and action S2 in an anchor manifest. Derive its local point from the actual sprite rect/pivot/PPU, then transform through the actual renderer/character rotation centre. Source coordinates have top-left Y; sprite coordinates have bottom-left Y. Do not infer the point from the widest alpha bound or use a fixed world offset, and do not mirror twice. `GetSpecialMotionPivot(S2)` silently falls back to generic attack root if no entry exists, so it is safe only with an explicit validated S2 pivot. Mirror the rig in the native preview and prove the light remains on the raised hand.

Guest `Velia` bakes the smile and full head as existing exports do. `Velia_Layered_v1_Projection` must remain clothing-only with native librarian head enabled. Do not replace a librarian's identity with Velia's head. Native librarian expression control is a separate runtime concern: preserve normal native expression behavior unless a scoped existing API can be verified; report that arbitrary projected librarian faces cannot be guaranteed to show Velia's exact mouth. User-specific smile acceptance is on actual Velia.

## Visual direction and timing

One coherent event of approximately **1.45 scaled seconds**, with the caster held in S2 through 0.90s if not interrupted. Timings are design targets and may be tuned slightly after native review.

| Age | Caster/hand | Actual recipient |
|---|---|---|
| 0–0.16s | Switch S2, palm light gathers from a small white seed | Faint local warm lift inside lower torso, no complete column |
| 0.16–0.34s | One short narrow white-gold ray/flaring star from palm, brightest hand accent | Narrow root at feet/lower torso begins to climb; white centre leads |
| 0.34–0.66s | Hand halo falls below recipient peak | Main fountain crosses torso then above head; 3–5 unequal tapered upward tongues, warm gold rim and scattered streaks |
| 0.66–1.0s | Hand cue gone; release owned pose at 0.90s | Top opens and fans outward, base emission shuts off; white centre breaks into thinner strokes |
| 1.0–1.45s | No forced pose writes | Remaining gold fragments drift outward/up, dim and disappear with no residual ring |

Use target standing height H as an art scale reference: peak rise about 1.25–1.45H from the feet; initial root width about 0.25H, peak fan width 0.65–0.85H. It should emerge from the body, not a disconnected ground explosion. An off-centre tall tongue and two lower side tongues establish motion hierarchy; avoid mirror-symmetric flames or parallel luminous fence bars. Foreground accents can cover legs/side edges briefly, but keep the face and centre torso readable, with most large light behind the sprite. The caster's ray should be modest (roughly 0.4H) and must not become a beam connecting both characters.

Colour roles: near-white warm core (`#FFF9E8`), pale gold transition (`#FFE9A3`), clear gold edge (`#E8AD42`); amber only in the faint fading edge. White must remain visibly inside gold at peak. Do not uniformly tint every layer yellow or sustain a white opaque rectangle. Feather edges, vary tongue thickness and leave negative gaps; a low-opacity soft light skirt supports sharp upward streaks. No opaque gold foil, plastic tube, closed capsule, duplicated transparent concentric shells, screen flash, falling sparks, magic circle, or added attack impact. No audio is required by this request.

## Runtime implementation boundary

Recommended new module surface:

- `SteriaBuild/VeliaPotentialObservationEffect.cs`: game host / per-caster session, S2 ownership, actual unit anchors, scaled lifecycle and disposal.
- `SteriaBuild/VeliaPotentialObservationVisual.cs`: pure Unity render driver, explicit `Advance(dt)`, hand/fountain evaluators, instance-owned render resources and deterministic local visual seed. It does not reference battle settlement or modify UnityEngine.Random state.
- `SteriaBuild/VFXSource/VeliaPotentialObservation/`: native builder/preview, shader/material source if needed, source verifier and focused lifecycle fixtures. Use its own Unity 2019.3.15f1 project or isolated existing-template copy excluding Library/Temp, not modifications to TideMist or Christasha projects.
- Scoped edits in `VeliaCards.cs`, `VeliaAbilities.cs`, `Steria.csproj`; model sources/config and generated S2 outputs/XML.

Existing build patterns permit a self-contained material/shader bundle plus a pure Unity driver. Prefer one local fountain mesh/material with white-to-gold cross-section, animated upward reveal/dissolve and sparse sprites/particles, rather than five large overlapping additive billboards. Dedicated shader/material shipped in a material-only bundle avoids depending on a shader name that exists only in Editor. Standard ParticleSystem modules are also viable, but colour and motion must match this design. Use Built-in/Gamma/D3D11 and the game's 2019 API, no URP/VFX Graph/new package dependency. Rendering must not advance time; update once per scaled frame.

**Enemy:** after successful existing `ApplyPotentialObservationEffect(target)` completes, store `target` as pending for this passive/round. Clear pending at Init/OnWaveStart/new round. In `OnAfterRollSpeedDice`, consume it exactly once and request visual with `yield return null` then `WaitForSeconds(0.22f)` to cover the next-Update jitter tail. Validate owner/target and request generation again before play. Clear old requests on wave/round replacement/death/destruction; no retry loop. Never move buff application after rolling: Quickness is one of the valid buffs and must retain current mechanics.

**Player:** after successful `ApplyEffect(unit,targetUnit)`, invoke the same host immediately, retaining cooldown behavior. Remove this card's old unconditional reset routine. Cooldown/invalid target paths produce no visual.

**Ownership:** replacement cancels only the previous same-owner session. Restore the previous/Standing pose only if this session still owns it and current motion is still S2. If combat changes motion, do not force it back every frame. Cancel or clean up gracefully on disabled/destroyed views, missing material, new round, target death, or changed session; mechanics already applied stay applied. Self-target retains a single caster pose, one hand accent and one body fountain. No BattleUnitView global patch or changes to shared VFX registration are needed.

## Minimal implementation checks and native preview

Use `VeliaTideMist/build_velia_tide_mist.ps1` as launch/source-copy/hash/log pattern only. `VeliaTideMistBundleBuilder.cs:25,42` supplies PreviewFirst/BuildBundle pattern and offscreen RenderTexture plumbing. Its `Character` helper crops alpha and normalizes height: **do not copy that coordinate behavior**, because this task must prove hand anchors. Use actual 512 sprites, actual XML pivots and 50 PPU consistently with runtime, preserving the full canvas. Character generation/build manifest is authoritative.

One hidden Unity invocation, with graphics, explicit primary log, distinct output directory per candidate. Export 0.10/0.30/0.55/0.90/1.25/1.50s frames, contact sheet or full short sequence, plus mirrored peak/hand close-up. Include actual Velia and a real selected ally sprite and one nonselected ally on a representative dark background; confirm only selected ally gets the fountain. Use the same production visual source/material and anchor metadata in the preview. Add a small real player-loop smoke if the chosen renderer uses Animator or ParticleSystem; no offscreen-only claim of live-frame behavior. Full reception simulation is not necessary.

Focused lifecycle evidence: enemy application precedes all rolls; no visual before completion; pending target identity unchanged; repeated after-roll callback emits once; invalid/cooldown player emits none; self-target emits once; zero dt does not advance; completion/cancel cleans resources; interrupted motion not overwritten. Source assertions cannot replace these outcomes. Build and model verifier once after stable implementation; rerun affected checks only after review fixes.

## Acceptance and rejection

PASS requires actual native frames showing anatomical right palm above head, open smile on Velia, light attached in both directions, white inner/gold outer fountain rising from the selected body, increasing upper spread followed by dissipating gold, readable character silhouettes and a clean final frame. Main must inspect returned changed surfaces and the independent reviewer must return PASS against these frames.

REVISE for wrong hand, prayer pose, neutral/closed mouth on Velia, severed shoulder/sleeve, changing head scale, ground floating, hand glow over empty space, reversed/growing-down motion, dominant full-body opaque block, symmetrical candle/fire read, wrong recipient, repeated cast, resetting an unrelated battle pose, or effect persisting into the next round. A successful build does not resolve these failures.

Final gates: `build_model.py`, `verify_model.py`, dedicated source/API/lifecycle verification, `dotnet build SteriaBuild/Steria.csproj -c Release`, Unity bundle build/readback when shader assets are used, hash manifest of source/DLL/material/S2 outputs. Preserve dirty unrelated XML/code/art. Existing `deploy_to_game.py` can package guest and projection assets from manifest, but do not broad-copy the dirty workspace. Game is currently running: do not stop/restart it; main owns deployment boundary and must report if candidate is staged rather than loaded. Full live battle and arbitrary librarian-face expression remain explicit external acceptance boundaries.

## External reference used

[Unity 2019.3 Velocity over Lifetime](https://docs.unity3d.com/2019.3/Documentation/Manual/PartSysVelOverLifeModule.html) documents local/world linear velocity, radial spreading and speed curves. Adaptation here is upward travel with later outward spread and decreasing speed; no orbit is required. This confirms a compatible engine technique, not an existing repository effect. No external art/code is copied.
