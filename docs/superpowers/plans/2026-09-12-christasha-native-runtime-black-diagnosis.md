# 克丽丝塔夏原生粒子游戏内发黑诊断

日期：2026-09-13。范围是后台白金预览与游戏内整体发黑的差异定位。此次只读检查，不启动游戏、Unity、桌面或浏览器，不修改生产代码、资源或部署。

## 现状与证据

`output/christasha-native-redesign/scoped-release-candidate/deployment.json` 记录的发布候选 DLL 为 `7208d473ce2ccca39e607fb0fee360da4b16d82a371cd9857e4f0725cda4d5f0`，安装前 DLL 为 `4ba01608…`。D 盘真实模组目录和 C 盘链接入口的已安装 `Steria.dll` 均实测为 `7208d473…`。当前工作区 `SteriaBuild/ChristashaNativeParticleLibrary.cs` 及候选 `prepared.json` 的源码 SHA256 均为 `4c74974a6473b7fd76cd888f69d6b864debbfbb6e709135846826602aed5139b`；候选源码与当前源码相同。因此现有证据不支持“游戏加载旧 DLL/旧白金源码”为首要原因。

部署的自有 brush `tide-brush-imagegen-v1.png` hash 为 `c805e472…`，与候选收据一致。CardInfo 收据仍是安装基线 `ab091603…`，其视觉字段已经指向 `Steria_ChristashaV2...`；接入注册在 `HarmonyPatches.cs` 也包含 SlashH/SlashV/Hit/PierceNear/PierceFar。部署路线本身暂无漂移证据。

Editor `ChristashaNativePreview` 通过隔离参考包的 `bundle.LoadAsset<GameObject>`，保留原编译 Shader/AnimationClip；游戏适配器 `ChristashaParticleInstance.GetLibrary` 使用 `Resources.Load<GameObject>(resource)` 取本机原生资源。导出收据与 Editor metadata 显示原 Shader supported，说明两边意图使用同一原版材质/网格/粒子来源；静态文件无法证明运行时 `Resources` 返回的 root、Animator controller 和 MPB 字段逐项相同。该 parity 是次级验证项，不能用 Editor bundle 通过替代游戏证据。

## 根因候选（按证据排序）

### 1. 最高：运行时没有提供某个 MPB 颜色字段，零值被当作真实原色并反馈累积

当前 `ChristashaNativeParticlePlayback.Step`（`ChristashaNativeParticleLibrary.cs` 约604–616行）在 Animator.Update 后直接：

```text
bladeRenderer.GetPropertyBlock(colorBlock)
nativeMainColor = colorBlock.GetColor("_MainColor")
nativeEdgeColor = colorBlock.GetColor("_OutColor")
nativeContourColor = contourRenderer.GetColor("_MainColor")
```

没有确认字段是否真的由 Animator/MPB 写入，也没有在字段缺失时读取该 Renderer 自己的材质颜色。随后映射使用 `nativeEdgeColor * (.68,.65,.53)` 作为 Main1 宽体，使用 `nativeContourColor * (1.05,.93,.72)` 作为 Main2 轮廓；当这些读取值为零，白金主体和轮廓立即变黑。`colorsSampled=true`后，下一帧又把这次黑色恢复回去，形成稳定的黑屏反馈。

这能解释 Editor 与游戏的差异：Editor 的原始重放记录确实采到 Main1 `Main=0, Out=1.5`、Main2 `Main=3`；候选记录的主题映射为白金值。游戏若 Animator 没写同一字段，`GetColor` 默认零值就会进入上述路径。即使游戏只写 Main1 的 `_MainColor` 而没写 `_OutColor`，现有代码仍会把 Main1 映射成零；Main2 也有同样问题。Hit 的 `HitColor.Map`、Mass 的 `Colors.Map` 通过 `native.isEmpty` 区分空块，但 MPB 只要含其它属性、缺少目标颜色字段，`GetColor(prop)`仍可能得到零，也会把多个动作一起压黑。

### 2. 次高：Resources prefab 或 Animator 与 Editor 隔离参考对象不相同

