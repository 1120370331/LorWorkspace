# Independent Visual Acceptance Review

1. 结论：PASS

2. 对原始用户意图的贯彻程度

已贯彻。候选 `a3c8437f4f93287847e331edf14f8d02ca60cc35e3714863c140f5b77ede2df2` 明确走“模拟即可”的交付边界，预览使用中性人形展示，不含 Bada 皮肤或原版 Effect(9) 层。六个 profile 仍保持海潮蓝水体、回忆黑紫加橙红火感、Guard 防御主题，并且在大小两种体型、左右朝向下都有足够可见的主体形状。

3. 用户体验问题

未发现必须返修的用户体验问题。主路径从首帧到 250ms 都能自然读到攻击域，400ms 进入合理退场，667ms 清空；相较被用户否定的“像没特效”，当前 Slash、Hit、Pierce、Guard、SeaPierce、SeaFarHit 都有明确体积、接触位置和角色关系。Guard 位于防御者身前，SeaFarHit 同时表达枪口与目标接触水花，近战特效没有漂到错误目标根。

4. 竞品参考差距

未低于同类成熟 2D 回合制战斗特效的合理预期。MemorySlash 具备纵向落斩轮廓和下前段重心；MemoryHit 是横向扫击主体，不再是误导性目标星爆；Sea 系以水体形状和白蓝锋面提供打击可读性。整体不是逐像素复刻 Bada，而是借用了成熟游戏常见的“大轮廓、首帧完整、短保持后退场”的时序语言。

5. 技术债和解耦问题

本次只做独立视觉验收，没有发现视觉层面的明显技术债。候选清单声明 `productionCopyMatches=true`、`nativeReferenceOnly=true`，发布内容为原创 PNG/JSON 与 mod DLL，不复制原版皮肤或 native 代码。Unity GPU、真实游戏镜头、音频和战斗执行未在本验收范围内验证，作为已声明边界保留。

6. 回归风险

视觉回归风险可接受。检查的帧覆盖 0ms、50ms、250ms、400ms、667ms，以及大小体型和左右镜像；未见首帧不可见、150ms 已碎成余迹、250ms 丢失攻击域、Guard 站位反向、远程命中明显延后、绿色边缘、透明薄壳、重复塑料层或误导性皮肤效果。

7. 证据：命令、截图、日志、路径、复现步骤

- 阅读规范：`docs/superpowers/specs/2026-09-09-anhier-native-impact-redesign.md`，重点核对最新用户约束、profile 目标、时序与拒收条件。
- 候选清单：`SteriaBuild/VFXSource/AnhierTextureCombat/previews/candidate-hashes.json`，candidateId 为 `a3c8437f4f93287847e331edf14f8d02ca60cc35e3714863c140f5b77ede2df2`。
- 哈希核对命令：用 Python 读取 `candidate-hashes.json` 并重新计算 `independent-two-size-contact.png`、`independent-phase-contact.png`、`independent-all-six-60fps.mp4`、`independent-all-six-30fps.gif` 以及 6 profile × 2 size × `00/03/15/24/40/60.png` 的 SHA-256；结果无 mismatch。
- 视觉检查路径：`SteriaBuild/VFXSource/AnhierTextureCombat/previews/independent-two-size-contact.png`、`SteriaBuild/VFXSource/AnhierTextureCombat/previews/independent-phase-contact.png`，并单独查看 `MemorySlash_large/15.png`、`MemorySlash_small/15.png`、`MemoryPierce_large/15.png`、`SeaFarHit_large/15.png`。
- 采用证据边界：本报告只验收独立模拟视觉表现；215 managed contract checks、source verifier、Release build PASS 作为主线程/实现证据引用，不在本报告中重复认证。

8. 必须返修项

无。

9. 可延期项

- MemoryPierce 的橙红亮点更靠近出手侧，当前仍能读作暗紫穿刺体积命中目标；若后续继续打磨，可增强目标侧的细亮锋面，让方向性更锐。
- 真机 Unity GPU、真实镜头、音频混合和战斗上下文仍属于后续外部验收边界。

10. 建议下一轮任务

进入主线程最终交付汇总：保留本候选哈希、模拟边界和媒体路径，向用户交付 `independent-all-six-60fps.mp4`、`independent-all-six-30fps.gif` 与两张 contact sheet 作为外部验收材料。
