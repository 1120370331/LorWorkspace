# 克丽丝塔夏 V2 真粒子制作源

本目录是可重建的原创金潮粒子库。设计依据在 `docs/superpowers/specs/2026-09-12-christasha-particle-v2-design.md`；原版/寒昼只提供结构参考，没有导入其图片、Mesh、材质、Shader 或 prefab。全部图形来自本目录构建器中的原创网格及基础 brush/leaf/mist mask，不是 ImageGen 输出，也不是整张攻击贴图。

当前媒体是 Unity 2019.3.15f1 / Built-in Gamma / D3D11 的真实 GPU 代理战场预览。灰色不透明人形约 3 世界单位高；它不证明实际游戏角色挂点、镜头后处理或音效。最终生产接入、独立接受与部署证据由主线程总指南补齐，`candidate-*` 不等于已通过视觉验收。

## 重建与交付

```powershell
powershell -ExecutionPolicy Bypass -File SteriaBuild/VFXSource/ChristashaParticleV2/build_preview.ps1 -Revision new-candidate -AffinityMask 4294901760
```

脚本只操作本目录独立项目，复制唯一生产源码 `SteriaBuild/ChristashaParticleVisual.cs` 到 `UnityProject/Assets/Scripts`。Unity 进程临时设置 `OPENSSL_ia32cap=:~0x20000000`，使用 Hidden 与 D3D11，不使用 `-nographics`。本机i9-14900HX后来出现Unity甚至`-version`启动级0xc0000005；已验证让新Unity从创建时继承高16逻辑核affinity4294901760可恢复。`AffinityMask`默认0不限制，上述本机复现命令显式传4294901760，仅临时设置启动器子树mask，创建Unity后恢复启动器值，不改系统/BIOS/电源/许可证，也不据此断言硬件原因。可用 `-Unity`、`-Python` 指定本机路径；Python 需要 Pillow 与 imageio-ffmpeg。脚本拒绝覆盖已存在候选，并校验本次进程exit、fresh log时间戳与新PASS标记，不能误读上一次旧日志。

输出 bundle 在 `UnityProject/AssetBundles/steria_christasha_particle_v2`。七个显式根为 `ChristashaV2_Slash/Hit/Pierce/Projectile/Cast/Area/Point`；仅包含 Transform、ParticleSystem、ParticleSystemRenderer 及原创素材。共享 driver 是普通 C# 类，在游戏 DLL 中编译，不序列化进 AB。

`evidence/<revision>` 包含 60fps MP4、深/中灰底阶段接触表、逐帧粒子数、原生 load/readback 与清理/重播检查、源文件/资产/meta/bundle/媒体 SHA256。`ProjectilePath-*` 是实际适配器的 0.26s 飞行并到达即清理；孤立 `Projectile-*` 展示完整开发 recipe，不应将其当成游戏实际飞行。`Mass-dark` 展示 Cast、单份 Area、三个成功目标 Point 和一个无 Point 的未命中目标。它不自行结算伤害。

正式接受的七个基础视觉为 `candidate-r4`：沿用r3已接受艺术资产bundle，只更新下面说明的新生粒子同步，不重新打包素材。群攻帧18=0.3s展开Area、帧30=0.5s释放成功Point，坐标与接入合同一致。70° SlashV caller虽经实际预览，但独立评审认为呈膝/脚下U形横扫并重叠小腿，故被拒；接入owner已按设计允许项让原SlashV页共用基础Slash，不再旋转70°。所有`SlashV-*`及`candidate-r3-slashv`均为被拒历史证据，最终showcase不含该段。`candidate-r3`还含修复前GPU异常，不能当最终预览。原source snapshot保持不变，主线程最终accepted-candidate记录接入后的完整候选。

重新运行应使用新 revision 保留被拒候选。输出 PNG/视频只是 GPU 证据，必须先看正常速度，再看阶段帧。构建成功不能代替独立视觉 PASS。部署只取最后接受候选的确切 bundle 和 Release DLL，并核对目标 SHA；本脚本不部署或提交。

## 分层与坐标

