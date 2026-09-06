# 潮雾晨光：厚云与穿云晨光参考图细化

2026-09-07。研究/视觉设计交付，供主线程审定后交给实施。本文未修改生产代码、生成资产或运行 Unity；不代表视觉验收通过。

## 意图与冻结项

将新参考图中的厚蓝灰云、云后暖白太阳、穿云光束和近地雾，转译为9004004的柔和全屏技能。卡图提供明亮晨光与安静祈祷的气质；新图提供云的体积、遮光关系与空间层次。人物、建筑、鸟群和战场不是本次新增资产。

保持既有两骰会话、一次揭幕、每骰一次光峰、成功目标局部反馈、末骰统一退出。四个 runtime 文件、XML、VeliaCards.cs、旧 Slazeya 资产及其他滤镜均冻结。首选改动只有本模块贴图、shader、material builder、相关验证/预览与资源说明；保留 Unity2019.3.15f1 Built-in/Gamma/D3D11、material-only 自包含 Bundle。

本文经主线程批准后，替代旧设计中“无需新贴图/只复用薄雾图集”和无角色云体 alpha 上限约 .48 的视觉假设；不替代已验证的游戏与相机协议。

## 已查看的依据与诊断

- 根目录没有 README 文件；已读根 AGENTS.md、ModGuideDocs/README.md、11实战的资产分层/先大形后细节/亮度质感噪声分离段、02的 FarAreaEach 章节。
- 已读2026-09-06设计、2026-09-07交付记录、模块 README、当前 shader、builder、controller、filter、source verifier、references.json，以及提交 `9a326ff`。
- 原始卡图：`SteriaBuild/SteriaModFolder/Resource/CombatPageArtwork/9004004.png`。
- 新参考：`C:/Users/rog/AppData/Local/Temp/codex-clipboard-a368e7e4-27d1-4e9a-b740-3563d821197c.png`。主线程已归档到本模块 `source_assets/reference_20260907/user_dawn_clouds.png`；本研究核对 SHA256 为 `732EE14EF17A896EB098282198573C65FDBD0B36CAD26E174CA08E7FFF760B28`。临时路径不作长期构建依赖。
- 原生基线：`preview_exports/velia_tide_mist/review-521db741/01_reveal.png`、`03_peak1.png`、`06_peak2.png`、`pulse2_plus_0p18s.png`、`08_fade.png`。已逐张查看；它们是代理场景，不是真实战斗/HUD。

R5的优势是人物和数字清楚、事件链稳定。与新图的差距是结构性的：云几乎不成形，中心主要是模糊亮斑，地面空气层很弱；两峰静帧过于接近。256px图集单格被拉到大半屏且只服务薄雾，三个 bank 的 RGB 被加权平均、A 取 max，进一步弱化前后层遮叠。只提高 alpha 会得到更浓的灰色薄片，不会凭空增加云团褶皱和迎光轮廓。现有三个 Gaussian beam 与实际云隙没有明确对应关系。

因此采用**新画的厚云颜色/覆盖贴图 + shader独立太阳与受云遮挡的光束 + 原图集仅作低雾**。不在全分辨率重新实现体积云 raymarch，不复制参考图的完整风景，也不将旧雾贴图继续当大云主体。

## 资产合同：主线程可立即制作

首选一张独立的 `source_assets/reference_refinement/dawn_cloud_frame_v1.png`。PNG、真正的透明 RGBA、优先2048×1152或生成工具支持的相近横幅尺寸，保持实际比例；不要拉伸圆形云团以凑分辨率。它是**颜色美术贴图**，RGB为云的冷灰体色及中性受光层次，A只表示软覆盖；不是旧R/G/B密度/光照/遮蔽数据图。

构图为两个不相连、不同高低的宏大云岸：左边更高更宽，右边稍低，厚实核心从画外伸进来。中央上方约(0.50,0.72)留不规则日光开口；左右内缘可分别接近日轮下左/下右，不能连成横贯屏幕的云条。中轴须有真正透明的通路，方便从同一贴图独立采左右云。画面下方约35–40%保持透明，只有最外侧可有少量下垂尾雾。主体体积由几块大连通云团支撑，每团有两三级尺度的凸起、凹谷和自阴影；不画满屏均匀小棉球。

厚核心alpha接近1，外围由清晰但不硬切的云脊过渡到少量半透明卷丝。云腹深蓝灰，面光偏冷银灰；朝上方中央光源的内缘有中性象牙白亮面，仍保留层次与余量。不要烘焙太阳、金色大光晕、放射光线、底色、黑边或棋盘格；动态暖色和峰值由shader负责。不是一张发光的金边云贴纸。

