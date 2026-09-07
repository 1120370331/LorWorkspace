# 倾覆万千之流：爆发增量与一次冲击抖动

2026-09-07；主线程已批准并冻结全部爆发参数表、粒子池、峰值预算≤640及 `ShakeCam(65f,.022f,.016f)` 合同。基线 `383b9f535a2a15e5344cab3de23ff95a5854a7a2`。本阶段只新增本文，未改生产、生成资产、运行构建或宣称新候选视觉 PASS。

## 意图与冻结边界

用户：「倾覆万千的爆发时扩撒的特效量可以增加，并加入一份镜头抖动」。爆发前沿推进期间增加分层水沫、飞滴，B+.12～+.45 的可读水沫量以原版约 1.5～2 倍为首候选审美目标；数量和面积不是视觉通过的替代证据。

保持内移云团及近核压实、中心起源外扩、全目标包络、Gather=1.35s、Tail=1.20s、真实伤害回调、两次原闪电及暗间隔。保留 `383b9f5` 的新 SFX、音量/暂停/去重/清理逻辑。Velia 整个模块、玩法数值、卡牌/XML 和并行脏文件均不在实施写面。用户此次明确要求抖动，覆盖旧设计中的禁止相机变化；不恢复旧卡牌重复特效。

## 已核对依据与诊断

已读根 AGENTS（根目录无 README）、ModGuideDocs/README、教程 11 的分层/运行时/透明叠加/容量/验证段、教程 10 的特效生命周期及 AB 段，以及 [云吸入设计](2026-09-06-slazeya-cloud-inflow-design.md)、[云吸入交付](../plans/2026-09-06-slazeya-cloud-inflow-delivery.md)、[云交付](../plans/2026-09-06-slazeya-cloud-delivery.md)、[新音效交付](../plans/2026-09-07-storm-dawn-audio-delivery.md)。核对当前 Controller、Builder、Flow/Particles shader、source verifier、直接回调调用点及最近相关提交。Serena C# 服务缺 .NET 10，按仓库规则回退为精确检索和源码阅读。

视觉依据为 `preview_exports/slazeya_storm_mass/round5/review-918b3213` 的 manifest/freeze-manifest/candidate-sha256、timeline 和原尺寸图片。Bundle SHA256=`918B3213A65BA542FAEC7F83CE6926A57CE464A81A8990DD65272B6E3AACB8C5`；当前 Controller/Unity 镜像 SHA256 均为 `3DA8605425D84085F1F62704A5BC69BC01629941C188813E17D4C0961EF025C9`，Flow/Builder 也与该清单一致。此目录旧音轨不作为新 SFX 基线；新声音仍取 `383b9f5`。

- `EmitSector` 14 个扇区实际仅 7 张 BurstSheets、90 颗初始飞滴。B0 保留一个核内扇区，其余在 B=.02～.045 出生；`Propagation=Ease(B/.23)` 此时约 .02～.10（60Hz 实际发射约 .057～.121），出生尺寸和动量随后不再跟随波前增长。故可见的是核边细碎点线，单增 `maxParticles` 无效。
- `EmitWaterfallStreaks` 只有 B=.06/.15/.24 三批，每批 6+6+6+3=21 条，共 63 条，另带 63 颗卫星飞滴。强水舌在 B≈.23 已很宽，但水舌前后缺少连续补出的浪沫流。
- `ExpandedRingPoint` 的四浪峰提供正确主形；Flow 的 `bridges`、`tongues`、`brokenFlow` 与 survival 主动开缝/退场。B+.25 主要是四片主水舌，B+.45 已迅速变成稀薄碎片；加全局 alpha、统一放大或叠一张水壳会抹掉现有透明沟槽和角色间隙。
- 已看 `battlefield_peak60/frame_004.png`（B+.05）、`battlefield_04d_flash_B7.png`（+.116667）、`battlefield_sequence/frame_0054.png`（+.25）、`battlefield_07_falling_fragments.png`（+.45）、sequence `0070/0071`（+.783333/+.816667）及持核图。+.8 邻帧只有低雾，必须保持这种退场层级。原 timeline 全局粒子峰值 415，不能把其中云 parcels 当成可见水沫量。

当前已有适用的单水面、显式有限批次、稳定随机和原生粒子寿命 atlas，无需外部资产/新渲染技术；本轮不另做外部研究或生成贴图。

## 首候选参数合同

以下 B 为实际接受回调后的秒数，H 为现有参考身高，P 为原 `WaveProgress(B)`，s 为原 `RingLobes(theta)`，U 为现有稳定 Hash 的 [0,1] 值。保持根 scale=1、足点/身体包络和所有出生点使用当时的 `RingPoint`；不得把新增粒子直接铺到终点或每个受击者身上。

