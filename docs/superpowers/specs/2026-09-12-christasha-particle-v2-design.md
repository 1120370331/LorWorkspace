# 克丽丝塔夏粒子特效 V2：研究与冻结设计

日期：2026-09-12。状态：主线程已批准技术方向、资源名及纯 Unity 播放合同；视觉参数以下述范围制作首个候选，经真实 Unity 预览审查后收敛。

## 1. Intent Contract 与完成范围

制作可变的真正粒子特效，以克丽丝塔夏现有卡图的金白海潮为身份，重制斩击、打击、突刺与「汐音：光耀万海」。原版罗兰、邵、异想体及寒昼作为结构和动作参考，原创制作图形、纹理与编排。旧 `ChristashaDawnCombat` 退出书页生产路径，不作为新视觉基线。

本轮交付三种近战攻击、兼容远程突刺的视觉与汐音群攻；指南同时给出挥舞、突刺、打击、防御、范围、特殊、点位七类可复现 recipe。防御/特殊 recipe 不等于授权修改所有防御书页。保留点数、类型、能力、Range、音乐与复制骰子机制。

主线程已确认基线有 46 处旧 EffectRes，跨 23 张页：Hit 2、PierceFar 8、PierceNear 8、SlashH 19、SlashV 9。全部映射到新粒子库对应动作；`9009006` 单独改为新的 FarAreaEach ActionScript。另五张既有 Xiao 群攻 ActionScript 保留；普通 EffectRes 的迁移不代表这些原有特殊群攻动画也已重制。最终数目以主线程保存的 XML 基线和差异验证为准。

## 2. 已读取的仓库依据

- 根 `AGENTS.md` 与 `ModGuideDocs/README.md`；教程 02、08、10、11、13 的书页、挂点、加载、材质、构建与验收条目。
- `SteriaBuild/VFXSource/NativeAttackReference/README.md` 与现成 `references/all/manifest.json`、选定 ParticleSystem/ParticleSystemRenderer/Material/Animation 元数据。
- `SteriaBuild/Assembly-CSharp/Battle/DiceAttackEffect/DiceAttackEffect.cs`，`DiceAttackEffect_BlackSilence.cs`，`BehaviourAction_BlackSilence_LongSword.cs`、`ShortSword.cs`、`SpecialDurandal.cs`，`DiceAttackEffect_XiaoEgo.cs`、`FarAreaEffect_Xiao_Taotie.cs`、`FarAreaEffect_EGO_Orchestra_Movement_4.cs`。
- `SteriaBuild/VFXSource/AnhierBlueWhiteSlash/hanzhou_vfx_reference_dump.txt`；寒昼已解包参考目录与可用 bundle 名称。复用现成只读导出，没有全量重新解包。
- 当前 `DiceAttackEffect_Steria_ChristashaDawnCombat.cs`、`FarAreaEffect_ChristashaGoldenMass.cs`、角色书页与本地化，仅用于识别旧接入和需要淘汰的职责。
- 近期提交 `440d8ba3`、`6557e8ac`、`342685d5` 的主题与邻近已接受贴图库规范。教程 13 的 250ms 完整 PNG 保持是另一套已接受贴图库的合同，不移植成本轮粒子动作的限制。

结构研究摘要及原元数据路径见 `../references/2026-09-12-christasha-particle-native-evidence.json`。原版像素、Mesh、Shader、prefab 均不进入发布资产。

## 3. 原版精确证据及可学习之处

