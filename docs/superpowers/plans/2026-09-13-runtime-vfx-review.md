# Runtime VFX candidate-22 独立验收复审

1. 结论：REVISE

candidate-22 的 Slash 离屏画面本身方向正确：正常、空 MPB、低 MPB 的早段和峰值都能读到白金主段宽体，没有回到整体黑，也没有明显变成 07/r4 那种持续油箔宽板。但本候选不能外部验收通过，原因是证据链不闭合：`candidate-22` 缺少 `candidate-manifest.json` 和源码快照，无法证明这些帧绑定当前工作区源码；当前 `SteriaBuild/ChristashaNativeParticleLibrary.cs` 的 SHA256 是 `596485480d883b78ac927d409658a1dd3cb041f7106a7abde94c1c5c5c719089`，而可对照的 `hit-slam-candidate-03` manifest 仍记录旧源码 hash `4c74974a...`。此外，candidate-22 只提供 Slash 帧，没有同候选 Hit 视觉帧，也没有实际 Play mode / 游戏 Resources 路径下 RuntimeColorNoiseFallback 生效证据。

2. 对原始用户意图的贯彻程度

已贯彻的部分：

- 字段级 MPB fallback 在 Slash 的空 MPB 与低 MPB 诊断里可见生效。`candidate/no-mpb-diagnostic/low-mpb-diagnostic` 在 17/50/100/167/283ms 都保持白金主形，不再整体黑。
- 源码包含运行时 ColorNoise fallback 开关：`DiagnosticForceColorNoiseFallback`、`RuntimeColorNoiseFallback = true`、`PrepareColorNoiseFallback()`，且当前 Slash 主层使用 `PrepareBrushMask(originalBrush, false, .50f)` 的受控覆盖实验。
- Slash/Hit 的延长逻辑在源码上保留早段原速：`Advance()` 只在 Slash `wallElapsed >= .18f`、Hit `wallElapsed >= .25f` 后使用 `recipeSpeed`，`targetDuration` 为 Slash `1.30f`、Hit `1.08f`。这意味着 Hit 的 0.15s 触地节点不应被时钟拖慢。
- Slash11 的动线、原 brush 来源、native torus/mesh 机制没有被重做；当前改动集中在派生 mask 覆盖、ColorNoise fallback 和颜色安全下限。

未充分贯彻的部分：

- 用户实测问题是“游戏内整体黑”，candidate-22 仍是 Editor 离屏重放，且 `color-noise-diagnostic.txt` 写明 `mode=native-source`。这没有证明 `Application.isPlaying && RuntimeColorNoiseFallback` 在游戏 Resources 路径真实生效。
- 验收要求包含 Hit 的 .15 落点与延长不拖早锋，但 candidate-22 目录没有 Hit 帧。只能从源码和旧 `timing-hit-03` 推断，不能作为当前候选视觉验收。
- candidate-22 没有 manifest/source snapshot。按本任务明确要求，“若 source hash/metadata 不匹配请 REVISE”，这里属于证据不匹配/证据不足。

3. 用户体验问题

- 从 Slash 画面看，暗底与中底主段在 0.017/0.05/0.10s 已可读，用户不会再第一眼看到全黑空挥。
- 0.5s 后正常/空 MPB 还留少量细碎余迹，低 MPB 退得更暗，符合“低值退场不被永久托亮”的预期。
- 最大体验风险仍在真实游戏路径：当前证据没有覆盖游戏中 Resources prefab、Animator controller、MPB 字段、ColorNoise 纹理是否与 Editor 参考包一致。如果游戏路径仍取到近黑 ColorNoise 或未进入 fallback，用户会再次遇到整体黑。

4. 竞品参考差距

成熟动作游戏/卡牌战斗 VFX 对这类“发黑修复”通常会保留可复现的帧证据、运行时诊断快照、候选源码 hash、以及至少一条真实接入路径日志。candidate-22 的视觉帧已经接近可接受，但证据包装低于合理发布验收预期：缺 manifest、缺源码快照、缺 Hit 同候选帧、缺 Play mode/runtime fallback 证明。