两条加载链不同：Editor 用导出 bundle 的明确资产名，游戏用原生 `Resources.Load` 的小写路径常量。当前收据证明 DLL 和 brush 已部署，但不包含游戏运行时返回的 root 名、实际 Shader、Animator controller、材质实例和 MPB 快照。资源路径指向同名但不同版本、Animator controller 在运行时被裁剪、或某字段绑定不在游戏版本中，都可能触发候选1的缺失颜色。

不能在未取得运行时日志前宣称 Editor 与 Resources 完全等价。应添加一次性带版本号的诊断（不改变颜色）记录 resolver 返回的 prefab/root、每个 Main/Contour renderer 的 sharedMaterial 名和 `shader.name`、Animator controller、`GetPropertyBlock().isEmpty`、四个目标颜色字段的有限性/非零性。只记录初始化和首个三帧，不打印每帧，不写游戏原图。若 root/Shader 不同，修资源路径/版本；若相同而颜色字段缺失，回到候选1。

### 3. 低：Alpha=0 或色彩空间把颜色解释成黑

已核对 MainTrail 片元方程只使用 `_MainColor.rgb`、`_OutColor.rgb`；两种颜色参数的 alpha 不参与片元 RGB。最终 alpha来自 MainTex 覆盖、Noise/ColorNoise 和粒子门限。原生 HDR 颜色属性的 alpha=0 是该 Shader 的序列化约定，不能单独导致 RGB 归零。Xiao 的已核实通道也使用 `_Color.rgb` 与独立 G 明暗，不能用 alpha 或 RGB 全局重染解释所有层发黑。

游戏与 Editor 的候选都以 Gamma/D3D11 为目标；brush 在游戏中以线性 Texture2D 创建，Editor importer 是 `sRGBTexture=false`，Clamp、无mipmap。Gamma/Linear 可能改变亮度，但没有证据会把所有 Renderer 精确变成黑色。不要先把颜色 alpha 统一改为1，也不要抹掉 Xiao 主纹理 G 通道。

### 4. 低：部署 DLL 不匹配

该候选已排除到低位：当前源码 hash 为4c749，构建 DLL及D/C两个安装入口均为7208。仍需主线程从最新 Player.log 确认实际加载的模组根；如果就是收据D盘路径，重复部署相同 DLL不会修复黑化。

## 最小修复面（只给实现 owner）

1. 对每个颜色职责保存克隆材质的有效 fallback：Main1 的 Main/Out、Main2 的 Main、Hit/Mass/Pierce 的各自颜色属性。采样后逐属性判断是否有有效值；缺失时回退对应材质值，而不是把共享零色当作原采样。Main1 主体为零但 Out 有效是合法原生状态，不能以“所有通道都非零”作判据。
2. 修复 Main1 映射来源：宽体优先使用有效 `nativeMainColor`；只有当原生主体确实为零且窄锋有效时，才使用明确的角色主题 fallback 或受控的锋色转填。Main2/Outline 单独使用其有效 `nativeContourColor`。不要把 Main1 的 OutColor 无条件当宽体来源。
3. 将相同的字段缺失回退应用到 `HitColor.Map`、`Mass Colors.Map` 和 Pierce 颜色适配。每帧顺序仍为“恢复上次原采样 → Animator.Update → 捕获本帧原值 → 主题映色”，不能让映射值再次作为下一帧原色。
4. 对游戏首帧、Editor首帧及三个后续帧输出同格式 `runtime-color-snapshot.json`，包括：字段是否有效、材质/Shader/controller 名、映色前后 RGB/A、纹理 linear 标记。先证明候选1，再决定是否需要资源路径修复；不要修改动作、尺寸、Slash11白金配色或47处绑定。

## 后台验收帧

在不启动游戏的前提下，使用相同 Unity 2019.3.15f1、D3D11、Gamma、固定相机、120Hz 手动时钟和 seed 重放：

- Slash：0/17/50/100/167/283/500ms；17ms已有白金锋，Main1/Main2字段均能判定有效或使用明确 fallback。
- Hit：17/50/100/150/200/250/400/600ms；主刀颜色不为零，触地次级不把整层压黑。
- Mass：Cast 100/300/500ms，Area 100/200/400/600/900ms，Point 17/100/300ms；主浪、弦线和点位颜色层级保持。

