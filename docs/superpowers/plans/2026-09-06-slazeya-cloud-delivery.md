# 斯拉泽雅风暴云环交付验收

2026-09-06。已接受并同步「倾覆万千之流」的连续大体积风暴云环：不均匀云瓣、深暗云腹、自旋与截面翻滚，全体收为一个圆润亮核，再由原伤害回调触发瀑布环喷和消散。保留既有闪电、浪沫雾气及技能音效，无角色台词。

## 根因与最终实现

前期噪声主要修饰已有平滑包络；保护深腹的正下限又使密度近乎恒定。仅改覆盖率、透明度或支持结构，不能把平滑球块变成云。

最终以连续环截面约束整体范围，2H周期的W4/W8/W16形成.5/.25/.125H结构，阈值密度允许真实空隙，并以同一时钟搬运和翻滚密度。主、侧图均保住连续厚云腹，原右端亮帽被内部暗凹和次级云瓣打破。

窄密度过渡暴露出视线散射积分条纹；单增光步数无效，H/48视线积分修复后通过原生对照。最终向光48步、视线最多1024段，超长路径增大步长而不截尾。最后独立逐帧复审发现填核包络显出方盒，已仅将maxabs(box)改为length(box)的平滑椭球填充并复核通过。

## 最终候选

| 项目 | SHA256 |
| --- | --- |
| Bundle | `1665C27E1A76465EF7C742D55BBF12D3AA0D6CAC0D44BF33D5C44AD0D0F9D72F` |
| 交付DLL | `8FA7046C6EEBE5892266F956328FB53709892C250AB38589419B01DF739922B5` |
| Controller及Unity镜像 | `5475193A02042FFD39D43EB5E6836D21D00797476861DD7D870E8F4EEDA867AD` |
| Density include | `CFE16231A4D3B7559B8C3564D568F55B5C4EB2D232B1D02A9FA067F6BEA9D323` |
| Volume shader | `D08974D8334E0A4B357FA5004909C065078CF71AD926C793FE25265A3B520BDB` |
| Noise carrier | `E8C0677459AE36D1C555728BF67CAF6F41EE63B6CED733E92751F838B73D4F7F` |

研究设计Mendel、实施Sagan、独立视觉评审Ramanujan；纹理烘焙复用此前Herschel已通过的结果。主线程审定设计与迭代、检查源码差异和原尺寸图、运行最终验证并执行同步。

## 验收证据

- 云体五相位主侧十图：`output/slazeya-cloud-recovery/implementation/zero-density-20260906-095842`，独立PASS。
- 全段审查：20张阶段原图、侧视/G2与91帧序列。云环、自旋、空间收束、从核外扩和消散PASS；方盒缺陷修复后，`round-core-20260906-102029`五张关键帧局部PASS。主线程核对相同修复源码进入最终Bundle，并检查最终Bundle的core_charge原图。
- 最终原生构建：`unity-build-20260906-102218-408-434ae90b.log`，Unity2019.3.15f1/D3D11，Bundle readback及预览PASS。
- 源码verifier、Unity API编译、DLL Release编译通过；DLL编译0错误、7个现有非本次特效警告。
- 最终PlayMode：`output/slazeya_storm_main_acceptance/r5-run-20260906-182532`，53 PASS、0失败；核对实际Bundle hash及三份运行源码不变。
- 音效：10项受控接口检查通过，原生加载/播放状态、暂停、音量、重复回调、清理通过。AV验证通过，91帧视频流不重编码，回调B=1.55s。
- 最终预览冻结：`preview_exports/slazeya_storm_mass/round5/review-1665c27e`，184个文件及哈希清单。带音效视频为该目录`audio/battlefield_with_audio.mp4`。

并行数值任务修改了SlazeyaAbilities.cs并替换共享bin中的DLL。特效运行源码及Bundle未变；主线程按当时完整工作区源码重新编译到独立目录`output/slazeya-cloud-recovery/delivery-20260906-181545`，核对69个C#输入及项目文件在编译前后不变，交付前再次复核。并行数值/文档/XML源码不纳入本特效提交；交付DLL保留交付时工作区已有数值修改，精确输入清单保存在该目录。不要用旧共享bin产物替换此交付快照。

## 同步与限制

2026-09-06 18:27，DLL、Bundle及.ab副本、两个WAV、云函数许可NOTICE共6文件同步到仓库Mod、C盘游戏Mod、D盘游戏Mod。18个目标在复制后及最终检查时均与交付哈希一致。

回执：`output/slazeya_storm_main_acceptance/deployment-receipt.json`。
备份：`output/slazeya_storm_main_acceptance/deployment-backup-20260906-182717`。

这是Unity原生代理场景与最小游戏接口替身的验收；未进行实际Library of Ruina战斗和主观音效试听。原生渲染/读回墙钟不能作为游戏FPS。少量侧面纵向褶皱与边缘尖突经独立评审列为非阻断观察。没有把本轮结果标成客观认证的“3A级”。