每个 prefab 只有五个职责系统，允许某个职责当前没有粒子。`Body` 是一个原创连续 Mesh 粒子；`Edge` 是短命小面积暖白锋；`Spray` 是独立速度/寿命的叶状飞沫；`Mist` 是少量低 alpha 暗部；`Glint` 是稀疏亮屑。不可用放大同形 Body 副本叠出“体积”。

默认向左 -X，XY 为战斗平面。普通近战完整继承挂点、镜像、角色 map scale 与原生攻击倍率一次。原生 S/M 的 map scale=3/2.25，攻击基础倍率=2，所以近战 recipe 内 `unit=1/3`。代理预览的近战根倍率=6/4.5，人物高度=3/2.25；切勿把 source Mesh 高度直接乘这些倍率后误称正常尺寸。世界 Projectile/Cast/Area/Point 的 unit=1，不再乘角色父尺度。

Area 的完整运动包络约 X[-6.3,+6.4]，单个时刻只占其中一段。根位于 caster+全体参与者脚底的横向包络中点，X 比例按 `max(1,(maxX-minX+2)/13)`；从 root 的 +X 向 -X 传播，镜像由适配器一次处理。Cast 使用施法者 feet，Point 使用成功目标 atkEffectRoot。失败目标不能因为出现在场地中而生成 Point。

## 唯一时钟与变体

```csharp
var visual = Steria.Vfx.ChristashaParticleVisual.Play(instance, seed: 194u, playbackSpeed: 1f);
visual.Tint = Color.white; // MPB 同时作用于现存和后续粒子；不修改共享材质。
// 调用方每帧一次：
visual.Advance(deltaTime);
// 正常完成或取消：
visual.StopAndClear();
```

driver 不设置根位移、面向、缩放，不创建 children，不拥有伤害/镜头/音效。每个系统禁用自动 emission，固定 seed，`Pause` + `Simulate(...false,false,false)` 以 1/120s 步长推进。外部传入 60fps 的真实秒；`Duration` 已除 playbackSpeed。取消/结束 `StopEmittingAndClear`，同一个实例可重新 `Play`，不增加 children。不要同时调用 `ParticleSystem.Play` 或另加 Update。

**新生粒子同步不能省略。** 每个Emit批次结束后，对实际新发射过的系统执行`Simulate(0f,false,false,false)`再Pause，让Unity 2019初始化渲染派生数据后才交给camera。没有这步，r3跨类型顺序预览中可复现巨雾、异色细针：CPU的position/currentSize/currentColor都正常，但辅助Billboard首帧渲染数据异常。只增加Camera.Render或隔离culling layer不足以证明修复。加入零时长同步后，两轮Slash/Hit×深/中底×稀疏/连续Render的全部PNG逐像素一致，CPU粒子CSV与旧时钟逐字节一致；记录在`evidence/gpu-sequence-sync/comparison.json`。零时长同步不预热、不改变事件时间或粒龄。

优先修改 root 均匀尺度、Z 旋转、Tint、playbackSpeed、seed。seed 只改变游离细节，不改接触原点、主弧、阶段顺序。主形参数在 builder 的 `Shape(kind)`；时间/发射/速度在 driver 的对应 recipe 方法。改主形或时间必须重建 AB 与 DLL，二者是一份候选。不要只保存 Inspector 改动或只部署 DLL。

## 七类配方的实际制作方法