验收通过条件：同一有效原采样在 Editor 和候选中维持非黑白金层级；缺失字段时仍回退到白金材质，不发生跨帧累积变暗；Alpha/mask 语义不变；同 seed 可重放。技术快照通过不等于游戏战斗视觉 PASS，但足以确认这次黑屏是否来自颜色字段读取。

## 结论

当前最有力证据指向 `GetPropertyBlock().GetColor` 缺失字段未回退、随后以零色进行角色映色并跨帧反馈。Editor 隔离 bundle 与游戏 Resources 的对象 parity 需要一次性日志确认；Alpha=0/色彩空间和 DLL 漂移目前没有支持证据。最小修复应先做逐属性材质 fallback 与零值保护，不重做特效，不重复部署相同 DLL。

## 时序延伸设计（2026-09-13）

用户补充反馈是“当前特效过快、展示不全；汐音要延伸更长，其它略微延伸”。这项调整只作用于视觉时钟和退场包络，不能移动汐音的游戏结算门闸：`FarAreaEffect_Steria_ChristashaXiyinV2` 的 `AreaStartTime=.3s`、`ReleaseTime=.5s`、`MissingCallbackDeadline=1.8s`保持不变。

### 当前时钟基线

源码中 `ChristashaNativeParticlePlayback.NativeDuration` 目前取 recipe hook duration，否则 Slash fallback 为1.2s、Hit 为1.0s。`ChristashaNativeMassRecipes.Playback.Duration` 为 Cast 0.64s、Area 1.15s、Point 0.42s；`ChristashaNativePierceRecipes.Playback.Duration` 为 Pierce 0.62s、Projectile 0.29s。适配器默认以 playbackSpeed=1 播放，FarArea在0.3s创建Area、0.5s收到结果后创建Point。

这些是源码事实，不代表最终画面寿命；主效果可能在末尾已经不可见，而根对象又被外部 `destroyTime` 提前销毁。延长时必须同时检查视觉Duration和根销毁时限，不能只改一个常量。

### 最小调整表

首轮建议使用 per-recipe playbackSpeed 保留原始曲线形状，再让尾部自然多显示一小段。目标Duration和等价speed如下；speed只改变视觉模拟时钟，不能修改事件时刻：

| recipe | 当前基线 | 首轮目标 | 等价 playbackSpeed | 变化边界 |
| --- | ---: | ---: | ---: | --- |
| Slash11 | 1.20s | 1.30s | 0.92 | 保持0–0.18s主锋速度；仅让后段侵蚀/余迹多约0.1s，白金材质与破口不重做 |
| Hit（纵砸候选） | 1.00s | 1.08s | 0.93 | 保持主刀落点相对时刻，延长下段退散；不可恢复拳式星核 |
| Pierce | 0.62s | 0.68s | 0.91 | 保持尖锋首拍，补尾部束流/烟的余势 |
| Projectile | 0.29s | 0.32s | 0.91 | 发射者Fire起点和到达时刻由adapter负责，不能以慢飞改变结算 |
| Xiyin Cast | 0.64s | 0.84s | 0.76 | 0.3s Area开始仍按manager时刻；Cast多出的时间用于弦线余音 |
| Xiyin Area | 1.15s | 1.40s | 0.82 | Area在0.3s创建，约1.70s自然结束；不改0.5s释放门闸 |
| Xiyin Point | 0.42s | 0.58s | 0.72 | Point在成功回调（通常0.5s）后开始，约1.08s退尽；不提前生成 |

等价speed按 `speed = 当前Duration / 目标Duration` 计算，仅为可复现的第一候选。若实现owner发现某个动作的首击被speed整体拖慢，应改为“保留主段关键帧、只扩展峰后退场曲线”，而不是降低全段速度；目标Duration可保持不变。不要同时把原始PS寿命、Animator速度和Playback speed三者都缩放，否则会产生三重延长。

### 汐音门闸和实例存活

Cast仍在0秒创建，Area仍在0.3秒开始，manager仍在0.5秒释放/结算，Point只在成功目标回调中创建。延长只是让这些实例在结算后继续退场，`isRunning=false`和`GiveDamageFromManager`调用次数不变。Area目标1.40s从0.3s开始，约1.70s完成，留在1.8s缺失回调保护前；Point目标0.58s从0.5s开始，约1.08s完成。零成功目标仍必须在Area完成后清理，不能因为没有Point而提前结束波浪。