| 权威资源 / 调用 | 实际观察 | 本轮采用的原则 |
| --- | --- | --- |
| `BehaviourAction_BlackSilence_LongSword.cs:21` → `FX_PC_RolRang_ShadowSlsah`（原名含拼写错误） | `customEffectRes` 覆盖普通书页资源；真正玩家罗兰粒子库在 `new/fx/pc/rolrang/`，不能仅查看 `blacksilence_j` | 从真实动作调用找 prefab，区分普通 Sprite 与粒子动作 |
| `fx_pc_rolrang_slash_down`，root `resources.assets:43475` | 3 个 PS：两个 Mesh 主拖迹分别 0.6/0.7s，t=0 各 burst 1；第三个 Billboard 粒子 t=0.1s burst 10，寿命 0.4–0.8s；主拖迹启用 Rotation/Color，碎屑启用 Shape/Size/Velocity/Noise | 少量清楚的主体 + 随后释放的运动碎屑便可形成完整动作，不追求系统数量 |
| 上述 `ParticleSystem:70147` 与 renderer `77163` | Mesh mode=4、Local alignment=2、sorting=2；Rotation 曲线高速起步、衰减至零；曲线 scalar 约 26.18 rad/s，不是固定贴图透明度动画 | 刀光必须有真实角运动与快起慢收；数值只作关系参考，不照搬 |
| `fx_pc_rolrang_shadowslsah`，root `42336` | 15 个 PS；`StartFlash1` 0.05s、主要 Slash 0.2s、EndParticle 0.75s、EndWave 0.8s；主锋 Mesh 寿命约 0.15–0.2s，辅烟、波和碎屑各自收尾；部分 SubEmitter | 具有起势、主锋、余波的事件层级；此特殊长动作时序不能原样用于普通骰子 |
| `fx_pc_rolrang_dagger`，root `46130` | 8 个 PS；`Slash1` 为 Mesh，寿命0.4s，启用 Velocity/Size；两组 Stretch 粒子与两层烟，另有 distortion | 突刺的长轴速度、锋头与短拖迹职责分离，不将斩击旋转90度冒充 |
| `FX_Mat_PC_RolRang_MainTrail`，material `285` | `_MainTex`、`_Noise`、`_ColorNoise` 各一；`_MaskPower=3`；主/外颜色分开。PS lifetime、材质纹理、Animator 均参与结果 | 大形、边缘、流动纹理分开；用原创 shader/masks 达到同类分工 |
| `fx_mon_xiao_areatrail1`，root `30442` | 11 个 PS；Mesh 主波0.8s，多层转动的副波0.4–0.7s，火体为15个 Billboard；两组 Spark t=0.1s 各20个，寿命0.5–1.5s | 范围主体清楚，碎屑晚于主体；本轮改为金潮与弦波，删除火焰/龙形身份 |
| `FarAreaEffect_Xiao_Taotie.cs` | 施法位置、世界范围和 collision 对象分离；`OnGiveDamage` 令 `isRunning=false`；动画监听和游戏 FarArea manager 驱动结算 | 视觉阶段与游戏伤害结算是不同合同，不在 PS 回调自行造成伤害 |
| `fx_illusioncard_4_n_orchestra_start`，root `40270` | 7 个 PS，四条独立 Mesh 线各 burst1、0.5s，并启用 Size/Rotation/Color；一组75个粒子有 Velocity/Noise | 汐音用稀疏弦线与明确层次，不在满屏环形背景贴图上泛光 |
| `fx_illusioncard_4_n_galaxycard_o`，root `34693` | 3 个 PS：背景、2个Plasma、6个带Trail的Particle | 点位/特殊反馈可由少量不同职责粒子建立，不必做大场景 |
| 寒昼 `寒昼小兵_刀光_斩击` | 导出11个PS含一个禁用发射的根；Mesh刀光 t=0、约0.5s；Billboard烟 t=0.02s；Stretch光针 t=0.05s；局部爆点 t=0.07s | 短延迟造成因果关系；本轮烟/光针/爆点必须可单独关闭、调色与变体 |
| 寒昼其他已导出例 | 丹顶血魔斩击7个PS、冷雨工坊斩击8个、洛里安灼火斩击11个 | 可学习组合语言，不复制资源与固定坐标；系统数是观察而非成功指标 |

未知边界：manifest 全注册根遍历完成、错误0，不代表动态 `Resources.Load` 全部覆盖。剥离的 Mono 字段仍不透明，Shader 与 Mesh 只记录依赖；材质名不等于已验证的最终混合方程。罗兰 Slash Animator 原片长约1.1167s，但攻击脚本会调整 Animator.speed，不能把原片秒数直接当作实战秒数。未将原版 PS 原尺寸18直接变成项目尺寸。

额外确认的公开技术依据（2026-09-12 已读取）：

- Unity 2019.3 Renderer module：<https://docs.unity3d.com/2019.3/Documentation/Manual/PartSysRendererModule.html>，Mesh、Billboard、Stretch 的职责及 Camera/Velocity/Length scale；Stretch 不等于只拉伸贴图。
- `ParticleSystem.Simulate`：<https://docs.unity3d.com/2019.3/Documentation/ScriptReference/ParticleSystem.Simulate.html>，手动推进真实引擎模拟，用于生产与预览同钟。
- Custom Data module：<https://docs.unity3d.com/2019.3/Documentation/Manual/PartSysCustomDataModule.html>，需要逐粒子 shader 年龄/侵蚀时使用统一数据流。无需复制第三方特效代码。

