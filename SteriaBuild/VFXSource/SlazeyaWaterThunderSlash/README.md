# 斯拉泽雅 · 水雷纵斩制作工程

[修改记录](CHANGELOG.md) · [制作与部署经验复盘](../../../docs/superpowers/references/slazeya-water-thunder/2026-11-16-experience.md) · [本机对话原文](../../设定/对话记录_2026-11-16_斯拉泽雅水雷斩击重置与部署.md)

**已按实测反馈重新部署游戏。** 前锋续向下45°、后缘从后往前退去45°；绑定攻击者固定前方，并成为斯拉泽雅核心书页的普通近战默认效果（含借用卡），远程保持原样；[部署记录与校验](deployment/DELIVERY.md)。本轮附加贴近刀痕的80水滴、白色四方流纹旋动、树状电枝及旧区段/低储能分支先衰减。

当前制作候选：**candidate-20-front45-core**，隔离发布release-04，原生集成145项检查通过。本轮按用户要求单人完成。本次补足真正的蒙版溶解：主体先按细碎水纹蚀出缺口，水滴后段同样侵蚀，再用短alpha淡出收尾；尖头入场和水滴流体变化保留。旧风系 Slash 的废弃标记位于 `../SlazeyaWindCombat/SLASH_DEPRECATED.md`；上一版横向水雷工程的源文件备份为 `history/candidate-05-sources.zip`。

- [溶解淡出专门视频](evidence/candidate-20-front45-core/dissolve-fade-60fps.mp4)、[固定Alpha=1的溶解分解](evidence/candidate-20-front45-core/dissolve-mask-only-stages.png)
- [视频：单次攻击＋标注0.5x慢放](evidence/candidate-20-front45-core/review-60fps.mp4)
- [水滴/水丝动态放大视频](evidence/candidate-20-front45-core/water-dynamics-60fps.mp4)、[尖头入场连续帧](evidence/candidate-20-front45-core/early-head-stages.png)、[水滴形变分解](evidence/candidate-20-front45-core/water-dynamics-stages.png)
- [白色四方流纹旋动](evidence/candidate-20-front45-core/white-fluid-spin-60fps.mp4)、[分支电势衰减](evidence/candidate-20-front45-core/lightning-discharge-stages.png)
- [全部资产清单](evidence/candidate-20-front45-core/asset-inventory.md) / [CSV](evidence/candidate-20-front45-core/asset-inventory.csv) / [JSON与hash](evidence/candidate-20-front45-core/asset-inventory.json)
- [六阶段](evidence/candidate-20-front45-core/six-stages.png)、[四层拆解](evidence/candidate-20-front45-core/four-layers.png)、[命中前后连续帧](evidence/candidate-20-front45-core/contact-frames-stages.png)
- [水纹散点扩散](evidence/candidate-20-front45-core/evaporation-only-stages.png)、[实际速度曲线](evidence/candidate-20-front45-core/impact-speed-curve.png)、[地面与朝向](evidence/candidate-20-front45-core/orientation-vertical-ground.png)

## 打开与调整

用 **Unity 2019.3.15f1 / Built-in** 打开 `UnityProject`。场景为 `Assets/WaterThunder/Scenes/WaterThunderAuthoring.unity`，菜单为 **Steria → Water Thunder → Open authoring stage**。

选中 `Slazeya Water + Thunder Slash`：`Auto Play` 控制编辑态与运行态循环播放；关闭后调整 `Sample Time`（秒）。`Playback Speed` 调速，`Mirror` 切换左右朝向；`Water / Splash / Lightning / Core` 独立显示四主层；`Evaporate / Impact Burst` 开关散点蒸发和命中支持组。制作场景有占位目标和真实XZ地面，不使用角色贴图。

月牙位于XY竖直面，长轴也为纵向：从高处左上轻尾，经右侧弧线，向前下方扫落，锋尖保持在地面以上。四层、粒子和蒸发散点残影共享一个根，只镜像一次。根的缩放可在Inspector中继续调整。

## 四主层与支持资源

| 职责 | Mesh / Material / 静态Prefab | Texture | Mask |
| --- | --- | --- | --- |
| 水刃主体 | L1_WaterBlade | L1_WaterBody.png | L1_WaterMask.png |
| 流体水滴与水丝 | L2_SplashWake | L2_Splash.png、L2_WaterNormal.png | L2_SplashMask.png |
| 雷电弧光 | L3_Lightning | L3_Lightning.png | L3_LightningMask.png |
| 前端亮芯 | L4_BladeCore | L4_Core.png | L4_CoreMask.png |

资产在 `Assets/WaterThunder/` 下的 `Meshes / Materials / Prefabs / Textures / Shaders`。组合可播放Prefab为 `SlazeyaWaterThunderSlash.prefab`。四层共享 `WaterThunderFormedArc.shader`，几何、遮罩与材质职责不同，没有同形缩放透明壳。

新增两个支持Prefab：`WaterEvaporationScatter.prefab` 使用一张 `Evap_WaterScatter.png` 蒸发水纹散点图，逐渐扩散、上浮、变淡；被否决的大圆环已从活动资产移除，并备份在history。`WaterThunderImpactParticles.prefab` 保留水滴38粒、紫电碎光14粒、局部压闪2粒，全部从190ms的同一命中位置发出。散点残影用 `EvaporationScatter.asset`，紫电粒子用 `ImpactArcShard.asset`，压闪用 `Impact_Flash.png`。

