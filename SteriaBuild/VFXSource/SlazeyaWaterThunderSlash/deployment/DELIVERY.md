# 水雷近战：最新版已部署

2026-11-17，单人完成。制作候选`candidate-40-volume-flow-uv`，隔离发布`release-06`。

最新版包含水流体积纹理、增强流光UV、沿流向/高度引导的有界UV偏移、白色线型光针及UV蓝暗部、贴合动态内缘的重力浪沫。水层外缘受亮层共同边界限制；亮层只向前裁短并同步蒙版溶解。

斯拉泽雅核心书页SteriaBuilding:99000002（司书）和SteriaBuilding:2（敌方）的普通/借用书页近战斩、打、突默认使用这款；绑定人物固定前方，跟随移动和朝向。远程、守备、FarArea、FarAreaEach、Fire/F与显式None保留，卡牌/核心XML未写。

## 验证

- 新DLL与真实游戏程序集、实际AB的原生夹具181项检查通过，包括真正PrintEffect/直接工厂、双朝向、人物跟随、同帧成功确认、空挥/取消及原远程/守备。
- 新增WaterEdgeFoam第七Prefab加载绑定，确认体积/流向数据、16–22根固定宽度光针、流光贴图、浪沫发射和亮层同步溶解均进入实际DLL。已查看三类代表GPU帧。
- 隔离构建0错误、8项既有警告；仅更新4份既有源码，100份其他源码和13份内嵌资源保持。DLL/PDB匹配。
- 原生证据：output/slazeya-water-thunder/native-09/checks.txt及acceptance.log。冻结发布：output/slazeya-water-thunder/release-06/release.json。
- [发布证明](release-06-verification.json) · [源码快照](release-06-sources.zip) · [部署收据](deployment-receipt.json) · [原生结果](native-verification/checks.txt)。

## 实际安装

`D:/Game Center/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/Mods/SteriaModFolder`。C盘Steam别名解析到同一目录；仓库SteriaModFolder同步相同payload。

| 文件 | SHA256 |
| --- | --- |
| Assemblies/Steria.dll | 8aad588342ba7372278c048bb3b27b37a3fcf0a806e617428da83326ab2e9b7c |
| Assemblies/AB/steria_slazeya_water_thunder_assets | 4c4d33ddab0b5bc57adfb16222754b95ab270897257ef67eb25eac061d9ac49f |
| Resource/ThirdPartyNotices/SlazeyaWaterThunderSlash.txt | c7e331f5433ae263e628a786bed08bb333c836a351d5979540133dba9dce99c9 |

备份与完整收据：output/slazeya-water-thunder/deployments/20261117T041549Z/，同目录backup包含旧DLL、AB及说明。CardInfo.xml、两份核心XML、旧风系AB及其说明逐项保持原hash。

素材来源说明已补充用户提供的法线图及其SHA256，其余刀光/高度/UV/噪点为项目绘制或生成。制作预览目录中的not-deployed-proof记录的是制作期状态，现由本次部署收据覆盖。

本轮未启动或重启游戏。原生验证使用合成角色变换，完整接待、真实HUD和战斗镜头仍需游戏内实测。