普通近战适配器目前在 `_visual` 创建前将 `_destroyTime`设为外部 `destroyTime`；而基类 `Update`会按该值销毁根。延长后，owner必须在创建visual后以 `max(externalDestroyTime, visual.Duration + epsilon)`维护根的视觉截止时间，或使用独立的视觉存活计时；`SetDestroyTime`不能把已接受的视觉尾部截断。该变化只影响视觉根，不改变骰子胜负、伤害或manager结算。

### 黑化修复与时序必须一起回归

当前工作区已加入Slash主层的缺失MPB fallback，但Hit的`HitColor`和Mass的`Colors`仍需按相同字段级规则检查；不能因为新增speed就掩盖零色。每个recipe在speed目标的首帧、峰值和退场帧都保存：字段是否由MPB有效写入、材质fallback RGB/A、映色前后RGB/A、shader名、Animator controller和纹理linear标记。

Editor回归至少运行两种状态：

1. 正常Animator/MPB采样：Slash Main1应可记录有效窄锋/主体职责，Main2独立采样；Hit/Mass/Pierce各层颜色与白金候选一致。
2. 缺失MPB诊断：利用已有 `DiagnosticForceMissingMpb` 或等价的逐Renderer模拟清空，确认无字段时回退到该材质的白金角色默认，仍保留alpha/mask，绝不写入全局白色常量。恢复采样后，同一seed不得跨帧累积变暗。

### 最小验收帧

| recipe | 必看视觉时间（秒） |
| --- | --- |
| Slash | 0、0.017、0.05、0.10、0.167、0.283、0.50、0.75、1.20、1.30 |
| Hit | 0、0.017、0.05、0.10、0.15、0.25、0.40、0.80、1.08 |
| Pierce / Projectile | Pierce 0、0.017、0.06、0.12、0.22、0.40、0.68；Projectile 0、0.017、0.06、0.12、0.26、0.32 |
| Xiyin组合 | 绝对时间0、0.10、0.30、0.50、0.70、1.00、1.40、1.70、1.80；另看Cast/Area/Point各自局部0、峰值、退场 |

验收要求：速度变慢后主锋仍在早期出现，扩展只增加可读的尾迹/弦余音；汐音主浪明显延伸且仍有起势→释放→退散；0.3/0.5游戏门闸不变；无MPB时颜色不黑；同seed回放不累乘变暗；根对象在目标视觉Duration前不被外部destroyTime销毁。任何“延长”只表现为全段慢、重复一份主形、或等到结算后才第一次出现，均应REVISE。

本节是设计与验收合同，尚未宣称源码、DLL或游戏内时序已修复。实现owner完成后需提供新的源码 hash、逐帧颜色快照、暗/浅底PNG和非游戏Editor日志，主线程再决定是否重新构建/部署。

## candidate-13b 后续视觉诊断：缺失MPB回退仍需提供宽体白金（2026-09-13）

主线程亲看 `slash-candidate-13b` 的暗底早期/峰值帧：白锋仍可见，但 MainTrail 宽体接近黑色。由此可知“缺失MPB时不要读零”只是必要条件，当前回退后的映色仍没有给主题提供足够的宽体能量。不能再把问题归咎于 MainTrail2/Outline 的默认黑色，也不能用全局白色常量解决。

### 直接证据与责任归属

candidate-13b 的 `no-mpb-diagnostic/metadata.txt` 在 f=1/3/6 记录 Main1 映色约为 `Main=(0.820,0.780,0.640,0)`、`Out=(4.909,4.545,3.773,0)`；Main2 约为 `Main=(3.308,2.595,1.555,0)`。Main2 的 RGB 已经很高而仍主要表现为窄轮廓，因此它不是宽体发黑的主因。candidate-12/13b 的白锋位置、Mesh、mask和粒子顶点状态没有消失，发黑集中在 MainTrail 宽填色职责。