| 层 / 精确入口 | 现值 → 首候选 | 目的与不可越界项 |
| --- | --- | --- |
| 主水面：Controller 与 Flow 的 `ExpandedRingPoint` | width `.10+.39*s+.02*sin(theta*11)` → `.11+.45*s+.02*sin(theta*11)`；crest `.18*s` → `.20*s` | 只增加水舌径向厚度/翻卷，保留四峰中心、角宽、side 抑制、落地/下落和 .23s 外扩。两份公式同步；不改 Footprint，不复制 RingJet。 |
| 主水面：Flow 的 ring survival | `1-ease((B-.22-.12*s)/.26)` → `1-ease((B-.24-.12*s)/.28)` | 强峰最晚约 .64s 自然归零，renderer 的 .72s 关闭保持；alpha 基值、tongues 阈值、bridges、破口、冷白泡沫/钢蓝水体、暗槽和闪电照明不动。 |
| 初始喷出：`TriggerBurst` / `EmitSector` | 现有 B0、14 扇区首次释放、7 sheets/90 drops、寿命、尺寸、种子全部保留 | B0 必须仍是原有限核心；第一批是冲破核心的细飞沫，不靠放大核心补量。 |
| 补喷：`Advance` → `EmitSector` / `EmitSpray` 的新有限第二批 | 每扇区仅一次，B=`.07+.025*Hash(sector,41)`；s>.12 各 1 sheet（合计 10），各扇区 `2+RoundToInt(5*s)` drops（合计 56） | 全部 14 扇区有细滴，主峰和肩部有薄沫，原四个低谷不填成水墙；新增独立 bool[14]，不复用第一次锁。 |
| 第二批薄沫：BurstSheets | 寿命 `.32+.12*U`；size=`H*Lerp(.025,.42+.24*U,P)`，其中 s≤.25 的肩部再乘 .70；`q=.28+.45*U` | 在已有波前长大时补出可读薄膜，主片高低错开；保持原速度/重力/速度衰减/alpha/atlas。新的 seed 区间与首批隔离，采用现有旋转随机，不同片不得同位复制。 |
| 第二批细滴：BurstDroplets | 寿命 `.38+.16*U`；size=`H*Lerp(.025,.035+.055*U,P)`；扇区角随机仍 ±.13rad | 增数量、降低新批尾寿命，保留各扇区椭圆法线外喷与切向偏移。原 .50～.82s 长寿首批不加长。 |
| 浪沫流：`Advance` / `EmitWaterfallStreaks` | 三批 .06/.15/.24 → 四批 .06/.12/.18/.24；每批 6/6/6/3 → 8/8/8/4，总 63 → 112 | 通过推进中四次补料形成深浅层次；保留三个强峰和一个弱峰，不加系统或新 mesh。`_waterfallReleased` 长度随批次数变为 4。 |
| 浪沫流：空间/寿命/宽度 | q `.28+.43*U` → `.22+.55*U`；寿命 `.28+.10*U` → `.32+.10*U`；size `H*(.075+.07*U)*P` → `H*(.080+.075*U)*P`；Builder lengthScale `3.1` → `3.35`，velocityScale `.055` 不变 | 保持重力 2.8 和原法线/切线/向下动量。四中心 `[2.25,3.72,5.10,.70]` 不动；角度 span 改为 `[.72,.54,.54,.44]`，使用 `center+((j+U)/N-.5)*span` 分层随机，避免单束重合。 |
| 浪沫卫星滴：`EmitWaterfallStreaks` | 每条仍带 1 颗，总 63 → 112；保持 size=`H*(.020+.025*U)*P`、寿命 `.30+.12*U` 和该条位置/动量 | 浪沫边缘有细碎分离，不让雾团代替水滴；seed 改为例如 `batch*64+lobe*12+j`，覆盖新增计数而不跨批重号。 |
| 空气/晚雾：`EmitMist`、相关 Builder 材质 | CrestLiftMist 仍 .04:12、.11:8；ReturnMist 仍 .36:12、.50:10（按现有落地检查可能少发）；大小/寿命/alpha 不变 | 只托底。保留低雾 authored A 一次，禁止 double fade；不靠加雾凑“更大量”。 |

第二批 seed 建议独立基址 10000、扇区 stride≥32、sheet/drop 独立 salt；不改变原随机流。第二批 emission age 上限 .16，新增瀑布四批上限 .30；超窗只标记已消费，不在低帧率大步推进后补放陈旧喷发。维持单一 `Simulate(deltaTime,false,false,false)` 路径，所有系统 emission.enabled=false、rateOverTime 不启用。这是明确的批次频度调整，不是持续喷泉。

