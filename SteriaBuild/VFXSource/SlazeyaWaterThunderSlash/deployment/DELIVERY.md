# 水雷近战：最新版已部署

2026-11-17，单人完成。制作候选`candidate-40-volume-flow-uv`，隔离发布`release-07`（挂点高度修订）。

挂点已在人物局部坐标下调0.18（三倍单位约0.54游戏单位）；前方距离、尺寸和朝向继承保持。只更换DLL，资源包和说明哈希不变。 [固定相机前后对比](native-verification/anchor-height-before-after.png)。

最新版包含水流体积纹理、增强流光UV、沿流向/高度引导的有界UV偏移、白色线型光针及UV蓝暗部、贴合动态内缘的重力浪沫。水层外缘受亮层共同边界限制；亮层只向前裁短并同步蒙版溶解。

斯拉泽雅核心书页SteriaBuilding:99000002（司书）和SteriaBuilding:2（敌方）的普通/借用书页近战斩、打、突默认使用这款；绑定人物固定前方，跟随移动和朝向。远程、守备、FarArea、FarAreaEach、Fire/F与显式None保留，卡牌/核心XML未写。

## 验证

- 新DLL与真实游戏程序集、实际AB的原生夹具189项检查通过，包括真正PrintEffect/直接工厂、双朝向、人物跟随、同帧成功确认、空挥/取消及原远程/守备。
- 新增WaterEdgeFoam第七Prefab加载绑定，确认体积/流向数据、16–22根固定宽度光针、流光贴图、浪沫发射和亮层同步溶解均进入实际DLL。已查看三类代表GPU帧。
- 隔离构建0错误、8项既有警告；仅更新1份挂点适配器源码，103份其他源码和13份内嵌资源保持。DLL/PDB匹配。
- 原生证据：output/slazeya-water-thunder/native-10/checks.txt及acceptance.log。冻结发布：output/slazeya-water-thunder/release-07/release.json。
- [发布证明](release-07-verification.json) · [源码快照](release-07-sources.zip) · [部署收据](deployment-receipt.json) · [原生结果](native-verification/checks.txt)。

## 实际安装

`D:/Game Center/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/Mods/SteriaModFolder`。C盘Steam别名解析到同一目录；仓库SteriaModFolder同步相同payload。

| 文件 | SHA256 |
| --- | --- |
| Assemblies/Steria.dll | d7feaef2372f3b2f9514ef3e5612eea2df39c2b90d42f59a1c7cf9b4f567a62e |
| Assemblies/AB/steria_slazeya_water_thunder_assets | 4c4d33ddab0b5bc57adfb16222754b95ab270897257ef67eb25eac061d9ac49f |
| Resource/ThirdPartyNotices/SlazeyaWaterThunderSlash.txt | c7e331f5433ae263e628a786bed08bb333c836a351d5979540133dba9dce99c9 |

备份与完整收据：output/slazeya-water-thunder/deployments/20261117T064056Z/，本轮backup只含被替换的旧DLL；原release-06备份仍在20261117T041549Z目录，AB/说明本轮跳过不写。CardInfo.xml、两份核心XML、旧风系AB及其说明逐项保持原hash。

素材来源说明已补充用户提供的法线图及其SHA256，其余刀光/高度/UV/噪点为项目绘制或生成。制作预览目录中的not-deployed-proof记录的是制作期状态，现由本次部署收据覆盖。

本轮未启动或重启游戏。原生验证使用合成角色变换，完整接待、真实HUD和战斗镜头仍需游戏内实测。
