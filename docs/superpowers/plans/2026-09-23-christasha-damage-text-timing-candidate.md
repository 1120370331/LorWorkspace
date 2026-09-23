# 克里丝塔夏特效与原版伤害跳字时序候选

用户要求确认攻击特效、命中反馈和内置伤害数字的显示关系。本轮限定普通 Slash/Hit 与「汐音：光耀万海」；其他群攻书页不变。

## 原版结算链

- 近战 `RencounterManager` 在同一结算段先调用 `PrintEffect`，随后 `PrintDamage`。普通 Slash/Hit 保留原生命中火花；HP 跳字立即创建，BP 跳字由 `DamageTextEffect` 延迟 `0.25s`。现有 Hit 的额外落点粒子在视觉年龄约 `0.15s`，不能冒充原版伤害结算时刻。
- 汐音 `BattleFarAreaPlayManager.ActionPhase` 在 `0.75s` 放行后逐成功目标 `GiveDamage`，原版 HP 跳字在 `BattleUnitModel.TakeDamage → BattleUnitView.Damaged` 中创建。然后 manager 调用 `GiveDamageFromManager`，同帧为成功目标创建 Point。失败目标没有 Point；BP 仍按原版延迟。

## 候选调整与证据

- 仅调整 `ChristashaNativeMassRecipes.cs` 的 Area 退场曲线。宽白金浪锋保持至全局约 `0.75s`；之后使用原生粒子的侵蚀和 alpha，在约 `0.80–0.85s` 收成细弦与碎迹，让 Point 和数字取得画面焦点。首轮直接压低 shader RGB 会产生黑色片，已舍弃。
- 候选 `output/christasha-vfx-timing/candidate-r2/frame-loop/`：Unity 2019.3.15f1 Play Mode 60fps、深/中底三目标时序、同 seed 重播/取消、0 MPB 颜色漂移 PASS。独立预览评审 PASS：结算前浪锋完整，结算后 Point 分离，无黑片。该代理预览没有游戏原版 `DamageTextEffect`。
- 主线程复跑 managed 契约 `230` 项 PASS、真实游戏 Managed API 编译 PASS、Release 构建 0 错误及 7 项既有警告。新旧 DLL 的 94 份 PDB 文档仅 `ChristashaNativeMassRecipes.cs` 和生成的 AssemblyInfo 改变，全部与当前源码哈希匹配。
- 已部署到仓库包和实际游戏目录，三处 Steam/模组入口 SHA256 均为 `B33DBFA8513A9AAA66A0F7045070828C311D087FACD645C822A55A712A941D0B`。逐文件备份及收据：`output/christasha-vfx-timing/deployment-20260923T030608Z/receipt.json`。包 DLL 不纳入源码提交，因为仓库原有二进制含其他未提交、已部署功能。

**待实际战斗确认：** 当前工具无法从游戏 loose assets 直接加载原版 `DamageTextEffect` 到隔离预览；需用本次已部署 DLL 录制普通 Slash/Hit 与书页 `9009006` 的真实战斗，逐帧确认原生命中火花、HP/BP 数字、三目标 Point 和防御目标。代理预览 PASS 不等于内置跳字叠放 PASS。
