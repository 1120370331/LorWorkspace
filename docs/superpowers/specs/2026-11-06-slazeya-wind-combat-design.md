# 斯拉泽雅：青玉风系斩、打、突、防、远程粒子设计

日期：2026-11-06。状态：研究设计完成，主线程已批准 FormedArc、原样 ContractV1 与五类青玉风压方向，进入独立实现阶段；本文不是实现或视觉验收记录。

当前实现以 `SteriaBuild/VFXSource/SlazeyaWindCombat/README.md` 和 `delivery-candidate.json` 为准。用户否决前期碎线/过曝效果后，主线程进入实际 Unity 编辑器调整。当前 Slash 采用固定 SweptWedge（Inspector保留 Alpha=0.814、Span=0.6、Emission=0.7），刀面倾角为 X55°、Z25°；其余四类采用固定 FormedArc。所有角色由同一游戏适配器按真实挂点和原生倍率接入。`candidate-12-final` 已完成原生帧、像素可见性、实际API工厂调用、清理和Release构建检查，仍为待用户美术评价的候选，未部署。下文早期数值和方案保留作设计历史，不覆盖当前交付记录。

## 最新用户修订：强可见性与炫酷风刃

用户随后明确要求不再使用子代理，由主线程亲自完成，并否决前七版过弱的视觉表现：『你这个特效一点都不明显，要炫酷一点』。后续制作与分开的视觉审查均由主线程完成；不将此前技术PASS视为视觉接受。

最新方向覆盖下文过于保守的亮度和细线预算：扩大并加厚主刃，采用青白强亮芯、青玉风体与同一主网格外缘的柔光过渡；用固定ContractV1的加法混合和材质参数实现，不新增后处理或改写shader。Slash增加一条短、错位、错时且独立旋速的回风副刃；主锋约45ms充分展开。细丝加粗并提高切向速度，远程弯刃加大，到达后采用短风压亮闪及高速散丝。五类仍保留不同轮廓，限制在每实例最多六个原生粒子系统内。

优先判据改为：常规战斗尺寸下第一眼能看清、白芯与青玉主体同时可辨、出刀和到达有明确爆发。此前小面积/低亮度初值不再作为限制。禁止用静态整张刀光贴片或同形放大透明壳补亮。

## 1. 意图与完成边界

用户原话：『使用特效制作skills，结合仓库教程，制作：斯拉泽雅的风系斩打突防远程unity 粒子型特效。制作刀光。自行设计配色、位置等信息。』本次交付五类可在实际普通书页路由播放的 Shuriken 效果，刀光为视觉重点。艺术方向是有清晰切锋、负形和空气速度的青玉风压；近战三种攻击、防御和远程各有独立轮廓。

保持玩法、伤害、流系统和既有群攻不变。只迁移 9002001/2/3/4/6/7/8 七张普通书页的 15 个 OceanCurrent EffectRes，以及 CardAbilities.cs 中追加防御骰的一个 EffectRes。9002009 群攻及其天气效果不在本任务范围。工作区已有大量未提交内容，禁止整文件回退、全表替换或重录其它任务基线。

采用 Unity 2019.3.15f1、Built-in、Gamma、D3D11。交付独立原生粒子 prefab AssetBundle 和游戏 DLL 适配；真实 Unity GPU 离屏预览、独立视觉 PASS、精确路由与清理检查后才可称为内部接受。完整战斗镜头、所有皮肤与所有体型仍可明确留作外部实战验收。

## 2. 已读依据与现状诊断

已读 ModGuideDocs/README.md，10 章实际工厂/挂点、11 章 AB/远程路径/连续挥出、14 章粒子职责/单时钟/真实角色/部署、15 章动作和 PRE 事件边界、08 章模拟优先及每次主日志。已读 SteriaBuild/设定/斯拉泽雅.md、SlazeyaAbilities.cs、DiceAttackEffect_Steria_WindSlash.cs、DiceAttackEffect_Steria_OceanCurrentTextureCombat.cs、DiceAttackEffect_Steria_AnhierTextureCombat.cs、CardAbilities.cs 的追加 Guard、CardInfo.xml 的对应七页和 HarmonyPatches.InitCustomEffects。最近提交标题为 3096acd1、f50f2acc、08244343；它们说明当前仓库同时发展其它角色效果，不能把脏工作区视为本任务成果。

