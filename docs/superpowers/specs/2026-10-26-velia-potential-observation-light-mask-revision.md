# 潜力观测：以真实光源感为目标的 mask 修订

2026-10-26，用户否决R5：「一点不像光源，更像飘散的海草。你不会做mask吗？不会的话网上下载开源素材」。此前视觉PASS失效。本修订替代flow-brush/金色卷丝设计；接受的S2、微笑、掌心挂点和玩家/敌人播放合同继续保持。

## 已观察的缺陷

直接检查R5 `canonical_033.png` 和当前 `VeliaObservationParticle.shader`：外围是可辨认的细卷曲实体，中间是叠在身体上的白色纱线。Shader把彩色RGBA笔触乘粒子颜色再做Alpha混合，保留了笔触的材质边缘。提高 gain 只能令海草发亮，不能建立光源。

根本修正：**贴图只提供标量覆盖/能量，不再提供彩色物体外观。** 中心暖白能量、外围淡金散射及动态遮罩分别决定显示；一个连贯的上涌光场承担主体，稀疏粒子只解释散开。不要再沿用错误的有机卷丝图集维持所谓一致性。

## 实际下载、许可与筛选

先下载至仓库之外 `C:/Users/rog/AppData/Local/Temp/velia-mask-reference-download/`，解压并检查实际PNG和包内许可，然后保存研究副本到 `output/velia-potential-observation/mask-research/`。未改生产资源。

两份官方来源均明确为CC0；包内License.txt也已读取。CC0允许复制、修改及分发，包括商业使用，不要求署名；发布仍保留来源与许可，避免以后失去溯源。

