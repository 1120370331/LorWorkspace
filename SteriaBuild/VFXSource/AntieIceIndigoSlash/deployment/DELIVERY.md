# 安蒂冰蓝靛紫斩击：部署记录

已部署candidate-05-convex-to-camera / release-02。

实际安装：`D:\Game Center\Steam\steamapps\common\Library Of Ruina\LibraryOfRuina_Data\Mods\SteriaModFolder`。C盘Steam入口解析到同一目录，仓库SteriaModFolder同步相同三文件payload。

## 生效范围

- 安蒂核心`SteriaBuilding:99000011`（司书）、`:13`（敌方），三形态、普通及借用书页和攻击型反击。
- 打击H：竖斩；斩击J：凸刃朝屏幕、凹侧略抬的横斩；突刺Z：已翻转正反的错时交叉双刀。
- 真正F/Fire、群攻和守备走原路由；Far书页中的普通J/H/Z仍按骰子使用新款。

## 精确发布与验证

- 以已安装DLL `d7feaef2372f3b2f9514ef3e5612eea2df39c2b90d42f59a1c7cf9b4f567a62e` 的匹配PDB重建，修改2份入口源码、新增2份安蒂源码；102份其他源码和13份内嵌资源保持。
- 隔离Release构建0错误、8项既有警告，PDB匹配、程序集引用与原有类型保持。
- 同一发布DLL和实际AB通过安蒂111项、斯拉泽雅189项原生检查；两套核心路由同时存在时互不干扰。
- 六张安蒂原生代表帧与已确认预览逐字节一致。
- 部署前确认游戏退出，备份旧DLL、AB/说明先写、DLL最后替换，SHA256逐项校验。
- 两份核心XML、CardInfo、斯拉泽雅水雷/旧风系AB和来源说明按游戏/仓库各自原hash保持。

| 安装文件 | SHA256 |
| --- | --- |
| Assemblies/Steria.dll | fc3ddab689c2147d791e6dd101e944aaec96354228b849c16febd88798ef326c |
| Assemblies/AB/steria_antie_ice_indigo_combat | aae7f5eafda06c851e6d5a1ccc33cb97d72284930c2e67f5ab1811d167b4aaaf |
| Resource/ThirdPartyNotices/AntieIceIndigoSlash.txt | d87903e346e78d1dfa6c5f95e77df4f4f9eef65cf0d1fd2fbbd62df43a3d5444 |

[部署收据](deployment-receipt.json) · [发布证明](release-02-verification.json) · [源码快照](release-02-sources.zip) · [入口差异](release-02-source-changes.diff) · [原生结果](native-verification/checks.txt)

完整备份：`C:\Users\rog\WorkSpace\projects\games\lor\output\antie-ice-indigo\deployments\20261117T111038Z`。冻结发布源码与匹配DLL/PDB保存在`output/antie-ice-indigo/release-02`。

本轮未启动游戏。验证使用真实游戏API/工厂和合成角色变换，完整接待实战仍由游戏内验收。制作阶段文件中的未部署标签保留当时状态，以本部署收据为当前状态依据。