5. 技术债和解耦问题

- `RuntimeColorNoiseFallback` 是全局静态开关，和 `Application.isPlaying` 绑定在库层；短期可用，但它把运行环境判断散在 VFX library 内部。后续更稳的做法是由 preview/runtime resolver 显式传入资源变体能力或诊断配置。
- `.50f`、`.45f/.60f`、`.18f/.25f`、`1.30f/1.08f` 等常量有设计来源，但仍直接散在运行时代码里。当前可接受为候选实验，发布前最好集中到 recipe/diagnostic 常量区并由 manifest 记录。
- HitColor 的字段 fallback 和 Slash 的 fallback 逻辑相似但重复实现，长期容易分叉。若后续继续扩展到 Pierce/Mass，应抽成按 renderer/material/property 的小型采样策略，避免每个动作各自修一次“缺字段变黑”。
- 证据生成流程存在治理缺口：`package-slash-evidence.py` 会生成 `candidate-manifest.json`，但 candidate-22 实际没有该文件，说明当前流水线允许“未打包候选”进入复审。

6. 回归风险

- Slash 视觉：低到中。早段没有被延长拖慢，f=1 已有清楚起锋；0.5s 低 MPB 退得偏暗但符合合同。油箔风险可接受。
- Runtime 黑屏：中到高。源码有 fallback，但 candidate-22 未证明 Play mode/game path 真正替换 `_ColorNoise`，也未记录游戏 Resources parity。
- Hit：中。源码时钟显示 0.15s 不被拖慢，旧 `timing-hit-03` 有粒子在 f=9/0.15s 增加的证据，但它不是 candidate-22 且不是当前源码 hash 绑定证据。
- 回滚：源码改动集中在 `ChristashaNativeParticleLibrary.cs`、Mass/Pierce recipe 少量时序相关文件，逻辑可回滚；但缺候选 manifest 会增加确认具体回滚点的成本。

7. 证据：命令、截图、日志、路径、复现步骤

执行过的只读命令与观察：

- `git status --short`：确认工作区存在多处未提交改动，相关 ChristashaNative 源码处于修改状态。
- `Get-FileHash -Algorithm SHA256 SteriaBuild\ChristashaNativeParticleLibrary.cs SteriaBuild\ChristashaNativeMassRecipes.cs SteriaBuild\ChristashaNativePierceRecipes.cs`
  - `ChristashaNativeParticleLibrary.cs` = `596485480d883b78ac927d409658a1dd3cb041f7106a7abde94c1c5c5c719089`
  - `ChristashaNativeMassRecipes.cs` = `d231c17224d694ac81431440924c954c6f3f2725bec8e2f4459da2381f8f4507`
  - `ChristashaNativePierceRecipes.cs` = `b3d9430511422a6a053befa8927b030a00caa3cfb0177087cba3c65b947ffd69`