- [Kenney Particle Pack](https://kenney.nl/assets/particle-pack)：`kenney_particle-pack.zip`，包内标记1.1。
- [Kenney Light Masks](https://kenney.nl/assets/light-masks)：`kenney_light-masks-1.0.zip`，包内标记1.0。
- [CC0 1.0正文入口](https://creativecommons.org/publicdomain/zero/1.0/)。

研究目录中的 `provenance.json` 记录实际下载URL、ZIP hash、每张入选源图hash、采样通道与原路径；`particle-pack/License.txt`、`light-masks/License.txt` 保留原文。

**实际看图后的选择：**

| 入选副本 | 原始文件 | 视觉选择理由与用途 |
|---|---|---|
| `selected/surge-mask.png` | `light-masks/Default/cone_d_blur.png` | 下部窄、向上展开，亮度向外/上方自然衰减，边缘软。用于连贯光涌的基础标量形状，不直接当一张静止探照灯贴画。 |
| `selected/soft-source.png` | `particle-pack/PNG (Black background)/star_05.png` | 软圆热点，弱柔射线，无同心圆。用于白核附近低频辉光。 |
| `selected/small-streak.png` | `particle-pack/PNG (Black background)/trace_01.png` | 短直软光迹，没有卷曲轮廓。仅少量上升散光，尺寸必须很小。 |
| `selected/breakup-reference.png` | `particle-pack/PNG (Black background)/smoke_01.png` | 不规则标量灰度；只能作为低对比度能量扰动/退散遮罩，不把它作为可见烟球发射。不是无缝纹理。 |

拒绝：Particle Pack light_01/02/03有同心环，fan_*是可辨扇叶，trace_02–05又引入弯曲实体。它们的文件名像光并不代表适合本需求。

查看 `selected-mask-contact.png`：每行包含原始标量、拟议加光公式在暗底/中灰底的诊断色样。它证明素材的明暗能力，**不是Unity效果预览或成品PASS**。完整筛选联系图为 `light-candidates.jpg`、`particle-masks-contact.jpg`。

## 素材与shader合同

从入选副本复制生产用mask，不重新ImageGen。保留原图和SHA；必要导入变换在manifest明确记录。全部入选黑底版**采样R通道**，不要把黑底图整张alpha=1当覆盖率，也不要将RGB再乘alpha造成二次衰减。Texture Import：linear/sRGB=false、Clamp、Bilinear、无mip、无压缩。基础mask按原尺寸保存，不必重画。

**主线程看过素材并批准后冻结的实现：保留现有prefab/API，只留三个角色，不再要求四系统。** `BodyRoot/CoreFlow`为单粒子定点quad光场；`BodyRoot/SoftLift`为单粒子软热点；`BodyRoot/Glints`为少量真实上升散光。删除本效果的GoldWisps实例/材质依赖，构造校验同步改为三个角色。

- `CoreFlow`：一个主要光涌载体（最多加一个弱且短的延迟脉冲），使用surge mask，输出白色热核与金色边缘能量。
- 金色外辐射合入Core同一个shader采样，使用更宽的UV覆盖或较低幂次、低能量；**不再发射GoldWisps卷丝**。
- `SoftLift`：一个软热点/低频光晕，使用soft-source mask，固定在下腹/身体下部附近，淡出而非漂成烟团。
- `Glints`：少量small-streak粒子，6–10个已足够，只有少量短流迹解释上升与扩散。

不能再随机发射20张彩色大笔触作为主体。真实ParticleSystem仍用于quad、生命周期、颜色和尺寸曲线；主体是光源场，允许一个定点遮罩载体内推进光流。无需新增框架、物理灯或依赖后期Bloom。

shader推荐明确采用 **Blend One One**，RGB中完成一次premultiplication，透明处输出0。示意职责：

```text
m = saturate(sampleR(shapeMask, warpedUV))
coverage = revealMask * bottomFade * dissolveMask * particleAlpha * faceProtection
core = pow(m, 1.5) * whiteEnergy
halo = pow(m, 0.65) * goldEnergy
rgb = coverage * (core * warmWhite + halo * paleGold)
```

也可用SrcAlpha One，但必须对应地把coverage放alpha、禁止重复乘coverage。按当前共享材质实例处理习惯保持MPB独立，模板不写。使用half/float保留高于1的短暂能量；不要在shader内提前saturate颜色。暖白 `(1,.98,.90)`，淡金 `(1,.78,.36)`，外围更低能量而不是棕绿实体。底部/中心peak可短暂饱和白，外围仍有渐变。起点白核energy约1.3–2.0，金halo约.20–.40，背景/重叠决定最终值，必须看本候选native画面调节。

严格禁止把已烘焙彩色丝带RGB继续送进新光源材质，也禁止把整个噪声图平铺显示成亮斑网格。alpha mask不是装饰术语，必须实际调制透明度/能量。

## 连贯喷涌的形状与时间

H仍为角色站高，BodyRoot实际脚点与一次H缩放保持。主体载体宽约.55–.75H，高约1.25–1.4H，下边在脚底稍上；中心横向对准身体而非武器。源mask原方向就是下窄上宽，确认UV方向不颠倒。可保留对目标面部的保护，但遮罩须柔软，不能硬切一条黑色横带。

主要时间不变，Age从播放开始累计：

1. **.16–.30s**：身体下部出现明显小白热源与淡金辉光，光涌高度从约.2H增长。不是整张图淡入。
2. **.30–.58s**：软推进前沿从下向上跨过身体，最终越过头顶。使用y方向reveal，前沿羽化约.10–.18H；前沿带约.15–.25H宽的能量加强。白核首先读成一股光从身体内部涌出。
3. **.58–.90s**：上部宽度展开约20–35%，白色能量下降，外层金光保留；基部逐渐关停。不要突然把根部向上平移，也不要剩下一个白色椭圆帽。
4. **.90–1.35s**：整个光场能量下降同时发生不均匀侵蚀；薄金光片/小光点向外上方分离，1.45s后为空。

动态要点：基础mask维持稳定坐标，不随剩余高度重新压缩；两层不同尺度的低幅度UV扰动（约.015–.035 UV）沿y向上平移，打破完美直边。可直接确定性生成无缝标量noise用于panner；下载smoke_01只作参考或Clamp下有限偏移，不声称它无缝。峰值阶段noise只扰动15–25%，**不能挖掉多数白核变回丝草**。主要穿孔发生在退散阶段：阈值由低升高，使光团解体，能量随之归零。mask front与能量peak共同上行，才不是静止手电筒光束。

冻结为：Shuriken主载体单次发射1粒、零velocity、寿命1.25s，靠shader Age进行连续光流；soft-source1粒；Glints 6–10粒寿命.3–.55s，向上速度约1.5H/s逐渐减速，横向.1→.5H/s，宽.008–.02H、长.04–.12H。短粒子亮度次于主体，不用长拖尾、不下坠。此前数量与贴图随机格要求不再适用。主光场不再用额外寿命alpha把整体压暗，shader envelope与particlealpha只各承担明确的一次职责。

生产mask目录建议 `source_assets/light-masks/`：保持 `surge-mask.png`、`soft-source.png`、`small-streak.png` 三个原字节副本，附上述provenance及两份License。保留材质键 `VeliaObservationFlow`、`VeliaObservationMist`、`VeliaObservationGlint`，分别绑定三个mask；旧彩色图集不再进入本候选bundle依赖。可选breakup只以灰度mask使用，不是必需第四素材。`VeliaObservationMaterial`仍只服务已接受掌心。

在现有 `VeliaPotentialObservationVisual.Advance` 的唯一时钟基础上，将 `_Age` 通过实例MPB传给需要动画mask的renderers；保留手部绘制不变。`SetAnchors` 继续传BaseY/Height，必要时全体光场renderers使用相同保护信息。现有manual Simulate/Pause/0dt同步及Host事件不改。

## 本次验收门槛

先让主线程查看已生成的selected-mask-contact与来源许可，再做一个真实素材+shader的本候选native测试。背景必须各有一套暗底与中灰底；不能只在近黑背景靠对比度伪装发光。

PASS必须同时满足：

- 第一眼是角色身上涌出的光源：小范围近白热点，向外淡金散射，亮度随距离明显衰减；中灰背景仍然是发亮，不是覆盖一块灰白物质。
- .30/.55/.90三个阶段读出下→上的推进、上方扩散与根部熄灭；不是静态锥形探照灯或整张图简单缩放。
- 大部分能量连成整体，细光屑是次要层；无海草边线、绸带、实体烟团、五根管、扇叶、光圈、方框。
- 目标脸及关键轮廓仍可读；非目标角色不被挂光；已接受姿势不变。1.25s尾光稀少、1.45s彻底清空。
- 全效图之外检查light-only帧：应见中心到外侧连续衰减，而非仅细线有颜色。记录native材质blend/通道/纹理readback，确认实际用了mask。

禁止仅因为使用CC0素材、真实PS或shader里出现mask字段而宣告完成。最终依据是用户此次明确的光源观感；先前任何R5技术或视觉报告不能覆盖这次否决。

## R7 后的主线程动态修订

R7 已建立加光与柔和衰减，但白亮热点停在下部，只有上方暗锥被揭开，独立评审判为手电筒感。后续保留三张原始 mask 和现有接口，改由 SoftLift 的单粒子承载主要上升亮头：从 0.10H 出发，Y 速度随生命周期从 1.8 衰减到 0.3H/s；横向尺寸约从 0.6 倍展开到 1.4–1.5 倍，纵向保持紧凑。亮头保留到展开期，在 0.75–1.35s 消退。CoreFlow 原地光场降为较弱的尾光，不能继续抢占最亮区域。对应能量为 CoreFlow 白/金 0.16/0.18，SoftLift 0.85/0.28；禁止以全场加亮替代能量上移。白核采用近中性的暖白，金色独立留在外围。

本条替代上文“SoftLift固定下腹”的初始建议。新增的决定性验证是实际亮区重心和宽度随 .30/.55/.90s 上升、展开；仅有低亮度前沿增高不再足够。已有角色、手部光点和玩家/敌方时序保持不变。
