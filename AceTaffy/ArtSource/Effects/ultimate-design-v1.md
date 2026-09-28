# 1883式·蒸汽爆轰：首骰专属视觉设计 v1

状态：研究与设计完成，按主线程收敛意见冻结实现合同。日期：2026-09-28。本文没有宣称新效果已实现、已实机观察或已通过视觉验收。旧候选由主线程保存在 `Verification/Vfx/ultimate-v1/baseline/`，新媒体不得覆盖它。

## 意图与边界

用户要求：“给4费大招来一个更明显的特效专属”。让卡 1883009 的首枚打击在正常速度下直接被认作一次高压爆轰，区别于普通 SteamBlunt。以独有轮廓、可读的前锋和短促压波建立辨识度，避免靠堆白雾遮住双方角色。

仅更新首骰 `AceTaffy_SteamDetonation` 的原创素材与专有表现。第2骰 SteamGuard、第3骰 SteamBlunt／SteamFollowThrough、费用、点数、威力、破坏骰子、机制、人物、卡图、音效和其它低费效果均沿用当前候选。保留现有文案修改，不还原历史 XML。声音继续由原版动作路径播放。

## 已读取与观察的证据

先阅读根 AGENTS、AceTaffy README、ModGuideDocs 第13、14、15章及第08章模拟验收章节；随后读取当前生产代码、书页、旧设计／提示词／v0.2审查、Unity真工厂夹具及准备与运行脚本、原版 DiceAttackEffect 源码。查看最近6条提交，其中 `08244343` 的预热方向与当前缓存策略一致；没有发现 AceTaffy 的既有提交记录可用作本轮差异基线。

实际查看下列图像：

- `Verification/Vfx/unity-preview/AceTaffy_SteamDetonation_event.png`。
- `Verification/Vfx/unity-preview/AceTaffy_SteamBlunt_event.png`。
- 包内 `Resource/Effects/AceTaffy/SteamDetonation.png` 与 `SteamBlunt.png`。
- `ArtSource/Cards/AceTaffy_SteamDetonation.png`。卡图表达从腕炮集中喷出的定向高压蒸汽，适合作为轴向压缩与展开的主题依据，不能照搬其全画面烟量。

具体源证据：

| 位置 | 事实与设计影响 |
| --- | --- |
| `src/TaffyVfx.cs:124`、`:294` | 当前爆轰已使用独立母图、PPU 790、BodyScale 1.25、前偏移 -2；不是仅把普通母图放大1.25倍。旧预览里其宽度已明显超过普通打击。 |
| `src/TaffyVfx.cs:241` | 主体始终是同一张图，只在0.25秒内整体扩大15%，0.16秒后整体淡出；8缕蒸汽和6个压扁染金的同一 wisp 是附属物，没有形成独立压波阶段。 |
| `src/TaffyActions.cs:7`、`:29`、`:52` | 安全的近战胜方已有0.30秒 Guard准备、0.15秒原生Hit出手、0.40秒收势，只有攻击段具有唯一 PRE。无须在 Initialize 伪造前摇。 |
| `AceTaffyMod/Data/CardInfo.xml:153` | 当前为 Near、Cost 4，首骰6—18 H、次骰6—10 G、末骰4—8 H；首骰有独立 EffectRes 与 ActionScript，末骰沿普通打击。 |
| `Verification/Vfx/UnityFixture/Assets/Editor/TaffyDispatchPreview.cs:121` | 旧预览使用真实工厂和实际人物PNG，但手点是人工 `(-1.4,0)`，图片 pivot 固定0.5，不能宣称还原了原版皮肤挂点。 |
| 同文件`:73`、`:170` | 仅事件／0.4秒退场图，固定960×540、ortho7.5。可证明GPU可见，不能证明正常速度中的阶段主次。 |
| 原版 `DiceAttackEffect.Initialize／SetScale` | `H`路由到攻击者 Hit pivot，继承父系，原生基础2倍后再乘 `(1+rollFactor)`；不得手工重复镜像或地图缩放。 |

旧大招母图的三条大云尾与普通打击的米白、灰褶、金点语言接近，缩小后仍是横向白雾。它的大部分差异来自尺寸，亮锋位于对方头胸前，右上大云瓣又分散注意。旧图在近间距情境遮住受击者面部，因此继续扩大整团云是错误方向。旧 PASS 仅对应当时的可见性与初版蒸汽家族，不代表满足本次更明显专属的新要求。

本次复用本地技术参考已足够：第14章记录的原版 Dagger“窄核＋宽反冲域”、Xiao“主波与异速副波”、第13章的透明贴图笔触与父系合同。只借鉴可读的分层关系，不复制那些原版素材或转入粒子包系统，无需额外无边界外部搜索。

