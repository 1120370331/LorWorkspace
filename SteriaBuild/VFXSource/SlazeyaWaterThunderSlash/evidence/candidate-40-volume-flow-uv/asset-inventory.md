# 水雷纵斩：全部资产清单

工程：`C:\Users\rog\WorkSpace\projects\games\lor\SteriaBuild\VFXSource\SlazeyaWaterThunderSlash\UnityProject`

本清单含 68 项 Unity 资产/源文件，每项均有配套 `.meta`；不含 Library、临时文件和历史场景备份。SHA256、文件尺寸与纹理导入设置见同目录CSV/JSON。

## 角色与引用

| 职责 | Mesh | Material | Texture / Mask | Prefab |
| --- | --- | --- | --- | --- |
| L1 水刃、发散尾流与旋动四方白纹 | L1_WaterBlade.asset | L1_WaterBlade.mat | L1_WaterVolume.png / L1_WaterDepthFlow.png / L1_WaterMask.png / L1_TailFanMask.png / L1_UserWaterNormal.png（用户提供原图） / L1_WhiteFluidQuadMask.png | L1_WaterBlade.prefab |
| L2 流体水滴与断开水丝 | L2_SplashWake.asset | L2_SplashWake.mat | L2_Splash.png / L2_SplashMask.png / L2_WaterNormal.png（Linear） | L2_SplashWake.prefab |
| L3 雷电弧光 | L3_Lightning.asset | L3_Lightning.mat | L3_Lightning.png / L3_LightningMask.png | L3_Lightning.prefab |
| L4 前端亮芯 | L4_BladeCore.asset | L4_BladeCore.mat | L4_Core.png / L4_CoreMask.png | L4_BladeCore.prefab |
| 水纹溶解与蒸发散点 | EvaporationScatter.asset | EvaporationWaterScatter.mat | Evap_WaterScatter.png（Linear；R=域扭曲多尺度溶解、G=水滴溶解、B=独立微细侵蚀、A=散点覆盖）；余纹扩散上浮 | WaterEvaporationScatter.prefab |
| 泡沫点缀（7粒，错峰出现） | Unity原生Billboard | ImpactFoam.mat | Impact_Foam.png | WaterThunderImpactParticles.prefab 内 Hit_FoamAccents |
| 单点线型光针（16–22粒） | Unity原生定宽Billboard | ImpactLightCone.mat | Shared_WaterNoiseRepeat.png / Shared_LightFlowRepeat.png / Impact_ConeShadowMask.png | 同上 Hit_RadialNeedles |
| 下段/内缘浪沫（28粒错峰） | Unity原生Billboard | WaterEdgeFoam.mat | EdgeFoam_WaveNoise.png | WaterEdgeFoam.prefab |
| 局部命中压闪（1粒） | Unity原生Billboard | ImpactFlash.mat | Impact_Flash.png | 同上 Hit_ContactFlash |

组合动画预制体：`Prefabs/SlazeyaWaterThunderSlash.prefab`。静态原生资源包有四主层和两个支持Prefab，不含自定义播放脚本；组合与驱动保留在制作工程。

刀光Shader：`Shaders/WaterThunderFormedArc.shader`保持辅助层兼容；主刃使用沿pathT运动的Clamp刷形楔形蒙版，Reveal固定为1；光锥另用简洁的WaterThunderLightCone.shader，白光加UV蓝暗面，不使用圆柱凹槽和反光贴图。`Shared_SweepBrushMask.png`控制扫过亮部和增强；`L1_TailFanMask.png`分离向外发散的水丝，材质TailSpread可调。`Evap_WaterScatter.png`同时驱动水体柔和破散与一张散点残影的扩散。无可见大圆圈。场景另使用Unity内置 `Unlit/Color`。

共享数据贴图：`Shared_FlowRepeat.png`、`Shared_CausticsRepeat.png`、`Shared_ErosionRepeat.png`。旧噪声侵蚀入口保留兼容，本版退场不使用它。

白色四方流纹仅在水体内旋动，WhiteFluidStrength和WhiteFluidSpin可调。L3为一条电主干、5条连接分支及少数二级枝；UV2.x为扫过/着电时间，UV2.y为局部能量储备。Shader做充能、阻尼脉冲和放电，旧区段与低储备小枝先衰减。旧命中碎电材质保留兼容，本候选命中使用蓝白针束。VoltageDecay控制L3电枝衰减。尾流水滴80颗，出生点和法向速度收紧以贴近刀痕。