MainTrail 片元方程明确把宽体写成 `MainColor * W`，其中 `W=saturate((1.5*ColorNoise.r)^3)`；`OutColor * E`只负责窄阈值锋。candidate-13b 的 fallback 仍以安全 `fallbackEdgeColor=(1,1,1)`乘系数生成 Main1 宽体 `.82/.78/.64`，当 ColorNoise 的 `W`偏低时，主体自然接近黑，只有 `E`窄带显白。Main1 原始 `MainColor=0`是原版暗体合法状态，但对于当前白金主题不能再把零体积直接作为最终 RGB；这不意味着要改 alpha、MainTex 覆盖或 ColorNoise。

Main2/Outline继续独立采样和映色。它的默认黑色不能被当作全局黑化开关，也不应为了补宽体而无限增亮。MainTrail Shader颜色属性的 alpha=0仍不参与该片元RGB；本次黑化不是alpha或色彩空间的解释错误。

### 最小可验证映色调整

仅调整 Main1 的**主题宽体 RGB**，保留每帧原采样、`MainTex.r`、Noise/ColorNoise、粒子 `COLOR.r`、alpha、mask、侵蚀和 OutColor 窄锋：

1. 若本帧 `nativeMainColor` 有有效非零 RGB，使用它做暖白基色的小幅映射；不要再无条件用 `nativeEdgeColor`替代宽体。若原版合法 `nativeMainColor=0` 且窄锋 `nativeEdgeColor` 有效，保留“原色为零”的事实用于诊断，但主题输出另赋受控的白金宽体基色。
2. 若MPB字段缺失或全部目标字段为零，使用每个克隆Renderer自己的 `PlatinumBodyFallback`，不依赖共享全局颜色。建议首轮宽体范围为 **RGB约 `(0.95–1.20, 0.90–1.14, 0.74–0.94)`**；在极短峰值（约0.10–0.18s）允许上限约 `(1.30,1.20,0.98)`，随后随原侵蚀包络退到约 `(0.65,0.60,0.48)`。这是白金的低HDR宽体，不是r4/07的持续油箔。
3. 保持 `OutColor`作为更亮、更窄的切锋；不要通过提升OutColor去填宽体。保留其alpha=0和Shader原门限语义，不能把alpha强设1。Main2/Outline仍用其自己的 `nativeContourColor`和fallback，不从Main1复制。
4. 缺失MPB与正常MPB必须共用相同的亮度/时间包络。回退只改变颜色输入，不改变破口位置、透明度、粒子数量、角速或退场时间；恢复有效采样后不能把上帧映色再次乘入原值。

这个范围是验证起点，不是最终美术常量。选择依据是 candidate-13b 的 `.82/.78/.64`宽体在暗底仍近黑，而 candidate-11白金帧已有可接受白锋/淡金层级；将宽体提高到约1.0–1.2可使`W`偏低区域获得可见暖白，同时远低于会把主体整体推成HDR油箔的宽面高亮。实际画面若在 `W≈1`区过曝，应优先回落宽体而不改mask。

### 必看帧与回归条件

在后台Editor使用与candidate-13b相同的固定相机、Gamma/D3D11、seed和120Hz时钟，同时输出正常Animator与`DiagnosticForceMissingMpb`两组：

- **第一拍：17ms（f=1）**。暗底和中底都必须看到连续的暖白/淡金宽体，不应只有一条白边；起刀破口和主锋方向不变。
- **峰值：50ms（f=3）、100ms（f=6）**。宽体暖白层级保持，窄锋更亮；`MainColor`落在上述受控范围，`OutColor`单独记录，不能用白色整片替代。
- **退场：167/283/500ms（f=10/17/30）**。宽体随原侵蚀和退场明显减弱，暗底仍保留少量白金细丝；不得持续一大片同亮面。

回归快照必须包含每个Renderer的：`isEmpty`、字段有效性、材质fallback、采样前RGB/A、主题后RGB/A、`MainTex/Noise/ColorNoise`引用及Shader名。通过条件是：正常MPB与缺失MPB在首拍和峰值都不发黑；Main1宽体确实变暖，Main2仍为独立窄层；alpha/mask/破口和时序逐帧一致；同seed不累乘变暗。仅看到Shader supported、MPB存在或白锋变亮，不足以证明宽体问题已解决。

本节不改变Slash11已接受的几何/动线或其它动作时间合同；它只给native_visual处理 candidate-13b 宽体黑化的最小颜色映射面。完成后需重新提供暗/浅底早/峰/退场帧和快照，主线程再决定是否进入发布候选。

