# 潜力观测：标准 Unity 粒子制作流程修订

2026-10-26，响应用户「请参考标准unity特效制作教程来做」「比如生成四方纹样和光影那些」。本修订替代旧设计的五条 mesh 光瓣及其 cap/noise 修补路线。保留已接受 S2、美术比例、微笑、右掌挂点、目标选择、敌方投骰后时序与生命周期。**不恢复原设计的 .12H 手顶间距要求。**

用户举例解释为：先制作方形画布中的纹样、透明度和明暗贴图，再配置 Unity 材质与真实粒子运动。没有授权增加可见的方形符文、魔法阵或方块轮廓。

## 教程与实际采用内容

已读仓库 Guide14 的逐层视觉职责、原生重放、材质所有权、单时钟和零时长粒子同步规则；特别遵守其对实心金针、规则几何与固定系统模板的否定。源码参考仅为 `ChristashaNativeParticleLibrary.cs:482` 附近 `MakeSupport` 的真实 ParticleSystem 模块配置，以及 `:805–824` 的 Simulate/Pause/零时长同步；不复用该角色攻击轮廓。

| 实际阅读来源 | 本次采用的技术 |
|---|---|
| [Unity Learn: Particles — Texture Sheet Animation](https://learn.unity.com/tutorial/particles-texture-sheet-animation-1)（页面标记2019.2，另与下项2019.3模块核对） | 先制作四格方形贴图，再建材质、ParticleSystem，随机选格而非复制多个同形 mesh。四格用于四种小笔触，不是整套特效四帧截图。 |
| [Unity 2019.3 Texture Sheet Animation](https://docs.unity.cn/cn/2019.3/Manual/PartSysTexSheetAnimModule.html) | Grid 2×2；每粒子保留随机起始格，避免同步图案复制。 |
| [Unity 2019.3 Standard Particle Shaders](https://docs.unity3d.com/2019.3/Documentation/Manual/shader-StandardParticleShaders.html) | 区分渲染混合与颜色混合：软主体使用透明混合，少量发光使用 Additive；通过贴图明暗保留热点，不把所有像素乘同一金色。 |
| [Unity 2019.3 Renderer](https://docs.unity3d.com/2019.3/Documentation/Manual/PartSysRendererModule.html) | 真 Billboard 贴图卡片；主体不用长距离 Stretched Billboard，避免重新拉成长针。后者即使 Velocity Scale=0 仍朝速度方向，故不能误当普通正立 Billboard。 |
| [Unity 2019.3 Color over Lifetime](https://docs.unity3d.com/2019.3/Documentation/Manual/PartSysColorOverLifeModule.html) | 粒子从低透明度进入、短暂亮起、淡金退散，末端 alpha=0。 |
| [Unity 2019.3 Velocity over Lifetime](https://docs.unity3d.com/2019.3/Documentation/Manual/PartSysVelOverLifeModule.html) | 初期向上速度强、后期水平展开加大，局部空间明确。不是仅把一整张光柱从下往上揭开。 |
| [Unity 2019.3 SizeOverLifetime API](https://docs.unity3d.com/es/2019.3/ScriptReference/ParticleSystem.SizeOverLifetimeModule.html) | 独立尺寸曲线控制出生、展开与散去。英文2019.3手册该页抓取失败，以版本API及已存在项目用法核对，不声称读到失败页面内容。 |
| [bkjam: Creating Wrap Around Textures With Krita for VFX](https://bkjam.github.io/posts/2024-11-10-creating-wrap-around-textures-with-krita-for-vfx/) | 亲自制作的贴图教程：方形文档、底层与画层分离，用不同尺寸软笔分层建立明暗，再用 smear 调整流动纹理；平铺纹理用 wrap-around 检查接缝。采纳绘制方法，素材重新原创。单粒子贴图使用透明留白，不需要平铺。 |

这些链接支持制作方法；下列数值是本效果设计起点，需通过实际渲染验证，不伪称来自教程。

## 第一步：独立贴图资产与可审查图板

所有文件位于 `SteriaBuild/VFXSource/VeliaPotentialObservation/source_assets/`。

1. **`flow-brush-atlas.png`：1024×1024 RGBA，2×2，每格512。** 四种独立向上流动的短笔触：一道略弯主束、两股分叉束、带细孔破口束、断开的侧卷。每格主体占宽35–60%、高60–75%，四边至少12%透明留白，边缘退成细丝，顶部有不等长碎边。主亮处暖白，宽度有变化的低亮度淡金区域和细外缘交错；明确画出内部透明负形和柔软暗隙。不要中央白色竖杠加两条均匀金边，不画管状明暗，不画完整喷泉、角色或背景。
2. **`mist-breakup.png`：512×512 RGBA。** 一小团向上拉开的不对称半透明流雾，有2–3处大负形、少量细孔、边缘灰度变化。整张不是实心白云圆盘；不需要实物烟的灰黑阴影。用于低亮度空气感，不能遮住角色。
3. **`glint.png`：128或256方形RGBA。** 一个细小、不等长的四向白色闪点，柔弱金晕、透明四角。可确定性绘制，不使用大实心菱形。
4. 可选 **`breakup-noise.png`：128/256方形线性标量纹理**。仅在基础贴图粒子仍有明确的整片滑动缺陷时加入；不是继续扩写先前大 mesh shader 的前提。

有机 brush/mist 推荐 built-in imagegen，输入仅为上述局部笔触美术说明，透明背景；保留提示词、原始输出及最终来源 hash。glint/glow/标量 noise 可确定性生成。不能用纯随机噪声代替需要绘制的流动轮廓，也不能把生成的整张喷泉概念画贴在一张卡片上。

在完整 Unity 场景之前，输出 `texture-contact-sheet.png`：原色 over 黑 / 中灰，各格 alpha 灰度，亮度灰度，按约64px高度的缩小观察。图板先由主线程看是否是流纹与明暗层次。技术上具备 PNG 或随机噪声不等于满足这一步。

导入：RGBA32，无压缩、无 mip、Bilinear；brush/mist/glint Clamp，颜色纹理 sRGB；alpha来自输入。atlas必须每格留足透明边，不以Repeat跨格采样。标量noise若使用则线性+Repeat。用实际Unity readback确认透明边无底色方框、明暗未被错误alpha乘法压黑。

## 第二步：冻结资产接口

沿用 bundle 键 `steria_velia_potential_observation`，新增 prefab 键 **`VeliaObservationParticles`**：

```text
VeliaObservationParticles
  BodyRoot
    CoreFlow    (ParticleSystem + ParticleSystemRenderer)
    GoldWisps   (ParticleSystem + ParticleSystemRenderer)
    SoftLift    (ParticleSystem + ParticleSystemRenderer)
    Glints      (ParticleSystem + ParticleSystemRenderer)
```

prefab只包含 Transform/ParticleSystem/ParticleSystemRenderer，无 MonoBehaviour、Animator、物理灯、角色资源和预览组件。这四层分别承担流束、外散、软光、碎光；数量不是审美目标，若一层无贡献应删弱而非强行看见。

材质键：`VeliaObservationFlow`、`VeliaObservationGoldWisp`、`VeliaObservationMist`、`VeliaObservationGlint`。实际贴图和shader为prefab依赖，由同一bundle导出。优先 Built-in `Particles/Standard Unlit` 的适配模式；在2019原生回读中验证shader支持和实际blend状态，不能仅设置 `_Mode` 数字却漏keywords/blend。允许很小的纹理采样粒子shader替代，但不是回到纯vertex-color绘图。

运行时调用契约：

```csharp
new VeliaPotentialObservationVisual(GameObject particlePrefab,
    Material handMaterial, int layer, int sortingLayer, int order)
```

现有 `VeliaObservationMaterial` 继续仅服务已接受掌心小mesh光芒。保留 `SetAnchors` / `Advance` / `HideHand` / `Dispose` / `Age` / `Complete` / Duration=1.45 / PoseDuration=.90，不改SpritePoint、S2、目标或触发回调。这里只替换目标的旧Fountain/Spray mesh。旧VertexCount诊断应改为真实ParticleCount并更新其直接预览消费者，不制造虚假粒子计数。

host-owner更新加载prefab及构造调用；builder-owner产出上述确切键并更新预览调用。材质、贴图、prefab实际传入预览和游戏，不各写一份近似配方。

## 第三步：Shuriken配方

H为目标现有standingHeight，BodyRoot以H统一缩放一次，其localPosition置于实际身体脚点，不用含武器alpha框横向中心。粒子坐标以下用H归一化。`simulationSpace=Local`，`scalingMode=Hierarchy`，`playOnAwake=false`，`loop=false`，固定randomSeed，禁止自动random seed。所有系统 Billboard，z深度很薄，粒子卡片在角色后；GoldWisps/Glints最多少量边缘碎光在前，不覆盖面部。

| 系统 | 纹理/混合 | 发射与寿命 | 运动和大小起点 |
|---|---|---|---|
| CoreFlow | flow atlas；透明混合，保留纹样白核淡金外缘；不要全层强Additive | .16–.56s，三次错峰小burst共约16–22粒；寿命 .60–.80s | 起点x±.07H、y .04–.18H；y速度2.4→1.5→.45 H/s；x速度出生±.04、后段±.40 H/s。卡片初始宽.22–.34H、高.45–.65H，纹理内部留白继续生效；角度±12°，不能横躺。 |
| GoldWisps | flow atlas同源细裂变体；淡金Additive，显著低于白核峰值 | .24–.62s，共约12–16粒；寿命 .52–.72s | 起点窄根、y .1–.25H；y2.2→.6H/s；x方向随机且符号在寿命内固定，幅值.08→.70H/s。卡片宽.10–.18H、高.25–.42H；角度±25°。 |
| SoftLift | mist；透明或低增益Additive，不用opaque黑/灰片 | .18–.50s，约4–6粒；寿命 .6–.85s | y1.3→.35H/s，横移少；大小.25–.40H增长至1.5倍，alpha峰值仅.10–.16。它连接窄根与光涌，不成为云团。 |
| Glints | glint；Additive | .30–.68s，约10–15粒；寿命 .3–.55s | y1.4→.5H/s，横向.1→.65H/s；尺寸.015–.035H，随机旋转。末段成为少量光点，而不是满屏星星。 |

上述 `→` 通过 Velocity over Lifetime 曲线实现；也可用一致起速+SpeedModifier衰减并配合x曲线。不要同时叠两份初始y速度。x用 Two Curves 同向固定随机系数，使每粒子逐渐外散；不要逐帧随机换方向。2D喷泉可使用x展开表达径向扩散，避免直接加全3D radial导致z方向或向下反冲。Shape使用薄Box即可；无需新增物理模拟。

Size over Lifetime：Core宽因子约 .65→1(.18life)→1.25(.7life)→1.45，长因子 .6→1(.18)→.85(.75)→.6；不要末端收成统一圆帽。靠透明度消失。Gold细丝类似但最终更薄。Color over Lifetime：主要维持纹样白金RGB，仅轻微暖化；alpha约 0→.55(.12life)→.45(.55)→.16(.82)→0；Gold峰值 .35–.45，末端0。这些alpha为单粒子目标，原图alpha仍需参与，相乘一次。

光影通过纹样亮度、alpha、图层与混合获得；不添加场景PointLight，不用Bloom掩盖无笔触的平片。主体纹理的白核应不连续地穿过几股流束，金色外散比白核更宽。最多局部短时间叠白，不形成全身不透明矩形。

## 时钟、构建与验收

继续由现有Age唯一推进，1/120秒固定子步可用于稳定大dt；每PS `Simulate(dt,false,false,false)` 后 `Pause(false)`。不能同时Play自动模拟。发射可由Emission bursts/曲线驱动；如使用Emit，在该批次后调用 `Simulate(0,false,false,false)` 同步再Pause，复用仓库已证实的2019实践。归零初始化只做一次，不在每次Advance重启；dt=0不发第二批。生命周期StopEmittingAndClear，Dispose释放本次实例，模板保持只读。

交付顺序固定：

1. 保存原创texture源与provenance，生成上述texture/alpha/light-mask图板，主线程检查。
2. builder建立真实PS prefab及四种材质，输出模块参数JSON（含burst、曲线、seed、render mode、纹理设置）并打包。
3. 同一候选bundle加载到现有native preview，既有角色上下文和时序不变。先看完整时标序列，再看 .30/.55/.90/1.25及最终清空；左右朝向只需代表帧。
4. 读主日志并记录各时点particleCount、最高位置、横向范围、最后alive=0，证明是粒子上升后展开；保留不选中目标无特效与既有生命周期检查。
5. 独立评审PASS后再走主线程原有最终构建/部署边界。不得用“现已使用标准PS”取代视觉PASS。

图像验收：.30s有从下部涌起的有纹理流束；.55s白核在内、金色外缘，若干不规则笔触组成一个连贯上涌势；.90s根部已停、上方和两侧仍有向外的碎丝；1.25s只留稀疏细雾和金屑，1.45之后完全清空。足够看清目标面部和服装。拒收五根管/手指、五个帽盘、棉花团、均匀矩形贴片、整体平移的大喷泉图片、从上往下掉的火花、只有圆点无流势、固定图案同步闪烁。接受S2与掌心视觉不因本修订返工。

## R4c 原生可见性修订

真实粒子上升与生命周期已通过，但独立视觉评审确认大部分光纹被目标不透明立绘遮住；0.30秒几乎看不到效果，0.90秒只有暗金薄烟。主线程因此批准对上述后层规则作精确修订：CoreFlow 在角色前层形成可辨的白色内光，并用归一化身体高度遮罩避开面部区间，头顶以上恢复可见；GoldWisps 保持后层并略扩大横向范围。适当增强 Core/Gold 材质亮度和 Core alpha，不增强 Mist，不以 Bloom 遮盖问题。纹样、实际粒子运动、已接受角色姿势和播放时序保持不变。该修订仍须新原生候选与独立视觉通过，不能把 R4c 技术 PASS 当作视觉验收。
