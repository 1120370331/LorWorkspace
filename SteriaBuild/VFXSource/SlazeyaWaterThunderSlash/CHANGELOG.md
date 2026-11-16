# 斯拉泽雅水雷特效：对话驱动的修改记录

记录日期：2026-11-16。对应实现与部署提交：`62946d59`。本文件记录实际发生的改动、被否决的方向和证据边界。

- [可复用经验复盘](../../../docs/superpowers/references/slazeya-water-thunder/2026-11-16-experience.md)
- [本机对话原文](../../设定/对话记录_2026-11-16_斯拉泽雅水雷斩击重置与部署.md)：按现有隐私规则保存在本机，不加入Git。
- [部署记录](deployment/DELIVERY.md) · [当前候选](accepted-candidate.json)
- [实现提交的逐文件清单](deployment/changes-62946d59.json)：记录62946d59的202个变更文件及Git blob，便于从原提交核对精确内容。

## 需求如何变化

| 顺序 | 用户反馈 | 最终落实 |
| --- | --- | --- |
| 1 | 旧斩击废弃，新建Unity项目，按参考图四层制作；角色为占位 | 新建SlazeyaWaterThunderSlash，分水刃、水花、紫电、亮芯；旧工程加废弃标记 |
| 2 | 一个人做，无需子代理；允许Python生成纹理 | 停止已启动的研究代理，后续由主线程顺序设计、实现、复查；技术纹理由Python生成 |
| 3 | 前锋更粗、后端更少；月牙从高处向下斩；加速—命中减速—再加速 | 重建纵向月牙、前粗后弱亮芯、统一绝对时间的扫光曲线；190ms命中 |
| 4 | 视频与所有资产清单 | 实际Unity GPU帧编码MP4，正常速度和标注慢放；资产清单提供MD/CSV/JSON |
| 5 | 最初提出圈扩大，随后澄清圆圈太夸张，改用水纹散点图扩散 | 移除活动圆环资产；一张RGBA水纹图驱动散点扩散和后续溶解 |
| 6 | “命中点多”随后被用户确认为圆环问题 | 撤回对真实命中数量的临时削减；保留同一命中原点发出的原配方 |
| 7 | 材质要亮部扫过，不能逐渐显形；尾流应发散 | Reveal固定1；移动刷形Mask扫光；尾部扇形空间配独立分叉水丝Mask |
| 8 | 水滴不真实，缺少流体波动；起手应尖头先出现 | 阻力/重力、顺速度拉伸回圆、独立毛细波、收颈分离；尖楔在前、尾部延迟展开 |
| 9 | 补上溶解淡出 | 水体和水滴按高度蒙版侵蚀，后段才做短alpha收尾；固定Alpha=1验证覆盖减少 |
| 10 | 部署，斩打突都用这一款，远程保持 | 现有三个近战工厂键改用同一水雷资源，8个攻击/反击入口生效；XML、远程等保留 |
| 11 | 水滴贴近痕迹、稍密；白色流体四方纹自旋；闪电分支与旧区段先衰减 | 水滴80颗、收紧散射；水体内四角白纹旋流；连接电树按着电时间和局部储能放电 |

## 实测反馈修订 · candidate-20 / release-04

- 前锋在原曲线末端连续追加向下转45°的弧；后缘于215–480ms沿弧线由后向前退去45°，继续保留细碎水纹溶解。重映射命中时刻，单点爆发仍为190ms。
- 删除目标胸口世界锚点和逐帧世界位置纠偏，改挂攻击者characterRotationCenter固定局部前方。归一化尺度0.50，继承角色朝向/移动，原生近战2倍及骰子倍率各一次。
- 在RencounterManager.PrintEffect按播放快照接入核心书页默认近战；精确匹配SteriaBuilding:99000002和SteriaBuilding:2，普通/借用卡的斩打突均覆盖。直接CreateAttackEffect也接入同一判断。Fire/F、远程、FarArea、FarAreaEach、守备和None保持原行为。
- 原生实际DLL/AB检查145项通过，含真实PrintEffect的普通/借用卡、核心ID、数据不变、排除条件、直接工厂、人物跟随、目标移动不干扰、双朝向、成功/空挥/取消。未运行完整接待实战。
- 仅4份既有源码变化，100份其他源码与13份内嵌资源保持。部署收据output/slazeya-water-thunder/deployments/20261116T153255Z/receipt.json；源码快照deployment/release-04-sources.zip。新预览视频和50项资产清单见README。

## 制作候选经过

候选目录是实现版本号，不代表每版都获得用户认可。早期候选和日志部分只保留在本机；当前可交付证据位于candidate-20；candidate-19是上次发布历史。

