# R5 云函数适配：造形、侵蚀与无带纹光照

2026-09-06，交Carson实施、主线程审定。仅纠正[R5规格附录B](2026-09-06-slazeya-storm-cloud-ring-design.md#附录-b宏观体积云承载纠偏覆盖卡片主体限制)的密度／采样函数；10个宏观体、排序／正交射线、内孔、24px画框、同向自旋／显式时钟、收核与原瀑布不重做。允许烘焙数值Texture3D，覆盖此前禁3D纹理的技术起点。本次仅写此设计，未改生产代码、资源、构建或部署，未派代理。

当前读取的是R5.5 manifest（Bundle `3E301BCD…`），不是已通过视觉验收的候选。`cloudDensity` 已有扭曲包络与边缘变化，但仍以平滑椭球包络构形、`lerp(0.6,1,field)`保底；噪声主要调制一个已有球块。`frag` 的每四步更新 `targetLight`、再 `lerp(light,targetLight,0.55)` 存在入云光照滞后，不能靠增加sigma或改颜色判定带纹已修。

## 1. 已读函数 → 本地落点

外部目录为 `C:/Users/rog/.codex/tmp/slazeya-cloud-references`。已读下列函数本体与许可；URL／完整commit／SHA沿用主线程 `reference-files.json`，不另选版本。

| 参考函数 | 直接适配职责 |
| --- | --- |
| stegu `classicnoise3D.glsl::pnoise(P,rep)`、`fade/permute/taylorInvSqrt` | 移植为离线bake的周期经典Perlin。当前本地文件在 `stegu-webgl-noise/classicnoise3D.glsl`，不是本地src子目录；文件头版本2024-11-07。GLSL `mod(x,y)`须用`x-y*floor(x/y)`，不能直接替换成负数行为不同的HLSL fmod；保留五次fade、梯度选择与2.2输出尺度。 |
| SebLague `NoiseGenCompute.compute::worley/CSWorley/CSNormalize`（`fcc997c…`） | 适配固定seed特征点、周期邻胞／环绕距离与归一化，只在Builder运行；运行时不做27邻胞搜索。 |
| sebh `main.cpp` 的Perlin-Worley组合／packed remap／detail配方（`1f879a…`） | 采用其`remap(P,0,1,W,1)`和不同频率Worley数据分工；不复制依赖GLM／Shadertoy的`Cells/noise/PerlinNoise`底层代码。其平方距离Worley与Seb的F1距离也不能不经归一化直接互换。 |
| SebLague `sampleDensity` | 变为`SampleCloudBase`＋`SampleCloudDensity`：覆盖／高度先构造base，独立detail以`(1-base)^3`侵蚀边缘，允许密度变为0；移除全域0.6正下限。 |
| SebLague `lightmarch/hg/phase` | 向光积分同一云密度并使用HG角分布；替换滞后光缓存，不移植全屏、_Time、全局深度或新Light。 |
| Graphics `EvaluateCloudProperties/DensityRemap/PowderEffect`与Worley工具（`a7e4c05…`） | 仅对照“先base、再侵蚀、光采样可简化”的分工；本轮不移植Companion许可的HDRP函数、LUT、宏或PowderEffect，不用额外粉末高光掩盖密度缺陷。 |

## 2. 一次离线烘焙，两张数值体纹理

在现有Builder旁增加一个 `SlazeyaStormCloudNoiseBaker.cs`，调用本效果专用compute完成烘焙／回读；产物写 `Assets/Textures/CloudVolume/CloudShape64.asset`、`CloudErosion32.asset`，材质引用后随同一个Bundle打包。均用线性RGBA32、Repeat、三线性采样和mip链，未压缩体素约1.125MiB，含完整mip约1.29MiB。不生成PNG，不在游戏启动或逐帧生成噪声。

固定seed与配方版本；体素坐标取中心 `(id+0.5)/resolution`，不把首尾两层强行复制成相同体素。周期性检查应比较函数／过滤采样在`u`和`u+1`处一致及跨缝邻域连续。bake及归一化只做一次，记录体素字节hash、尺寸、通道含义／范围与seed，避免换硬件或改源码后误用旧资产。

| 资源／通道 | 明确配方与尺度依据 |
| --- | --- |
| Shape64.R | `P=saturate(0.5+0.5*normalizedFBM(pnoise))`，频率4/8/16、振幅1/.5/.25，`pnoise(u*f+seedOffset,float3(f,f,f))`；`W=.625*W4+.25*W8+.125*W16`，`PW=lerp(W,1,P)`。Wn是归一化反向F1，近特征点高密度。 |
| Shape64.G/B/A | 分别存W4/W8/W16。与sebh原通道打包方式不同，故本地语义必须固定，不能又当RGB法线或覆盖alpha读取。最高16胞／64体素留4体素／胞，避免32胞仅2体素产生粗糙格纹。 |
| Erosion32.R/G/B | 独立seed的W2/W4/W8，A留1；细节取`.625R+.25G+.125B`。最高8胞／32体素同样留4体素／胞。 |
| 采样周期 | 主形纹理初值每4H重复一次，因此W4/8/16分别形成约1/.5/.25H云瓣；detail每1.2H重复，形成约.6/.3/.15H卷边。这些尺度来自云体切向2H、径向约.8H的现有尺寸，不按屏幕像素猜频率。 |

保留共享 `cloudCarrier` 返回的H尺度三维Q和实际带符号自旋／密度时钟；`shapeUV=Q/4`，`detailUV=Q/1.2+DensityPhase*(.015,-.025,.010)`。detail比主形略有差速，仍是全体共享空间，禁止逐proxy随机平移体纹理造成接缝。runtime新增 `_CloudShapeTex`、`_CloudErosionTex`、`_ShapePeriodH=4`、`_DetailPeriodH=1.2`、`_Coverage=0.82`、`_ErosionStrength=0.30`；其他光／消光／时钟MPB沿用。

## 3. 运行时密度：让噪声真正决定哪里有云

保留现有未扭曲的`e_i`、`E=1-product(1-e_i)`、`owned/total`分摊与proxy硬支持边界。删除现有`warpedEnvelope/rolls/body>=0.6`造形路径，换下式；E仅参与支持／覆盖，不再乘一个永远为正的场作为最终云形。

```text
tex = sample Shape64(shapeUV, explicitLOD)
lowW = dot(tex.gba,(.625,.25,.125))
shape = saturate(remap(tex.r, -(1-lowW), 1, 0, 1))
cov = Coverage * E;  if(cov <= 1e-4) return 0
base = saturate(remap(shape*heightMask, 1-cov, 1, 0, 1))
if(base <= 0) return 0                    // 不再读取detail
detail = dot(sample Erosion32(detailUV,detailLOD).rgb,(.625,.25,.125))
rhoUnion = max(0, base - (1-detail)*pow(1-base,3)*ErosionStrength)
rhoOwned = rhoUnion * owned / max(total,1e-4)
```

`heightMask`采用Seb的上下渐变思想：各体`h=.5+.5*dot(p-center,axisYInverse)`，下缘0–.12、上缘.82–1之间smoothstep，中间保持1，按e_i加权得到共享高度mask。它比参考大天气盒的.2/.7保留更宽云腹，适合现有1.0–1.4H高的局部云墙。`remap`分母统一保护，但不要用epsilon制造本应为空的密度。

0.82覆盖是稠密环的起点：中腹E≈1时阈值0.18、外缘E≈.5时阈值0.59，因此同一噪声会在外缘造出实质凹凸／空隙。不是把Coverage直接乘最终alpha。侵蚀0.30时，base=.8的损失最多.0024，base=.2可损失.1536，保护云腹而咬掉薄边；仍不够自然时先检查base/detail实际值及尺度，不能继续提高alpha。沿原sigmaT约4.5，典型.8H路径、平均密度.5–.7对应光学厚度约1.8–2.5，足以维持厚暗连续主体。

仅在收核末段`Collapse=.95–1`平滑把侵蚀减到0、造形密度混向受限E，让已缩进来的宏观体填成实心小核；不用重新启用水泡。核的proxy角点／.06H核心／.12H完整支持、Charge、等待和B后瀑布交接保持原合同。

## 4. 光照与带纹：先建立正确采样基线

1. **定位而非猜色：** 同一原尺寸帧输出光学厚度／T（无散射光）与正常光照。T里已有同心带，查视线步长／纹理LOD；仅正常光出现带，查lightmarch及缓存；先不加Powder、噪声抖动或提高sigma。
2. **视线改固定世界步长：** 起点`ds=H*当前均匀缩放/24`，单体最长约2H路径最多48步，末段使用实际剩余长度，保持中点积分。不要为每像素按弦长重新切成固定32段。非均匀局部D不normalize、正交射线、前表面ZTest、预乘积分继续沿用。
3. **直接光采样作为本轮验收基线：** 每个`rhoOwned>epsilon`的视线采样点立即计算向光3点积分，首次非零点也立即更新；删除`step%4`与`lerp(light,targetLight,.55)`。方向光采样用同一base场，保留3个世界步长中点；可先用base省掉detail读取，若与完整侵蚀密度有明显阴影差异再在光采样也读detail。两张体纹理已替代昂贵程序多octave，因此先以正确路径实测性能，不先恢复旧缓存。
4. **尺度与LOD一起处理：** 每次3D取样显式LOD，按`log2(max(1,本次世界步长/对应体素世界尺寸))`起算并考虑当前carrier缩放；主形体素约4H/64、detail约1.2H/32。光步长较大应读较粗mip，不能强制全用LOD0。避免循环内隐式导数；采样路径过长须增加有界步数，不能跳过尾端云体。
5. **HG只补方向性：** 用Seb的`hg`，`mu=dot(cameraToSampleDir,dirToLight)`，初值`phase=4*pi*(.8*HG(mu,.35)+.2*HG(mu,-.2))`，再乘Tlight；4π使各向同性g=0时回到1，维持现有光强尺度。保留低环境填充和原局部双闪，不叠旧`pow(light,1.25)`及滞后滤波。性能记录绑定新候选，不能用R5.5耗时证明新路径。

## 5. 有界落点、来源与可见接受点

- **代码面：** Builder增加一次bake／回读／材质绑定；新Editor baker、专用 `CloudNoiseBake.compute` 及保留许可的Perlin helper；在现有 `SlazeyaStormCloudNoise.cginc`／Volume shader落`SampleCloudBase/SampleCloudDensity/lightTransmission`与积分修改。两个Texture3D `.asset`及meta入包并记录hash。Controller只补必要参数与清理接线，两份镜像一致；不改变宏观位置、manager、SFX、G1.35／Tail1.20。
- **来源交付：** 实际移植stegu pnoise及Seb Worley／density／HG代码时，保留作者、完整MIT文本、来源函数／文件／版本及主线程URL/hash清单；stegu新文件也加入该清单。若借用了Seb的`Noise.compute`，还须保留其内嵌Ashima／Stefan Gustavson MIT与Lex-DRL署名；本推荐直接用stegu pnoise，不需要该simplex文件。sebh只参考组合配方并记来源，不移入GLM／Shadertoy底层；HDRP仅作设计对照。许可文本随源码与最终分发携带，不能只留仓库外临时目录。
- **验收仅补关键证据：** ①bake数值周期连续、线性格式／mip／Bundle回读hash正确；②同一已固定体积切片并列base、侵蚀后密度，能看到正密度确实降为0，厚腹仍连续；③原尺寸初环和侧视，关闭168粒子仍为厚暗连续云墙，边缘有大云瓣与小破口，不能看出10个光滑球／枕头；④同一帧T与完整光照、邻近3帧无同心带／阶梯阴影，切换到48步只是细节收敛，不能换一套环带图案；⑤沿用内孔／24px、同向平流与自旋、G末／B0核心支持、实际回调／瀑布／双闪／SFX清理检查。

调参顺序固定为 **base占据形状 → detail边缘侵蚀 → 光学厚度与光采样**。噪声材质读到了纹理、shader编译通过或参数更大均不是接受点；结果必须同时保有大体积连续云腹与自然破碎云边。新候选使用新hash／round5 review目录，旧冻结证据不覆盖。

## 原生输入诊断后的有界校准

R5.6已核实140个GPU输入点：Shape实际R约0.554–0.922、视线LOD0，GPU与Bundle体素过滤最大误差约0.001458，未采成默认白或单一均值。Coverage0.30–0.60与SigmaT4.5/7.5的8组诊断仍不能消除代理球块；0.30时侵蚀归零约4.51%，0.82初值仅约0.77%。这些均不是通过候选。

主线程批准下一次限定比较直接以Shape64.R作为Perlin-Worley云形，取消额外的packed低频重映射（本地R已完成PW组合，不必叠加另一种通道配方）。固定Coverage0.25/0.30/0.35、SigmaT7.5、ErosionStrength0.50，保持几何与其他时序；用原生主/侧视和切片检查。另补同一侧视的T图区分光照与积分台阶，不能仅凭主视T断言问题已排除。仅在有实际合格画面后确定最终值。
