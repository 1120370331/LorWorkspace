# 水雷近战：实测反馈修订已部署

2026-11-16，单人完成。制作候选`candidate-20-front45-core`，隔离发布`release-04`。

前锋沿原末端切线向下续转45°，后缘于215–480ms从后向前逐渐退去45°，水纹溶解继续保留。游戏根挂攻击者characterRotationCenter固定前方，跟随移动和转身；目标不再控制摆放。

斯拉泽雅核心书页SteriaBuilding:99000002（司书）和SteriaBuilding:2（敌方）的普通近战斩/打/突默认使用水雷，包含普通和借用的战斗书页。播放读取当次快照，原卡牌数据不变；远程、FarArea、FarAreaEach、守备、Fire/F和显式None保留。

## 验证与证据

- 原生实际DLL＋游戏程序集＋AB：145项检查通过，包含真实PrintEffect普通/借用卡、核心ID、排除条件、直接CreateAttackEffect、固定前方、人物移动跟随、目标移动不干扰、双朝向、倍率、成功/空挥/取消及原远程/守备。已查看Slash/Hit/Pierce GPU代表帧。
- Unity2019.3.15f1 / Built-in / Gamma / D3D11通过构建、纹理导入、45°角度/退尾、先加速/命中减速/再加速、冷资源包重载像素一致性检查。视频为Unity实际GPU帧编码，包含正常速度及0.5x慢放。
- 程序集构建0错误、8项既有警告；104份源码中仅4份变化，100份其他源码和13份内嵌资源保留；DLL/PDB匹配。
- 本机冻结候选：output/slazeya-water-thunder/release-04/release.json；原生结果output/slazeya-water-thunder/native-07/checks.txt。
- 部署/备份：output/slazeya-water-thunder/deployments/20261116T153255Z/receipt.json及同目录backup。
- 可版本管理记录：[发布证明](release-04-verification.json)、[源码快照](release-04-sources.zip)、[简化收据](deployment-receipt.json)、[原生结果](native-verification/checks.txt)。

## 安装位置

`D:/Game Center/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/Mods/SteriaModFolder`。C盘Steam别名解析至同一目录；仓库SteriaModFolder同步一致。

| 文件 | SHA256 |
| --- | --- |
| Assemblies/Steria.dll | c8a6615376dd11279c3981cab886574072b45c3c2b97608b7f9d7db7ea0d574f |
| Assemblies/AB/steria_slazeya_water_thunder_assets | 28a4263ef3c73b38830680aada95f42d28bcf7da511318b729c3051a47613dfa |
| Resource/ThirdPartyNotices/SlazeyaWaterThunderSlash.txt | 2013b7b0eb8411b6995eefc06dc4d520e06b1859bca6781d7d72c6f7a86d6634 |

CardInfo.xml、两份核心书页XML、旧风系AB和其说明逐项保持原hash。旧DLL/AB/说明均有备份。未启动或重启游戏；完整接待、真实战斗镜头/HUD以及玩家实战观感留待游戏内验收。