## 专属形态：活塞爆压＋脱离的破裂压锋

主形是一束扁阔、短粗、向左推出的高压蒸汽。右侧靠近拳套的一端很窄，向左展开为一片钝而厚的破裂压力前沿；中段由上下2股压缩蒸汽夹出狭长的不规则透明通道。最前方是有缺口的暖白／旧黄铜硬锋，后面是少量灰褶，不再长出右上、右下2团大棉絮。整体看起来像集中压力撕开空气，不能像长针、火球、实体金属盾或剑气。

第二张独立素材是一条薄、断裂、不闭合的竖向压波，凸面向左，开口向右。它从主锋附近在50毫秒后向前分离，并短促扩开、消散；主形仍在原处保持。这条压波只承担“高压释放后的空气前锋”，绝不作为目标受伤爆点。其亮质量低于主形，不制作第二个完整云壳，不沿角色周围画环，也不持续存在成护盾。

只有3种视觉职责：完整主形、短压波、3—4缕退散蒸汽。新的金属碎闪直接画在母图前锋局部，不继续把 SteamWisp 压成6条假火花。普通效果保留原样。

材质为干刷压锋与湿润蒸汽褶皱相结合：暖象牙白是受压亮面，冷灰是背光体积，旧黄铜仅在破口处点亮。深褶必须保留RGB颜色，不按亮度抠空。主形内部有明确透明窗和断笔；高亮像素集中在前部约20%的长度范围，整片主体不泛白，不添加 Bloom、灯光、屏幕白闪、暗屏或镜头震动。

## 有界时间线

所有时间为缩放战斗秒，相对于真实攻击效果事件 `t=0`。视觉时钟使用实例 `_elapsed`，后续 `SetDestroyTime` 不重启或拉伸时间线。对象若被原版提前销毁，服从取消，不为演完尾巴延长战斗。

| 时间 | 主形 | 破裂压波 | 游离蒸汽 |
| --- | --- | --- | --- |
| -0.30—0 | 现有Guard准备姿势，尚未创建效果 | 无 | 无 |
| 0—0.05 | 第一帧完整主形，alpha约0.95；向前短推，位移上限0.08个人物高度 | 尚未出现 | 无或极少 |
| 0.05—0.10 | 主形保持；均匀缩放只允许1.00→1.06 | 从主锋稍后位置出现，alpha 0→0.70，沿前向分离 | 后部开始出现 |
| 0.10—0.22 | 清晰的重击保持，主形alpha不低于0.85 | 均匀尺度1.00→1.25，继续向前短推；总前移上限0.18个人物高度，alpha到0 | 3—4缕，alpha不高于0.28 |
| 0.22—0.40 | 平滑淡出至alpha不高于0.40，主形不突然缩回小点 | 完全清除 | 向后上方散开 |
| 0.40—0.60 | 主体淡尽，最迟0.60秒为0 | 无 | 继续淡出 |
| 0.60—0.80 | 无 | 无 | 最迟0.80秒全部为0 |

0.30秒准备与0.40秒收势保持现有值，不修改ActionScript。主体＋压波承担本轮全部专属视觉增强。

## 尺度与遮挡合同

以实际当前 Hit 人物alpha内容高度为 `H`，固定同一镜头与父系，在 rollFactor=0.5 的中档原生倍率下标定。

- 主形可见宽1.35—1.55H，高0.55—0.70H。保持素材原比例，只通过PPU和统一缩放标定，不能非等比拉长。与普通打击同尺度比较，宽至少约1.7倍，高至少约1.6倍；若旧普通实测与文档不符，以实际画面为准，优先满足局部性和人脸读取。
- 压波高度0.65—0.85H，宽约其高度的0.3—0.45；它的位置由攻击者局部主锋确定，不读取目标位置来追踪脸或胸口。最大位移与展开均计入整体包围盒。
- 主形尾端靠近拳套；必要时调整仅爆轰的 ForwardOffset，不能直接沿用旧-2后再不断加宽。高亮前锋上下端要有缺口，避免在目标脸上形成整片不透明白云。
- 攻击者脸部峰值高不透明覆盖应小于10%；对手脸部在中档与高档缩放下，持续超过50毫秒的alpha≥0.6效果覆盖应小于25%。这是固定预览人物脸框内的效果层覆盖率，不能用角色遮住效果来作弊。若无法满足，先削尾、开洞、下移局部或收高，不更换镜头／拉远对手。
- 峰值不能用全屏受光代替局部轮廓。原生低／高掷骰倍率照常保留，禁止钳制原生SetScale。

## 美术交付：主线程独占素材生成