| 候选 | 主要改动或发现 | 处理结果 |
| --- | --- | --- |
| 01 | 新工程、原始四层输入与构建器；电弧端点出现NaN | 对sin端点做非负保护后再取小数次幂 |
| 02 | 第一轮可渲染四层偏暗、过细；系统ffmpeg缺libx264 | 视觉返修；编码改用imageio-ffmpeg自带可执行文件 |
| 03 | 水膜更宽、更亮，亮芯增厚，水滴形状调整 | 继续检查破碎、编辑器预览与保存 |
| 04 | 亮芯先收、主体破散；加入编辑态时钟和保存保护 | 白模暴露背面剔除问题 |
| 05 | 统一三角面朝向，白模可见；四层资源及AB重载验证 | 作为首轮制作候选交付 |
| 06–08 | 改成纵向下斩、命中速度曲线、命中水雷粒子、扩大圆环退场 | 修复冷启动Shuriken首次Simulate清空粒子；08通过技术验证，圆环随后被用户否决 |
| 09 | 将“多个命中点”误解为真实命中粒子太多，临时削减 | 用户澄清后撤回，未部署 |
| 10–11 | 移除大圆环，散点残影扩散；刷形扫光；独立发散尾流Mask | 调高扫后余光，避免水体和尾流太暗 |
| 12–14 | 水滴法线与体积感、顺速度形变、毛细波、水丝收颈；尖头先入场 | 修复Liquid标记持久化和数据色漏显；距离场过渡去除重影，14为稳定候选 |
| 15 | 增加真正的高度阈值溶解、后段alpha收尾 | 低频缺口太大，像整块切断，未采用 |
| 16 | R主体细碎溶解、G水滴柔和溶解、A散点覆盖 | Alpha=1的可见覆盖从23181逐步减至453像素；补退场专门视频 |
| 17 | 水滴收紧到80颗、四方白纹、连接电树与局部放电 | MonoBehaviour字段初始化创建原生对象报错，修为正常采样时延迟创建 |
| 18–19 | 电枝与白纹隔离预览，增强白纹四角辨识与强度 | 采用19作为最终部署美术资源 |

## 游戏接入与发布经过

| 阶段 | 实际发现/改动 | 结果 |
| --- | --- | --- |
| release-01/02 | 以现有已部署e3d7385... DLL及其PDB源码为基线，加入5份新源码 | 避免把工作区其他未发布改动带入游戏 |
| native-01 | 默认脚本程序集与游戏Assembly-CSharp的导入/引用边界不清，入口编译失败 | 改为独立Runtime/Editor asmdef |
| native-02 | BaseMod缺少LorIdExtensions、NAudio、BaseBridge等依赖，程序集未加载 | 从本机匹配的游戏/Workshop环境补齐夹具依赖 |
| native-03/04/05 | 新前缀已注册priority=800，直接初始化也成功，但真正工厂仍返回旧类型 | 不把注册日志当接管证据；放弃新增竞争前缀，改用既有注册表 |
| release-03 | 精确替换三个注册值，复用现有成功命中路由 | 仅两份旧源码作限定修改；97份旧业务源码、13份内嵌资源保留 |
| native-06 | 实际Steria.dll、游戏程序集、实际AB和真实Update/GPU路径 | 102项通过；三类、双朝向、缩放、世界命中点、成功/空挥/取消、远程守备保留 |
| 部署 | 先备份，再原子替换DLL并新增AB/说明，D游戏、C别名、仓库包核哈希 | DEPLOYED_VERIFIED；没有启动或重启游戏 |

“priority=800而真实工厂仍返回旧类型”是本机测试观察。本记录不据此断言Harmony本身存在某个通用缺陷。

## release-03 修改面（历史）

| 文件/目录 | 职责 |
| --- | --- |
| `SteriaBuild/SlazeyaWaterThunderCombat.cs` | 六个静态Prefab缓存、三类适配器、J/H/Z挂点、目标胸前世界锚点、原生缩放、确认命中、取消清理 |
| `SteriaBuild/HarmonyPatches.cs` | 只将三个旧近战资源键指向新类型；其他工作区改动未带入发布包 |
| `SteriaBuild/SlazeyaWindImpact.cs` | 既有同帧/同目标命中配对扩展到水雷三类 |
| `SteriaBuild/Steria.csproj` | 显式编译制作工程中四份共享Runtime源码 |
| `UnityProject/Assets/WaterThunder/Runtime/` | 时钟、扫光、形变、水滴物理与命中粒子；落地判定使用TransformPoint |
| `UnityProject/Assets/WaterThunder/Editor/` | 资产生成、制作场景、编辑态播放、保存保护、GPU输出与验证 |
| `generate_inputs.py` | 原创水体、分叉尾流、尖楔、法线、RGBA溶解散点、四方白纹等输入 |
| `package_preview.py` / `write_asset_inventory.py` | 视频/分解图及50项资产清单 |
| `deployment/` | 隔离发布、原生夹具、限定变更检查、部署脚本、收据及源码快照 |
| `SteriaModFolder/Assemblies/Steria.dll` | 已部署新DLL，SHA256见DELIVERY.md |
| `SteriaModFolder/Assemblies/AB/steria_slazeya_water_thunder_assets` | 新水雷资源包；旧风系AB不改 |

## release-03 证据与剩余边界（历史）

- 制作工程：`UnityProject`，Unity **2019.3.15f1 / Built-in / Gamma / D3D11**。
- 当前艺术与清单：[candidate-19](evidence/candidate-19-close-spin-voltage/visual-review.md)、[资产清单](evidence/candidate-19-close-spin-voltage/asset-inventory.md)。
- 发布：[DELIVERY.md](deployment/DELIVERY.md)、[简化部署收据](deployment/deployment-receipt.json)、[原生检查结果](deployment/native-verification/checks.txt)。
- 精确发布源码：[release-03-sources.zip](deployment/release-03-sources.zip)，参考DLL仍取本机现有Libs/游戏安装，不在快照中重分发。
- 本机完整收据与备份：`output/slazeya-water-thunder/deployments/20261116T134410Z/`。
- 已验证实际资源和调用链；完整接待、真实角色最终皮肤、完整HUD/战斗镜头及玩家实战观感尚未验证。部署授权与最终实战美术认可分别记录。