已读两个 skill：rng-vfx 的 anime、unity、brightness、motion 参考；unity-sword-slash-vfx 的 library-usage、advanced-authoring、production-standard，并读 canonical FormedArc shader。用户已允许自行创作，按高级定制执行。

生产库实际搜索：

```text
python -X utf8 C:/Users/rog/.codex/skills/unity-sword-slash-vfx/scripts/slash_library.py search "wind teal formed arc slash"
mode=ordinary, entity=preset, status=production
matches=[]
gap="No indexed production entry satisfies the requested filters. Default authoring is forbidden."
nearest_production=[]
```

不存在可安装的 production 近似预设；不能编造 preset ID。新增资产保持 candidate，项目内部通过验收不等于向技能库自动晋级 production。

当前问题与证据：

- WindSlash 是 1 张攻击者纵向 Sprite 加 4 张目标交替斜 Sprite，同步扩张和统一淡出；没有独立粒子、逐段挥出或前后缘速度关系。它不能满足本次粒子风刀，而且当前七页根本不绑定它。旧 Hurricane 的 customEffectRes 会引用它，但当前七页没有该 ActionScript，无须扩大修改。
- 真正路径为 CardInfo 的 OceanCurrent* → HarmonyPatches 精确字典 → 五个 OceanCurrent 类 → AnhierTextureBase → OceanCurrentTextureCombat 的多阶段 RGBA。已实际查看 CurrentFarHit_body_05.png：蓝白水刃与回卷水纹组成完整轮廓，适合水流身份，但风系要求更轻、更锐、更开放的空气动势。不能只给现有整图染绿。
- 14 章明确记录用户否决 ChristashaParticleV2 candidate-r4 美术；该 README 的历史 accepted 文案不能覆盖更新的拒绝。仅复用其单时钟、原生 PS、Emit 后 Simulate(0)、构建/证据机制，不复用规则月牙、金针、固定五系统或其视觉合格结论。
- 已实际查看 Native Slash11 comparison-mid.png：短时强锋、后缘粗细不一、透空破口与异速细迹形成可读层级。它是本次质感参考，不是新角色必须继承的白金颜色或同形副本。未声称观看其连续视频。
- SlazeyaStormMass 的灰蓝雾和冷白闪电仅提供角色家族色背景。普通攻击使用更显著青玉饱和度，与群攻灰蓝大气形成层级；不更改群攻源或 bundle。

## 3. 外部参考及观察边界