建议生成 prompt（以新图和9004004卡图为参考；只生成以下资产）：

> Create a production VFX cloud framing texture on a genuinely transparent RGBA background, wide landscape composition. Use the references only for the monumental sculpted blue-gray cloud volume and restrained cinematic painterly finish. Two disconnected asymmetrical dense cumulus cloud banks enter from the left and right outside edges: the left bank is higher and broader, the right slightly lower. Large connected billowing masses with secondary folds, deep slate-blue self-shadowed bellies, silver-gray lit planes, and restrained ivory highlights on ridges facing an implied light source near the upper center, about 28 percent down from the top. Keep an irregular transparent opening around this implied sun and a clear transparent vertical passage through the center. The lowest 35 to 40 percent, especially the lower middle battlefield area, must remain transparent; only sparse wisps may descend at the far sides. Opaque substantial cloud interiors, crisp-soft natural scalloped silhouettes, fine translucent edge wisps, coherent scale, no repeated cotton-ball stamps. The texture is only clouds: no sun, sky, solid background, baked rays, gold glow, people, buildings, landscape, horizon, birds, text, frame, checkerboard or black matte. Preserve cloud color in antialiased transparent edges. This is an isolated reusable game compositing asset, not a finished scene.

主线程选择生成结果前检查 alpha通道、中央/下方透明区、大团轮廓、内侧受光方向；不合格则按相同合同修正资产。不要用大量shader噪声弥补错误美术。若生成器始终无法给出可用透明云框，可以将同一合同拆成左右两张独立云岸，各自从对应外侧受裁切、向中央渐疏；这是资产包装退路，不改变设计。当前不需要额外角色、太阳或光束生成图。

导入：新云颜色贴图 `sRGB=true`、FromInput alpha、Clamp、Bilinear、无mips、Uncompressed，最大2048，RGBA32结果回读；不要套用旧数据图 `sRGB=false` 的语义。透明边RGB必须有相邻云色的合理延展；shader按straight-alpha输入乘A一次，不重复预乘。旧 `mist_density_lighting_4x4.png`继续按原设置、原SHA只读导入。

## 构图、材质与光影

所有位置使用左下原点 viewport UV，圆与宽度按aspect修正。下列范围是首版调参起点，最后以原生画面判断。

| 层 | 职责与目标 | 实现约束 |
| --- | --- | --- |
| 晨光 | 中心约(.50,.74)，明显的暖白核心宽约屏宽.14–.19，低强度光晕延展到.40–.50；云切断下缘和部分侧缘 | 核心有亮度坡度，外围迅速转柔；不能复现R5的一整团失焦乳白。不是硬描边圆盘，也不靠全屏Bloom |
| 厚云岸 | 左云占上左，右云略低；在等待与峰值均可认出成团云腹、褶皱、迎光沿。至少上方与左右外侧出现真正遮住背景条纹的厚实核心 | 不重复盖三张同质透明雾。左右图块独立慢移，维持体积尺寸；无角色的厚核心合成A起点.65–.85，局部可接近.90，但边缘自然降低 |
| 穿云光 | 2–3道主要宽光束从中央上方云隙向下/斜外扩散，其间保留暗隙；少量次级条纹依附主束 | 先看见方向和遮光，再加细节；光束应被云遮断，不能隔着整团云直穿。高频源图不能进入径向采样 |
| 云面光 | 金白集中于朝光面和薄沿；冷暗云腹保持蓝灰，不随峰值全部漂白 | 由新贴图中性亮面与覆盖/方向共同提取surface mask，不能仅对所有alpha边描统一金线。暖色仅加在已存在云材质上 |
| 近地潮雾 | 腿脚附近低速横流，浅冷雾在光束落点有短暂暖亮，底部可读出空气层 | 继续用旧图集中段。左右各一条低扁、非对称薄雾即可；不引入地形、水面倒影或横贯画面的实心白带 |

色彩起点：云腹 `#344654` / 深部 `#293845`，云面 `#8295A5`，银亮面 `#B4C0C9`，受光沿 `#F2D9AE`，日心 `#FFF6DF`。不是全局着色表：保留原角色颜色，暖色集中于光源及迎光表面，冷暗团块提供峰值对比。