保留旧母图原文件作为历史研究输入后，生产 `SteamDetonation.png` 换成新母图；新增 `SteamPressureArc.png`。复用现有 `SteamWisp.png`、共享材质与缓存。Blunt／Pierce／Guard图字节不变。不生成角色、卡图、声音，不用代码噪声或几何图形替代新母图。

| 产物 | 输入／输出规格 |
| --- | --- |
| 主体源图 | `ArtSource/Effects/ultimate-v1/SteamDetonation-master.png`，1536×1024 RGBA；内容宽高比约2.1—2.3，四周至少8%全透明安全边；左为前锋、右为腕部尾端 |
| 压波源图 | 同目录 `SteamPressureArc-master.png`，1024×1536 RGBA；单个开放破裂压弧，内容高宽比约2.5—3，四周至少10%全透明安全边 |
| 生产图 | `AceTaffyMod/Resource/Effects/AceTaffy/SteamDetonation.png`，1536×1024；同目录 `SteamPressureArc.png`，1024×1536。均在当前加载器2048单边限制内；按alpha内容尺寸重新计算PPU，不非等比改形 |
| 溯源 | 保存精确提示词、原响应、来源、透明清理参数、源图和生产图SHA256；主图与压弧必须分别生成，不从多宫格拼图切片 |

主母图精确提示词：

> Create ONE original 2D game VFX texture, isolated on real transparent RGBA, landscape 1536 x 1024. This is a signature high-pressure steam piston detonation for a brass steam inventor in a dark painterly anime battle game. ALL motion travels LEFT. The narrow rear origin is at the RIGHT, where an unseen gauntlet releases compressed vapor. The plume opens toward the LEFT into a broad, blunt, forceful pressure crown. Use TWO unequal compressed steam ribbons, upper and lower, enclosing a clearly visible irregular transparent channel through the middle; they join only near the leading crown. Short body, visible content width to height about 2.2. One dominant left-front pressure edge, broken dry-brush ivory with tiny old-brass fractures, cool grey folded steam behind it. Brightness belongs to the narrow fractured front edge, not the entire body. Carve real transparent notches into the upper leading edge and real airy gaps between folds. The rear right tail is narrow and short, no large upper-right or lower-right cloud lobes. Convey a piston blow tearing through air, not a pointed spear. Rich brush detail must remain a strong graphic silhouette at small size. At least 8 percent completely transparent padding on ALL four sides. No character, no hand, no gun, no metal object, no gear, no emblem, no text, no scenery, no floor, no shadow, no checkerboard, no background color, no rectangular haze, no full circle, no magical aura, no giant smoke cloud, no fireball, no laser, no broad glow halo. Do not put a detached shock ring in this texture; it will be a separate animation layer. High quality single usable game asset, no contact sheet.

压波精确提示词：

> Create ONE original 2D game VFX texture on real transparent RGBA, portrait 1024 x 1536. A thin BROKEN pressure wave released ahead of a LEFT-moving steam piston blast. Single vertically elongated open curved shock edge, convex strongest side toward LEFT, empty opening toward RIGHT. Visible height roughly 2.7 times visible width. The arc consists of several connected and separated irregular dry-brush fragments, strongest around the left-middle, with a conspicuous missing upper-front fragment and tapered broken ends. Warm ivory edge, very small dull brass accents, a tiny grey vapor fringe. At least 75 percent of the bounding interior must remain genuinely transparent. No filled center, no cloud body, no secondary parallel shell. It should read as an instant violent air-pressure fracture when moving forward and expanding briefly, not a solid shield or persistent magical ring. Asymmetric, rough, clear at small size, keep the front contour decisive. At least 10 percent fully transparent padding on every side. No character, weapon, text, gear, rune, scenery, floor, shadow, checkerboard, background color, opaque haze, full circle, ellipse outline, glowing halo, white disk, or particle collage. One asset only.

若图生工具未生成真正透明背景，主线程依原有美术流程处理，禁止把灰褶亮度当alpha。透明是否正确须在深／中灰底实际查看，不能从PNG扩展名判断。

## 实现交接与职责

实现代理接收主线程批准的两张生产图后，才开始运行时代码；不自行重设计。建议最小改动面：

1. `src/TaffyVfx.cs`：仅为Detonation增加独立Sample分支、压波renderer和预热缓存项；仍由当前四资源精确工厂派发，仍使用 `TaffySteamEffect` 以保留工厂强制转换合同。无需新Harmony、公共控制器、目标命中回调或新AssetBundle。保留首图节点名 `SteamDetonation`，便于现有夹具读取。其余3种Sample分支的数值／材质／图像保持原值。Detonation不再创建旧6枚假BrassFleck，仅保留少量wisps。独立压波在取消与OnDisable时一同禁用。
2. `Verification/Vfx/`：在现有真实工厂夹具上增加本次分阶段、原版挂点和媒体输出；准备脚本应记录新增素材和生产源哈希。增加输出目录参数，每次写新的 `ultimate-v1/<run-id>/acceptance.log`，不覆盖旧unity-preview和旧PASS。
3. 必要的源验证只验证新缓存项、绑定唯一性、资源可读、普通素材不变、原生事件／缩放／取消边界，不把新美术包围盒常数硬写成无意义“测试实现自己”。沿用 `Verification/verify_package.py` 的现有入口，若新增资源白名单确有需要，只加明确资源项。

