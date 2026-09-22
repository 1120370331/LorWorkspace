# 克丽丝塔夏：游戏帧循环白金修复

**最新结果：`through-r12` 已部署，详见 [最终交付记录](2026-09-22-christasha-platinum-delivery.md)。** 下文“未部署”均是被后续用户规模/加速/爆开/轻抖要求取代的历史阶段状态。

用户要求：单线程，不启动智能体；修复各类特效在游戏内变黑，并增强到原画的白金表现。研究、实现、预览复核均由主线程顺序完成。

## 意图与固定边界

- 七类真实入口：Slash、Hit、Pierce、Projectile、Cast、Area、Point，均通过 `ChristashaNativeParticlePlayback` 驱动原生资源克隆。
- 保留 Slash 已认可的破锋笔触、主副刀势；Hit 保持剑刃纵落和 150ms 落点；汐音保留 0.3s 展开、0.5s 结算门与成功目标反馈。
- 以《汐音：光耀万海》卡图为配色参考：暖白锋、香槟金主体、淡金弦线和碎光。增强主次对比，不增加同形宽面、遮脸雾幕或伤害/震屏。
- 不分发游戏原资源，不修改技能数值、书页或原生全局材质。

## 初始证据与待证假设

已读原生源 README、粒子制作指南、既有 black diagnosis、runtime review、三份 recipe 和接入代码。此前通过的是显式 `Animator.Update` 后立即 `Camera.Render` 的 Editor replay，没有覆盖普通 Unity player loop。

共享 driver 将 Animator 的 speed 设为 0，但仍保持 enabled。Unity 的动画求值位于脚本 Update 与 LateUpdate 之间，可能重写已经提交的主题 MPB。新增真实 Play Mode 探针，记录同帧 Update/LateUpdate 的逐 Renderer 颜色与最终 GPU 画面，先验证这一假设再决定修复。

突刺当前逐 RGB 相乘；原生黑色/缺失颜色乘金色仍是黑。群攻也有部分空通道回退及非法浮点未完全处理。不同 Shader 的 alpha、mask、侵蚀通道不能用统一提亮替代。

## 实施与验收

1. 先保留未改生产代码的真实帧循环证据，再修共享动画所有权，保证每帧只有一次手动求值。
2. 逐角色处理颜色，保持原色快照与输出分离，避免累乘；不把退场的合法透明度抬亮。
3. 同一份生产源跑七配方 Play Mode：早段、峰值、退场、取消与同 seed 重播；深/中底输出。颜色从 Update 到 LateUpdate 不得变化。
4. 检查真实游戏 Managed API 编译、现有适配器/群攻生命周期契约和 Release 构建。
5. 所有初始 88 份 PDB 源文件校验和均与工作区一致；安装 DLL、包 DLL、bin Release DLL 均为 `47c561286f8597a30e34300300c67751bacfb7ad4470f422095df756a9d2fa4c`。因此可在保留全部已安装源码的工作区构建，再只更新已验收 DLL，保留备份和哈希。

初次 Unity 运行因许可证失效阻断，用户已刷新。证据保存在 `output/christasha-platinum-fix/`。Play Mode 使用本地原资源提取包，属于实际 Unity 帧循环验证，不等于完整游戏战斗录像。

## 已确认根因及追加形态要求

基线 `baseline-loop/frame-loop.txt` 记录 1487 次同帧主题色被 Animator 改写；Slash 的 `_MainColor` 由 `(1.140,1.050,.825,0)` 变成 `(0,0,0,0)`。将克隆 Animator 限定为手动求值期间启用、保留 controller state 后，`clock-fixed` 及后续候选全部为 0。此前 ColorNoise 差异只能算艺术填色/兼容假设，不能再当共同黑化的已确认根因。

用户进一步明确：“pierce…向前突刺的棱锥+扩散击中光圈”；远程为“光芒飞剑，有拖尾+击中金光”。该明确形态请求授权定制，应用 sword-slash-vfx 的网格/透明/时序规范；production 搜索无匹配且无 nearest production。现有 native Dagger 粒子组合不能仅通过调色得到所需轮廓，因此以原创连贯分面网格、既有 Built-in 着色器、小型解析发光横截面实现，不引入第三方候选资产，不修改技能库的资产目录。

棱锥 0.16s 到目标；飞剑 0.26s 抵达，尖端为原点，拖尾记录世界轨迹。命中按 `RencounterManager` 同步呈现里的 CreateBehaviourEffect/Result.Win/CreateDamagedEffectByBehaviour 真实合同配对，仅拥有同帧同目标的克丽丝塔夏突刺可替换原生火花。目标光圈、短金光及碎屑只触发一次。取消、未命中、失效、不同目标及其它角色保留各自语义。

## 当前交付边界（用户造型反馈后）

`candidate-r3` 和 `flow-r4`–`flow-r8` 的形态已被用户拒绝，不得部署。用户随后选定原角色佩剑的细长直刃、窄剑脊、小护手；`model-r9/model-study` 是该方向的三视角不发光灰模，主线程检查后修正尖端退化三角的朝向，并进入 `slender-r10` 的真实 Play Mode 预览。

当前构建候选在 `output/christasha-platinum-fix/slender-r10/release/`，含匹配 DLL、PDB 和新 Shader AB。DLL SHA256：`74127c0d5b2fe8eb84898e19b3ec1bbfbd1886c92f8694008b30a30be27c1d1d`；AB SHA256：`9a40423e09c8128b8cea55ce2826be6f7a5b9ed7468f605509b5a403fdaf39fa`。完整源/输出指纹在同候选的 `candidate-manifest.json`。

技术检查：真实 Unity 2019.3.15f1 / D3D11 从新 AB 加载 Shader，通过七配方及未命中/重播/取消/角色上下文；帧循环颜色覆盖为 0；实际游戏 API 编译通过；接入 fixture 193 断言通过；Release 构建 0 错误、7 项既有警告。主线程查看了早段、飞行、接触及退场帧，灰模与光效均有可审查媒体。此证据不等于用户已接受美术，也不等于完整游戏实战。

**没有部署或提交。** 已安装游戏和仓库模组 DLL 仍为初始 `47c561286f8597a30e34300300c67751bacfb7ad4470f422095df756a9d2fa4c`，未将任何被拒版本覆盖到游戏。原工作区其余已安装源码均经 PDB 校验确认没有漂移，其他任务的改动保持。

### 弧线飞剑追加（当前最新：arc-r11）

用户追加优雅曲线弹道后，生产 adapter 与 Unity 预览共用 `ChristashaProjectileArc`：0.38s 单条上扬后收落的 Bézier 弧线，剑身沿解析切线转向，尾迹保留世界坐标，命中圈跟随到达方向。长距离弧高封顶、左右均向上、同点不造环，保持真实目标与成功事件所有权。总视觉寿命 0.86s。

当前复核的是 `output/christasha-platinum-fix/arc-r11/`，包含 60fps 视频、逐帧图、manifest 与 `release/`。Unity shader AB 构建/加载、帧循环、重播/取消/未命中通过，颜色覆盖为 0；207 项接入断言和真实游戏 API 编译通过；Release 0 错误、7 项既有警告。主线程亲看角色上下文的上扬、转向、收落与接触帧。当前 DLL：`c50d1acddd8f70c084e0d7deb6709670e7a3c744daab940ea13bd3dec62c5e69`，AB：`9a40423e09c8128b8cea55ce2826be6f7a5b9ed7468f605509b5a403fdaf39fa`。构建候选已准备，游戏目录仍未覆盖；最终美术接受留待用户查看。