合成顺序：原始相机颜色 → 后方太阳与透射光 → 有自身暗腹/亮面的厚云 → 很轻的前景雾和受控空气散射 → 已确认成功位置的局部光痕。光透射与云覆盖不能当成数层相同alpha反复压暗。源图只读取/原样合成，不模糊、不做径向拖影；维持 `source.a`。

厚云已经有烘焙的体积明暗，无需每像素多层fBM。新云主采样约2次（分别限于源图左右区域），局部UV扰动幅度约1–3个纹理像素，不能让整个云像胶布融化。表面亮面可以由RGB亮度的平滑阈值和朝中央方向权重得到；需要轮廓光时加1–2个朝光方向的alpha偏移采样并与体内亮面混合，避免纯轮廓描边。

光束采用独立云覆盖遮罩。有限采样方案：在宽主束所在区域，沿像素到日心的线段做最多6次低频云alpha采样，估计透射/阴影，再调制柔边主束；使用固定采样和柔遮罩防抖，验收检查台阶。不要采 `_MainTex` RGB，也不要将人物保护椭圆当遮光物。此处是屏幕空间舞台近似，不宣称真实体积散射。若需要更多采样才消除带状伪影，先将同一美术云形的光隙/透射场离线烘成小遮罩，而非扩大为几十次全屏密度计算或改相机链。

## 运动和打击：事件时长保持原值

- 揭幕0–.32s：先显厚云暗形，再让中央晨光穿出；使用现有Envelope与SunReveal。云岸轻微从两侧向构图位置平移，总行程约屏宽.01–.02；云形不放大缩小，也不在一次峰值后重置UV。
- 等待：云慢速横移，累计位移有界，细褶皱轻微变化；保持明显但较低亮度的云岸与日光。无循环闪烁，无长等待贴图硬跳帧。
- 每骰回调后0–.04s升亮，.04–.07s短峰，首骰到.27s回落、第二骰到.32s回落；沿用 `_PulseAge` / `_State`。命中只驱动光、亮面和薄雾散射，不令厚云轮廓突然扩圈或缩放。
- 第一峰：日心与中央附近迎光沿首先升亮，主光束获得清楚方向。第二峰：亮面沿已存在云层向左右/偏下传播，约+.10s能看到更远的暖沿，+.18s能看到外围余光与更弱的内侧亮面；可扩大受光范围约15–20%，云体积和整体白色峰值不相应增大。用填充式、宽羽化的传播场，不能出现发光环。
- 成功命中光痕保留当前位置与max混合语义；不增加新伤害/事件/音效。最后.40s统一退出，可用Envelope的幂次让光先退、云体后退；所有层在Envelope=0必须严格归零，不延长原生命周期。

## 人物/HUD可读性

贴图本身先为战斗区域让路，再使用现有投影范围软减权。大云主要在头顶上方与外侧，不依靠覆盖满屏后抠出人形洞。禁止矩形缺口、锐利椭圆洞、人物周围暗圈，以及用全屏水平保护带把所有上方云腹一起消掉。

保留 `softBody()` 的连续保护和已验证投影协议。有角色时按其范围减云/光，无角色范围才使用保守中央带回退。角色核心总雾覆盖维持约.08–.12上限，脸与武器不能被金白盖住；光束在人物上温和减权而非突然截断。外侧厚云不要与保护系数相乘多次。顶部/底部HUD维持既有边缘保护；代理数字必须清晰，真实HUD仍需主线程实际路径确认，不能由本设计保证。

## 文件级实施边界