- `Get-ChildItem SteriaBuild\VFXSource\ChristashaNative\evidence\candidate-22 -Recurse -File -Include *.json,*.txt,*.cs.txt,*.md`：candidate-22 只有 `metadata.txt`、`timeline.csv`、`focused-checks.txt`、`mpb-diagnostic.txt`、`color-noise-diagnostic.txt`、`complete.txt`、`unity.log`，未发现 manifest 或源码快照。
- `Get-Content candidate-22\complete.txt`：`UNITY_COMPILE_AND_REPLAY_PASS`，Unity 2019.3.15f1 / Direct3D11，但明确是 Editor replay，不是 actual combat choreography。
- `Get-Content candidate-22\color-noise-diagnostic.txt`：`mode=native-source`，未证明强制或 Play mode runtime ColorNoise fallback。
- `Get-Content candidate-22\mpb-diagnostic.txt`：说明 normal 使用 Animator MPB，no-mpb 清空 renderer blocks，两者同 shader/相机；但未覆盖 Play mode Resources。
- `rg -n "DiagnosticForce|ColorNoise|coverage|targetDuration|postPeakSpeed|fallback"`：确认源码中存在字段级 fallback、ColorNoise fallback、`.50f` coverage、Slash/Hit post-peak 时钟延长。
- `Import-Csv candidate-22\candidate\timeline.csv`：f=1/3/6/10/17/30/45/72/78 分别对应 0.017/0.05/0.10/0.167/0.283/0.5/0.75/1.2/1.3s；f=78 后清空。
- 生成并查看复审拼板：
  - `output/christasha-native-redesign/runtime-vfx-review-candidate-22/candidate22-dark-diagnostic-grid.png`
  - `output/christasha-native-redesign/runtime-vfx-review-candidate-22/candidate22-mid-diagnostic-grid.png`
  - `output/christasha-native-redesign/runtime-vfx-review-candidate-22/candidate20-21-22-frame6-compare.png`
- 代表帧：
  - `SteriaBuild/VFXSource/ChristashaNative/evidence/candidate-22/candidate/dark_0001.png`：17ms，早锋已白金可读。
  - `.../candidate/dark_0006.png`：100ms，主段宽体可读，白锋更亮，未成油箔整片。
  - `.../candidate/dark_0030.png`：500ms，正常候选留细碎余势。
  - `.../low-mpb-diagnostic/mid_0030.png`：500ms，低 MPB 退场变暗，未被 fallback 永久托亮。
- 对照 hash：
  - candidate-20/21 `candidate/dark_0006.png` 均为 `6b60afc45ce00a807baf0bf110946910b739dc5b33f71168482e5f2bc2eb686c`
  - candidate-22 `candidate/dark_0006.png` 为 `9b03a03c0914bdb6151210640f11539d10cacaacf26e7f73c138951f25612b3c`
  - 说明 candidate-22 与 20/21 画面确有差异。

8. 必须返修项

- 为 candidate-22 重新生成完整 `candidate-manifest.json`，并包含当前源码快照：`ChristashaNativeParticleLibrary.cs.txt`、Mass/Pierce 源码快照、preview 脚本、brush、代表帧 hash。manifest 中的源码 SHA 必须等于当前工作区真实 hash，或明确给出冻结候选对应的源码 hash。
- 补一组同候选 Hit 验收帧，至少覆盖 0.017/0.05/0.10/0.15/0.25/0.40/0.80/1.08s，并记录 0.15s 触地组是否仍在原落点时刻出现。
- 补 Play mode 或等价 runtime 诊断，证明 `Application.isPlaying && RuntimeColorNoiseFallback` 时 MainTrail1 `_ColorNoise` 实际替换为 `Christasha_TideBrush_ColorNoiseFallback`，并记录 renderer/material/shader/controller/MPB 字段有效性。只用 `mode=native-source` 不能证明游戏内黑屏修复。
- 把 candidate-22 的报告证据和 `package-slash-evidence.py` 流水线对齐，避免未打包候选进入验收。

9. 可延期项

- 将 Slash/Hit/Mass/Pierce 的颜色采样 fallback 抽象为共享 helper，减少重复逻辑。
- 将视觉时序和颜色阈值常量移入 recipe 配置或集中常量表，并在 manifest 记录。
- 做实际游戏 Player.log 的一次性 Resources parity 快照：root 名、shader 名、Animator controller、ColorNoise 名、目标 MPB 字段是否有效。

10. 建议下一轮任务

让 implementation owner 只做证据修复和最小 runtime 诊断补齐：重新打包 candidate-22 或 candidate-23，包含源码快照/manifest、Slash 正常/空/低 MPB 暗/中底帧、Hit 同候选时序帧，以及 Play mode ColorNoise fallback 快照。若这些证据绑定当前源码且画面维持当前表现，可进入下一轮 PASS 复审。