尾流水滴启用Liquid材质模式（本候选命中主体另用蓝白针束、泡沫仅为点缀）：线性法线、各粒子独立相位的衰减表面波动、局部动态高光与水丝收颈；WaterDropletMotion.cs计算阻力/重力飞行和顺速度拉伸回圆。水滴顶点/粒子Color.r=seed，g=归一化年龄，b=水丝标记，a=透明度；其它层保留原始Color含义。LiquidWobble、LiquidShine可在材质中调整。

溶解淡出：主体按R通道的大/中尺度场叠加B通道细碎场、水滴按较柔和的G通道阈值逐步侵蚀，后段再做短alpha收尾；同一RGBA的A通道显示散点残影。蒸发散点材质UseShapeRGB=0，用BaseColor染青蓝色，避免把RG数据当颜色读取。EvapDissolveSoftness调边缘柔度，LiquidDissolve调水滴溶解开关。

刀光纹理和Mask由项目重新绘制；L1_UserWaterNormal.png为用户提供原图，其SHA256见provenance.json；其余辅助纹理由项目Python生成。水刃、飞沫、紫电、亮芯和支持几何为项目原创。Shader为本机skill ContractV1兼容派生，来源记录见 `provenance.json`。本轮未使用ImageGen；没有游戏原图或公开仓库图片依赖。

## 本轮预览变化（未部署）

主体水体无静态镂空。L1_WaterVolume.png提供厚度、波峰、暗部与透光色面；L1_WaterDepthFlow.png的RG存流向、B存高度、A存厚度。颜色UV沿切线双相推进，高度引导侧向偏移，并限制在U±0.10、V±0.035；覆盖率仍采原始UV，避免边缘漂移。Shared_LightFlowRepeat.png增强移动亮带与蓝暗部，仅调内部颜色，不改变光针几何。Shared_WaterNoiseRepeat.png通过旋转/拉伸/平移控制流动明暗；水层与亮层共用边界，水层向内留0.028单位余量，材质禁止摆动向外越界。L1_UserWaterNormal.png逐字节使用用户提供的法线图；双相流动在Clamp范围内采样，改变水面法线和高光。L1_RippleNormalRepeat.png保留作兼容输入。五条随机飘带使用独立宽度/端点/摆动相位，Mask仅用于尾迹。主弧统一圆心，刃体加宽约15%，退尾45°，175–340ms完成。亮层尾端只向前裁短，并与水体蒙版同步溶解，最后才淡出。定宽直光针由同一原点四周射出，16–22根、近似正态长度、72%主弧半径范围，白光直线仅由UV亮度衰减呈现前后收尖，通过UV阴影形成暗蓝面，噪点旋转拉伸形成内部流动；先加速，再受空气阻力减速，泡沫作为周围稀疏点缀。另有下段/内缘浪沫尾缀，出生点复用水层同一网格三角形、展开遮罩与边缘切线，贴边后短暂跟随，再受轻微重力下降并消散。旧ImpactLightCone.asset已停用；ImpactLightCone.mat和WaterThunderLightCone.shader保留历史文件名，实际Shader为LightNeedle，只绘制UV光晕直线，不使用锥体几何。旧ImpactWater/ImpactElectric/ImpactNeedle材质、旧针束贴图及ImpactArcShard网格作为历史兼容资源保留，不再被本候选命中预制体引用。

## 按类型逐项列出

### C# source（8）

| 文件 | 规格 / 导入 |
| --- | --- |
| [Editor/WaterThunderAuthoringCommand.cs](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Editor/WaterThunderAuthoringCommand.cs) | Unity原生资产 / 可编辑源码 |
| [Editor/WaterThunderBuilder.cs](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Editor/WaterThunderBuilder.cs) | Unity原生资产 / 可编辑源码 |
| [Editor/WaterThunderEditorClock.cs](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Editor/WaterThunderEditorClock.cs) | Unity原生资产 / 可编辑源码 |
| [Editor/WaterThunderPreview.cs](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Editor/WaterThunderPreview.cs) | Unity原生资产 / 可编辑源码 |
| [Runtime/WaterDropletMotion.cs](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Runtime/WaterDropletMotion.cs) | Unity原生资产 / 可编辑源码 |
| [Runtime/WaterThunderImpact.cs](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Runtime/WaterThunderImpact.cs) | Unity原生资产 / 可编辑源码 |
| [Runtime/WaterThunderMotion.cs](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Runtime/WaterThunderMotion.cs) | Unity原生资产 / 可编辑源码 |
| [Runtime/WaterThunderSlash.cs](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Runtime/WaterThunderSlash.cs) | Unity原生资产 / 可编辑源码 |

### Material（14）

