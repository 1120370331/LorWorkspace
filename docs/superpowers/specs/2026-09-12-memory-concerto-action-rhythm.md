# 回忆协奏之刃：三击调整动作与节奏设计

2026-09-12。阶段 1 只读研究产物；主线程已批准下列时长与原生移动保留策略，尚不是实现或预览验收报告。

## 冻结意图

用户最初提出“长—中—长—更长”，核对本卡是三颗攻击骰加一颗闪避骰后，用户明确改为“那就3段就行，长-中-更长”。仅为 9001004 的斩、打、突三个攻击动作建立长—中—更长的准备和收势；第 4 骰闪避完全保持原状，不增加第四次出刀、不改骰子机制。

完成边界：三个 ActionScript 精确绑定、可重现动态节奏预览、原生契约与回退检查、游戏 API Release 编译、新的制作指南及索引。材质、已接受贴图、技能数值、弃牌加威力、闪避、其他书页与角色皮肤不在变更面内。没有资源变更时不为此任务重建 Unity 或 AssetBundle。

## 诊断与已核实参考

- `SteriaBuild/SteriaModFolder/Data/CardInfo.xml:73` 起的三骰是 Slash/J 2–7、Hit/H 3–6、Penetrate/Z 4–8，ActionScript 全空；`:76` 是 Def/Evasion/E 1–6。现有普通效果已各有主题贴图，缺口在动作调度，而非素材质量。
- `SteriaBuild/AnhierCards.cs:180` 的 OnUseCard 仅处理弃牌与威力，没有演出动作。该逻辑不应承担全局计数或时间控制。
- `SteriaBuild/Assembly-CSharp/RencounterManager.cs:2921` 的原生胜利近战通常返回“按伤害量前移 + Stop”，各段最低 0.125s。没有每骰准备与长短乐句，三个不同攻击因而以近似同节奏连放。
- 寒昼 `寒昼事务所V4.0/unpack/ColdSun/HZ_Core/BehaviourAction_ColdSun_BasicAction.cs:24` 与 DefaultAction 的可核实做法是：胜利且非近对远时拆成动作段；第一个段 PRE 打印反馈，后段 NONE 保持；输平/远程回到基类。它示范了按骰动作脚本安排节奏，但自身 0.10 + 0.20s 不是本次要直接照搬的数值。
- 寒昼 `BehaviourAction_5_scar_lian_Special02.cs:40` 起显式填充 opponent.infoList，使双方逐段同步。此例使用专属姿势、数值枚举与独立特殊特效；这里只参考两侧队列编排，不复制角色图或复杂效果资源。
- `ModGuideDocs/02_战斗书页完整制作.md:171` 已说明 Motion，`:204` 附近有 ActionScript XML 示例，但缺少普通书页动作队列和事件时序教程；寒昼的 `LoR高级模组制作教程.md:373` 主要解释角色全局 HZ_SetActionBase，不能取代本卡的局部 ActionScript。
- `ModGuideDocs/13_贴图攻击特效制作流程.md:23` 起已接受回忆效果首帧完整、保持 250ms、约 667ms 淡尽。此时间线维持不变，动作节奏负责“何时开始播放”和“多久保持姿势”。
- 最近提交 `440d8ba3` 已重做原生时间与挂接的独立攻击效果，本次保留这套设计。工作树中其他模型及特效未提交变更不参与本次重新设计。

## 可执行动作设计

所有数值都是缩放游戏时间中的段最短时长，未包含移动完成耗时、两段间的逐帧 yield、骰子 UI 和原生交锋间隔，不是承诺的真实墙钟总长。

| 攻击骰 | 无事件准备 | 唯一反馈段 | 无事件收势 | 标称最低合计 |
| --- | --- | --- | --- | --- |
| 第 1 骰，长 | Guard / Stop，0.30s | 原生 Slash 首段，0.15s | Slash 原生 Stop，0.35s | 0.80s |
| 第 2 骰，中 | Guard / Stop，0.10s | 原生 Hit 首段，0.15s | Hit 原生 Stop，0.25s | 0.50s |
| 第 3 骰，更长 | Guard / Stop，0.50s | 原生 Penetrate 首段，0.15s | Penetrate 原生 Stop，0.55s | 1.20s |

视觉层级为“收刀架势 → 单次明确打击 → 留住完成姿势”。Guard 与 J/H/Z 有可见区分，并且是通用皮肤动作，避免依赖不存在的 S1/S2。三个动作保持斩、打、突的原有方向与姿势。最后一击把更长时间分配到准备和收势，构成明确终句；中击仍保留 0.10s 呼吸，不做闪烁跳帧。