## 4. 根问题诊断

旧系统混合 `_Reveal/_Retreat` 固定月牙、重复材质实例、运行时弹道、目标朝向和距离补偿、统一震屏/音效与后备粒子。其主要问题是没有清楚区分“主题资产”“粒子动作”“游戏适配”；改一种形状容易触碰其他动作。用户已明确废弃，因此本轮不再修饰旧月牙或复用其 core-only 资产。

`FarAreaEffect_ChristashaGoldenMass` 给新 SpriteRenderer 设置材质却未赋 sprite；主线程还确认它缺乏正确释放 FarArea manager 的阶段推进。把普通 EffectRes 写在群攻骰子上不能替代 `SetFarAreaAtkEffect`。

“用了 ParticleSystem”本身不是视觉验收条件。只有一个原画整图不动、附加一圈小点，仍达不到本轮意图。主锋本身必须有粒子角运动/位移/大小变化，细屑有独立速度和寿命，边缘侵蚀与残光回收能够改变动作轮廓。Mesh particle 是原版真实使用的合法实现；禁止借此把旧固定月牙控制器搬回来。

## 5. 卡图身份与美术方向

实际查看 `金火璀璨之潮.jpg`、`海银工坊：埃利耶.jpg`、`迸射流光.jpg` 与 `汐音：光耀万海.jpg`。前三者的共同身份是暖白剑锋、厚薄变化的蜂蜜金光流、局部深琥珀体积及少量金叶状飞屑。汐音是低位开放海潮环、上方细弦线与金色浪尖。无需生成新参考图即可确定本轮方向。

建议调色板（是新设计选择，不声称精确采样值）：

| 职责 | 色值 | 约束 |
| --- | --- | --- |
| 锋口 / 接触瞬亮 | `#FFF4C4` | 高亮只占主体一小部分，不能覆盖大半轮廓 |
| 主金潮 | `#EDBE55` | 保持可读实体与笔触宽度，是视觉主体 |
| 金色内层 | `#B77A2E` | 低亮暖体积，增加厚薄，不做橙红火云 |
| 深色负形 / 雾影 | `#51452E` 或偏海灰 `#394345` | 仅约10–20%局部衬托，alpha低；不染黑角色 |
| 游离金屑 | `#FFD983` | 尺寸/亮度分布，不能整齐等大圆点 |

主体使用有方向的断墨/潮丝纹理：破碎主要发生在内缘和尾部，外锋连续可辨。噪声仅侵蚀边缘与局部纹理，不能把主形打成沙子。单一道金潮具备颜色与厚度变化；额外白锋负责刀口，雾负责空气，不得用同轮廓半透明副本堆厚度。不开自动 Bloom、不做全屏闪白、不默认追加震屏或音效。

## 6. 冻结的资产、驱动与坐标合同

Bundle：`steria_christasha_particle_v2`。七个目标prefab，候选经独立接受后进入部署：

| prefab | 职责 |
| --- | --- |
| `ChristashaV2_Slash` | 近战金潮挥舞；旧SlashH/V由同主题参数变体消费，不复制整个实现 |
| `ChristashaV2_Hit` | 前重后轻的短促打击 |
| `ChristashaV2_Pierce` | 原生Z挂点的近突 |
| `ChristashaV2_Projectile` | Fire发射的远突锋头/短拖迹；不负责伤害判定 |
| `ChristashaV2_Cast` | 汐音施法低位潮环与上行细弦 |
| `ChristashaV2_Area` | 一次向敌阵展开的金潮范围前锋 |
| `ChristashaV2_Point` | 成功目标的点位释放与余音 |

纯 Unity 共享源码类：`Steria.Vfx.ChristashaParticleVisual`。

```csharp
public static ChristashaParticleVisual Play(GameObject instance, uint seed, float playbackSpeed = 1f);
public void Advance(float deltaTime);
public float Duration { get; }
public void StopAndClear();
```