| 文件 | 规格 / 导入 |
| --- | --- |
| [Materials/EvaporationWaterScatter.mat](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Materials/EvaporationWaterScatter.mat) | Unity原生资产 / 可编辑源码 |
| [Materials/ImpactElectric.mat](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Materials/ImpactElectric.mat) | Unity原生资产 / 可编辑源码 |
| [Materials/ImpactFlash.mat](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Materials/ImpactFlash.mat) | Unity原生资产 / 可编辑源码 |
| [Materials/ImpactFoam.mat](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Materials/ImpactFoam.mat) | Unity原生资产 / 可编辑源码 |
| [Materials/ImpactLightCone.mat](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Materials/ImpactLightCone.mat) | Unity原生资产 / 可编辑源码 |
| [Materials/ImpactNeedle.mat](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Materials/ImpactNeedle.mat) | Unity原生资产 / 可编辑源码 |
| [Materials/ImpactWater.mat](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Materials/ImpactWater.mat) | Unity原生资产 / 可编辑源码 |
| [Materials/L1_WaterBlade.mat](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Materials/L1_WaterBlade.mat) | Unity原生资产 / 可编辑源码 |
| [Materials/L2_SplashWake.mat](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Materials/L2_SplashWake.mat) | Unity原生资产 / 可编辑源码 |
| [Materials/L3_Lightning.mat](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Materials/L3_Lightning.mat) | Unity原生资产 / 可编辑源码 |
| [Materials/L4_BladeCore.mat](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Materials/L4_BladeCore.mat) | Unity原生资产 / 可编辑源码 |
| [Materials/StageGround.mat](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Materials/StageGround.mat) | Unity原生资产 / 可编辑源码 |
| [Materials/StageTarget.mat](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Materials/StageTarget.mat) | Unity原生资产 / 可编辑源码 |
| [Materials/WaterEdgeFoam.mat](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Materials/WaterEdgeFoam.mat) | Unity原生资产 / 可编辑源码 |

### Mesh（7）

| 文件 | 规格 / 导入 |
| --- | --- |
| [Meshes/EvaporationScatter.asset](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Meshes/EvaporationScatter.asset) | Unity原生资产 / 可编辑源码 |
| [Meshes/ImpactArcShard.asset](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Meshes/ImpactArcShard.asset) | Unity原生资产 / 可编辑源码 |
| [Meshes/ImpactLightCone.asset](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Meshes/ImpactLightCone.asset) | Unity原生资产 / 可编辑源码 |
| [Meshes/L1_WaterBlade.asset](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Meshes/L1_WaterBlade.asset) | Unity原生资产 / 可编辑源码 |
| [Meshes/L2_SplashWake.asset](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Meshes/L2_SplashWake.asset) | Unity原生资产 / 可编辑源码 |
| [Meshes/L3_Lightning.asset](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Meshes/L3_Lightning.asset) | Unity原生资产 / 可编辑源码 |
| [Meshes/L4_BladeCore.asset](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Meshes/L4_BladeCore.asset) | Unity原生资产 / 可编辑源码 |

### Prefab（8）

| 文件 | 规格 / 导入 |
| --- | --- |
| [Prefabs/L1_WaterBlade.prefab](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Prefabs/L1_WaterBlade.prefab) | Unity原生资产 / 可编辑源码 |
| [Prefabs/L2_SplashWake.prefab](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Prefabs/L2_SplashWake.prefab) | Unity原生资产 / 可编辑源码 |
| [Prefabs/L3_Lightning.prefab](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Prefabs/L3_Lightning.prefab) | Unity原生资产 / 可编辑源码 |
| [Prefabs/L4_BladeCore.prefab](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Prefabs/L4_BladeCore.prefab) | Unity原生资产 / 可编辑源码 |
| [Prefabs/SlazeyaWaterThunderSlash.prefab](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Prefabs/SlazeyaWaterThunderSlash.prefab) | Unity原生资产 / 可编辑源码 |
| [Prefabs/WaterEdgeFoam.prefab](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Prefabs/WaterEdgeFoam.prefab) | Unity原生资产 / 可编辑源码 |
| [Prefabs/WaterEvaporationScatter.prefab](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Prefabs/WaterEvaporationScatter.prefab) | Unity原生资产 / 可编辑源码 |
| [Prefabs/WaterThunderImpactParticles.prefab](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Prefabs/WaterThunderImpactParticles.prefab) | Unity原生资产 / 可编辑源码 |

### Scene（1）

| 文件 | 规格 / 导入 |
| --- | --- |
| [Scenes/WaterThunderAuthoring.unity](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Scenes/WaterThunderAuthoring.unity) | Unity原生资产 / 可编辑源码 |

### Shader（2）