保留已有黑紫实体、局部橙红燃烧和自然 alpha 退场。不要增加泛光、重复透明壳、额外刀光层、时停或新的震屏。首击收势与中击准备之间可能保留少量自然淡出的余迹，但不可同时出现两次强态主形。末击收势应让现有效果接近淡尽后再进入下一骰。

### 生产实现约束

建议在单一 `SteriaBuild/BehaviourAction_Steria_MemoryConcerto.cs` 中放置三个公共入口类型和一个局部共用基类/构建器：

- `BehaviourAction_Steria_MemoryConcerto_Long`
- `BehaviourAction_Steria_MemoryConcerto_Medium`
- `BehaviourAction_Steria_MemoryConcerto_Final`

分别以 `Steria_MemoryConcerto_Long`、`Steria_MemoryConcerto_Medium`、`Steria_MemoryConcerto_Final` 写入前三骰 ActionScript；第 4 骰仍为空。三个类型仅声明本骰动作与时间配置，不使用静态刀数、运行时骰子索引或依赖上一击是否成功的状态。重投、额外骰复制、首次攻击输拼点以及多名使用者并行时，后续动作不会串位。

正常近战胜利路径先取得基类 self 与 opponent 原生队列，再包装成两侧对齐的三段。保留原生首攻击段的 moveState/dstRatio/speed 和对手原生受击/击退，不为了让模拟数值整齐而把所有移动改为 Stop。将自身原生首段最低时长设为 0.15s，自身末个原生 Stop 设为对应收势时间；前置一个 Guard/Stop 准备段。

对手准备段使用 Default/Stop、与自身相同准备时长，无事件；后续原生反应延后到自身出手段才开始。对手原生只有一段时，仅使用一次该对象，尾部另建无事件 Stop 保持该反应姿势，绝不能重复 Knockback 对象；原生两段时逐段保留。最终两侧必须同为三个索引段。若实际原生队列形状与预期不符，完整回落，不能截断未知阶段。

不要盲目 Clear 对手列表。对手已有非空自定义序列，或对手携带未知 ActionScript 而包装会跳过其专属失败动作时，应回落基类，让原生流程继续调度该脚本。可明确允许本任务三个无状态脚本相互对战，因为它们的失败契约是基类；是否增加此白名单由实施者按实际调用字段做最小处理并测试。

### 事件合同与原版边界

`RencounterManager.cs:3717` 构造签名为 `(actionDetail, moveState, dstRatio, updateDir, delay, speed)`；`delay` 是段已开始后的最低持续时间，不是攻击生效之前的等待。`CharMoveState.Stop` 只禁止位移，仍受 delay 约束（`:2402`、`:2476`）。`speed` 影响移动收敛，不是 Sprite 动画或整个书页的播放倍速。

`EffectTiming.cs:5`：PRE=段开始、POST=双方该索引都完成后的段末、NONE=本段无显式时点。枚举还有 NOT_PRINT 与 WITHOUT_DMGTEXT，不应把寒昼反编译数字 0/2/4 当成等待时长或照抄。

准备段与收势段 `SetEffectTiming(NONE,NONE,NONE)`；唯一攻击段 `SetEffectTiming(PRE,PRE,PRE)`，保持 `customEffectRes` 空与 `callAtkEffectEvent` 原默认值，不伪造额外伤害或强制效果。顺序是 `(atk, recover, damaged)`。

原版 `MoveRoutine` 在 `:2105` 检查双方是否全无显式时点；若全 NONE，会在整段动作开始前直接打印效果/恢复/伤害。因此只加“全 NONE 前摇”会失败，必须保留唯一显式事件段。`:2249` 与 `:2494` 消费胜方列表 PRE/POST；`:2203` 起按双方相同索引运行，只有双方均完成才移除两侧首项（`:2543`），总段时长由较慢一方决定。

注意 `:2140` 起的 afterAction/succeedAtk/takeDamage 机制回调仍在动作循环之前由原版调用。EffectTiming 只改变原生视觉反馈的打印时机，不意味着把真实骰子结算或被动机制延迟到该秒数。不要另加协程延迟技能或手动调用伤害方法。

### 回退与机制兼容