- visual 不自行 Update、不销毁根。adapter 或 preview 每帧只调用一次 Advance。内部 Pause + Simulate 推进全部 PS，杜绝 Unity 自动模拟与手动模拟叠加；先清除、设置确定 seed，再播放，首帧行为明确。
- Duration 是考虑 playbackSpeed 后的预期视觉秒数；Advance接收实际帧秒数。所有 PS、shader age与任何辅助曲线使用同一时钟。避免对子树先 `withChildren=true` 再逐子系统重复模拟。
- 输入 instance 已由游戏方选 prefab/父级/position/rotation/scale。visual 不接受 BattleUnit、不翻朝向、不定位目标、不改变碰撞、HP、音效、镜头或伤害事件。
- prefab 仅序列化 Unity 原生组件和原创资产；不把模组 DLL 自定义 MonoBehaviour 序列化进AB。运行时和 preview 都通过同一个 Play 附加共享类。
- 统一 authoring：默认向左 `-X`，`XY` 为战斗平面，`Z`只表示渲染深度。近战挂点完整继承角色镜像一次；pure visual不再次翻转。
- 近战按照真实 `ActionDetail.Slash/Hit/Penetrate` 挂 caster 原生 pivot；不把近战移到target，不按双方斜率改刀口方向。未准备好的 target允许为空。
- 近战倍率沿用原生 2×与后续`SetScale(1+factor)`，保留角色父系倍率；视觉尺寸先在基准皮肤上测，再调本地 recipe，不引入旧的target-distance缩放。
- 远程、范围和点位使用中立 scene root。远程位置来源 self Fire世界点，朝向来自发射者，不继承目标的负缩放。Point使用被结算成功目标的atkEffectRoot世界点。世界位置不得再乘角色倍率。
- 生命周期分开：视觉是可重复的 recipe时钟；后来的 SetDestroyTime 只改存活时钟，不重新拉伸已经开始的粒子。正常完成与取消/目标销毁都清理全部实例。原生基类所需空 SpriteRenderer/Animator可保留但禁用，不承担绘制。
- 材质与纹理共享；改每实例参数使用 MaterialPropertyBlock 或粒子 CustomData，不逐帧访问`Renderer.material`。所有层使用项目Effect对象layer和Particle sorting layer，局部order建议 shadow0/body2/core3/debris4。显式核查透明排序和负缩放；不复用原版 shader。

## 7. 四个首发动作及时间设计

时间为新设计初始默认值，以普通1×播放为准。不要将原版特殊动作的0.2s等待生搬为普通骰子蓄力。近战从攻击事件立即出现清楚前锋，1–2帧形成力度，约0.08–0.16s达到主要形体，随后尾部先退。

| 动作 | 大形 / 动势 | 粒子层级与时序 | 首轮预算参考 |
| --- | --- | --- | --- |
| Slash | 一条开放纵向到斜向金潮弧；高处轻尾、前下方厚刀口；约130–175°开放弧，不闭环 | body为1个或少量Mesh粒子+原创条带mask，前0.14s快速旋过约70–110°后减速；细白锋短于主体、领先而不复制整层；0.03–0.18s从路径发射10–18枚切向金屑；2–4枚低alpha雾粒晚退 | 4–6个有效PS；主体0.35–0.5s、余屑0.65s；峰值高度约角色1.15–1.45倍 |
| Hit | 短横向压力团，前部宽钝、后部数条收束流丝；不出现长斜刃或整圆法阵 | 1个短楔/扇形Mesh粒子向-X突进后急减速；1个白金核心约0.08s；5–8条扇向Stretch碎屑，3–5枚受压雾团；尾部在0.18s后收缩 | 4–6个PS；峰值约0.05–0.1s、整体0.5–0.6s；宽约角色高度0.95–1.4倍 |
| Pierce | 单个明确尖锋与至少3:1长轴，后部薄；两侧稀疏平行金丝；不能像粗激光圆柱 | 主尖锋与束流在0–0.08s快速伸长/推进；主体在0.18s后沿长轴收回；6–12枚光针在0.02–0.12s向后游离，2–3枚雾残迹 | 3–5个PS；主锋0.3–0.4s、余迹0.55s；长约角色高1.2–1.6倍、最厚处小于0.3倍 |
| 汐音 | 施法者脚下开放金潮环与3条细弦上行；随后沿场地向敌方展开一股低位浪锋；成功目标浮现短促金浪点位 | Cast 0–0.32s收拢/上行，约0.3s起Area展开，约0.5s视觉释放并允许FarArea manager结算；收到manager成功列表才放Point；0.5–1.1s余波顺势变细、下沉并退场 | cast4–6/area4–6/point3–4个PS，Area只一份；多目标Point数量受真实列表限制，不叠多个满屏光环 |