| 文件 | 规格 / 导入 |
| --- | --- |
| [Shaders/WaterThunderFormedArc.shader](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Shaders/WaterThunderFormedArc.shader) | Unity原生资产 / 可编辑源码 |
| [Shaders/WaterThunderLightCone.shader](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Shaders/WaterThunderLightCone.shader) | Unity原生资产 / 可编辑源码 |

### Texture / Mask（28）

| 文件 | 规格 / 导入 |
| --- | --- |
| [Textures/EdgeFoam_WaveNoise.png](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Textures/EdgeFoam_WaveNoise.png) | 256x128 RGBA · sRGB · Clamp |
| [Textures/Evap_WaterScatter.png](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Textures/Evap_WaterScatter.png) | 512x512 RGBA · Linear · Clamp |
| [Textures/Impact_ConeShadowMask.png](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Textures/Impact_ConeShadowMask.png) | 256x128 RGB · Linear · Clamp |
| [Textures/Impact_Flash.png](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Textures/Impact_Flash.png) | 128x128 RGBA · sRGB · Clamp |
| [Textures/Impact_Foam.png](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Textures/Impact_Foam.png) | 96x96 RGBA · sRGB · Clamp |
| [Textures/Impact_Needle.png](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Textures/Impact_Needle.png) | 256x128 RGBA · sRGB · Clamp |
| [Textures/Impact_NeedleFlowRepeat.png](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Textures/Impact_NeedleFlowRepeat.png) | 256x256 RGB · Linear · Repeat |
| [Textures/L1_RippleNormalRepeat.png](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Textures/L1_RippleNormalRepeat.png) | 256x256 RGB · Linear · Repeat |
| [Textures/L1_TailFanMask.png](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Textures/L1_TailFanMask.png) | 1024x256 RGB · Linear · Clamp |
| [Textures/L1_UserWaterNormal.png](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Textures/L1_UserWaterNormal.png) | 256x256 RGB · Linear · Clamp |
| [Textures/L1_WaterBody.png](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Textures/L1_WaterBody.png) | 1024x256 RGBA · sRGB · Clamp |
| [Textures/L1_WaterDepthFlow.png](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Textures/L1_WaterDepthFlow.png) | 1024x256 RGBA · sRGB · Clamp |
| [Textures/L1_WaterMask.png](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Textures/L1_WaterMask.png) | 1024x256 RGB · Linear · Clamp |
| [Textures/L1_WaterVolume.png](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Textures/L1_WaterVolume.png) | 1024x256 RGBA · sRGB · Clamp |
| [Textures/L1_WhiteFluidQuadMask.png](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Textures/L1_WhiteFluidQuadMask.png) | 256x256 RGB · Linear · Clamp |
| [Textures/L2_Splash.png](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Textures/L2_Splash.png) | 128x128 RGBA · sRGB · Clamp |
| [Textures/L2_SplashMask.png](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Textures/L2_SplashMask.png) | 128x128 RGB · Linear · Clamp |
| [Textures/L2_WaterNormal.png](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Textures/L2_WaterNormal.png) | 128x128 RGB · Linear · Clamp |
| [Textures/L3_Lightning.png](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Textures/L3_Lightning.png) | 256x64 RGBA · sRGB · Clamp |
| [Textures/L3_LightningMask.png](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Textures/L3_LightningMask.png) | 256x64 RGB · Linear · Clamp |
| [Textures/L4_Core.png](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Textures/L4_Core.png) | 256x64 RGBA · sRGB · Clamp |
| [Textures/L4_CoreMask.png](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Textures/L4_CoreMask.png) | 256x64 RGB · Linear · Clamp |
| [Textures/Shared_CausticsRepeat.png](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Textures/Shared_CausticsRepeat.png) | 256x256 RGB · Linear · Repeat |
| [Textures/Shared_ErosionRepeat.png](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Textures/Shared_ErosionRepeat.png) | 256x256 RGB · Linear · Repeat |
| [Textures/Shared_FlowRepeat.png](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Textures/Shared_FlowRepeat.png) | 256x256 RGB · Linear · Repeat |
| [Textures/Shared_LightFlowRepeat.png](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Textures/Shared_LightFlowRepeat.png) | 512x128 RGB · Linear · Repeat |
| [Textures/Shared_SweepBrushMask.png](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Textures/Shared_SweepBrushMask.png) | 512x256 RGB · Linear · Clamp |
| [Textures/Shared_WaterNoiseRepeat.png](C:/Users/rog/WorkSpace/projects/games/lor/SteriaBuild/VFXSource/SlazeyaWaterThunderSlash/UnityProject/Assets/WaterThunder/Textures/Shared_WaterNoiseRepeat.png) | 256x256 RGB · Linear · Repeat |