上述参数为一个有界候选。若原生图仍稀疏，先检查第二批尺寸/出生时点和 atlas 生命周期，不再全面乘系数；若出现遮挡，先减第二批肩部片和 streak 宽度，保留分批数量/向外动势。任何超出表中写面的重新设计返回主线程。

## 阶段包络与预算

| B 范围 | 应看到的主次关系 |
| --- | --- |
| 0～.05 | 原核心破开成小前沿，细滴先出；唯一镜头冲击开始。新主水舌略厚，不能在远端突然出生。 |
| .05～.15 | 第二批薄沫跟随推进中的波前，浪沫先后补入；保留闪电暗间隔，不能用闪白遮住增量。 |
| .15～.30 | 一层主水舌承重，多条粗细浪沫在前后沿外落，细滴越出边缘；全体目标仍被原外扩包络涵盖，低谷和人物间留空气。 |
| .30～.65 | 停止水沫出生；主水舌开裂下落，浪沫流/滴短暂续接，地面回雾承接。+.45 明显比基线充足，但弱于峰值。 |
| .65～1.20 | 仅少量原长寿细滴及低雾；+.8 不剩宽白片或喷泉，1.20 强制清空所有本实例视觉。 |

保持 8 个 ParticleSystem、一个 RingJet、原 mesh 拓扑/材质数、原云 proxy 和采样预算。仅 Builder `BurstDroplets.maxParticles` 180→320，`WaterfallStreaks.maxParticles` 72→128；BurstSheets 48、其余 40/64/32/24/168 不变，总容量 628→824。容量增加不得顺带改原系统随机 seed：`MakeParticles` 当前 `ps.randomSeed=capacity+1945` 与容量耦合，应对改容量的两系统显式保留旧值 2125/2017。

理想完整时步的发射上界：sheets=17，drops=90+56+112=258，streaks=112，合计 387（旧 223，约 1.74 倍）；这不是屏幕像素/可见面积测量。加未退出的 168 云 parcels、20 lift、最多22 return，保守总量≤597，首候选原生峰值门设≤640；每系统不触顶截断。沿用尾末 `Clear`、幂等 Dispose、取消与缺资源退出；无新增常驻对象、循环 emission、全局后处理或云积分成本。截图读回耗时只作相同环境对照，不充当游戏 FPS。

## 一次镜头冲击：采用主线程已确认的游戏合同

主线程已查 `Assembly-CSharp/BattleCamManager.cs:323–337,1205–1233`：`ShakeCam(float speed=50,float xAmount=.015,float yAmount=.015)` 使用既有 character camera 的 EarthQuake，保留更强的在途震动；`VibeCam` 运行固定 .25 个 scaled seconds 后恢复 Speed/X/Y 并关闭。不采用会新增 EarthQuake+AutoScriptDestruct 且无实例清理句柄的 `SteriaEffectHelper.AddScreenShake`。

首候选调用一次 `ShakeCam(65f,.022f,.016f)`：较默认速度约1.3倍、X约1.47倍、Y约1.07倍，短横向冲击、轻纵向扰动；这只是 API 参数比，不解释为世界单位或像素。时长遵从游戏固定 .25s，不新增自定义衰减、Transform/FOV 动画或第二次抖动。更强的现有震动被保留时不得排队再震一次。

主线程已冻结最小接入合同：在 `FarAreaEffect_Steria_OceanWave.GiveDamageFromManager` 复用现有 `_burstTriggered` guard，每次真实爆发至多一次 API 请求，不新增第二套去重门。Init 缓存“有有效 recipients”的 bool，独立于 AudioRoot、音频创建和 prefab 可用性；缓存为 false 就不请求抖动。空 `damagedUnitList` 可表示全部防御，不能因此跳过有目标的真实爆发。请求前按现有 Update 取消条件排除陈旧/已结束的 owner、card、manager；可抽取条件完全相同的共用 Cancelled helper，不扩展玩法条件。缺 BattleCamManager 则平稳跳过，原视觉/SFX/结算语义保持。

不新增 camerafilter，不改公共 helper 或全局相机 pose。更强震动重叠、暂停和恢复均交由游戏处理；已发出的原生震动即使源技能随后取消，也允许游戏完成其有界 .25 scaled seconds，禁止为清理本技能而关闭其他游戏滤镜。取消守卫负责阻止新的陈旧请求，不追求撤回已发出的游戏自管震动。