## candidate-13b 视觉复核补充：回退后仍需受控的宽体白金

主线程亲看 candidate-13b 暗底早期与峰值：白锋在，宽体仍近黑。此前“缺失 MPB 时回退”已经能让 Main1 输出 `Main=(.820,.780,.640,0)`、`Out=(4.909,4.545,3.773,0)`（见 `evidence/slash-candidate-13b/no-mpb-diagnostic/metadata.txt`），但这组宽体值仍不足以让 MainTrail 的主体在实际背景上可读。该结果把诊断从“仅缺失字段”推进为“缺失字段回退 + 主题宽体映色过弱”两部分。

### 责任不在 Main2 黑轮廓或 alpha

candidate-13b 同帧的 Main2/Outline 已有 `Main=(3.308,2.595,1.555,0)`，视觉上主要贡献窄边，因此不是大面积宽体发黑的来源。MainTrail 片元方程为：`rgb = OutColor*E + MainColor*W`，`E`是窄锋阈值，`W=saturate((1.5*ColorNoise.r)^3)`是宽体填色；当 `ColorNoise.r`低时，`.82/.78/.64`的MainColor会进一步被W压低。主体轮廓和破口仍存在，说明不是粒子alpha门限把整片裁掉，而是宽体RGB能量不足。

两种颜色属性的alpha在该原版MainTrail片元中不参与RGB；不能把`alpha=0`改成1作为修复。保留MainTex.r覆盖、Noise/ColorNoise、粒子COLOR.r和原始侵蚀，才能保持Slash11已经满意的断口和锋势。

### 最小主题映射

实现owner只需改Main1宽体主题输入，保留每帧采样和字段级fallback：

1. 采样到有效、非零的原`nativeMainColor`时，以它做小幅暖白映射；不要把`nativeEdgeColor`无条件当宽体来源。
2. 原版合法状态为`nativeMainColor=0`而`nativeEdgeColor`有效时，零值只代表原版暗体角色，不代表白金主题必须黑。给Main1一个**每帧受控的非零白金宽体基色**，建议RGB在`(0.95–1.20, 0.90–1.14, 0.74–0.94)`；峰值短段可到约`(1.30,1.20,0.98)`，退场按原侵蚀包络降至约`(0.65,0.60,0.48)`。这组范围针对宽体，不是把OutColor或整张材质提到HDR白板。
3. 没有有效MPB时同样使用克隆材质/角色的白金fallback，不能继续使用安全edge颜色乘`.82`后得到`.82/.78/.64`作为唯一宽体。`OutColor`仍独立保持更窄、更亮的刀口；Main2继续按自己的采样与fallback处理。
4. 映色只改变RGB职责，不改alpha、mask、破口、PS COLOR.r、Noise采样或角速。保持“恢复原值→Animator.Update→捕获→映色”的单时钟顺序，避免下一帧把白金结果再乘一次。

该范围是最小验证起点：它足以在`W`偏低的宽体区域产生可见暖白/淡金，又低于r4/07那种持续油箔宽面。若`W≈1`区域过亮，先把宽体回到1.0附近，不动MainTex或mask；若仍黑，优先记录实际`ColorNoise.r`和Renderer颜色字段，再判断是否是资源Shader变体问题。

### 必看帧

沿用 candidate-13b 的同相机、Gamma/D3D11、seed和120Hz时钟，输出正常采样与`no-mpb-diagnostic`两组：

- **17ms（f=1）**：暗/中底均应看到连续的暖白/淡金宽体，不能只有边缘白锋；`MainColor`需达到上述宽体下限附近。
- **50ms（f=3）、100ms（f=6）**：宽体保持可读但不整片过曝，OutColor更亮且更窄；Main2不能被用来填宽体。
- **167/283/500ms（f=10/17/30）**：宽体按原侵蚀退出，剩下少量白金细丝；不能因fallback常量而整段同亮。

通过条件：正常MPB与缺失MPB都不发黑；Main1宽体在f=1已变暖，Main2仍为独立窄层；原始裂口、alpha/mask和退场帧逐帧不变；同seed不累积变暗。技术`shader.isSupported`和颜色字段存在不等于达到该视觉条件。