| 类型 | 本库入口及参数 | 制作时保持的判据 |
|---|---|---|
| 挥舞 | `Slash()` + `Shape("Slash")`；开放175°弧，前0.14s旋过82°；主粒子0.43s，16枚切向飞沫，4枚薄雾；总0.65s | 上尾轻、下前锋重；UV.x=尾0→锋1；内缘破碎，外锋连贯；不闭环 |
| 打击 | `Hit()`；短前宽楔从-X突进，0.31s主体，8枚45°半扇附近飞沫、5枚薄雾，总0.62s | 短、宽、钝；不能拉长成第二种突刺，不以目标星爆替代发力主体 |
| 突刺 | `Pierce(false)`；原创2.15参考单位针形，两侧共10枚轴向飞沫，0.35s主体，总0.62s | 唯一尖锋，长轴优先；轴向速度与根位置分工；远程用true且根由适配器投递 |
| 防御 | 已实现`Guard()`+`Shape("Guard")`：170°迎击弧，半径0.85/1.38、中段半宽0.14；主形0.43s，总0.62s，中央Edge+上下成对8枚反向飞沫；`_Guard=1`使两端向中段同步退场 | 使用G半倍率，所以代理根=map3/2.25；中心稳定、上下均衡，没有下重落斩。`PreviewGuardRecipe`可运行，此实验prefab不列入七根AB、不绑定防御页 |
| 范围 | `Area()`；一次主波从+3.8以-8.2u/s传播，主体0.92s，26枚飞沫/7枚薄雾，总1.16s | 一份场地范围；按全体参与者包络布置；高度低于脸，不为每目标复制全场 |
| 特殊 | `Cast()`；开放低位潮环、3条独立上行弦路、8枚稀疏亮屑，总0.72s | 事件语义是起势；0.32s前弦线可读、不遮脸；可单独调整弦数/相位/波动与环尺寸 |
| 点位 | `Point()`；短小压力锋+三叉飞溅+9枚独立飞沫，主体0.23s，总0.62s | 原点稳定、短爆而非小月牙；仅真实成功事件触发，取消必须清空 |

新增配方的顺序：先写一行轮廓/方向/事件语义；固定调用者挂点与缩放；只开一个 Body 校验大形；补有明确职责的 Edge/Spray/Mist；校验时序与真实粒子运动；再调色和局部纹理；最后才增加系统。凡是只靠更亮、更多层或更多粒子才能成立的轮廓，都应回到 Body 修形。

Guard实验已由正常构建命令一起生成`Guard-dark/-mid`证据，也可在Unity菜单`Steria → Christasha V2 → Preview non-shipping Guard recipe`独立运行，只更新source素材和实验预览、不构建/部署AB。实际代码入口是 `ChristashaParticleBuilder.PreviewGuardRecipe`，固定输出`evidence/guard-example`。若要新主题守备，先改`Shape("Guard")`的角度/半径/对称width，再改`Guard()`的Edge接触时刻和成对Spray速度；不要打开Slash的82°扫动。

特殊类型直接运行现有Cast：在`Cast()`中把`lane<3`改为5、横向间距0.55改为0.35，可得到五弦而不扩总宽；改`0.025+t*0.23`调整上行次序，改`Shape("Cast")`的1.3/0.25调整脚下椭圆。保持施法0.3s后Area的游戏事件时标；若增加弦数导致太密，减少每弦9个采样或弱化金屑，不能加全屏底图。主形和时间参数变更仍必须重新输出候选并接受。

`StrokeBrush` 是 512×128 的局部条带，不包含完整攻击轮廓；`SprayLeaf`128×64、`MistGrain`128²。RGBA32/Bilinear/Clamp/no mipmaps，三张解码像素共360,448字节。每个主体96段、194顶点、192三角形；每实例5个系统、主PS容量8/其他各96，但发射数量由recipe限制，容量不是目标。Stroke 有颜色，因此使用 sRGB；shader 不把其RGB当数学噪声。多尺度非周期噪声只控制内缘断墨和少量色块，保留外锋。不要重新加入整面等间距正弦条纹：首候选已证明它会读成木纹硬塑料。

通用 shader 只做 alpha 混合、粒子色/实例 Tint、按 Custom1X 粒龄沿UV.x尾→锋退场。没有HDR叠加、GrabPass或屏幕滤镜。`Particle` sorting layer和0/2/3/4子顺序在prefab明确序列化；游戏对象的Effect layer由适配器递归处理。

## 验证与素材追踪

`verify_source.py` 在Unity启动前守住纯driver API、七入口、单时钟与无旧效果依赖。它最初在缺少driver时失败后再实现。Unity原生check逐prefab检查首帧粒子、0.1s寿命推进、同seed位置/数量重播、清理、children不增长、无自定义脚本以及材质shader可用。媒体进一步判断轮廓/接缝/面向/色块/退场。

资产 `.meta` 与生成源一同保留。已有材质/Mesh通过CopySerialized更新、prefab按同名层级保存，不能删光.meta重导入。候选manifest列出每个输入和.meta的SHA256。Unity的Library、Temp和视频逐帧序列是可再生缓存，不是发布素材；原版/寒昼参考资源永远不进入本目录输入或AB依赖。