共享数据为 `Shared_FlowRepeat`（RG流动偏移）、`Shared_CausticsRepeat`（内部亮纹）；旧 `Shared_ErosionRepeat` 为兼容属性保留；退场使用水纹高度通道，不采用旧Repeat噪声切片。Shape读RGBA、Mask读线性R；单次形状Clamp，流动数据Repeat；不使用GrabPass或后处理Bloom。场景占位体与地面另用Unity内置Unlit/Color。

## 冲击时序与蒸发

六个代表时刻为 **45 / 155 / 215 / 330 / 535 / 790ms**。0–190ms加速，190ms到达原主体曲线72%的命中点（扩展路径参数为61.92%）；190–250ms短暂减速，250–320ms再次加速扫落。速度来自同一条C1 Hermite进度曲线，只改变刀锋视觉速度，不修改全局timeScale。

完整主层网格的Reveal固定为1；前端尖、后方宽的刷形Mask先产生运动尖头，只有经过前锋的位置才逐渐横向展开成水流；尾部TailGrowth晚于前锋增长，起手不再先出现扇开的尾丝。使用 `Shared_SweepBrushMask.png` 这张Clamp刷形楔形蒙版沿pathT扫过，使局部亮部移动并逐渐增强，扫后留下水体余光。主层使用SweptWedge模式，辅助粒子沿用兼容的FormedArc属性。材质的SweepWidth控制亮部范围，SweepWake控制余留水体强度。

L1尾端不再捏成尖点。尾部网格预留扇开空间，`L1_TailFanMask.png` 将其分成长度、宽度不同的断续水丝。材质TailSpread控制尾部扩散，TailMaskStrength控制分叉蒙版强度；这两个参数可以独立调整，不增加同形透明外壳。

215–480ms先按弧线从后往前退掉45°的尾段；360ms起，主体按水纹蒙版的高度阈值逐步蚀出细碎缺口，先侵蚀、后淡出；水滴和水丝也在各自寿命后段执行蒙版溶解，最后12%寿命才做alpha收尾。同一张Linear RGBA的R=主体溶解高度、G=较柔和的水滴溶解高度、A=可见散点覆盖；散点残影继续扩大约38%、上浮、变淡，材质用BaseColor染青蓝色。无大圆圈。整体1040ms清除，循环额外保留250ms空档。

飞沫和命中粒子均按绝对时间确定性采样。水滴使用重力与线性阻力的闭式运动，顺当前速度拉伸并逐渐回圆，宽厚按近似体积守恒缩放。每颗水滴有独立相位的衰减表面张力波动，少量水丝先拉长、收颈、分成小水滴。线性法线和局部移动高光替代整圈等亮描边。源Mesh只读，每实例只克隆自身飞沫网格，场景保存时恢复源引用。OnValidate只标记刷新，实例化在正常Editor update完成，避免Unity反序列化期间创建对象。

## 复现与来源

```powershell
python -m pip install numpy Pillow imageio-ffmpeg matplotlib

# 这个工程的Editor关闭时，选择全新版本号后台构建与渲染
powershell -NoProfile -ExecutionPolicy Bypass -File SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/build_preview.ps1 -Revision candidate-next

# 工程保持打开时，可以用当前已保存的资源快照离屏渲染
powershell -NoProfile -ExecutionPolicy Bypass -File SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/capture_snapshot.ps1 -Revision candidate-snapshot-next
```

脚本使用本机 `C:/Program Files/Unity/Editor/Unity.exe`、隐藏窗口、D3D11及4个job worker；本机需要的CPU affinity和OpenSSL兼容变量只传给子进程。输出为真实Unity RenderTexture帧编码的视频，不是游戏实战录屏。复核覆盖Mesh/UV、纹理导入和接缝、Shader、先加速/命中减速/再加速、命中前无粒子、命中位置冻结、重复采样、最终清理、原生资源包构建及重新加载。

贴图与Mask由 `generate_inputs.py` 原创生成，没有旧刀光、游戏或公开仓库图片依赖。本轮未使用ImageGen。Mesh由 `WaterThunderBuilder.cs` 创建；Shader基于本机unity-sword-slash-vfx FormedArc ContractV1兼容派生，保留Reveal/Retreat/Phase、UV和混合合同，新增移动刷形蒙版扫光、尾流发散与水纹散点蒸发控制。原Shader及hash在 `source_assets/`、`provenance.json`；Cyanilux文章只作方法参考，未复制许可不明下载资源。

水滴和命中水花材质保留Liquid=1，LiquidWobble调整表面波动，LiquidShine调整湿润高光，LiquidDissolve切换水滴溶解；主体EvapDissolveSoftness调整侵蚀边缘柔度；其顶点Color存储seed、年龄、水丝标志和透明度，不能关闭Liquid后继续按普通RGB颜色读取。

静态资源包 `steria_slazeya_water_thunder_assets` 只含四主层和两个支持Prefab（Transform、MeshFilter、MeshRenderer、ParticleSystem、ParticleSystemRenderer），不含自定义MonoBehaviour；组合播放脚本留在制作工程。现已部署：既有注册表三个近战入口改用新实现，DLL与新AB已核验安装；CardInfo.xml、旧风系AB与远程/守备/群攻路径保持。尚未启动完整接待实战，未晋升公共skill库。