这些尺寸是与真实相机中角色高度比较的美术目标，不是直接抄到PS.startSize里的世界尺寸。SlashH/V可通过recipe弧平面比例/起始角/范围参数形成小幅差异；若首候选中横纵变体没有真实独立意义，允许共同使用本轮已接受挥舞姿态，不制造无意义两套资产。不能靠整体旋转把Hit或Pierce假装独立动作。

远突使用Projectile尖锋与短条带，在Fire起点沿发射者方向迅速脱离，方向/跨度由adapter提供；普通攻击的原生受击反馈继续生效。Initialize没有成功受击信息时不得自行在目标上放“确认命中”的Point。远突不能等旧0.34s慢飞后才首次可见，也不为视觉改变原版结算时刻。

群攻状态合同来自主线程精读 `BattleFarAreaPlayManager.cs:440–630`：BeginAction创建FarAreaEffect；ActionPhase等待`isRunning=false`后结算，调用`GiveDamageFromManager(successfulList)`，之后等待`IsDoneEffect`。本实现不得直接调用GiveDamage或造成伤害。必须处理零成功目标、重复回调、复制骰导致的下一轮播放、取消/销毁；每颗真实群攻骰的manager周期恰好释放一次。

## 8. 七类可变 recipe 与稳定制作方式

共享模块按视觉职责命名：`Stroke`（开放弧/楔/针几何和纹理）、`Core`（小面积亮锋）、`Streak`（有向光针）、`Mote`（低频亮屑）、`Mist`（低alpha实体雾）、`Wave`（开放环或横波）、`Chord`（稀疏弦线）。不是每个效果都要全部启用。

| 类型 | 可复现组合 | 主要可变轴 | 必须保留的不变量 |
| --- | --- | --- | --- |
| 挥舞 | Stroke弧 + Core + 切向Streak + 少量Mist | 弧130–175°、角程70–110°、厚度、起手角、尾部破碎比例、seed | 主弧连续可读；有前锋/拖尾；不闭环、不整片长期悬停 |
| 突刺 | Stroke针 + Core + 平行Streak | 长宽比3–6、速度、侧丝数0–3、前锋展宽 | 尖锋唯一，轴向优先；非扇爆 |
| 打击 | Stroke短楔 + 小Core + 扇出Streak/Mist | 扇角45–100°、压缩量、散射数、短反冲 | 钝/重、先压后散；不得与突刺同轮廓 |
| 防御 | Wave开放竖弧 + 中段Core + 反向Streak | 开口120–165°、0.06–0.15s收压、上/下端对称衰减 | 迎击中心稳定，上下重量均衡，不表现下端重斩；挂防御者G |
| 范围 | Wave横向前锋 + 低位Mist + 稀疏Mote | 敌阵宽度、前进方向、传播曲线、波高 | 一份场景范围，不为每目标复制全场；脸与UI仍可见 |
| 特殊 | Chord3–5条 + 小Wave + Mote | 弦相位、波动幅度、间隔、色比 | 事件语义与攻击区分，密度受限，不加满屏装饰 |
| 点位 | 小Core + 5–9枚Streak + 2–4枚Mote | 点半径、扇向、0.15–0.45s寿命、seed | 稳定附着世界点，真实事件一次触发，离开target负缩放 |

变化先动配方参数、色板和原创mask，再决定是否需要新模块。保留命名recipe、seed、版本与sha，禁止调完Unity Inspector却丢失参数。随机变化限定在大小约±10%、角度约±8°、发射数量小范围；接触点、前锋方向、阶段顺序不随机。

纹理生产可由原创程序化mask开始：细长断墨条带、偏心软雾、尖叶/光针、低频流噪；256–512px按功能选，不用超大完整动作PNG掩盖粒子设计。若程序化mask出现机械梯度或重复花纹，补原创手绘/imagegen素材；新素材应单独验收并保留来源。原版贴图仅留reference，不能重新染金后输出。

