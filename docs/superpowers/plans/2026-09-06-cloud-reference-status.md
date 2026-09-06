# 成熟云函数参考与交付状态

**当前交付已更新为 cloud-inflow.1。** 用户指出此前的整体缩放不符合“云被吸入”的要求后，
已改为保持高度的XZ向内输运，进入中心附近才局部压缩。完整视觉复审及54项原生运行检查通过，
2026-09-06 20:53再次同步三个Mod目录、18个文件。当前Bundle为`918B3213…AACB8C5`，
详见[吸入修正交付](2026-09-06-slazeya-cloud-inflow-delivery.md)。下文为此前云形版本的交付记录。

用户要求寻找已实现云函数后，已实际下载、读取并固定下列来源：

- Sebastian Lague / Clouds：周期Worley、sampleDensity的逆密度边缘侵蚀、lightmarch及HG相位。MIT。
- Sébastien Hillaire / TileableVolumeNoise：Perlin-Worley与多尺度Worley组合配方。MIT配方参考，未复制GLM/Shadertoy底层。
- Stefan Gustavson / webgl-noise：periodic classic 3D pnoise及辅助函数。MIT，已适配离线烘焙。
- Unity HDRP VolumetricCloudsUtilities：覆盖率重映射、形状/侵蚀分工及散射的设计对照，未移植HDRP管线代码。

精确URL、commit与文件SHA256见`../references/2026-09-06-cloud-source-references.json`；本地适配及许可见`SteriaBuild/VFXSource/SlazeyaStormMass/ThirdParty/SOURCES.md`和`NOTICE.txt`。

**云形修复已通过独立预览验收并同步。** 最终使用单个包围盒中的连续环形体积场、可归零的多尺度Worley密度与截面翻滚；保留已验证的64³/32³纹理烘焙。视线H/48、向光48步读取完整密度，消除了前期细密积分条纹。填核末段改为平滑椭球包络，修复了中间显出方盒的问题。

最终Bundle：`1665C27E1A76465EF7C742D55BBF12D3AA0D6CAC0D44BF33D5C44AD0D0F9D72F`。
交付DLL：`8FA7046C6EEBE5892266F956328FB53709892C250AB38589419B01DF739922B5`。
2026-09-06 18:27已同步仓库Mod目录、C盘和D盘游戏Mod目录，18个文件逐项哈希复核通过。

Unity原生Bundle构建/预览、源码验证、DLL编译、音效数值检查及53项PlayMode生命周期检查通过。
预览使用代理角色，PlayMode使用最小游戏接口替身；未把这些结果称为实际游戏战斗、主观试听或游戏帧率验证。

完整交付记录见[云环交付验收](2026-09-06-slazeya-cloud-delivery.md)，设计演变见[云形恢复](../specs/2026-09-06-cloud-shape-recovery.md)。
旧`output/slazeya-supported-density/RESULT.md`、`first-*`、`revise-sharp-*`以及Bundle `66687ed0`是已被后续修复取代的实验，不作为最终候选使用。
