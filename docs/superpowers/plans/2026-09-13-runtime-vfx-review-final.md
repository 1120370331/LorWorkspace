# Runtime VFX 独立验收：candidate-hit-runtime-03 + candidate-17-rerun

日期：2026-09-13

## 1. 结论：PASS

本轮 Editor 离屏视觉验收通过。Slash 的白金主体、早段起锋、断裂刷纹和峰后尾迹符合原始意图；Hit 的动作读法明确为更重的刀剑纵砸，并在约 150 ms 的剑尖落点产生局部接触反馈。真实游戏中的角色挂点、尺度、遮挡和最终 fallback 仍属于未覆盖风险，但不否定本轮已完成的 Editor 视觉验收。

## 2. 对原始用户意图的贯彻程度

Slash 使用 `candidate-17-rerun/candidate/`。暗底与中底的 17 ms、100 ms、167 ms、283 ms、500 ms 代表帧均能读到窄白金锋和连续但受控的暖灰/白金主体，透明破口与不等宽边缘保留。早段没有退回整体黑，未出现候选07那种持续高 HDR 油箔宽板；1.30 s 延展只影响峰后尾迹，未拖慢起锋。

Hit 使用 `candidate-hit-runtime-03/candidate/`。`mid_0001`→`mid_0009`（约 17→150 ms）呈现高位弧线向左下的主刀势；`mid_0009` 的剑尖到达接触位置，`mid_0010` 才出现小范围碎击，`mid_0015`/`mid_0017` 的反馈继续附着在同一落点。这个顺序建立了蓄力、下切、触地和收势，未变成拳击式中心爆点，也没有用横向爆点替代纵砸。

## 3. 用户体验问题

主观看路径自然：先读主刀势，再读接触反馈，最后退为细线。正常和 no-MPB Hit 帧均保持可见白金主体，未见黑化或单独黑屏。

Hit 的 `mid_0024`（400 ms）仍有上方细弧，落点附近可见低对比烟雾；`mid_0030`（500 ms）已只剩稀疏细线。该残留没有抢主动作或遮挡落点，属于可接受的短尾。

## 4. 竞品参考差距

成熟动作游戏的重击通常采用“高位蓄力/主锋→落点→短促冲击→细尾迹”的层级。当前 Hit 已符合，接触组尺寸明显低于主弧，时间也落在主锋之后；Slash 的窄锋优先级、主体破口和尾迹落差符合快速横斩的常见读法。

当前证据是固定相机的孤立 Editor replay，没有覆盖角色比例、脚底接触、目标遮挡、镜头反馈和同屏技能层级。这些是下一阶段真实上下文验收项。

## 5. 技术债和解耦问题

Hit 使用独立的 `HitPlayback`/`HitColor` 分支和自己的实例材质；Slash 的既有材质职责与时序没有被 Hit 改写。共享 Animator/ParticleSystem 时钟、实例材质和取消清理路径保持可追踪，当前没有证据显示破坏 Slash API 或数据边界。

保留的治理债务包括：recipe 通过层级字符串识别，`HitColor` 通过粒子名和硬编码色值分派，`HitCoreResource` 当前未使用。这些会增加未来原生资源改名和 recipe 扩展的维护成本，列为后续重构，不构成本轮视觉阻断。

## 6. 回归风险

- Slash：`candidate-17-rerun/unity.log` 含 `SLASH11_REGRESSION_PASS`；candidate、no-MPB、low-MPB 均有早段白金主体，低 MPB 在尾段变暗，没有永久托亮。
- Hit：`candidate-hit-runtime-03/unity.log` 含 `CHRISTASHA_NATIVE_HIT_PREVIEW_PASS`；`focused-checks.txt` 记录 .14 s 重播一致、取消清理、层级稳定和原始 source material identity 保持。
- 两项均为 Unity 2019.3.15f1 / D3D11 / 固定相机 Editor replay；真实游戏资源变体与实战合成仍未覆盖。

## 7. 证据：命令、截图、日志、路径、复现步骤

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File SteriaBuild/VFXSource/ChristashaNative/render-hit.ps1 -Output SteriaBuild/VFXSource/ChristashaNative/evidence/candidate-hit-runtime-03
python -X utf8 SteriaBuild/VFXSource/ChristashaNative/package-slash-evidence.py SteriaBuild/VFXSource/ChristashaNative/evidence/candidate-hit-runtime-03 --recipe Hit --timing
python -X utf8 SteriaBuild/VFXSource/ChristashaNative/package-slash-evidence.py SteriaBuild/VFXSource/ChristashaNative/evidence/candidate-17-rerun --timing
```

关键截图与数据：

- Slash：`candidate-17-rerun/candidate/dark_0001.png`、`dark_0006.png`、`dark_0010.png`、`dark_0017.png`、`dark_0030.png`；对应 `no-mpb-diagnostic/`、`low-mpb-diagnostic/`。
- Hit：`candidate-hit-runtime-03/candidate/mid_0001.png`、`mid_0009.png`、`mid_0010.png`、`mid_0015.png`、`mid_0017.png`、`mid_0024.png`、`mid_0030.png`；对应 `no-mpb-diagnostic/`。
- Hit 时序：`candidate-hit-runtime-03/candidate/timeline.csv` 显示 150 ms 开始接触粒子，167 ms 达 35 个粒子，400 ms 降至 24 个，500 ms 降至 6 个，600 ms 后只剩 1 个尾粒，750 ms 清空。
- Manifest：`candidate-hit-runtime-03/candidate-manifest.json` 与 `candidate-17-rerun/candidate-manifest.json` 均绑定当前库 SHA256 `596485480d883b78ac927d409658a1dd3cb041f7106a7abde94c1c5c5c719089`；Hit manifest 绑定 `ChristashaNativeHitPreview.cs` SHA256 `ff75d055e14772113483353246ee92c5c37dc1e8b612a6d4caa4094659d21d29`。

## 8. 必须返修项

无本轮必须返修项。保持 Slash 峰前时刻和 1.30 s 尾段，保持 Hit 的 150 ms 落点、低强度接触组和纵向下降段；不要将反馈扩大成中心星爆或把 Hit 改成 Slash 的横向副本。

## 9. 可延期项

- 在真实角色 Hit.png 挂点中确认剑尖、脚底和目标接触面的位置，并校准世界尺寸。
- 在普通战场相机、目标遮挡和同屏技能下复核尾弧、烟雾和排序。
- 补充真实游戏资源变体下的 MPB fallback 验收；当前只证明 Editor candidate 与 no-MPB 离屏路径。
- 将 recipe 粒子角色和色值集中配置，清理或说明未使用的 `HitCoreResource`。

## 10. 建议下一轮任务

以当前两个 manifest 绑定的候选为输入，进入主线程真实上下文 smoke：检查 Slash/Hit 的角色挂点、尺度、脚底落点、遮挡、同屏层级和运行时 MPB fallback。若发现偏移，优先只调整外层适配参数，不改动本轮已通过的原生主刀时序、白金材质职责和 Hit 接触时间。
