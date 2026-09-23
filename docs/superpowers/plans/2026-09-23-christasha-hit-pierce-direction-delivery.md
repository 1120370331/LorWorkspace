# 克里丝塔夏打击方向与近刺命中修正

针对用户反馈“打击方向似乎颠倒；突刺没看到击中特效”，已在 2026-09-23 部署候选 r13。

## 修正

- Hit 保留原 Greatsword 三段主刀与约 `0.15s` 的目标落点；按来源说明给视觉实例施加一次外倾 `Euler(-10,0,0)`。原主刀早期覆盖随施术者侧的短破锋逐渐打开，双向角色预览均呈现施术者向目标的扫落，不整颗翻转导致落点移位。
- Near Pierce 成功结果保留原版 Penetrate 火花，与原版伤害跳字处于同一结果段；Projectile 仍阻止抵达前的原生火花。近刺自有亮核与前景断续冲击环在真实目标 XY 处出现，刺身短暂退亮，环扩到目标外缘后消散。未命中、错误目标和取消没有自有接触环。
- 修正只涉及视觉与结果反馈，伤害结算、汐音时序、Slash 与飞剑几何未改变。

## 验收与部署

- `output/christasha-hit-pierce-fix/candidate-r13/frame-loop/` 含 60fps 深/中底双向 Hit、Near Pierce 命中/未命中及其他配方回归帧。Unity 2019.3.15f1 Play Mode PASS，MPB 漂移 0，同 seed 重播及取消 PASS；候选源码哈希见 `source-hashes.json`。
- 独立预览复审 PASS：Hit 起势方向明确、落点不移；近刺成功目标有可辨亮核和环，未命中没有。代理预览没有游戏原版 Penetrate 火花或伤害数字，不能替代真实战斗录像验收。
- 主线程复跑 managed 契约 `234` 项 PASS、真实游戏 Managed API 编译 PASS、Release 构建 0 错误及 7 项既有警告。94 份 PDB 文档与上次已部署 DLL 相比仅四份本轮生产源码及生成的 AssemblyInfo 改变，全部与当前源码哈希匹配。
- 仓库模组包、实际游戏目录及 C 盘 Steam 链接入口的 DLL SHA256 均为 `2BE37D94F822BAE1A8B50C560A8280C9DAE1F4703D02F7373C6D4CCC6C6F0827`；Shader AB 沿用 `B40C46753A718612FA1804665A6820999AC905E9EE6088B1B2A27B40D3C76B94`。备份与收据：`output/christasha-hit-pierce-fix/deployment-20260923T044836Z/receipt.json`。

仓库已有其他未提交、已部署功能，因此包 DLL 保留为工作区修改而不纳入本次源码提交。已查仓库预览、近期用户媒体目录和 Steam 截图目录，未找到此版本实战录像；完整游戏内火花/跳字层显仍须实战观察。