`TaffyActions.cs`、CardInfo.xml、机制源码、声音与角色文件当前排除。若新增实现表面会迫使修改这些文件，先把具体阻碍报主线程批准。

挂接与战斗边界必须保持：

- `Initialize`调用原生，H挂点为攻击者的Hit；左右朝向从完整父系继承一次，不增加flipX，不改成世界坐标爆点。
- 原版负责胜负与伤害。`HasDamagedEffect=false`；新压波是出手压力，不宣称命中，不在目标处盲生爆炸。Lose／Draw／远程／外来专属队列沿现有回退，不强迫播放准备动作。
- 一侧攻击仍由原版队列及现有适配器处理；不要把必须 opponent.result==Lose 误当成所有单方面情境都已被实测支持，至少观察一个实际构造的无骰对手结果。
- 复制／复投实例各自独立计时，不使用静态骰序或“首击已经播过”的共享状态；末骰复投仍消费其普通SteamBlunt绑定。
- 工厂先Initialize后SetScale；第一帧与后续样本不能因此跳变。缺失新增压波图时允许只保留主形并记录一次错误，不抛出导致整次原生战斗失败。

## 验收与拒收

使用真实Unity2019工厂、生产代码和原版 `DiceAttackEffect` 的完整调用。预览标注“Unity原生工厂隔离模拟”，不称用户游戏录像。主线程负责查看，第三名独立代理给出PASS／REVISE／BLOCK。

角色上下文先修正证据：读取当前 Taffy `ModInfo.xml` 的Hit／Guard／Default pivot与PPU，按原版加载规则创建实际PNG；优先复用本地原版 `[Prefab]Appearance_Custom` 的实际Hit挂点与层级，或从其原始数据提取一致变换并标记为数据重建。第14章的 `(-1.92,2.52,0)` 是原版通用根参考，不能未经核实直接声称是本次Hit最终点。保留父系镜像／地图缩放；对照人物可用已导出的原版Roland。完整人物加载不足时明确局限，不能继续用旧人工手点冒充原版挂点。

最小交付证据：

| 场景／产物 | 接受条件 |
| --- | --- |
| 深／中灰底原图 | 无矩形雾、硬裁边、透明彩边；小图能读到主压锋和中间负形；旧3瓣长云尾被替换 |
| 普通／旧大招／新大招同镜头并排 | 新大招在约240px宽的单栏缩略图中仍具独特压缩通道和独立压波，不只是更白或更大；普通图／帧输出保持现有视觉 |
| 左／右向阶段帧 | 0、17、50、100、220、400、600、800毫秒，前2帧已有主形；100毫秒可清楚分辨前压波与主形，不形成两片重复云壳；220毫秒压波消失 |
| 正常速度 | 60fps MP4及30fps GIF；开头显示现有0.30秒准备，接真实样本出手与0.40秒收势，附时钟，不把视频压成慢动作；如未实际重放MoveRoutine，明确称为生产队列驱动姿态模拟 |
| 实际挂点上下文 | 实际当前人物、原版挂点、同一相机、接近原生交战间距；中档与高档roll，左右镜像各一次；不为隐藏盖脸拉远对手。记录脸框遮挡与主形尺寸 |
| 生命周期与边界 | 实例各自计时；SetDestroyTime不重播；取消立即清空全部新旧renderer；800毫秒无残留；两个实例／复投不串状态；未知资源走原生fallback |
| 唯一性 | 仅1883009首骰仍指向SteamDetonation；费用与3骰玩法完整；第3骰及其复投不冒出大招压波；没有新增声音播放 |

明确拒收：只有放大旧云；开场不足完整可读主形；前后两层同形叠白；压弧像常驻魔法盾；面部持续被雾盖住；右向反转两次；为了大招改动其它低费效果；在Initialize提前播放0.30秒蓄力或延迟伤害；新增自制声音；只提供0秒和0.4秒静帧就声称动态通过。

通过独立视觉审查后，主线程运行相关包验证、正式net472构建和本候选Unity工厂检查，读取唯一主日志，核对最终生产源／两图／DLL哈希。该路线无新Shader或AB，故不需要虚构AB构建步骤。部署时逐项核对构建与目标文件一致；完整游戏镜头、HUD和特殊对手实战仍明确留给外部验收。