## 9. 实现分工与文件级建议

纯视觉owner：`SteriaBuild/ChristashaParticleVisual.cs`（上述无游戏依赖API），独立 `SteriaBuild/VFXSource/ChristashaParticleV2/` 的Unity 2019.3.15f1项目、recipe、原创Shader/texture/mesh生成、prefab构建器、真实Unity预览与七类指南。Unity项目引入同一共享driver源码；不要改旧Anhier或Christasha Unity工程。输出七prefab并验证无missing script、粒子类型/材质引用和loop=false。

游戏适配owner：新增 `DiceAttackEffect_Steria_ChristashaParticleV2.cs`、`BehaviourAction_ChristashaParticleMass.cs`、`FarAreaEffect_ChristashaParticleMass.cs`；按已有Harmony注册方式和项目编译项加入；修改精确46处EffectRes与9009006独立ActionScript。旧类/注册/bundle清理由主线程按直接引用结果收口，不让新代码默默回退到旧效果。缺资源需具名日志并安全结束视觉/群攻，不能卡住manager。

预览owner必须使用生产共享driver和新bundle重新加载后的prefab；纯视觉owner可自行验证但不能代替独立预览审查。主线程负责整合、游戏契约验证、最终部署目录和hash比对。

当前主线程已用Unity 2019.3.15f1完成空项目probe exit0，允许真实引擎GPU粒子预览和AB构建。旧指南的历史license故障不构成本轮阻塞；不复制旧bundle当新输出。

## 10. 预览与明确接受 / 拒绝标准

先用1×正常速度短片判断动作，再看阶段帧；两者都必须通过。至少输出Slash/Hit/Pierce的0、17、50、100、180、350、600ms及汐音0、150、320、500、750、1100ms接触表。时间可随候选真实Duration略调，但必须包含早期、峰值、退场。固定同一正交相机，不为了放大效果改变不同动作的镜头。

每种近战至少在相反朝向、基准与一个较小体型各检查一次；只需代表性矩阵，不做泛滥截图。使用实际动作/挂点的代表皮肤或明确标注的中性剪影，画出空间辅助层只供诊断；孤立展示不是挂点验证。另给浅灰底与暗战斗底的合成检查。预览记录seed、driver/source/bundle hash、帧秒、相机与坐标。

接受条件：

1. 主锋可见并真实移动；至少两种不同职责粒子具有不同速度/寿命；正常速度看清Slash的弧扫、Hit的短重击、Pierce的轴向推进，三者不能混淆。
2. 外锋连续、内尾有方向破碎；退场先旧尾、后新锋，不能整个效果等alpha消失。无闭合硬圆、透明矩形、阶梯缝、机械重复短条、单层平面塑料感。
3. 金白与琥珀层次仍可区分；大白区域不吞掉金色体积。没有全屏淡黄色雾、满屏等大圆点、重复透明壳。两个朝向不双翻；pivot位置与武器动作一致。
4. 汐音早期看得见施法者，群攻时金潮有一个明确传播方向，点位在成功目标世界位置；零目标仍完整收尾；多目标不把全场叠成白屏。
5. True ParticleSystem运行证据：逐帧PS粒子数量/首粒位置与生命周期变化、实际GPU渲染、渲染层与材质；只看PNG或代码断言不满足视觉完成。
6. 资源variation保持同一契约：至少固定seed可重放，再用第二seed证明变体只影响细节；guide能从recipe重新生成，不能依赖丢失的手工Inspector状态。
7. 适当边界检查：缺prefab/取消可清理；正常每帧只推进一次；manager只在正确阶段各结算一次。旧资源不再从克丽丝塔夏书页调用。Release DLL和新AB经过构建与部署hash匹配。

拒绝条件包括：仅将旧月牙换色；导出原版素材当成原创；用完整动作PNG加小点冒充粒子重制；靠加亮掩盖轮廓；普通近战延迟到长蓄力后才可见；点位模拟伤害或失败目标星爆；群攻manager卡死；自动与手动模拟叠加；只凭build成功宣布视觉通过。

独立评审必须返回PASS/REVISE/BLOCK并指明帧、问题职责与最小修改；每次返修复用原owner、仅重跑受影响媒体和契约检查。最终指南追加实际接受参数与可运行命令，将本设计的初值明确区分于最终数据。
