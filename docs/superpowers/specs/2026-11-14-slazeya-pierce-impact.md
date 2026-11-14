# 斯拉泽雅突刺：前刺与命中光圈

用户要求：『突刺都是一个穿刺动作+命中光圈扩散的』。本次只修改已部署风系套件的近突刺，不修改斩、打、防和远程，也不改骰子数值或伤害结算。按此前要求由主线程实现，部署阶段沿用指定的Terra owner。

用户随后强调『按skills来。她那个效果一般般的』：Christasha仅保留为原生命中事件和目标锚定的技术依据，不作为美术标准。视觉按rng-vfx的主形、亮度等级、快出慢退及单职责分层要求；光圈优先选用已索引、已查看的skill库素材。先检查真实目标方向上的白模，再看带材质的刺入、接触、扩散与退场帧。

依据：ModGuideDocs 08/14，以及现有 ChristashaNative/Pierce README、ChristashaStrikeImpact.cs、ChristashaThrustEnvelope.cs、ChristashaNativePierceRecipes.cs。已查看 through-r12/Pierce 的0.167s与0.283s实渲：前者是锐锋到达和短闪，后者是贯穿主体与已扩大的接触圈。只借鉴时序与命中合同，不复制白金主题或修改它的已部署实现。

视觉与事件：

- 从攻击者挂点出发，前0.12秒沿真实目标方向快速伸出尖锐风刺；后0.075秒穿过目标身体，根部保持在手侧。
- 确认命中时，在目标胸口前侧短闪，保留原生命中高度；Bada的攻击挂点远离躯干，X使用身体中心朝攻击者方向0.35个角色尺度的偏移，避免光圈悬在空中。青白光圈约0.22秒扩张到目标身体高度约75%的直径，并在0.44秒内淡出。清晰光圈是第二视觉重点，少量光尘作尾散。
- 刺穿后0.20–0.34秒快速收回风刺，避免变成长时间悬停的激光；收势后由扩散圈承担余韵。
- 原生 RencounterManager 在同一胜利表现块中调用 CreateBehaviourEffect → CreateDamagedEffectByBehaviour。独立路由只配对本次风突刺、同一目标、同一帧；不根据距离或计时猜命中。
- 风突刺匹配到成功事件时，用自己的延时接触反馈替代那一次原生通用火花；未匹配、其它角色、其它类型仍走原生行为。
- 命中圈冻结世界接触点和半径，不随攻击者后续移动；取消、丢失目标、停用均清空。未命中不出圈，也不继续穿到目标背后。

结构：沿用原生Shuriken与固定FormedArc主体Shader。光圈使用技能库 `texture.kenney.transparent.light-02`（CC0，已看接触表cell3及原图），作为辅助Billboard粒子；不用新的一次性Shader。公开的纯视觉边界为 AimPierce(worldContact,worldExit,worldRadius) 与 ConfirmPierceImpact()；只改Pierce分支。新增的Harmony命中路由独立文件，不改已有Christasha路由。

验收：双朝向、目标距离变化、未命中、错误目标/跨帧回调、重复确认和取消；查看出手、接触、扩张、淡出；实际Unity运行证明主刺到达后才出圈。其它四类与上一候选同时间帧保持一致。通过后部署这一窄改动，并保留可回滚版本。

候选14独立复核退回：主刺动作可接受，Kenney light_02 在扩张期仅呈两团柔光，缺少明确环缘。候选15仅将光圈换为 skill 索引的 `incoming.vfx-texture-maker.texture-candidate.circle-01.2bb8978990`（接触表15格，haw2fregel，MIT，原PNG字节不变），使用原粒子材质与同一时序。没有导入该工具的 Unity 2022 代码，也未提升库素材状态。另修复超过120fps时无模拟tick的帧也需要恢复光圈世界锚点的问题。