- 仅对 `self.result == Result.Win` 且普通近战攻击、实际动作与当前入口一致、基础结果可用时包装。单方面攻击以原版生成的 Win 结果走同一路径，不要求对手必须有真实骰子。
- Lose/Draw、被封锁转 Draw、近对远、数据异常、未知原生序列形状、已有对手专属序列回到基类原结果，不施加攻击命中反馈。
- 第 4 闪避既不绑定也不包装，包括可反复闪避与储存至后续交锋。
- 借用/复制书页仍按所携带的 ActionScript 工作；额外重投同一骰仍重复该骰自己的节奏。不同使用者之间没有共享状态。
- 接入不需新的 Harmony patch：`AssemblyManager.cs:48` 自动登记继承 BehaviourActionBase 且以 `BehaviourAction_` 开头的类型，`:187` 按去掉前缀后的 ActionScript 创建实例。

## 最小变更面和冻结基线

1. 新增上述动作源码文件；CardInfo 只改 9001004 前三骰 ActionScript。
2. `SteriaBuild/Verification/MemoryConcertoRhythm/` 下建立有界契约/动态预览 fixture，直接链接正式源码，测试桩只提供必要原版类型。输出实际 GetMovingAction 双侧队列与事件时标，渲染端不得另写一份固定时间表。
3. `SteriaBuild/VFXSource/AnhierTextureCombat/verify_source.py:36` 起旧 Canonical CardInfo 验证需只许可这三项确切新增属性；逆转这三个 ActionScript 后继续验证原基线哈希，其他 18 骰字段锁仍保留。`accepted-six-baseline.json` 原文件保持字节不变，不通过重录整张 CardInfo 哈希掩盖扩大修改。
4. 新建普通动作节奏指南并在 `ModGuideDocs/README.md`、战斗书页指南的相关入口添加链接。第 13 章已有并行工作，避免整篇改写。

## 验收矩阵和拒收条件

主要动态样本是正常速度完整三击，输出至少 60fps MP4 或浏览器可可靠播放的 GIF/HTML，并保留可读的阶段和事件 JSON。画面须显示两名有明确准备/攻击/受击姿势的角色或中立人体、当前骰、时间轴和事件指示；抽样静帧仅辅助比较准备、反馈首帧、收势与末尾，不代替动态评审。

可使用本仓库既有 managed CPU simulation 路径，不启动游戏或 Unity。要清楚标注它是实际动作源码队列驱动的 CPU 调度预览；角色艺术、真实游戏镜头、真实声音和完整交锋墙钟仍是证据边界。若不播放真实效果，应明确说只验收姿势与节奏，不能将其标作游戏截图。现有 VFX 九 Profile 未改文件的验证与视觉接受证据可复用。

| 情景 | 必须通过的证据 |
| --- | --- |
| 三次近战胜利 | self/opponent 同段数；前三骰各唯一 PRE；最低总时长分别 0.80/0.50/1.20，且中击显著短、末击显著长；调整姿势有可见区分 |
| 对手攻击/守备/无骰单方 | 准备期间不先受伤/击退；原生单段 Knockback 执行一次；恢复/伤害打印不重复；适用原生反应保持 |
| 输、平、近对远、被封锁 | 精确进入基类路径，不残留准备、不新造命中、不影响对手专属列表 |
| 首击失败后第二/三击胜、重投、另一使用者 | 节奏由本骰入口决定，仍然中/更长；不串计数 |
| 第 4 闪避和 XML 边界 | 最后骰字节/语义不变；仅前三个 ActionScript 获许可；其余数值、EffectRes、Script、类型、关键词均不变 |
| 最终候选 | 新契约检查通过，旧 source verifier 通过，Release 编译通过；独立预览 review=PASS；无新 AssetBundle/图像依赖 |

拒收：静态图片冒充节奏证明；在效果已出现后只延长 delay 却声称加了前摇；全 NONE 导致起手提前打印；准备段先把对手改为 Damaged/Knockback；重复 Knockback；复制一个 MovingAction 对象至多个索引；丢失胜负/对远回退；末闪避被改成攻击；修改技能机制或全局速度；对手自定义序列被覆盖；用硬编码预览时序替代实际队列；扩大旧基线豁免。

## 制作指南补足内容

指南须用本卡给出可编译的类与对应 XML，解释 Motion、ActionScript、EffectRes、Script 四者的分工；列明构造参数与 PRE/POST/NONE 含义；展示双队列三个索引如何配对以及单段受击如何补 Stop；说明为何同一帧首次打印与机制回调不是同一时钟；给出胜负/对远/单方/重投回退和通用姿势选择；记录 0.8/0.5/1.2 仅为最低合计、完整预览和验证命令、基线如何增加精确许可，以及不改素材时无需 Unity/AB 构建。引用寒昼真实文件和原生 API，不承诺本次未覆盖的特殊群攻或角色全局替换。