独立 Unity 预览缺专有 `CameraFilterPack/FX_EarthQuake` shader。除非主线程取得并验证真实游戏 shader，预览只能标注“确定性 .25s 镜头近似，非游戏 EarthQuake 像素证据”；仅预览使用的镜头近似由视觉 worker 实现。水量 A/B 使用真实原生粒子的固定镜头预览；另以相同模拟帧/时间制作单独镜头近似版，不靠摇镜强化水量差异。主线程拥有原生验收夹具与最终集成，负责 spy 真实 API 参数、一次性、无目标和陈旧回调，并做实际游戏 API 编译。上述证据与近似预览分别报告，不能把替身调用 PASS 当成真实 shader 已看过；游戏自管恢复不是本次自定义清理实现或额外滤镜验收要求。

## 实施写面与原生 PASS 条件

视觉 worker 拥有全部爆发实现：`SteriaBuild/SlazeyaStormVisualController.cs` 及 `SteriaBuild/VFXSource/SlazeyaStormMass/UnityProject/Assets/Scripts/SlazeyaStormVisualController.cs` 同步；同 UnityProject 下 `Assets/Editor/SlazeyaStormMassBundleBuilder.cs` 与 `Assets/Shaders/SlazeyaStormFlow.shader`；直接消费者 `SteriaBuild/VFXSource/SlazeyaStormMass/verify_slazeya_storm_source.ps1`，以及仅预览使用的镜头近似。后续生成 prefab/AB 由独立实施阶段产生。本研究不写这些文件。Particles shader、六张原贴图、Cloud 系列 shader/include/noise、音频控制器/WAV/生成器保持原样。主线程编排镜头工作，将 OceanWave host（`SteriaBuild/FarAreaEffect_Steria_OceanWave.cs`）的游戏镜头接入交给独立 camera worker；主线程不实现应用源码，拥有原生验收夹具、最终集成及验收/部署。

source verifier 的旧 `AddScreenShake` 禁令只约束 `CardAbilities` 中旧重复路径，应保留，不应为了新增游戏 API 而删掉该检查。只增改本次参数/CPU-GPU一致性/有限第二批/容量相关验证；原云、包络、音效及回调门不得弱化。

预览继续从新 AB LoadFromFile → 无脚本 prefab → canonical Controller → Unity2019.3.15f1/D3D11 原生渲染。与 round5 同 H=6、1280×720、正交镜头位置/朝向/尺寸13.8、五目标布局、显式时钟和真实预览回调 B≈1.55；将候选放入新目录并记录源码/Bundle SHA，不覆盖 accepted round5。主线程固定采样策略：比较请求 B+.05/.12/.25/.45/.8；60Hz 的 .12 对照实际 +.116667 要明确标记，若补出精确 .12，基线与候选采用相同余步策略。旧 .8 只有邻帧，不伪称精确样本；新输出补齐 .8。

1. **+.05 起势**：核内→小前沿连续，原 B0 有限支持仍通过；量增加不导致整幅大水圈/全屏白闪提前出现。
2. **+.12 分层**：固定镜头已可区分主水舌、第二批薄沫和细滴；三个强峰/一个弱峰可读，前后层错开而非贴片重影，原暗间隔保留。
3. **+.25 峰值**：相对基线显著充足，主水面/浪沫/卫星滴三层共同达到约1.5～2倍的视觉量感；至少两个清楚的峰间透空区，五个人物轮廓与头部仍易认。不能靠主轮廓统一变大、alpha 变白、低雾增量或透明壳堆叠通过。
4. **+.45 下落**：可追踪上一阶段的水沫向外/下落，明显比旧稀薄尾部充足，同时面积/亮度弱于+.25；没有新出生的中心爆点、整圈宽墙、贴片长方边、重复平行膜或规则梳齿。
5. **+.8 退场**：宽水片和长浪沫已消失，仅低雾/少量细滴；B=1.20 时粒子零、renderer 全关。循环第二次不继承 release flags、粒子或相机请求。
6. **保持项**：抽查原起吸/补入/持核图及现有权威云/包络/CPU-GPU波前验证；相反阵营/稀疏或高大目标沿用现有布局验收，不按审美删掉远端目标。主侧或分层隔离图仅在固定镜头无法判断遮挡/重叠时补充。
7. **抖动**：spy 确认首次有效实际回调只请求一次 `ShakeCam(65f,.022f,.016f)`；Gather、重复回调、失效/取消 owner/card/manager、Init 无有效 recipients、缺 BattleCamManager 均不新增请求。全部防御的空成功列表仍请求一次；缺音频/prefab 不改变 Init 的目标缓存。取消后既有游戏震动可自行完成 .25 scaled seconds，不清其他滤镜。真实 API 编译/dispatch 结果与近似镜头预览分别标注。

按已批准冻结的合同进入独立实施、原生预览和独立视觉复审。以上均为待验证接受条件，非本设计阶段已完成的测试。运行时测试、真实游戏观察、最终构建及部署/hash 对齐由主线程保持独立合同。
