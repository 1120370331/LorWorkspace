# 潜力观测

当前重做候选为 [candidate-r8b-movinglight](accepted-candidate.json)：使用 Kenney CC0 灰度光罩，主要亮核从下身升至头顶上方，外围金光展开后消退。暗底、中灰底、镜像与独立光效的原生帧已通过内部复核；构建及 13 项真实程序集逻辑检查通过。9 个发布文件已备份、写入工作区 `SteriaModFolder` 并逐一核对 SHA256，已于2026-10-26按用户要求同步到实际游戏安装目录，9个文件均已备份并核对SHA256；记录见 `accepted-candidate.json` 的 `game_deployment`。用户美术评价和完整实战仍待外部验收。查看 [新动画](evidence/candidate-r8b-movinglight/dark-canonical-60fps.mp4)、[中灰预览](evidence/candidate-r8b-movinglight/mid-canonical-stages.jpg)、[mask 来源与许可](source_assets/light-masks/source-provenance.json)。

`candidate-r5-visibility` 已被用户否决为“海草感”，其内部视觉 PASS 已撤销。旧卷曲图集及后续未接受候选仅保留为历史，不再参与当前 bundle。

Dedicated S2 cast pose, anatomical right-palm white/gold light and recipient-centred upward light fountain. Current design: `docs/superpowers/specs/2026-10-26-velia-potential-observation-light-mask-revision.md`. R5 was explicitly rejected by the user as drifting seaweed; its former internal PASS is void. A candidate directory and a technical PASS are not independent visual acceptance.

Production sources are `SteriaBuild/VeliaPotentialObservationEffect.cs` (game host) and `VeliaPotentialObservationVisual.cs` (pure Unity Shuriken playback and palm cue). Enemy mechanics still execute in `PassiveAbility_9004001.OnRoundStart`; the exact successfully applied recipient is held until `OnAfterRollSpeedDice`, consumed once, then played after one frame and 0.22 scaled seconds to cover the native 0.2-second roulette tail. The player Instance page invokes the same host after successful settlement. No new target selection, cost or buff change occurs in visual code.

S2 art comes from `CharacterModels/Velia_Layered_v1/sources/observation`; its `generation.md` records built-in ImageGen prompts and source provenance. Original head registration, all old actions and Special/S1 prayer are preserved. Guest Velia has the new baked open smile; librarian projection remains clothing-only with its own native head. Arbitrary librarian faces are not replaced or guaranteed to have Velia's smile.

The light uses the actual S2 renderer's sprite rect/pivot/PPU plus `rightPalm` output pixels, transformed once through the renderer (including flip). Recipient horizontal origin is its standing sprite pivot, not the weapon-inclusive alpha centre. Alpha measures only height/sole. The current three-system Shuriken prefab uses one fixed low-energy CoreFlow trail, one rising and widening SoftLift luminous head and eight small moving Glints. GoldWisps and coloured brush-atlas dependencies have been removed from this runtime contract. `BodyRoot` receives the measured height scale once. The driver uses fixed 1/120-second `Simulate`/`Pause` steps and zero-time newborn synchronization, preserving the prefab's deterministic seeds; rendering does not advance time or modify shared materials. Each renderer receives per-instance Age/BaseY/Height. The shader samples linear scalar R, emits warm-white core plus pale-gold radiance with `Blend One One`, and applies coverage exactly once into emitted RGB. Age drives the upward reveal, widening, root shutoff and uneven retreat without squashing the remaining geometry. Current materials disable the face mask: compact white energy and natural gold falloff preserve readability without splitting the light. Full-height UVs retain the original upper fade, and both card ends are feathered. Cancellation releases only this host's S2; another motion hides the hand light and cannot be overwritten by the delayed restore. Each instance belongs to the current cast and is stopped/cleared and destroyed on replacement, end, disable, death or changed round/wave.

The earlier ribbon-mesh and coloured-brush experiments are rejected historical evidence. Their files remain for provenance but are not shipping material inputs. Current masks are unchanged CC0 Kenney source PNGs under `source_assets/light-masks/`; `manifest.json`, `source-provenance.json` and both package licenses preserve exact sources, hashes and permissions. `import_light_masks.py` copies the three reviewed masks byte-for-byte and performs no image generation/recolouring. The bundle's `VeliaObservationParticles` prefab contains only stock particle components, and its three light materials reference the actual linear masks used in both game and preview. Only the already accepted small hand cue remains a mesh.

## Rebuild and checks

Run from repository root. Normal builds use saved image sources and never call the image service.

```powershell
python SteriaBuild/CharacterModels/Velia_Layered_v1/prepare_observation.py
python SteriaBuild/CharacterModels/Velia_Layered_v1/build_model.py
python SteriaBuild/CharacterModels/Velia_Layered_v1/verify_model.py
python SteriaBuild/CharacterModels/Velia_Layered_v1/deploy_to_game.py
python SteriaBuild/VFXSource/VeliaPotentialObservation/import_light_masks.py
python SteriaBuild/VFXSource/VeliaPotentialObservation/verify_source.py
dotnet build SteriaBuild/Steria.csproj -c Release --no-restore
dotnet build SteriaBuild/VFXSource/VeliaPotentialObservation/Checks/Checks.csproj -c Release
SteriaBuild/VFXSource/VeliaPotentialObservation/Checks/bin/Release/net472/VeliaObservationChecks.exe
powershell -ExecutionPolicy Bypass -File SteriaBuild/VFXSource/VeliaPotentialObservation/run_preview.ps1 -Revision new-candidate
```

`deploy_to_game.py` without `--game-mod` only packages and does not deploy. Do not use its broad deploy for this task. Final delivery selects only the accepted new S2/S2_front images, exact two skin XMLs, anchors, material bundle and candidate DLL, with backup/hash checks by the main thread.

The managed fixture executes candidate-DLL passive/card methods with actual game units/buffs/cooldown classes. Only native random/logging calls and the render-dispatch boundary are isolated through scoped Harmony patches; it proves target retention/one-shot consumption, unchanged buff timing, player rejection paths, self-target, wave reset and the actual host coroutine's frame/WaitForSeconds sequence. It is not a simulated replacement of game business types and is not a live battle or engine scheduler test.

Native Unity 2019.3.15f1 / Built-in Gamma / D3D11 preview uses actual complete 512 sprites and XML pivots at 50 PPU, same production driver and bundle material, both facings, real selected/nonselected ally art, zero-delta/cleanup and a live Editor play-loop smoke. It does not create full Library of Ruina unit rigs, reception, combat camera or HUD. Final game-context behaviour beyond these boundaries remains external acceptance.

## Exact shipping inputs

- `SteriaBuild/bin/Release/net472/Steria.dll` → `Assemblies/Steria.dll`.
- `UnityProject/AssetBundles/steria_velia_potential_observation` → `Assemblies/AB/steria_velia_potential_observation`.
- `CharacterModels/Velia_Layered_v1/sources/observation/anchors.json` → `Resource/Effects/VeliaPotentialObservation/anchors.json`.
- Keep `source_assets/light-masks/source-provenance.json` and both `licenses/*/License.txt` with the release evidence/license directory. The three mask images themselves are dependencies inside the bundle.
- From `CharacterModels/Velia_Layered_v1/build/runtime/Resource/CharacterSkin/`: `Velia` and `Velia_Layered_v1_Projection`, only `ModInfo.Xml`, `ClothCustom/S2.png`, `ClothCustom/S2_front.png`.

Never copy provisional/rejected evidence or a previous bundle merely because the filename matches. Game was running during implementation and has not been stopped, restarted or written by this worker.
