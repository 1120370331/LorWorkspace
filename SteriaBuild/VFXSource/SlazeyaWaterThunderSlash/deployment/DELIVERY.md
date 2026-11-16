# 水雷近战部署完成

2026-11-16，单人完成。采用制作候选 `candidate-19-close-spin-voltage` 与隔离发布 `release-03`。

`Steria_SlazeyaWindSlash / Hit / Pierce` 三个已有入口统一到水雷斩击，覆盖8颗普通攻击/反击骰子；J/H/Z动作挂点和原生缩放各自保留。远程FarHit、守备、FarArea群攻与天气保持原路径，CardInfo.xml未写入。命中爆发通过原生同帧、同目标、同攻击类型配对确认，空挥/错误目标/过期配对不产生成功命中爆发。

本轮追加的贴近刀痕水滴（80颗）、白色流体四方旋纹、树状电枝和按区段储能/扫过时间衰减，已包含在游戏包内。原尖头入场、水滴形变和溶解淡出保留。

## 发布证据

- 实际候选DLL＋游戏程序集＋实际AB的原生Unity夹具通过102项检查：三个入口、双朝向、J/H/Z挂点、缩放只施加一次、世界命中位置、真实Update、成功确认/空挥/错误目标/过期/取消、远程与守备原实现。已查看DLL实际渲染的Slash/Hit/Pierce代表帧。这是合成角色变换的原生API场景，不冒称完整接待实战。
- 隔离构建0错误、8项既有警告。旧99份业务源码中97份及13份嵌入资源不变；仅3行注册和现有成功命中路由作限定修改，另加5份新源码。DLL/PDB相互匹配，原有类型及程序集引用保留。
- 冻结来源与逐项hash：`output/slazeya-water-thunder/release-03/release.json`。
- 原生检查：`output/slazeya-water-thunder/native-06/acceptance.log`、`checks.txt` 与18张GPU帧。
- 部署与备份：`output/slazeya-water-thunder/deployments/20261116T134410Z/receipt.json`、同目录backup。

## 实际文件

主目录：`D:/Game Center/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/Mods/SteriaModFolder`。C盘Steam入口解析至同一目录，按别名再次核验。仓库SteriaModFolder同步相同payload。

| 文件 | SHA256 |
| --- | --- |
| Assemblies/Steria.dll | 556c676774607c06a883da936f4f9e5055521ed9075f3b03cc756d2db4326643 |
| Assemblies/AB/steria_slazeya_water_thunder_assets | a91a86bd7cc124f88487d25214186815193686dc0cae96784d4cd86345ef2a2d |
| Resource/ThirdPartyNotices/SlazeyaWaterThunderSlash.txt | 2013b7b0eb8411b6995eefc06dc4d520e06b1859bca6781d7d72c6f7a86d6634 |

远程/守备沿用的旧风系AB仍为cf8bf99f90f5595dfc0ee3a31b3429b7cec6f0ca1f8a4bde42f94ab685e1eaaf，CardInfo.xml仍为c06089249ad0d57fc1b9de005bae848270a1314bcc956a3dd450c8c0535a1b4b。安装先备份再原子替换；本轮没有启动或重启游戏，完整战斗镜头、HUD与实战美术反馈留待游戏内检查。