1. [Cyanilux Sword Slash Shader Breakdown](https://www.cyanilux.com/tutorials/sword-slash-shader-breakdown/)：已打开正文，确认环状预制 Mesh、Clamp 形状纹理、独立无缝噪声、动画属性及单个 Mesh 粒子可替代普通 Renderer 的方法。借鉴 UV/形状/生命周期分工；其 URP Shader Graph 与 Bloom/ACES 配置不搬入本项目。不下载或复制未核许可证的 FBX/PNG。
2. [Gabriel Aguiar：Made this Cross Slash with the Unity Particle System](https://realtimevfx.com/t/made-this-cross-slash-with-the-unity-particle-system/25569)：已打开作者原帖，确认 Shuriken 交叉斩及后续圆弧变体的展示。其 GIF 的网页抓取不支持格式，因此不声称逐帧量测了作品的 0/+2/+6 帧，不杜撰其颜色/时长。只采用“独立主斩和少量次级形可组成方向层级”的方法参考。

本设计的逐帧分解见第 5 节，是明确的原创目标值，不是假冒参考作品的实测。近距离形体和角色比例优先以本仓库可重放的 Native Slash11、Bada 上下文及本次新预览作判断。

## 4. 统一美术与尺度契约

### 颜色、材质与笔触

| 职责 | 颜色 | alpha / 明度起点 | 作用 |
|---|---|---|---|
| 窄切锋 | #E6FFF2 | 0.88–1.0；仅窄带可短时超过1 | 青白最亮线，峰值连续，不覆盖整片 |
| 主体青玉 | #59D5B3 | 0.55–0.78 | 清晰风系身份，中段保持饱和度 |
| 内缘深青 | #197B79 | 0.24–0.40 | 有限局部色差与断笔，不形成厚黑边 |
| 退场浅雾 | #8CBFB8 | 0.08–0.16 | 极少量空气余势，不能遮脸或盖满刀光 |
| 游离细丝 | #B7F5D8 | 0.45–0.75 | 随切向飞离，长度与寿命错开 |

主体一个连续 Mesh 粒子、一个 FormedArc 材质，颜色与窄锋由同一材质分带处理。初始采用 SrcAlpha / OneMinusSrcAlpha、Cull Off、ZWrite Off，避免全体加法叠白。窄锋占可见宽度约 12–20%；最亮覆盖少于主体可见面积 15%。预览先关闭 Bloom，确认形体；不新增后处理或 GrabPass。若目标场景有 Bloom，应实测阈值 B，再让大体保持约 0.6–0.9B、窄锋约 1.1–1.6B，不拿这些比值伪称当前场景已测得 B。

局部 brush 是沿带铺开的断笔覆盖信息，不能包含整张月牙、角色或完整攻击构图。需要新作 raster brush 时，实现者使用 imagegen 技能和工具，不以 Python 伪造绘画；也可复用许可证和来源明确的现有局部 mask。外缘主锋连续，内侧宽体有 3–5 个尺度不等的向后裂隙，其中至少两处在峰值形成真正透空负形。裂隙不能横向等间距排成木纹；尾端逐步裂为 2–3 根不等长细丝。主锋最厚处偏向运动前 2/3，尾端较细，不做均匀香蕉片。速度纹理沿 pathT，噪声固定 seed 连续移动，不能逐帧随机。主线程已亲自对照 Native Slash11 与被拒旧V2，明确拒绝仅把规整月牙换青色的方案。

### 角色、朝向与空间

主线程已确认斯拉泽雅实际皮肤为 Bada。只读参考缓存：VFXSource/AnhierTextureCombat/UnityProject/Assets/Reference/native-skins.json、bada-native-effect-pivots.json、Bada-*.png。Bada 的 atkEffectRoot 局部位置为 (-2.2, 2.34, 0)，H/J/Z/G/F 专属 pivot 引用为空，应让实际原生 API 执行 fallback，不根据不存在的专属点自行伪造。Fire 姿势缺失；远程预览用 Bada 现有近似姿势并明确标注。

下表 H 是按 Bada 实际显示身体（脚到头顶）的高度，不是 PNG 画布高度；尺寸是最终世界呈现目标，必须在完整父系后量测。XY 为战斗平面，原型默认向 -X，适配器只处理一次左右镜像。近战沿真实动作挂点，不为了追目标任意拉长。防御用自身 G fallback。远程使用 Fire 世界点到目标 atkEffectRoot 的实际向量，在中立 scene root 下运动，不继承角色镜像/地图缩放第二次。

| 类型 | 最终主要包围范围 | 位置/朝向 |
|---|---|---|
| Slash | 高 1.10–1.30H，横向 0.80–1.05H，最大笔宽 0.13–0.19H | 主弧中心在胸腹前约0.35H，末锋落向前下方；最低处不钻地 |
| Hit | 长 0.65–0.85H，高 0.38–0.52H | 从自身发力点向前下压约18°，峰值重心在腰前；短宽钝压 |
| Pierce | 长 1.05–1.35H，最宽0.12–0.20H，后方聚束域高0.3H | 前向单尖，针芯和聚束共轴，不穿过自身脸部 |
| Guard | 高0.85–1.05H，宽0.28–0.40H | 身前约0.35H，上下对称稳定迎击；重心不向脚下偏 |
| Projectile | 飞行形高0.42–0.58H，长0.55–0.75H | 锐锋面向飞行方向；尾丝只在后方，命中后不得穿过目标 |

普通 DiceAttackEffect 的基类倍率、Guard 半倍率、SetScale(roll) 必须由适配器按真实合同只施加一次。不能照抄 Native 原型 no-op SetScale 的例外。H 比例验收优先于任意初始数值。

## 5. 五类轮廓与 60fps 时间线

所有秒数使用唯一游戏视觉时钟；帧表只是 60fps 表达。近战 t=0 是实际攻击特效事件，不能在视觉代码额外结算命中或修改全局 timeScale。首次方向性亮锋在 1 帧内可见，所有主形先完整揭示，再开始后退。

| 类型 | 0帧 / 2帧 / 6帧 / 峰后与结束 | 支持层及预算 |
|---|---|---|
| Slash | f0 只见起刀细锋；f2 展开约45%，有明显运动前端；f5 完整峰；f6 主锋已扫过约55–70°，主体仍清晰；f8–19 从起刀端撤去；f24主体清空，f34全部结束(0.57s) | 1主弧，最多1条错位细回风（不能同形放大壳）；10–14切向细丝，4–6短碎屑，最多3雾粒。峰值≤28粒 |
| Hit | f0 收束窄口；f2 短宽风楔压到前方；f3–5 最大厚度；f6 已开始前端破裂而非继续伸长；f15主体消失，f28结束(0.47s) | 1短宽主体，1个小面积接触压纹；8–10扇形短丝、最多2雾粒。峰值≤18粒 |
| Pierce | f0 后部细线聚束；f2 单尖到位；f3–5 保持清晰长轴；f6 两侧反冲丝出现，主刺开始尾→尖撤去；f16主体清空，f29结束(0.48s) | 1主刺；2条非等距后方聚束细弧；8–10轴向细屑、最多2雾粒。峰值≤18粒 |
| Guard | f0 身前小接触线；f2 上下展开；f4 达约145°迎击弧；f6 中段亮、上下端稳定；f8–20两端向中心收细，f30结束(0.50s) | 1弧主体；上下成对共6–8反弹短丝，最多2雾粒。峰值≤14粒 |
| Projectile | 发射f0–2从小锋形成压缩弯刃；飞行中主形不膨胀，尾部2–3根细丝间歇生灭；标准飞行0.26s，约f16到目标；到达后f0–2接触压闪，+6主体已明显破开，+16残丝结束 | 飞行1主弯刃+最多2短尾迹+8游离粒；抵达后复用缩小Hit式压裂（独立点位职责，不是第六类公共攻击）。同刻峰值≤22粒 |

Slash 固定模式 **FormedArc**：完整弧约155°只是外包络起点，不能画成均匀圆弧。椭圆/轻微非同心中心线，宽度从尾端0经中前段增厚再向锋尖收尽；局部 brush 产生不规则内缘。f0–5 `_Reveal` 从0至1（快出缓收），f8后 `_Retreat` 从0至1；两者都沿 UV.x 尾0→锋1。不能在 Reveal 完成前就撤空主体。回风延迟3帧，弧长不到主弧45%，亮度约主体35%，有独立位置/转速/寿命；先只做主弧，通过白模和单材质后才决定是否需要回风。

Hit 的短宽钝压来自紧凑非对称楔形及前端碎裂，不是把 Slash 旋转90°。本角色没有要求 Christasha 的重剑触地语义，故不引入地裂/拳星爆。Pierce 必须同时有唯一锐头、轻薄轴向主体和后方聚束，拒绝纯实心长绿针。Guard 曲率可用约145°开放弧，但中段承压最亮，两端成对回应；不套 Slash 的角向扫动。Projectile 是飞行弯刃，既不拉成常驻光束，也不沿用水纹完整贴片。

远程适配器负责真实路径：保存发射世界点，目标存活时跟踪其当前 atkEffectRoot 世界点；以固定 travelDuration=0.26s 的连续 ease-out 插值抵达。近距离路径仍不反向、不越过目标；到达帧先设置精确目标坐标再清掉飞行系统。攻击事件本身不保证伤害成功，因此只在可靠成功信号存在时追加确认性命中点；无法证明成功时允许普适接触压纹，不播放伤害专属爆点。若现有实际动画事件/结算合同要求其它飞行时长，由主线程冻结接入时标并记录偏离，不改玩法来迁就表格。

## 6. 实现合同与文件建议

独立文件建议（名称可由主线程冻结，职责不可混合）：

- `SteriaBuild/SlazeyaWindParticleVisual.cs`：普通C#驱动，提供 `Play(GameObject instance,uint seed,float playbackSpeed=1)`、`Advance(float dt)`、`StopAndClear()`、`Duration`、`IsComplete`。驱动只管理配方/粒子/材质时间，不管理伤害、根坐标、相机、音效。固定1/120s子步，关闭自动emission/Play，Emit批后Simulate(0,false,false,false)并Pause。Play可重放，Stop幂等，不增children。
- `SteriaBuild/SlazeyaWindParticleAssets.cs`：独立bundle缓存/根查找/实例化/资源缺失错误路径；不得修改其它角色缓存和资源。
- `SteriaBuild/DiceAttackEffect_Steria_SlazeyaWind.cs`：五个游戏入口、原生挂点/镜像/地图倍率/生命周期/远程路径；缺失资源可诊断且及时退出。成功信号接线若没有可靠现成合同，先不上伤害专属点位。
- `SteriaBuild/VFXSource/SlazeyaWindCombat/UnityProject/Assets/Editor/SlazeyaWindParticleBuilder.cs`：原创主形、局部brush、材质、五个纯原生prefab、AB构建和GPU预览；正常prefab仅Transform/ParticleSystem/ParticleSystemRenderer，不在AB中序列化业务MonoBehaviour。
- `.../Assets/SkillSwordSlash/ContractV1/`：从skill canonical包按manifest/hash检查后安装固定基础设施。使用 `SwordSlashFlowBuiltInContractV1.shader` 原样，不改成特效专用 `_Slash` 变体。其现有 COLOR 和 UV0 输入能接收Mesh粒子，MPB可直接驱动全属性。选择性使用 Shape/Mask/Highlight，Flow/Distort以零强度起步，保留完整契约。
- `.../build_preview.ps1`、`verify_source.py`、`README.md`：各自只负责构建入口、关键源契约检查、复现与证据边界。预览目录 `evidence/candidate-01`，返修递增不覆盖。
- `SteriaBuild/Verification/SlazeyaWindCombat/`：精确十五项绑定和动态Guard检查、五入口真实工厂/缩放/取消/远程到达合同。避免通过放宽旧整表校验掩盖无关字段变化。
- 集成面由专属owner修改 `HarmonyPatches.cs` 五个精确新资源键、`CardInfo.xml` 十五项、`CardAbilities.cs` 的动态Guard与项目编译项。建议新键 `Steria_SlazeyaWindSlash/Hit/Pierce/Guard/FarHit`，新bundle `steria_slazeya_wind_combat`，prefab `SlazeyaWind_Slash/Hit/Pierce/Guard/Projectile`。不改旧OceanCurrent类以免影响其它调用者。

最多两个主轮廓Renderer，辅助系统按视觉职责增加，不强制五系统模板。每主Mesh≤256顶点，整套候选峰值粒子预算见上表，单实例建议≤6材质draw。素材建议局部brush512×128、辅助丝/碎粒128×64，数据纹理linear、Clamp；周期数据如果启用必须Repeat且通过1/2/4倍与完整周期接缝检查。只用原创素材和明确来源的skill基础设施，保存来源、许可证、meta和hash，不发布本地Bada/游戏参考资源。

## 7. 构建、预览与接受条件

本机由主线程确认 Unity=`C:/Program Files/Unity/Editor/Unity.exe`，版本2019.3.15f1。启动沿用 Hidden、D3D11、`OPENSSL_ia32cap=:~0x20000000`、子进程继承 affinity4294901760、`-job-worker-count 4`，不能同时打开同一项目，不用 `-nographics`。沿用V2构建脚本的fresh log/PID/exit/source hash检查思想，独立命令建议：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File SteriaBuild/VFXSource/SlazeyaWindCombat/build_preview.ps1 -Revision candidate-01 -AffinityMask 4294901760
```

这是待实现入口，不声称目前已存在或运行。每次候选一份 `acceptance.log`，附源码/资产/meta/AB SHA、frame时间及粒子数。初次先建Slash白模、单材质早/中/峰/退预览接受大形，再完成同风格五类；最终从打出的AB重新加载取样，不能只看编辑器未打包资产。

独立审查必须实际查看五类正常速度媒体或明确注明只看了逐帧图；附代表帧 early=f1–2、mid=f3、peak=f5–6、retreat=f12–20、end，远程再附flight25/50/75/100%及到达后一帧。背景为暗灰、中灰和实际Bada角色上下文；左右各一组固定相机，至少一个改变距离的远程场景。Fire近似姿势、合成敌方距离、未复现游戏后处理须写在媒体或报告中。

PASS要求：

1. 单帧可区分扫斩、短压、细刺、迎挡、飞行；Slash的主锋、内缘真负形和尾丝是第一视觉信息；无均匀香蕉片、塑料厚板、整块水纹贴图、等距木纹或同步透明双壳。
2. f1有方向线索，f5附近完整峰可见，先出现的尾部先退；白芯不吞青玉体，浅背景能读到形体，深背景不成白团；辅助粒子不能掩盖主刃。
3. Bada脸、持握、脚点可识别；不把高度误按512画布或参考人形估算；双向镜像均前向，Guard不变落斩；尺寸不重复乘父尺度。
4. 远程从发射者真实世界点到目标世界点，抵达即停止飞行，无穿透/回弹/镜像二次翻；未命中或取消不产生成功专属反馈。
5. 五类均有真实PS运动，不是单Sprite换皮；固定seed重播一致，不逐帧随机、不首帧巨雾、不增children；关闭、销毁、取消后粒子清零且无残留Renderer。
6. 真工厂五键能创建并Initialize/SetScale；十五项XML及动态Guard精确接入；群攻及非视觉字段保持原候选状态。源验证、Release DLL和AB load检查与候选hash一致。

REVISE时按具体类型/时间/画面缺陷返回原实现owner，只重建受影响候选；源码编译或Shader supported不能替代视觉接受。最后由主线程审查最终候选并执行相关源检查、项目build、AB build/load及部署一致性检查；若任务包含部署，只部署接受候选的确切文件并核对目标hash，不将整个脏工作区打包。

## 8. 已知风险与批准点

- 固定FormedArc shader虽声明Unity2019兼容，仍需本机目标版本import/PS Mesh/AB实际GPU验证。完整属性保持，不能为了起效临时改成另一套shader契约。
- Bada F动作缺失，Fire挂点fallback已知，但当前导出参考不能单独证明真实游戏Fire姿态；预览如实标近似。近战倍率必须按适配器实际基类而非Native no-op示例决定。
- 远程工厂创建不等于成功命中回调，确认性点位必须有可靠事件；否则保留只表达接触的反馈。
- 旧V2文档美术接受结论过期；不能将其技术PASS或本设计数值当美术PASS。
- 旧全XML锁与其它未提交更改可能导致既有检查失败，需精确归因；不得为通过检查更新全表基线。

主线程批准本设计后，实施owner按本文冻结轮廓、FormedArc材料合同、时间和接入责任；技术阻碍可请求具体偏离，不能自行重设计为整图、规则环或大量透明层。