| 文件 | 实施内容 |
| --- | --- |
| `SteriaBuild/VFXSource/VeliaTideMist/UnityProject/Assets/Shaders/VeliaTideMist.shader` | 新增 `_CloudPlate`；将旧三bank主体改为有层次的颜色云岸，旧atlas降为低雾。重做日心/云隙透光关系，保留aspect、sourceUV方向处理、显式时钟、保护/命中接口与有界增亮 |
| 同项目 `Assets/Editor/VeliaTideMistBundleBuilder.cs` | `PrepareMaterial`绑定新云；使用正确颜色导入语义；`BuildBundle`回读检查新增纹理、格式/尺寸/alpha与自包含依赖；保留单material根。`WriteSourceReceipt`含新美术SHA；把固定 `REVIEWED_R5`文案改为当前实际状态，不提前宣称新版本PASS |
| `export_velia_tide_mist_video.ps1` | 视频文件名含实际PreviewName，去掉固定 `reviewed_r5`；将 `REVIEWED_R5_FULL_VIDEO_PASS`改为中性的编码/解码成功状态。视频编码成功与独立视觉审核PASS分别记录，不能由脚本自动推导后者 |
| `source_assets/reference_refinement/` | 主线程归档选用美术、prompt/模式/生成来源/原尺寸/SHA；用户参考沿用已保存的 `source_assets/reference_20260907/user_dawn_clouds.png`。生产只依赖选用云资产，不依赖临时附件位置 |
| `source_assets/references.json`、模块 `README.md` | 记录颜色图与数据图不同语义、新设计路径/当前审核状态；移除已过时的“noNewArt”断言，历史R5交付仍可引用 |
| `verify_velia_tide_mist_source.ps1`及builder验证 | 生产变更前先加入新资产存在/元数据与hash检查、纹理依赖回读预期；保留旧atlas SHA与所有生命周期保护。不要只验证新uniform文字出现 |
| 四个runtime文件、其Unity镜像、`VeliaCards.cs`、`CardInfo.xml`、Slazeya模块 | 本轮视觉方案无需改动。若实现遇到必须更改的阻碍，报告具体原因给主线程；不能自行更换接入或结算协议 |

沿用原生代理scene/回调frame36和90/145帧60fps/原始人物与数字，可直接与R5同帧比较。不美化验收背景来掩饰云弱，不借换场景提高效果评价。预览按新revision目录输出，保留原版本；正式打包、DLL必要性、运行时回归与同步由主线程负责。

## 独立预览PASS门槛

1. **大形通过**：等待、两个峰值和+.18s帧缩到50%后，左右仍明确是两座有连通大体积和褶皱的厚云岸；能同时指出冷暗云腹、面光、暖沿。相比R5应是直接可见的结构变化，不接受“同样光斑+稍浓灰雾”。
2. **光源关系通过**：太阳具有亮核/渐变晕，至少部分日缘被云自然遮断；2–3道宽主束确实从亮隙通向下方，且云后有阴影间隔。没有大乳白椭圆、硬金圆、针状星爆或贯穿云体的均匀条纹。
3. **两峰运动通过**：完整60fps播放能读出一次揭幕、两次快起缓退光峰、一次退出；第二峰在+.10/+.18s有比第一峰更远的云面受光与下侧余光，不能仅日心更白。无云体缩放、贴图重置、径向环、相同云团平铺或变形胶布感。
4. **材质通过**：峰值仍保留冷灰厚云暗腹；透明边无黑/白晕、棋盘/矩形接缝、alpha挤压、混色脏灰或重复半透明层。近地雾能看见漂移和短暂受光但不形成地板白墙。
5. **可读性通过**：峰值和传播帧中三个代理角色的脸、衣褶、武器与全部代理数字可辨，保护区域没有可见挖洞；HUD方向探针与源图方向保持。维持现有新增近白面积≤15%的检查，不以把整个效果削弱到R5水平换取通过。
6. **退出/证据通过**：检查frame10揭幕、30等待、39/93峰值、两个骰各+.10/+.18s、66骰间、122退出及140完成；完成应恢复基线，shader新层不得有残留。审查first原生帧后再打包；正式Bundle回读再确认同候选。真实游戏/HUD及性能未实际测量时必须明确未验证，不能用代理图冒充。

## 查证的外部参考与采用范围

- [NVIDIA GPU Gems 3，第13章 Volumetric Light Scattering as a Post-Process](https://developer.nvidia.com/gpugems/gpugems3/part-ii-light-and-shadows/chapter-13-volumetric-light-scattering-post-process)，已读13.3–13.5：向光源采样估算遮蔽、exposure/weight/decay控制散射；直接采场景纹理会产生不希望的条纹，独立遮蔽源可避免。这里仅采用“云遮罩决定透光”的思想，使用本效果自己的云alpha与有界采样，不复制全屏场景径向模糊或其源码。
- [Guerrilla，The Real-Time Volumetric Cloudscapes of Horizon Zero Dawn](https://www.guerrilla-games.com/read/the-real-time-volumetric-cloudscapes-of-horizon-zero-dawn)，已读官方摘要：厚积云、可指导的大形、模型/动画/光照分离是明确目标。此处借用形体与职责分离的设计原则；没有阅读下载演讲全文，也不声称移植其体积算法或达到其GPU耗时。

本研究仅写本文档。尚待主线程审批资产/设计、实施agent生成原生预览，以及独立reviewer作PASS/REVISE/BLOCK判定。
