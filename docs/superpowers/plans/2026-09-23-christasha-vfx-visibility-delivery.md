# 克里丝塔夏近战与汐音特效可读性调整

2026-09-23 已部署到仓库模组包和实际游戏目录。范围只包括 Slash、Hit 与「汐音：光耀万海」的群攻配方；其他 Xiao 群攻动作未修改。

## 画面与时序

- Slash / Hit 的角色挂点视觉倍率由 `0.35` 调到 `0.45` / `0.55`，保留已有白金破锋与 Greatsword 纵落。双向角色上下文预览中，主形覆盖近战目标。
- 汐音在战斗 `0.30s` 展开 Area；原生主载体保持纹理亮度，并加一条有透明破口的弯曲笔锋，使白金浪锋在结算前持续覆盖目标列。`0.75s` 才放行 manager，相比原来每颗骰子增加 `0.25s` 展示时间。
- 只有 manager 确认的成功目标才产生 Point；每个 Point 以 `2.2` 倍视觉尺寸显示约 `0.12s` 的短亮核和随后退出的碎光。三目标时仍可分别辨认。

## 验收与部署

- 候选：`output/christasha-vfx-visibility/candidate-r7/frame-loop/`，含 60fps 视频、深/中底帧、双向 Slash/Hit 角色场景和三目标汐音代理时序。Unity 2019.3.15f1 Play Mode 结果为 PASS，主题色帧间漂移为 0，同 seed 重播与取消通过。
- 独立视觉复审 PASS：结算前可见连续破锋，结算后三个命中核分离；无必须返修项。代理时序用模拟成功回调生成画面，真实 manager 回调由 managed fixture 验证，不将代理场景视为完整游戏录像。
- 主线程复跑 managed 契约 `230` 项 PASS；真实游戏 Managed API 编译 PASS；Release 构建 0 错误、7 项既有警告。新 DLL 的 94 份 PDB 文档仅三份生产源码和生成的 AssemblyInfo 与上次部署不同，且全部与当前源码哈希一致。
- 仓库包、实际游戏目录及 C 盘 Steam 链接入口的 DLL SHA256 均为 `B37008E380B7202CBBD6ABF9A5EB9F9AB3986BFFD96AFF72D86EA974CF1CF244`。Shader AB 未改，仍为 `B40C46753A718612FA1804665A6820999AC905E9EE6088B1B2A27B40D3C76B94`。逐文件备份和部署收据：`output/christasha-vfx-visibility/deployment-20260923T022540Z/receipt.json`。

仓库原有工作区包含其他未提交源码与此前已部署二进制，因此本次源码提交不纳入包 DLL；部署 DLL 与验收候选由上述 PDB、SHA256 和收据约束。完整游戏战斗中的镜头与后处理观感留待实战验收。
